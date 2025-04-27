using cocoding.Tests;

namespace cocoding.Data;

public static partial class LoginTable
{
    /// <summary>
    /// Tests the LoginTable class.
    /// </summary>
    internal static void Test()
    {
        int id1 = Database.Transaction(cp =>
            UserTable.CreateUser(cp, "logintable1", "Password1#", null))?.Id ?? throw new Exception("Failed to create user for LoginTable tests.");

        int id2 = Database.Transaction(cp =>
            UserTable.CreateUser(cp, "logintable2", "Password1#", null))?.Id ?? throw new Exception("Failed to create user for LoginTable tests.");

        Session generatedSession = Database.Transaction(cp =>
            GenerateToken(cp, id1));

        Testing.ShouldBeTrue(() =>
            generatedSession.Token.Length == 64,
            "Bad login token was generated.");

        Testing.ShouldBeTrue(() =>
            generatedSession.Expiration > DateTime.UtcNow.AddDays(39) && generatedSession.Expiration < DateTime.UtcNow.AddDays(41),
            "Bad login token expiration was set.");

        LoginEntry? idEntry = Database.Transaction(cp =>
            GetByToken(cp, generatedSession.Token));

        Testing.ShouldBeTrue(() =>
            idEntry != null,
            "Failed to get login entry by user ID.");
        if (idEntry == null) return;

        Testing.ShouldBeTrue(() =>
            idEntry.UserId == id1,
            "Incorrect user ID for login entry retrieved by user ID.");

        Testing.ShouldBeTrue(() =>
            idEntry.Token == generatedSession.Token,
            "Incorrect token for login entry retrieved by user ID.");

        Testing.ShouldBeTrue(() =>
            idEntry.Expiration.Ticks == generatedSession.Expiration.Ticks,
            "Incorrect expiration for login entry retrieved by user ID.");

        Database.Transaction(cp =>
            DeleteToken(cp, generatedSession.Token));

        Testing.ShouldBeTrue(() => Database.Transaction(cp =>
            GetByToken(cp, generatedSession.Token) == null),
            "Login token wasn't properly deleted.");

        Session s1 = Database.Transaction(cp =>
            GenerateToken(cp, id1));
        Session s2 = Database.Transaction(cp =>
            GenerateToken(cp, id2));
        Database.Transaction(cp =>
            DeleteAllTokens(cp, id1));

        Testing.ShouldBeTrue(() => Database.Transaction(cp =>
            GetByToken(cp, s1.Token) == null),
            "Deleting all login tokens didn't delete a token.");

        Testing.ShouldBeTrue(() => Database.Transaction(cp =>
            GetByToken(cp, s2.Token) != null),
            "Deleting all login tokens deleted another user's token.");

        DateTime expiration = DateTime.UtcNow.AddDays(13);

        s1 = Database.Transaction(cp =>
            GenerateToken(cp, id1));

        Database.Transaction(cp =>
            SetExpiration(cp, s1.Token, expiration));

        idEntry = Database.Transaction(cp =>
            GetByToken(cp, s1.Token));

        Testing.ShouldBeTrue(() =>
            idEntry != null && idEntry.Expiration == expiration,
            "Expiration wasn't set properly.");
    }
}