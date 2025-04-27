using cocoding.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace cocoding;

/// <summary>
/// Delegate for functions that invoke the calling page for state updates.
/// </summary>
public delegate Task InvokePageDelegate(Func<Task> invoker);

/// <summary>
/// Injectable service for authentication.
/// </summary>
public class AuthService
{
    /// <summary>
    /// This event gets called whenever the login state changes.
    /// </summary>
    public event Action? Reloaded = null;
    
    /// <summary>
    /// This event gets called whenever the theme state changes.
    /// </summary>
    public event Action? ThemeChanged = null;

    /// <summary>
    /// The function to execute when UpdateTokenIfNecessary is called.
    /// </summary>
    private Func<Task>? UpdateTokenFunction = null;

    /// <summary>
    /// The login session or null if nobody is logged in.
    /// </summary>
    private UserSession? _User = null;

    /// <summary>
    /// Connection to the client's JavaScript environment.
    /// </summary>
    private IJSRuntime JSRuntime {get; set;}

    /// <summary>
    /// Connection to the client's navigation system.
    /// </summary>
    private NavigationManager NavigationManager {get; set;}
    
    /// <summary>
    /// The difference in minutes between the user's time zone and UTC.
    /// </summary>
    public int TimeOffset = 0;

    /// <summary>
    /// Whether the theme is dark (true) or light (false).
    /// </summary>
    public bool IsDarkMode = true;

    /// <summary>
    /// Creates a new authentication service instance, loads the login token (if present) and renews it if necessary.
    /// </summary>
    public AuthService(IServiceProvider serviceProvider)
    {
        JSRuntime = serviceProvider.GetService<IJSRuntime>() ?? throw new Exception("Failed to inject IJSRuntime!");
        NavigationManager = serviceProvider.GetService<NavigationManager>() ?? throw new Exception("Failed to inject NavigationManager!");
        HttpContext httpContext = Global.HttpContext;
        string? token = httpContext.Request.Cookies["Token"];
        if (token != null)
        {
            var us = UserManager.ResolveToken(token);
            if (us == null || us.Session == null || us.Session.Expiration < DateTime.UtcNow)
                UpdateTokenFunction = DeleteExpiredToken;
            else
            {
                if (us.Session.Expiration < DateTime.UtcNow.AddDays(39) && us.Session.Expiration > DateTime.UtcNow.AddMinutes(5))
                    UpdateTokenFunction = RenewToken;

                _User = us;
            }
        }
    }

    /// <summary>
    /// Returns whether a user is logged in.
    /// </summary>
    public bool LoggedIn => _User != null;

    /// <summary>
    /// Returns whether an administrator is logged in.
    /// </summary>
    public bool IsAdmin => _User != null && _User.Role == Role.Administrator;

    /// <summary>
    /// Returns the current user session or throws an exception if nobody is logged in.
    /// </summary>
    public UserSession User => _User ?? throw new Exception("Not logged in!");

    /// <summary>
    /// Applies planned login token changes if present.
    /// </summary>
    public async Task UpdateTokenIfNecessary()
    {
        if (UpdateTokenFunction != null)
        {
            var f = UpdateTokenFunction;
            UpdateTokenFunction = null;
            await f.Invoke();
        }
    }

    /// <summary>
    /// Redirects to the login page if nobody is logged in, or to a proper page if the login page is being visited while someone is logged in.
    /// </summary>
    public bool RedirectIfNecessary()
    {
        switch (NavigationManager.Path())
        {
            case "/login":
            case "/register":
                if (LoggedIn)
                {
                    NavigationManager.NavigateTo(NavigationManager.GetQuery("r")??"/");
                    return false;
                }
                else return true;
            default:
                if (!LoggedIn)
                {
                    NavigationManager.NavigateToLogin();
                    return false;
                }
                else return true;
        }
    }

