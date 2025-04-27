using System.Reflection;
using cocoding.Components;
using cocoding.Data;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace cocoding;

/// <summary>
/// Global methods and constants.
/// </summary>
public static class Global
{
    public static Version Version { get; private set; } = new(0, 0, 0, 0);
    
    public static string VersionString => $"{Version.Major}.{Version.Minor}.{Version.Build}.{Version.Revision}";
    
    /// <summary>
    /// The ASP.NET web application object.
    /// </summary>
    private static WebApplication? App = null;
    
    /// <summary>
    /// Provides a HttpContext object in places where it wasn't given as a parameter.
    /// </summary>
    private static IHttpContextAccessor? ContextAccessor {get; set;} = null;

    /// <summary>
    /// The server configuration object.
    /// </summary>
    public static Config Config {get; internal set;} = new();
    
    /// <summary>
    /// Provides the current HttpContext object.
    /// </summary>
    public static HttpContext HttpContext
        => (ContextAccessor ?? throw new Exception("ContextAccessor is null!")).HttpContext ?? throw new Exception("HttpContext is null!");

    /// <summary>
    /// Stops the web server, shutting down the application in the process.
    /// </summary>
    public static void Stop()
    {
        App?.StopAsync(TimeSpan.FromSeconds(1)).GetAwaiter().GetResult();
        Environment.Exit(0);
        Environment.FailFast("Failed to exit softly.");
    }

    /// <summary>
    /// Starts the web server and blocks the thread until it has stopped.
    /// </summary>
    public static void Start()
    {
        LoadConfig();
        
        // Start building the application
        var builder = WebApplication.CreateBuilder();

        // Services: Razor, HttpContextAccessor, AuthService, SharedDataService, CircuitService
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddCircuitOptions(blazorOptions => blazorOptions.DetailedErrors = builder.Environment.IsDevelopment());
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped(serviceProvider => new AuthService(serviceProvider));
        builder.Services.AddScoped<SharedDataService>();
        builder.Services.AddScoped<CircuitService>();
        builder.Services.AddScoped<CircuitHandler>(serviceProvider => serviceProvider.GetRequiredService<CircuitService>());

        // Add SignalR service
        builder.Services.AddSignalR()
            .AddMessagePackProtocol();

        // Configure kestrel ports
        builder.WebHost.ConfigureKestrel(kestrelOptions =>
        {
            if (Config.HTTP != default)
                kestrelOptions.ListenAnyIP(Config.HTTP);
            if (Config.HTTPS != default && Config.Certificate != null)
                kestrelOptions.ListenAnyIP(Config.HTTPS, listenOptions => listenOptions.UseHttps(Config.Certificate));
        });

        // Development settings (detailed errors) // Production settings part 1 (disable ASP.NET logs)
        if (builder.Environment.IsDevelopment())
            builder.WebHost.UseSetting(WebHostDefaults.DetailedErrorsKey, "true");
        
        // Disable ASP.NET logs if configured
        if (!Config.AspNetLogs)
            builder.Services.AddLogging(logging => logging.ClearProviders());
        
        //add lifetime service
        builder.Services.AddHostedService<LifetimeService>();

        // Finish building the application
        App = builder.Build();

        // Save HttpContextAccessor
        ContextAccessor = App.Services.GetRequiredService<IHttpContextAccessor>();

        // Production settings part 2 (exception handler)
        if (!App.Environment.IsDevelopment())
            App.UseExceptionHandler("/Error", createScopeForErrors: true);

        // HSTS and HTTPS redirection
        if (Config.HTTPS != default)
        {
            App.UseHsts();
            App.UseHttpsRedirection();
        }

        // Add security middleware
        App.UseStaticFiles();
        App.UseAntiforgery();

        // Add Razor middleware
        App.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        // Map SignalR hub for the editor
        App.MapHub<EditorHub>("/editor-hub");

        // Test database connection
        Database.TestConnection();

        // Start worker
        Worker.Start();

        // Start application
        Console.WriteLine("Starting...");
        App.Run();
    }

