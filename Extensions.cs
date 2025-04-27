using System.Diagnostics.CodeAnalysis;
using System.Web;
using cocoding.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MySqlConnector;

namespace cocoding;

/// <summary>
/// Extension methods.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Returns the path of the current URI.
    /// </summary>
    public static string Path(this NavigationManager navMan)
        => new UriBuilder(navMan.Uri).Path;

    /// <summary>
    /// Returns whether a query entry with the given key exists and returns its decoded value to <c>value</c>.
    /// </summary>
    public static bool TryGetQuery(this NavigationManager navMan, string key, [MaybeNullWhen(false)] out string value)
        => (value = HttpUtility.ParseQueryString(new UriBuilder(navMan.Uri).Query)[key]) != null;

    /// <summary>
    /// Returns the decoded value of the query entry with the given key or null if no such query entry exists.
    /// </summary>
    public static string? GetQuery(this NavigationManager navMan, string key)
        => HttpUtility.ParseQueryString(new UriBuilder(navMan.Uri).Query)[key];

    /// <summary>
    /// Navigates to the login page while adding the current path and query string as a redirect query.
    /// </summary>
    public static void NavigateToLogin(this NavigationManager navMan)
    {
        UriBuilder qb = new(navMan.Uri);
        navMan.NavigateTo($"/login?r={HttpUtility.UrlEncode(qb.Path + qb.Query)}");
    }

    /// <summary>
    /// Returns the value of the cookie with the given key or null if no such cookie exists.
    /// </summary>
    public static async Task<string?> GetCookieAsync(this IJSRuntime jsRun, string key)
    {
        string cookieString = await jsRun.InvokeAsync<string>("eval", "document.cookie");
        if (cookieString == null || cookieString == "")
            return null;
        
        foreach (string item in cookieString.Split(';'))
        {
            int index = item.IndexOf('=');
            if (index >= 0 && item[..index].Trim() == key)
                return item[(index+1)..].Trim();
            
        }

        return null;
    }

    /// <summary>
    /// Sets a cookie with the given key, value and expiration date.
    /// </summary>
    public static async Task SetCookieAsync(this IJSRuntime jsRun, string key, string value, DateTime? expiration = null)
        => await jsRun.InvokeVoidAsync("eval", $"document.cookie = \"{key}={value}; {(expiration == null ? "" : $"expires={expiration.Value:R}; ")}sameSite=Lax; path=/\"");

    /// <summary>
    /// Deletes the cookie with the given key.
    /// </summary>
    public static async Task DeleteCookieAsync(this IJSRuntime jsRun, string key)
        => await jsRun.SetCookieAsync(key, "", DateTime.UtcNow.AddDays(-7));

    /// <summary>
    /// Gets the value of the column with the given ordinal as a nullable string.
    /// </summary>
    public static string? GetNullableString(this MySqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    /// <summary>
    /// Gets the value of the column with the given ordinal as a nullable int.
    /// </summary>
    public static int? GetNullableInt32(this MySqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);

    /// <summary>
    /// Gets the value of the column with the given ordinal as a fixed-length byte array of the given length.
    /// </summary>
    public static byte[] GetFixedBytes(this MySqlDataReader reader, int ordinal, int length)
    {
        byte[] buffer = new byte[length];
        reader.GetBytes(ordinal, 0, buffer, 0, length);
        return buffer;
    }

    /// <summary>
    /// Downloads the file to the given path. If the given byte limit is exceeded, the download is cancelled and false is returned, otherwise true.
    /// </summary>
    public static async Task<bool> Download(this IBrowserFile file, string path, long limitBytes)
    {
        if (file.Size > limitBytes)
            return false;
        using Stream input = file.OpenReadStream(limitBytes);
        try
        {
            if (input.Length > limitBytes)
                return false;
        }
        catch (NotSupportedException) { }
        catch { throw; }

        try
        {
            using Stream output = File.Create(path);
            byte[] buffer = new byte[32768];
            long totalBytes = 0;
            int lastBytes;

            while (totalBytes <= limitBytes)
            {
                lastBytes = await input.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, limitBytes - totalBytes)));
                if (lastBytes <= 0)
                    break;
                totalBytes += lastBytes;
                if (totalBytes <= limitBytes)
                    await output.WriteAsync(buffer.AsMemory(0, lastBytes));
            }

            if (totalBytes > limitBytes)
            {
                output.Close();
                File.Delete(path);
                return false;
            }

            return true;
        }
        catch
        {
            try
            {
                File.Delete(path);
            }
            catch { }

            throw;
        }

    }

    /// <summary>
    /// Translates the given role's name into the German language.
    /// </summary>
    public static string ToGerman(this Role role)
        => role switch
        {
            Role.User => "Nutzer",
            Role.Administrator => "Administrator",
            _ => role.ToString()
        };

    /// <summary>
    /// Translates the given theme option's name into the German language.
    /// </summary>
    public static string ToGerman(this Theme theme)
        => theme switch
        {
            Theme.System => "System",
            Theme.Dark => "Dunkel",
            Theme.Light => "Hell",
            _ => theme.ToString()
        };

    /// <summary>
    /// Translates the given role's name into the German language.
    /// </summary>
    public static string ToGerman(this ProjectRole role)
        => role switch
        {
            ProjectRole.None => "Nein",
            ProjectRole.Participant => "Teilnehmer",
            ProjectRole.Manager => "Verwalter",
            ProjectRole.Owner => "Besitzer",
            _ => role.ToString()
        };

    /// <summary>
    /// Reads the saved set of expanded folders or returns an empty set if no valid set was saved.
    /// </summary>
    public static async Task<HashSet<int>> GetExpandedFolders(this IJSRuntime javaScript, int projectId)
    {
        HashSet<int> result = [];

        string? cookie = await javaScript.GetCookieAsync("ExpandedFolders" + projectId);
        if (!string.IsNullOrEmpty(cookie))
            foreach (var idString in cookie.Split(','))
                if (int.TryParse(idString, out int id))
                    result.Add(id);
        
        return result;
    }

    /// <summary>
    /// Saves the given set of expanded folders.
    /// </summary>
    public static async Task SetExpandedFolders(this IJSRuntime javaScript, int projectId, HashSet<int> expandedFolders)
        => await javaScript.SetCookieAsync("ExpandedFolders" + projectId,
            string.Join(',', expandedFolders.Select(id => id.ToString())),
            DateTime.UtcNow.AddDays(30));
}