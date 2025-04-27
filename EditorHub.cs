using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using cocoding.Data;
using Microsoft.AspNetCore.SignalR;

namespace cocoding;

using Components.Pages;

public class EditorHub : Hub {
    public static readonly ConcurrentDictionary<int, FileGroupData> FileGroups = [];
    public static readonly ConcurrentDictionary<int, bool> Locks = new ();

    public static event Action? UserListChanged = null;
    
    public static void CallUserListChanged()
        => UserListChanged?.Invoke();

    #region EDITOR SYNCHRONIZATION RELATED METHODS
    public override Task OnConnectedAsync()
    {
        var context = Global.HttpContext;
        if (context.Request.Cookies.TryGetValue("Token", out var token))
        {
            var us = UserManager.ResolveToken(token);
            if (us != null)
            {
                context.User = new(new ClaimsIdentity([new Claim("uid", us.Id.ToString())], ClaimTypes.Authentication, "uid", ClaimTypes.Role));
                return Task.CompletedTask;
            }
        }

        Context.Abort();
        return Task.CompletedTask;
    }

    public static string? GetUserId()
        => Global.HttpContext.User.Identity?.Name;

    public async Task EnterFile(int fileId)
    {
        var userIdString = GetUserId();
        if (userIdString != null && int.TryParse(userIdString, out int userId)
                && Database.Transaction(cp =>
                {
                    var file = FileTable.GetById(cp, fileId);
                    return file != null && AssignmentTable.GetRole(cp, file.ProjectId, userId) >= ProjectRole.Participant;
                }))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, fileId.ToString());
            if (FileGroups.TryGetValue(fileId, out var fileGroup))
                Context.Items["FileId"] = fileId;
        }
    }

    public async Task LeaveFile(int fileId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, fileId.ToString());
        Context.Items.Remove("FileId");
    }

    public async Task Load()
    {
        if (FileDeleted(out var fileGroup))
        {
            await NotifyFileDeleted();
            return;
        }
        foreach (byte[] update in fileGroup.State)
            await Clients.Caller.SendAsync("ApplyUpdate", update);
    }

    public async Task PushUpdate(byte[] update, string sessionId, int lineNumber)
    {
        if (FileDeleted(out var fileGroup))
        {
            await NotifyFileDeleted();
            return;
        }

        lock(fileGroup)
        {
            fileGroup.State.Add(update);
            fileGroup.Changed = DateTime.UtcNow;
            fileGroup.UserId = int.Parse(GetUserId()??"");
            fileGroup.Dirty = true;
            
            UpdateSessionInformation(fileGroup, sessionId, lineNumber);
        }

        
        await Clients.OthersInGroup(fileGroup.FileId.ToString()).SendAsync("ApplyUpdate", update);
    }

    public async Task PushState(byte[] update)
    {
        if (FileDeleted(out var fileGroup))
        {
            await NotifyFileDeleted();
            return;
        }

        var oldState = fileGroup.State;
        if (oldState.Count != 1 || !oldState[0].SequenceEqual(update))
            lock(fileGroup)
            {
                fileGroup.State = [update];
                fileGroup.Dirty = true;
            }
    }

    public async Task BroadcastAwareness(byte[] clientStates, string sessionId, int lineNumber)
    {
        if (FileDeleted(out var fileGroup))
        {
            await NotifyFileDeleted();
            return;
        }

        UpdateSessionInformation(fileGroup, sessionId, lineNumber);

        await Clients.OthersInGroup(fileGroup.FileId.ToString()).SendAsync("ReceiveAwareness", clientStates);
    }

    public async Task BroadcastAwarenessRequest(string issuerSession, string issuerName)
    {
        if (FileDeleted(out var fileGroup))
        {
            await NotifyFileDeleted();
            return;
        }

        await Clients.OthersInGroup(fileGroup.FileId.ToString()).SendAsync("BroadcastAwarenessRequest", issuerSession, issuerName);
    }

    private bool FileDeleted([MaybeNullWhen(true)] out FileGroupData fileGroup)
    {
        if (Context.Items["FileId"] is int fileId && FileGroups.TryGetValue(fileId, out fileGroup))
            return false;
        
        fileGroup = null;
        return true;
    }

    private async Task NotifyFileDeleted()
        => await Clients.Caller.SendAsync("FileDeleted");


    public static List<byte[]>? GetCurrentState(int fileId)
    {
        FileGroups.TryGetValue(fileId, out var fileGroup);

        return fileGroup?.State;
    }
    /// <summary>
    /// Update the information of the session in the file group associated with the sessionId.
    /// </summary>
    private void UpdateSessionInformation(FileGroupData fileGroup, string sessionId, int lineNumber)
    {
        int? line = lineNumber > 0 ? lineNumber : null;
        string? userIdString = GetUserId();

        if (userIdString != null && int.TryParse(userIdString, out int userId)){
            foreach (var session in fileGroup.Sessions.Values)
            {
                if (session.UserId == userId && session.Id == sessionId && session.LineNumber != lineNumber)
                {
                    session.LineNumber = line;
                    session.TimeStamp = DateTime.UtcNow;
                    UserListChanged?.Invoke();
                    return;
                }
            }
        }

    }
    #endregion
    
    #region VERSION RELATED METHODS 
    /// <summary>
    /// If the file the client is currently registered to is locked, invoke the LockEditor call.
    /// </summary>
    public async Task CheckLocked()
    {
        if (Context.Items.TryGetValue("FileId", out var fileIdObj) && fileIdObj is int fileId)
            if (Locks.TryGetValue(fileId, out var locked))
                if (locked)
                    await Clients.Caller.SendAsync("LockEditor");
    }

    /// <summary>
    /// Create a database entry for a new version and make the state persistent.
    /// </summary>
    public async Task CreateVersion(string? label, string? comment, byte[] update)
    {
        if (FileDeleted(out var fileGroup))
        {
            await NotifyFileDeleted();
            return;
        }

        if (Context.Items.TryGetValue("FileId", out var fileIdObj) && fileIdObj is int fileId)
        {

            var fileEntry = Database.Transaction(cp => FileTable.GetById(cp, fileId));
            if (fileEntry == null)
            {
                Console.WriteLine("File not found when trying to create a version.");
                return;
            }
            
            var userIdString = GetUserId();
            if (userIdString != null && int.TryParse(userIdString, out int userId))
            {
                try
                {
                    FileManager.CreateVersion(
                        fileId,
                        fileEntry.Name,
                        DateTime.UtcNow,
                        userId,
                        label,
                        comment,
                        [update]);
                }
                catch (Exception e)
                {
                    await Clients.Caller.SendAsync("Error", e.Message);
                }
            }
        }

    }

    /// <summary>
    /// Set the current file to the version given.
    /// </summary>
    public async Task SetVersion(int versionId)
    {
        if (FileDeleted(out var fileGroup))
        {
            await NotifyFileDeleted();
            return;
        }
        
        //get user name
        var userIdString = GetUserId();
        string user = "<unbekannt>";
        int? userId = null;
        if (userIdString != null && int.TryParse(userIdString, out var uid))
        {
            userId = uid;
            user = UserManager.GetPrintableUserName(uid);
        }

        //set the version
        if (Context.Items.TryGetValue("FileId", out var fileIdObj) && fileIdObj is int fileId)
        {
            var fileEntry = Database.Transaction(cp => FileTable.GetById(cp, fileId));
            if (fileEntry == null)
            {
                Console.WriteLine("File entry not found when trying to create a version.");
                return;
            }
            
            var versionEntry = Database.Transaction(cp => VersionTable.GetById(cp, versionId));
            if (versionEntry == null)
            {
                Console.WriteLine("Version entry not found when trying to create a version.");
                return;
            }
            
            var state = FileTable.GetFileState(fileId, versionId, true);
           
            if (state == null)
                return;
            
            //lock the file -> new users entering the file will not be able to access it but the lock screen will be invoked
            Locks[fileId] = true;
            //invoke the lock screen of user already registered to the file
            await Clients.OthersInGroup(fileGroup.FileId.ToString()).SendAsync("LockEditor", user);

            bool aborted = false;
            try
            {
                FileManager.RenameFile(fileEntry, versionEntry.Name);
                //set the file group to the version given and make changes persistent
                lock(fileGroup)
                {
                    fileGroup.State = state; 
                    fileGroup.Changed = versionEntry.Changed;
                    if (userId != null)
                        fileGroup.UserId = userId.Value;
                    fileGroup.Dirty = true;
                }
                fileGroup.Persist();
            }
            catch (Exception)
            {
                await Clients.Caller.SendAsync("Error", "Version kann nicht wiederhergestellt werden: eine andere Datei oder ein Ordner mit demselben Namen existiert bereits.");
                aborted = true;
            }
            
            //await Task.Delay(5000);//UNCOMMENT THIS LINE FOR TESTING PURPOSES
            
            //lift the lock
            Locks[fileId] = false;
            
            //make everyone reload the page to load the set version
            await Clients.OthersInGroup(fileGroup.FileId.ToString()).SendAsync("UnlockEditor", versionEntry.Changed, user, aborted);
            
        }
        
    }
    #endregion
    
    #region DOWNLOAD RELATED METHODS

    public Task<List<byte[]>> GetFile(int fileId)
    {
        List<byte[]>? result = null;
        
        if (Context.Items["FileId"] is int joinedFileId)
        {
            FileEntry? joinedFile = null, file = null;
            Database.Transaction(cp =>
            {
                joinedFile = FileTable.GetById(cp, joinedFileId);
                file = FileTable.GetById(cp, fileId);
            });
            if (joinedFile != null && file != null && joinedFile.ProjectId == file.ProjectId)
            {
                result = FileGroups.TryGetValue(fileId, out var fileGroup)
                    ? fileGroup.State
                    : FileTable.GetFileState(fileId);
            }
        }
        
        return Task.FromResult(result ?? throw new Exception("File state wasn't found!"));
    }
    #endregion
    
    #region SELECTION RELATED METHODS

    /// <summary>
    ///  Update the client and clock id if a selection with corresponding old client and clock id is present including all client ids +0, +1, ...., +len-1.
    /// </summary>
    public void UpdateSelection(double clientIdOld, double clockIdOld, double clientIdNew, double clockIdNew, int len, bool endOnly = false)
    {
        if (FileDeleted(out var fileGroup))
            return;
        
        lock(fileGroup)
        {
            List<SelectionEntry> selectionsToRemove = [];
            List<SelectionEntry> selectionsToAdd = [];
            foreach (var selection in fileGroup.SelectionList)
            {
                for (var i = 0; i < len; i++)
                {
                    if (!endOnly && selection.ClientStart == clientIdOld && selection.ClockStart == clockIdOld + i)
                    {
                        Database.Transaction(cp => SelectionTable.UpdateSelection(cp, selection.SelectionId, clientIdNew, clockIdNew + i, selection.ClientEnd, selection.ClockEnd));
                        selectionsToRemove.Add(selection);
                        selectionsToAdd.Add(new SelectionEntry(selection.SelectionId, selection.FileId, clientIdNew, clockIdNew + i, selection.ClientEnd, selection.ClockEnd));
                    }
                    if (selection.ClientEnd == clientIdOld && selection.ClockEnd == clockIdOld + i)
                    {
                        Database.Transaction(cp => SelectionTable.UpdateSelection(cp, selection.SelectionId, selection.ClientStart, selection.ClockStart, clientIdNew, clockIdNew + i));
                        selectionsToRemove.Add(selection);
                        selectionsToAdd.Add(new SelectionEntry(selection.SelectionId, selection.FileId, selection.ClientStart, selection.ClockStart, clientIdNew, clockIdNew + i));
                    }
                }
            }
            foreach (var selection in selectionsToRemove)
                fileGroup.SelectionList.Remove(selection);
            foreach (var selection in selectionsToAdd)
                fileGroup.SelectionList.Add(selection);
        }
    }
    
    /// <summary>
    /// Add a selection to the file group in case it exists.
    /// </summary>
    public static void AddSelectionToFileGroup(int fileId, SelectionEntry selection)
    {
        FileGroups.TryGetValue(fileId, out var fileGroup);

        fileGroup?.SelectionList.Add(selection);
    }

    /// <summary>
    /// Remove a selection from the file group in case it exists.
    /// </summary>
    public static void RemoveSelectionFromFileGroup(SelectionEntry selection)
    {
        FileGroups.TryGetValue(selection.FileId, out var fileGroup);

        fileGroup?.SelectionList.Remove(selection);
    }


    #endregion
    
    #region METHODS FOR TESTING PURPOSES
    private static readonly List<ISingleClientProxy> RegisteredClients = [];
    private static readonly int Runs = 5;
    private static readonly int NumberOfInsertsInLine = 10;
    private static bool TestLocked;
    private static int TestIssuer;
    /// <summary>
    /// Tests the correct propagation of updates between clients for
    /// simple insertions and deletions.
    /// </summary>
    public async Task SimpleInsertionAndDeletionTest()
    {
        
        
        if (RegisteredClients.Count < 1)
        {
            await PrintToBrowser("No clients registered!");
            return;
        }

        if (TestLocked)
        {
            await PrintTestLockedMessage();
            return;
        }
        TestLocked = true;
        await SetupTest();
        
        await PrintToBrowser("CHECKING SIMPLE INSERTION AND DELETION");

        for (var k = 0; k < Runs; k++)
        {
            for (var j = 0; j < NumberOfInsertsInLine; j++)
            {
                for (var i = 0; i < RegisteredClients.Count; i++)
                {
                    await RegisteredClients[i].SendAsync("InsertText", 0, "Hello " + i);
                    await RegisteredClients[i].SendAsync("DeleteText", 0, 5);
                }
            }
            foreach (var client in RegisteredClients)
            {
                await client.SendAsync("InsertText", 0, "\n");
            }
            
        }

        await PrintDoneMessage();
        TestLocked = false;
    }

    private static readonly List<List<byte[]>> StalledUpdates = [];
    private static readonly List<bool> UpdatesSent = [];
    
    /// <summary>
    /// Sends updates in different order for each client.
    /// Updates are not sent via the "normal" PushUpdate method
    /// but passed through PushMingled.
    /// </summary>
    public async Task MingledUpdatesTest()
    {
        if (RegisteredClients.Count < 1)
        {
            await PrintToBrowser("No clients registered!");
            return;
        }

        if (TestLocked)
        {
            await PrintTestLockedMessage();
            return;
        }
            
        TestLocked = true;
        await SetupTest();

        //remove data from previous test
        StalledUpdates.Clear();
        UpdatesSent.Clear();
        foreach (var unused in RegisteredClients)
        {
            StalledUpdates.Add([]);
            UpdatesSent.Add(false);
        }
        
        foreach (var client in RegisteredClients)
        {
            await client.SendAsync("SetMingledUpdates", true);
        }
        
        
        for (int k = 0; k < Runs; k++)
        {
            for (var j = 0; j < NumberOfInsertsInLine; j++)
            {
                for (int i = 0; i < RegisteredClients.Count; i++)
                {
                    await RegisteredClients[i].SendAsync("InsertText", 0, "Hello " + i + k);
                    await RegisteredClients[i].SendAsync("DeleteText", 0, 5);
                }
            }

            foreach (var client in RegisteredClients)
            {
                await client.SendAsync("InsertText", 0, "\n");
            }
        }
        
        
        foreach (var client in RegisteredClients)
        {
            await client.SendAsync("SetMingledUpdates", false);
        }

    }
    public async Task PushMingled(byte[] update, int id)
    {
        if (FileDeleted(out var fileGroup))
        {
            await NotifyFileDeleted();
            return;
        }

        lock (fileGroup)
        {
            fileGroup.State.Add(update);
            fileGroup.Changed = DateTime.UtcNow;
            fileGroup.UserId = int.Parse(GetUserId()??"");
            fileGroup.Dirty = true;
        }

        StalledUpdates[id].Add(update);

        if (StalledUpdates[id].Count == Runs*(NumberOfInsertsInLine*2 + 1) && !UpdatesSent[id])

        {
            Console.WriteLine("Sending updates for client " + id);

            //Iterate over the updates of the client and send them with an offset:
            //client 0 starts at 0, client 1 starts at 1...
            //Using (baseIndex + clientIndex) % numberOfUpdates
            for (int i = 0; i < StalledUpdates[id].Count; i++)
                for (int j = 0; j < RegisteredClients.Count; j++)
                {
                    await RegisteredClients[j].SendAsync("ApplyUpdate", StalledUpdates[id][(i+j)%StalledUpdates[id].Count]);
                }
           
            UpdatesSent[id] = true;
        }

        if (UpdatesSent.Any(sent => !sent))
        {
            return;
        }
        await PrintDoneMessage();
        TestLocked = false;
    }

    private async Task SetupTest()
    {
        //Pushing state prevents the update array from becoming 
        //too big in case of many tests in quick succession.
        await Clients.Caller.SendAsync("PushState");
        //Wait a second for state to be pushed
        Thread.Sleep(1000);
        foreach (var client in RegisteredClients)
        {
            await client.SendAsync("SetPrintStatusInformation", false);
            await client.SendAsync("ResetEditor");
        }

        await Clients.Caller.SendAsync("SetIssuerId");
    }

    /// <summary>
    /// Sets the id for the client that issued the test.
    /// Used to print information to the appropriate client.
    /// </summary>
    /// <param name="id"></param>
    public Task SetIssuerId(int id)
    {
        TestIssuer = id;
        return Task.CompletedTask;
    }
    public async Task NumberOfRegisteredClients()
    {
        await PrintToBrowser(RegisteredClients.Count.ToString());
    }
    public async Task RegisterForTest()
    {
        lock (RegisteredClients)
        {
            RegisteredClients.Add(Clients.Caller);
            Clients.Caller.SendAsync("SetTestId", RegisteredClients.Count - 1);
        }

        await PrintToBrowser("Client Registered for test\nRegistered clients:" + RegisteredClients.Count);
    }
    public async Task ClearRegisteredClients()
    {
        RegisteredClients.Clear();
        await PrintToBrowser("All clients removed");
    }

    private async Task PrintToBrowser(string text)
    {
        await Clients.Caller.SendAsync("Console.log", text);
        
    }

    public async Task Options()
    {
        string pre = "connection.invoke(\"";
        string post = ")";
        string infix = "\"";
        await PrintToBrowser( "printStatusInformation = false" + "\n" +
                        pre + "RegisterForTest" + infix + post + "\n" + 
                        pre + "NumberOfRegisteredClients" + infix + post + "\n" +
                        pre + "ClearRegisteredClients" + infix + post + "\n" + 
                        pre + "SimpleInsertionAndDeletionTest" + infix + post + "\n" +
                        pre + "MingledUpdatesTest" + infix + post + "\n" 
                       );
    }

    private async Task PrintToIssuer(string text)
    {
        await RegisteredClients[TestIssuer].SendAsync("Console.log", text);
    }
    private async Task PrintDoneMessage()
    {
        
        await PrintToIssuer( "Sent out all messages. please wait for changes to synchronize in all editors.\n" +
                                  "When everything is synced check the texts for differences");
    }
    private async Task PrintTestLockedMessage()
    {
        await PrintToIssuer("Another user is running a test. Please wait for them to finish.");
    }
    #endregion

}