    /// <summary>
    /// Attempts to load a configuration file (cocoding.conf) from the current folder or any of its parents folders.
    /// </summary>
    public static void LoadConfig()
    {
        int parents = Environment.CurrentDirectory.Replace("\\\\", "").Replace('\\', '/').Split('/').Length;
        string path = "cocoding.conf";
        for (int i = 0; i < parents; i++)
        {
            try
            {
                if (File.Exists(path))
                {
                    Config config = new();

                    foreach (string line in File.ReadAllLines(path).Select(x => x.Trim()).Where(x => x != "" && !x.StartsWith('#')))
                    {
                        int index = line.IndexOf('=');
                        if (index == -1)
                        {
                            Console.WriteLine($"Invalid configuration line \"{line}\", ignoring...");
                            continue;
                        }

                        string key = line[..index];
                        string value = line[(index+1)..];

                        switch (key)
                        {
                            case "DatabaseConnection":
                                config.DatabaseConnection = value;
                                break;
                            case "HTTP":
                            {
                                if (int.TryParse(value, out int port) && port >= 0 && port <= 65535)
                                    config.HTTP = port;
                                else Console.WriteLine($"Invalid value \"{value}\" for configuration key \"HTTP\", ignoring...");
                            } break;
                            case "HTTPS":
                            {
                                if (int.TryParse(value, out int port) && port >= 0 && port <= 65535)
                                    config.HTTPS = port;
                                else Console.WriteLine($"Invalid value \"{value}\" for configuration key \"HTTPS\", ignoring...");
                            } break;
                            case "Certificate":
                            {
                                if (File.Exists(value))
                                    config.Certificate = value;
                                else Console.WriteLine($"Invalid value \"{value}\" for configuration key \"Certificate\", ignoring...");
                            } break;
                            case "AspNetLogs":
                            {
                                if (bool.TryParse(value, out bool valueBool))
                                    config.AspNetLogs = valueBool;
                                else Console.WriteLine($"Invalid value \"{value}\" for configuration key \"HTTPS\", ignoring...");
                            } break;
                            default:
                                Console.WriteLine($"Invalid configuration key \"{key}\", ignoring...");
                                break;
                        }
                    }

                    if (config.HTTP == default && config.HTTPS == default)
                        Console.WriteLine("HTTP and HTTPS can't both be disabled, ignoring configuration...");
                    else if (config.HTTP == config.HTTPS)
                        Console.WriteLine("HTTP and HTTPS can't both use the same port, ignoring configuration...");
                    else if (config.HTTPS != default && config.Certificate == null)
                        Console.WriteLine("HTTPS can't be enabled without a certificate, ignoring configuration...");
                    else
                    {
                        Config = config;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read \"{path}\": {ex.Message}\n{ex.StackTrace}");
            }
            path = "../" + path;
        }
    }

    /// <summary>
    /// Returns the given path while appending a query string like "?v=[VERSION]" in production mode or "?t=[TIMESTAMP]" in development mode.
    /// </summary>
    public static string MakeFilePath(string path)
        => App != null && App.Environment.IsDevelopment()
                       && path.StartsWith('/') && File.Exists($"wwwroot/{path}")
            ? $"{path}?t={File.GetLastWriteTimeUtc($"wwwroot/{path}").Ticks}"
            : $"{path}?v={VersionString}";

    /// <summary>
    /// Sets the saved version to the version of the given assembly.
    /// </summary>
    public static void SetVersion(Assembly assembly)
    {
        var version = assembly.GetName().Version;
        if (version != null)
            Version = version;
    }

    private class LifetimeService(IHostApplicationLifetime hal) : IHostedService
    {
        private readonly IHostApplicationLifetime HostApplicationLifetime = hal;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            HostApplicationLifetime.ApplicationStopping.Register(ApplicationStopping);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;

        private static void ApplicationStopping()
        {
            Console.WriteLine("Stopping...");
            foreach (var fileGroup in EditorHub.FileGroups.Values)
                fileGroup.Persist();
        }
    }
}
