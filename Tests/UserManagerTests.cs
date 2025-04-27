using cocoding.Tests;

namespace cocoding.Data;

public static partial class UserManager
{
    /// <summary>
    /// Tests the UserManager class.
    /// </summary>
    internal static void Test()
    {
        UserSession us = Register("a-._1", "12345aA#", "12345aA#");

        Testing.ShouldFail(() =>
            Login("500", "12345aA#"),
            "Invalid username accepted while logging in.");

        Testing.ShouldFail(() =>
            Login("a-._1", "Password2#"),
            "Invalid password accepted while logging in.");

        Testing.ShouldBeTrue(() =>
            us.Session != null,
            "Session wasn't set while registering.");
        if (us.Session == null) return;

        Testing.ShouldBeTrue(() =>
            ResolveToken(us.Session.Token) != null,
            "Token wasn't generated properly while registering.");
        
        Testing.ShouldFail(() =>
            Register("te", "Password1#", "Password1#"),
            "Very short username was accepted while registering.");
        
        Testing.ShouldFail(() =>
            Register("01234567890123456789a", "Password1#", "Password1#"),
            "Very long username was accepted while registering.");
        
        Testing.ShouldFail(() =>
            Register("tes#t", "Password1#", "Password1#"),
            "Unacceptable username character was accepted while registering.");
        
        Testing.ShouldFail(() =>
            Register(".test", "Password1#", "Password1#"),
            "Symbol at the start of username was accepted while registering.");
        
        Testing.ShouldFail(() =>
            Register("test.", "Password1#", "Password1#"),
            "Symbol at the end of username was accepted while registering.");
        
        Testing.ShouldFail(() =>
            Register("test", "Password1#", "Password2#"),
            "Unequal passwords were accepted while registering.");
        
        Testing.ShouldFail(() =>
            Register("test", "Password1", "Password1"),
            "Password without symbol accepted while registering.");
        
        Testing.ShouldFail(() =>
            Register("test", "assword1#", "assword1#"),
            "Password without uppercase letter accepted while registering.");
        
        Testing.ShouldFail(() =>
            Register("test", "PPPPPPP1#", "PPPPPPP1#"),
            "Password without lowercase letter accepted while registering.");
        
        Testing.ShouldFail(() =>
            Register("test", "Password#", "Password#"),
            "Password without digit accepted while registering.");
        
        Testing.ShouldFail(() =>
            Register("test", "1234aA#", "1234aA#"),
            "Very short password accepted while registering.");
            
        us = Login("a-._1", "12345aA#");

        Testing.ShouldBeTrue(() =>
            us.Session != null,
            "Session wasn't set while logging in.");
        if (us.Session == null) return;

        Testing.ShouldBeTrue(() =>
            ResolveToken(us.Session.Token) != null,
            "Token wasn't generated properly while logging in.");

        string oldToken = us.Session.Token;

        RenewToken(us);

        UserSession? oldUS = ResolveToken(oldToken);

        Testing.ShouldBeTrue(() =>
            oldUS != null && oldUS.Session != null && oldUS.Session.Expiration < DateTime.UtcNow.AddMinutes(7),
            "Old token wasn't changed properly after renewing it.");

        Testing.ShouldBeTrue(() =>
            ResolveToken(us.Session.Token) != null,
            "New token doesn't exist after renewing it.");

        Logout(us);

        Testing.ShouldBeTrue(() =>
            ResolveToken(us.Session.Token) == null,
            "Token still exists after logging out.");
        
        Testing.ShouldFail(() =>
            Register("a-._1", "Password1#", "Password1#"),
            "Existing username accepted while registering.");

        us = Register("test", "Password1#", "Password1#");

        SetPassword(us.Id, "Password2#", "Password2#");

        Testing.ShouldFail(() =>
            SetPassword(us.Id, "Password1#", "Password2#"),
            "Unequal passwords were accepted while setting password.");
        
        Testing.ShouldFail(() =>
            SetPassword(us.Id, "Password1", "Password1"),
            "Password without symbol accepted while setting password.");
        
        Testing.ShouldFail(() =>
            SetPassword(us.Id, "assword1#", "assword1#"),
            "Password without uppercase letter accepted while setting password.");
        
        Testing.ShouldFail(() =>
            SetPassword(us.Id, "PPPPPPP1#", "PPPPPPP1#"),
            "Password without lowercase letter accepted while setting password.");
        
        Testing.ShouldFail(() =>
            SetPassword(us.Id, "Password#", "Password#"),
            "Password without digit accepted while setting password.");
        
        Testing.ShouldFail(() =>
            SetPassword(us.Id, "1234aA#", "1234aA#"),
            "Very short password accepted while setting password.");

        EditUser(us, "test2", "Paul Elstak", "Password2#", "Password1#", "Password1#");

        Testing.ShouldBeTrue(() =>
            us.Username == "test2",
            "UserSession.Username wasn't set after editing user.");

        Testing.ShouldBeTrue(() =>
            us.Name == "Paul Elstak",
            "UserSession.Name wasn't null after editing user.");

        us = Login("test2", "Password1#");
        Testing.ShouldBeTrue(() =>
            us != null,
            "Login failed after editing user (1).");
        if (us == null) return;

        Testing.ShouldBeTrue(() =>
            us.Username == "test2",
            "User's username wasn't set after editing user and logging in again.");

        Testing.ShouldBeTrue(() =>
            us.Name == "Paul Elstak",
            "User's name wasn't set after editing user and logging in again.");

        Testing.ShouldFail(() =>
            EditUser(us, "test2", "Paul Elstak", "WrongPassword1#", "Password2#", "Password2#"),
            "Invalid current password accepted while editing user.");

        Testing.ShouldFail(() =>
            EditUser(us, "test2", "Paul Elstak", "Password1#", "Password2#", "Password3#"),
            "Unequal passwords were accepted while editing user.");
        
        Testing.ShouldFail(() =>
            EditUser(us, "test2", "Paul Elstak", "Password1#", "Password1", "Password1"),
            "Password without symbol accepted while editing user.");
        
        Testing.ShouldFail(() =>
            EditUser(us, "test2", "Paul Elstak", "Password1#", "assword1#", "assword1#"),
            "Password without uppercase letter accepted while editing user.");
        
        Testing.ShouldFail(() =>
            EditUser(us, "test2", "Paul Elstak", "Password1#", "PPPPPPP1#", "PPPPPPP1#"),
            "Password without lowercase letter accepted while editing user.");
        
        Testing.ShouldFail(() =>
            EditUser(us, "test2", "Paul Elstak", "Password1#", "Password#", "Password#"),
            "Password without digit accepted while editing user.");
        
        Testing.ShouldFail(() =>
            EditUser(us, "test2", "Paul Elstak", "Password1#", "1234aA#", "1234aA#"),
            "Very short password accepted while editing user.");
        
        Testing.ShouldFail(() =>
            EditUser(us, "test2", "01234567890123456789012345678901234567890123456789a", "Password1#", "", ""),
            "Very long name was accepted while editing user.");
        
        EditUser(us, "test", null, "Password1#", "", "");

        Testing.ShouldBeTrue(() =>
            us.Name == null,
            "UserSession.Name wasn't null after editing user.");

        us = Login("test", "Password1#");

        Testing.ShouldBeTrue(() =>
            us != null,
            "Login failed after editing user (2).");
        if (us == null) return;

        Testing.ShouldBeTrue(() =>
            us.Session != null,
            "Login didn't provide a session after editing user.");
        if (us.Session == null) return;

        Testing.ShouldBeTrue(() =>
            us.Name == null,
            "User's name wasn't null after editing user and logging in again.");

        Testing.ShouldFail(() =>
            EditUser(us, "a-._1", null, "Password1#", "", ""),
            "Existing username accepted while editing user.");

        Testing.ShouldFail(() =>
            EditUser(us, "a", null, "Password1#", "", ""),
            "Bad username accepted while editing user.");

        //Used functions in DeleteUser are tested in 
        //LoginTableTests, ProjectManagerTests and UserTableTests respectively
        DeleteUser(us.Id);

        Testing.ShouldFail(() =>
            Login("test", "Password1#"),
            "Login still works after deleting user.");

        Testing.ShouldBeTrue(() => Database.Transaction(cp =>
            LoginTable.GetByToken(cp, us.Session.Token) == null),
            "Login token still exists after deleting user.");
    }
}