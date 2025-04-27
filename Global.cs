using System.Reflection;
using cocoding.Components;
using cocoding.Data;
using Microsoft.AspNetCore.Components.Server.Circuits;
using uwap.WebFramework;

namespace cocoding;

/// <summary>
/// Global methods and constants.
/// </summary>
public static class Global
{
    public static Version Version { get; private set; } = new(0, 0, 0, 0);
    
    public static string VersionString => $"{Version.Major}.{Version.Minor}.{Version.Build}.{Version.Revision}";

    /// <summary>
    /// The server configuration object.
    /// </summary>
    public static Config Config {get; internal set;} = new();
    
    /// <summary>
    /// Provides the current HttpContext object.
    /// </summary>
    public static HttpContext HttpContext
        => Server.CurrentHttpContext ?? throw new Exception("HttpContext is null!");

    /// <summary>
    /// Stops the web server, shutting down the application in the process.
    /// </summary>
    public static void Stop()
    {
        Server.Exit(false);
    }

    /// <summary>
    /// Starts the web server and blocks the thread until it has stopped.
    /// </summary>
    public static void Start()
    {
        LoadConfig();
        
        if (Server.DebugMode = Config.Debug)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("RUNNING IN DEBUG MODE");
            Console.ResetColor();
        }
        
        Server.Config.AllowMoreMiddlewaresIfUnhandled = true;

        Server.Config.ConfigureServices = services =>
        {
            // Services: Razor, HttpContextAccessor, AuthService, SharedDataService, CircuitService
            services.AddRazorComponents()
                .AddInteractiveServerComponents()
                .AddCircuitOptions(blazorOptions => blazorOptions.DetailedErrors = Server.DebugMode);
            services.AddHttpContextAccessor();
            services.AddScoped(serviceProvider => new AuthService(serviceProvider));
            services.AddScoped<SharedDataService>();
            services.AddScoped<CircuitService>();
            services.AddScoped<CircuitHandler>(serviceProvider => serviceProvider.GetRequiredService<CircuitService>());
            
            // Add SignalR service
            services.AddSignalR()
                .AddMessagePackProtocol();
        };
        
        // Configure kestrel ports
        if (Config.HTTP != default)
            Server.Config.HttpPort = Config.HTTP;
        if (Config.HTTPS != default && Config.Certificate != null)
        {
            Server.Config.HttpsPort = Config.HTTPS;
            Server.LoadCertificate("any", Config.Certificate);
        }

        // Disable ASP.NET logs if configured
        Server.Config.Log.AspNet = Config.AspNetLogs;
        
        // Set lifetime event
        Server.ProgramStopping += () =>
        {
            foreach (var fileGroup in EditorHub.FileGroups.Values)
                fileGroup.Persist();
        };

        Server.Config.ConfigureWebApp = app =>
        {
            // Production settings part 2 (exception handler)
            if (!Server.DebugMode)
                app.UseExceptionHandler("/Error", createScopeForErrors: true);

            // Add security middleware
            app.UseStaticFiles();
            app.UseAntiforgery();

            // Add Razor middleware
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            // Map SignalR hub for the editor
            app.MapHub<EditorHub>("/editor-hub");
        };

        // Test database connection
        Database.TestConnection();

        // Set worker
        Server.Config.WorkerInterval = 240;
        Server.WorkerWorked += Worker.Work;
        
        // Certificate requesting
        if (Config.AutoCertificateEmail != null)
        {
            Server.Config.AutoCertificate.Email = Config.AutoCertificateEmail;
            Server.Config.AutoCertificate.Domains.AddRange(Config.AutoCertificateDomains);
        }

        // Start server
        Server.Start();
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
                                else Console.WriteLine($"Invalid value \"{value}\" for configuration key \"AsNetLogs\", ignoring...");
                            } break;
                            case "Debug":
                            {
                                if (bool.TryParse(value, out bool valueBool))
                                    config.Debug = valueBool;
                                else Console.WriteLine($"Invalid value \"{value}\" for configuration key \"Debug\", ignoring...");
                            } break;
                            case "AutoCertificateDomains":
                                config.AutoCertificateDomains = value.Split(',');
                                break;
                            case "AutoCertificateEmail":
                                config.AutoCertificateEmail = value;
                                break;
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
                    else if (Config.AutoCertificateDomains.Length > 0 && Config.AutoCertificateEmail == null)
                        Console.WriteLine("AutoCertificate can't be enabled without an email address, ignoring configuration...");
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
        => Server.DebugMode && path.StartsWith('/') && File.Exists($"wwwroot/{path}")
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
}
