using System.Collections.Concurrent;
using cocoding.Data;

namespace cocoding;

public class FileGroupData(FileEntry file)
{
    public readonly int FileId = file.Id;
    
    public List<byte[]> State = FileTable.GetFileState(file.Id) ?? throw new Exception("File state wasn't found!");

    public DateTime Changed = file.Changed;

    public int UserId = file.UserId;

    public bool Dirty = false;
    
    public readonly ConcurrentDictionary<string, FileGroupSession> Sessions = [];

    public readonly List<SelectionEntry> SelectionList = Database.Transaction(cp => SelectionTable.ListSelectionsByFile(cp, file.Id));

    public void Persist()
    {
        if (Dirty)
            lock (this)
            {
                FileTable.SetFileState(FileId, State);
                
                Database.Transaction(cp =>
                    FileTable.SetChangeData(cp, FileId, Changed, UserId));
                
                Dirty = false;
            }
    }
}
