namespace cocoding.Data;

/// <summary>
/// Methods for user management.
/// </summary>
public static partial class UserManager
{
    /// <summary>
    /// Registers for a new account while checking the parameters.<br/>
    /// Issues are returned as exceptions.
    /// </summary>
    public static UserSession Register(string username, string password1, string password2)
    {
        if (username == "")
            throw new Exception("Geben Sie einen Nutzernamen ein!");
        if (password1 == "")
            throw new Exception("Geben Sie ein Passwort ein!");
        if (password2 == "")
            throw new Exception("Bestätigen Sie das Passwort!");
        if (password1 != password2)
            throw new Exception("Die Passwörter stimmen nicht überein!");
        if (!CheckUsernameFormat(username))
            throw new Exception("Nutzernamen müssen zwischen 3 und 20 Zeichen lang sein und dürfen nur Kleinbuchstaben, Ziffern, Bindestriche, Punkte und Unterstriche enthalten. Das erste und das letzte Zeichen dürfen nur Kleinbuchstaben oder Ziffern sein.");
        if (!CheckPasswordFormat(password1))
            throw new Exception("Passwörter müssen mindestens 8 Zeichen lang sein und Großbuchstaben, Kleinbuchstaben, Ziffern und Symbole enthalten.");
        
        return Database.Transaction(cp =>
        {
            if (UserTable.GetByUsername(cp, username) != null)
                throw new Exception("Dieser Nutzername wird bereits von einem anderen Nutzer verwendet.");

            var us = UserTable.CreateUser(cp, username, password1);
            us.Session = LoginTable.GenerateToken(cp, us.Id);
            return us;
        });
    }

    /// <summary>
    /// Logs in using the given credentials while checking the parameters.<br/>
    /// Issues are returned as exceptions.
    /// </summary>
    public static UserSession Login(string username, string password)
    {
        if (username == "")
            throw new Exception("Geben Sie Ihren Nutzernamen ein!");
        if (password == "")
            throw new Exception("Geben Sie Ihr Passwort ein!");
        
        return Database.Transaction(cp =>
        {
            var user = UserTable.GetByUsernameWithPassword(cp, username);
            if (user == null || !user.PasswordHash.SequenceEqual(Security.HashPassword(password, user.Salt)))
                throw new Exception("Falscher Nutzername oder Passwort!");

            var session = LoginTable.GenerateToken(cp, user.Id);
            
            return new UserSession(user.Id, user.Username, user.Name, user.Role, user.Theme, session);
        });
    }

    /// <summary>
    /// Deletes the authentication token behind the given session.
    /// </summary>
    public static void Logout(UserSession us)
        => Database.Transaction(cp => LoginTable.DeleteToken(cp, us.Session?.Token));

    /// <summary>
    /// Builds a UserSession object for the given token or returns null if the token or the connected user doesn't exist.
    /// </summary>
    public static UserSession? ResolveToken(string? token)
        => Database.Transaction(cp =>
        {
            var session = LoginTable.GetByToken(cp, token);
            if (session == null)
                return null;

            var user = UserTable.GetById(cp, session.UserId);
            if (user == null)
                return null;

            return new UserSession(user.Id, user.Username, user.Name, user.Role, user.Theme, new(session.Token, session.Expiration));
        });

    /// <summary>
    /// Deletes the current session and generates a new one.<br/>
    /// The provided <c>UserSession</c> object will be modified accordingly.
    /// </summary>
    public static void RenewToken(UserSession us)
    {
        if (us.Session == null)
            return;

        Database.Transaction(cp =>
        {
            var session = LoginTable.GenerateToken(cp, us.Id);
            LoginTable.SetExpiration(cp, us.Session.Token, DateTime.UtcNow.AddMinutes(5));
            us.Session = session;
        });
    }

    /// <summary>
    /// Sets the password of the user with the given ID while checking the parameters.<br/>
    /// Issues are returned as exceptions.
    /// </summary>
    public static void SetPassword(int userId, string password1, string password2)
    {
        if (password1 == "")
            throw new Exception("Geben Sie ein Passwort ein!");
        if (password2 == "")
            throw new Exception("Bestätigen Sie das Passwort!");
        if (password1 != password2)
            throw new Exception("Die Passwörter stimmen nicht überein!");
        if (!CheckPasswordFormat(password1))
            throw new Exception("Passwörter müssen mindestens 8 Zeichen lang sein und Großbuchstaben, Kleinbuchstaben, Ziffern und Symbole enthalten.");
        
        Database.Transaction(cp => UserTable.SetPassword(cp, userId, password1));
    }

