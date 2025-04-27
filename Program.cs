using System.Reflection;
using cocoding;
using cocoding.Data;
using cocoding.Tests;

Global.SetVersion(Assembly.GetExecutingAssembly());
Console.WriteLine($"cocoding v{Global.VersionString}");
Database.UpgradeTables();

Directory.CreateDirectory("../FileStates");

if (args.Length > 0)
    switch (args[0])
    {
        case "make-admin":
            if (args.Length == 2)
            {
                var user = Database.Transaction(cp => UserTable.GetByUsername(cp, args[1]));
                if (user != null)
                {
                    Database.Transaction(cp => UserTable.SetRole(cp, user.Id, Role.Administrator));
                    Console.WriteLine($"Set the role of user \"{args[1]}\" to \"Administrator\".");
                }
                else Console.WriteLine("Unable to find the provided username!");
            }
            else Console.WriteLine("Invalid number or arguments for command \"make-admin\"! Run \"cocoding help\" for help.");
            return;
        case "test":
            if (args.Length == 1)
            {
                try
                {
                    Testing.RunAll();
                    Console.WriteLine("All tests succeeded!");
                }
                catch (TestFailedException ex)
                {
                    Console.WriteLine($"Test failed: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception thrown: {ex.Message}\n{ex.StackTrace}");
                }
            }
            else Console.WriteLine("Invalid number or arguments for command \"test\"! Run \"cocoding help\" for help.");
            return;
        case "help":
            Console.WriteLine("Help for \"cocoding\":");
            Console.WriteLine();
            Console.WriteLine("cocoding");
            Console.WriteLine("Starts the web server for the application normally.");
            Console.WriteLine();
            Console.WriteLine("cocoding help");
            Console.WriteLine("Lists possible CLI arguments (this page).");
            Console.WriteLine();
            Console.WriteLine("cocoding make-admin [username]");
            Console.WriteLine("Sets the role of the user with the provided username to \"Administrator\"");
            Console.WriteLine();
            Console.WriteLine("cocoding test");
            Console.WriteLine("Runs the included tests using the test database connection.");
            return;
    }

Global.Start();
