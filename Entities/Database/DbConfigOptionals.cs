namespace RRBot.Entities.Database;
[FirestoreData]
public class DbConfigOptionals : DbObject
{
    #region Variables
    [FirestoreDocumentId]
    public override DocumentReference Reference { get; set; }
    [FirestoreProperty]
    public List<string> DisabledCommands { get; set; } = new();
    [FirestoreProperty]
    public List<string> DisabledModules { get; set; } = new();
    [FirestoreProperty]
    public List<string> FilterRegexes { get; set; } = new();
    [FirestoreProperty]
    public List<string> FilteredWords { get; set; } = new();
    [FirestoreProperty]
    public bool InviteFilterEnabled { get; set; }
    [FirestoreProperty]
    public List<ulong> NoFilterChannels { get; set; } = new();
    [FirestoreProperty]
    public bool NSFWEnabled { get; set; }
    [FirestoreProperty]
    public bool ScamFilterEnabled { get; set; }
    #endregion

    #region Methods
    public static async Task<DbConfigOptionals> GetById(ulong guildId)
    {
        if (MemoryCache.Default.Contains($"optionalconf-{guildId}"))
            return (DbConfigOptionals)MemoryCache.Default.Get($"optionalconf-{guildId}");

        DocumentReference doc = Program.database.Collection($"servers/{guildId}/config").Document("optionals");
        DocumentSnapshot snap = await doc.GetSnapshotAsync();
        if (!snap.Exists)
        {
            await doc.CreateAsync(new { NSFWEnabled = false });
            return await GetById(guildId);
        }

        DbConfigOptionals config = snap.ConvertTo<DbConfigOptionals>();
        MemoryCache.Default.CacheDatabaseObject($"optionalconf-{guildId}", config);
        return config;
    }
    #endregion
}