    /// <summary>
    /// Deletes the user and all associated login tokens.
    /// </summary>
    public static void DeleteUser(int userId)
        => Database.Transaction(cp =>
        {
            MessageManager.DeleteAllMessagesByUser(cp, userId);
            LoginTable.DeleteAllTokens(cp, userId);
            ProjectManager.DeleteForDeletedUser(userId);
            UserTable.DeleteUser(cp, userId);
        });

    /// <summary>
    /// Edits the user with the given ID while checking the parameters.
    /// </summary>
    public static void EditUser(UserSession us, string username, string? name, string currentPassword, string password1, string password2)
    {
        if (username == "")
            throw new Exception("Geben Sie einen Nutzernamen ein!");
        if (currentPassword == "")
            throw new Exception("Geben Sie Ihr aktuelles Passwort ein!");
        if (name == "")
            name = null;
        if (name != null && name.Length > 50)
            throw new Exception("Namen dürfen höchstens 50 Zeichen lang sein!");
        if (password1 != password2)
            throw new Exception("Die neuen Passwörter stimmen nicht überein!");
        if (!CheckUsernameFormat(username))
            throw new Exception("Nutzernamen müssen zwischen 3 und 20 Zeichen lang sein und dürfen nur Kleinbuchstaben, Ziffern, Bindestriche, Punkte und Unterstriche enthalten. Das erste und das letzte Zeichen dürfen nur Kleinbuchstaben oder Ziffern sein.");
        if (password1 != "" && !CheckPasswordFormat(password1))
            throw new Exception("Passwörter müssen mindestens 8 Zeichen lang sein und Großbuchstaben, Kleinbuchstaben, Ziffern und Symbole enthalten.");
        
        Database.Transaction(cp =>
        {
            if (us.Username != username && UserTable.GetByUsername(cp, username) != null)
                throw new Exception("Dieser Nutzername wird bereits von einem anderen Nutzer verwendet.");

            var user = UserTable.GetByUsernameWithPassword(cp, us.Username);
            if (user == null || !user.PasswordHash.SequenceEqual(Security.HashPassword(currentPassword, user.Salt)))
                throw new Exception("Falsches aktuelles Passwort!");

            if (password1 == "")
                UserTable.EditUser(cp, us.Id, username, name);
            else UserTable.EditUser(cp, us.Id, username, name, password1);

            us.Username = username;
            us.Name = name;
        });
    }

    /// <summary>
    /// Check whether the given username satisfies the username requirements.
    /// </summary>
    private static bool CheckUsernameFormat(string username)
    {
        if (username.Length < 3 || username.Length > 20)
            return false;
        if ("-._".Contains(username.First()) || "-._".Contains(username.Last()))
            return false;
        string supportedChars = "abcdefghijklmnopqrstuvwxyz0123456789-._";
        return username.All(x => supportedChars.Contains(x));
    }
    
    /// <summary>
    /// Return the user name if it exists and the display name otherwise.
    /// If the user doesn't exist at all return <user deleted> string.
    /// </summary>
    public static string GetPrintableUserName(UserEntry? userEntry)
    {
        if (userEntry == null)
            return ("<Nutzer gelöscht>");
        return userEntry.Name ?? userEntry.DisplayName;
    }

    /// <summary>
    /// Return the user name if it exists and the display name otherwise.
    /// If the user doesn't exist at all return &lt;user deleted&gt; string.
    /// </summary>
    public static string GetPrintableUserName(int userId)
        => Database.Transaction(cp => UserTable.GetById(cp, userId))?.DisplayName ?? "<Nutzer gelöscht>";


    /// <summary>
    /// Check whether the given password satisfies the password requirements.
    /// </summary>
    private static bool CheckPasswordFormat(string password)
    {
        if (password.Length < 8)
            return false;
        string capitalChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string lowerChars = "abcdefghijklmnopqrstuvwxyz";
        string digitChars = "0123456789";
        bool capital = false, lower = false, digit = false, special = false;
        foreach (char c in password)
        {
            if (capitalChars.Contains(c))
                capital = true;
            else if (lowerChars.Contains(c))
                lower = true;
            else if (digitChars.Contains(c))
                digit = true;
            else special = true;
        }
        return capital && lower && digit && special;
    }
}