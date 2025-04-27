namespace cocoding.Data;

/// <summary>
/// Server configuration.
/// </summary>
public class Config()
{
    /// <summary>
    /// The database connection string for MySQL-compatible databases.
    /// </summary>
    public string DatabaseConnection = "server=127.0.0.1; database=cocoding; user=cocoding; password=cocoding";

    /// <summary>
    /// The HTTP port or 0/default to disable.
    /// </summary>
    public int HTTP = 9000;

    /// <summary>
    /// The HTTPS port of 0/default to disable.
    /// </summary>
    public int HTTPS = default;

    /// <summary>
    /// The path to the SSL certificate to use for HTTPS.
    /// </summary>
    public string? Certificate = null;

    /// <summary>
    /// Whether to show ASP.NET logs.
    /// </summary>
    public bool AspNetLogs = false;

    /// <summary>
    /// Whether to start in debug mode.
    /// </summary>
    public bool Debug = false;

    /// <summary>
    /// The domains to request certificates for.
    /// </summary>
    public string[] AutoCertificateDomains = [];

    /// <summary>
    /// The email address to request certificates with.
    /// </summary>
    public string? AutoCertificateEmail = null;
}