    /// <summary>
    /// Registers as a new user while validating the inputs and logging into the new account.<br/>
    /// If there's an issue with the inputs, an appropriate exception will be thrown.
    /// </summary>
    public async Task Register(string username, string password1, string password2, InvokePageDelegate pageInvoker)
    {
        _User = UserManager.Register(username, password1, password2);
        await pageInvoker.Invoke(ApplyLoginToClient);
    }

    /// <summary>
    /// Validates the given credentials and logs into the attached account.<br/>
    /// If there's an issue with the inputs, an appropriate exception will be thrown.
    /// </summary>
    public async Task Login(string username, string password, InvokePageDelegate pageInvoker)
    {
        _User = UserManager.Login(username, password);
        await pageInvoker.Invoke(ApplyLoginToClient);
    }

    /// <summary>
    /// Logs the user out.
    /// </summary>
    /// <returns></returns>
    public async Task Logout()
    {
        UserManager.Logout(User);
        await ApplyLogoutToClient();
        _User = null;
        await UpdateTheme();
    }

    /// <summary>
    /// Applies a new token to the client and redirects to a proper page.
    /// </summary>
    private async Task ApplyLoginToClient()
    {
        if (User.Session == null)
            throw new Exception("No token was set!");
        await JSRuntime.SetCookieAsync("Token", User.Session.Token, User.Session.Expiration);
        NavigationManager.NavigateTo(NavigationManager.GetQuery("r")??"/");
        Reloaded?.Invoke();
        await UpdateTheme();
    }

    /// <summary>
    /// Applies a logout to the client and redirects to the login page.
    /// </summary>
    private async Task ApplyLogoutToClient()
    {
        await JSRuntime.DeleteCookieAsync("Token");
        NavigationManager.NavigateToLogin();
        Reloaded?.Invoke();
    }

    /// <summary>
    /// Deletes the client's invalid login token.
    /// </summary>
    private async Task DeleteExpiredToken()
    {
        await JSRuntime.DeleteCookieAsync("Token");
    }

    /// <summary>
    /// Renews the current login token and sends the new token to the client.
    /// </summary>
    private async Task RenewToken()
    {
        UserManager.RenewToken(User);
        if (User.Session == null)
            throw new Exception("No token was set!");
        await JSRuntime.SetCookieAsync("Token", User.Session.Token, User.Session.Expiration);
    }

    /// <summary>
    /// Returns whether the given user has at least the given role in the given project.
    /// </summary>
    public ProjectRole GetRole(int projectId)
        => Database.Transaction(cp =>
            AssignmentTable.GetRole(cp, projectId, User.Id));

    /// <summary>
    /// Returns whether the given user has at least the given role in the given project.
    /// </summary>
    public bool HasRole(int projectId, ProjectRole minRole)
        => GetRole(projectId) >= minRole;
    
    /// <summary>
    /// Loads the user's time zone as an offset in minutes and saves it for future use.
    /// </summary>
    public async Task LoadTimeOffset()
        => TimeOffset = await JSRuntime.InvokeAsync<int>("getTimeOffset");

    /// <summary>
    /// Loads the theme option (dark or light).
    /// </summary>
    public async Task LoadTheme()
        => IsDarkMode = _User?.Theme switch
            {
                Theme.Dark => true,
                Theme.Light => false,
                _ => await JSRuntime.InvokeAsync<bool>("isSystemDarkMode")
            };

    /// <summary>
    /// Loads the theme and calls the event for the theme change.
    /// </summary>
    public async Task UpdateTheme()
    {
        await LoadTheme();
        ThemeChanged?.Invoke();
    }

    /// <summary>
    /// Converts the given UTC date to the user's local time.
    /// </summary>
    public DateTime LocalTime(DateTime dateTime)
        => dateTime.AddMinutes(TimeOffset);

    public string LocalTimeString(DateTime dateTime)
    {
        var l = LocalTime(dateTime);
        return $"{l.Day:D2}.{l.Month:D2}.{l.Year} - {l.Hour:D2}:{l.Minute:D2} Uhr";
    }
}