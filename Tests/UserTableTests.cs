using cocoding.Tests;

namespace cocoding.Data;

public static partial class UserTable
{
    /// <summary>
    /// Tests the UserTable class.
    /// </summary>
    internal static void Test()
    {
        Testing.ShouldBeTrue(() => Database.Transaction(cp => 
            GetById(cp, 404) == null),
            "Non-existent user ID was found.");

        Testing.ShouldBeTrue(() => Database.Transaction(cp => 
            GetByUsername(cp, "404") == null),
            "Non-existent username was found.");

        UserSession createdUser = Database.Transaction(cp => 
            CreateUser(cp, "usertable", "Password1#", null));

        UserEntry? idUser = Database.Transaction(cp =>
            GetById(cp, createdUser.Id));
        
        Testing.ShouldBeTrue(() =>
            idUser != null,
            "Created user wasn't found using ID.");
        if (idUser == null) return;
        
        Testing.ShouldBeTrue(() =>
            idUser.Name == null,
            "Incorrect null-name for user retrieved by ID.");
        
        Testing.ShouldBeTrue(() =>
            idUser.Username == "usertable",
            "Incorrect username for user retrieved by ID.");
        
        Testing.ShouldBeTrue(() =>
            idUser.Role == Role.User,
            "Incorrect role for user retrieved by ID.");
        
        Testing.ShouldBeTrue(() =>
                idUser.Theme == Theme.System,
            "Incorrect theme for user retrieved by ID.");

        UserEntryWithPassword? usernameUser = Database.Transaction(cp =>
            GetByUsernameWithPassword(cp, "usertable"));
        
        Testing.ShouldBeTrue(() =>
            usernameUser != null,
            "Created user wasn't found using username (1).");
        if (usernameUser == null) return;
        
        Testing.ShouldBeTrue(() =>
            usernameUser.Name == null,
            "Incorrect null-name for user retrieved by username.");
        
        Testing.ShouldBeTrue(() =>
            usernameUser.Username == "usertable",
            "Incorrect username for user retrieved by username.");
        
        Testing.ShouldBeTrue(() =>
            usernameUser.Role == Role.User,
            "Incorrect role for user retrieved by username.");
        
        Testing.ShouldBeTrue(() =>
                usernameUser.Theme == Theme.System,
            "Incorrect theme for user retrieved by username.");
        
        Testing.ShouldBeTrue(() =>
            usernameUser.PasswordHash.Length == 32 && usernameUser.PasswordHash.Any(x => x != default),
            "Bad password hash for user retrieved by username.");
        
        Testing.ShouldBeTrue(() =>
            usernameUser.Salt.Length == 16 && usernameUser.Salt.Any(x => x != default),
            "Bad salt for user retrieved by username, Might be randomly generated 0-array, retest first!");

        Database.Transaction(cp =>
            DeleteUser(cp, createdUser.Id));

        Testing.ShouldBeTrue(() => Database.Transaction(cp =>
            GetById(cp, createdUser.Id) == null),
            "User wasn't deleted properly.");

        createdUser = Database.Transaction(cp => 
            CreateUser(cp, "usertable", "Password1#", "Paul Elstak"));

        Testing.ShouldBeTrue(() => Database.Transaction(cp =>
            GetById(cp, createdUser.Id)?.Name == "Paul Elstak"),
            "Incorrect non-null name for user retrieved by ID.");

        Database.Transaction(cp =>
            SetRole(cp, createdUser.Id, Role.Administrator));

        Testing.ShouldBeTrue(() => Database.Transaction(cp =>
            GetById(cp, createdUser.Id)?.Role == Role.Administrator),
            "Role wasn't set properly.");

        Database.Transaction(cp =>
            SetTheme(cp, createdUser.Id, Theme.Dark));

        Testing.ShouldBeTrue(() => Database.Transaction(cp =>
                GetById(cp, createdUser.Id)?.Theme == Theme.Dark),
            "Theme wasn't set properly.");

        Testing.ShouldBeTrue(() => Database.Transaction(cp =>
            EditUser(cp, createdUser.Id, "usertable2", null)),
            "User wasn't edited.");

        idUser = Database.Transaction(cp =>
            GetById(cp, createdUser.Id));
        
        Testing.ShouldBeTrue(() =>
            idUser != null,
            "Created user wasn't found using ID.");
        if (idUser == null) return;
        
        Testing.ShouldBeTrue(() => Database.Transaction(cp =>
            idUser.Username == "usertable2"),
            "Username wasn't edited properly.");
        
        Testing.ShouldBeTrue(() => Database.Transaction(cp =>
            idUser.Name == null),
            "Name wasn't set to null properly.");

        usernameUser = Database.Transaction(cp =>
            GetByUsernameWithPassword(cp, "usertable2"));
        
        Testing.ShouldBeTrue(() =>
            usernameUser != null,
            "Created user wasn't found using username (2).");
        if (usernameUser == null) return;

        Testing.ShouldBeTrue(() => Database.Transaction(cp =>
            EditUser(cp, createdUser.Id, "usertable", "Paul Elstak", "This is a very safe password.#")),
            "User wasn't edited with a password.");

        UserEntryWithPassword? usernameUser2 = Database.Transaction(cp =>
            GetByUsernameWithPassword(cp, "usertable"));
        
        Testing.ShouldBeTrue(() =>
            usernameUser2 != null,
            "Created user wasn't found using username (3).");
        if (usernameUser2 == null) return;
        
        Testing.ShouldBeTrue(() =>
            usernameUser2.PasswordHash.Length == 32 && usernameUser2.PasswordHash.Any(x => x != default),
            "Bad password hash after password change.");
        
        Testing.ShouldBeTrue(() =>
            usernameUser2.Salt.Length == 16 && usernameUser2.Salt.Any(x => x != default),
            "Bad salt after password change. Might be randomly generated 0-array, retest first!");
        
        Testing.ShouldBeTrue(() =>
            !usernameUser.PasswordHash.SequenceEqual(usernameUser2.PasswordHash),
            "Password hash wasn't modified after password change.");
        
        Testing.ShouldBeTrue(() =>
            !usernameUser.PasswordHash.SequenceEqual(usernameUser2.PasswordHash),
            "Salt wasn't modified after password change.");

        Testing.ShouldBeTrue(() => Database.Transaction(cp =>
            SetPassword(cp, createdUser.Id, "Password1#")),
            "User wasn't edited with a password.");

        usernameUser = Database.Transaction(cp =>
            GetByUsernameWithPassword(cp, "usertable"));
        
        Testing.ShouldBeTrue(() =>
            usernameUser != null,
            "Created user wasn't found using username (4).");
        if (usernameUser == null) return;
        
        Testing.ShouldBeTrue(() =>
            usernameUser.PasswordHash.Length == 32 && usernameUser2.PasswordHash.Any(x => x != default),
            "Bad password hash after password set.");
        
        Testing.ShouldBeTrue(() =>
            usernameUser.Salt.Length == 16 && usernameUser2.Salt.Any(x => x != default),
            "Bad salt after password set. Might be randomly generated 0-array, retest first!");
        
        Testing.ShouldBeTrue(() =>
            !usernameUser.PasswordHash.SequenceEqual(usernameUser2.PasswordHash),
            "Password hash wasn't modified after password set.");
        
        Testing.ShouldBeTrue(() =>
            !usernameUser.PasswordHash.SequenceEqual(usernameUser2.PasswordHash),
            "Salt wasn't modified after password set.");
    }
}