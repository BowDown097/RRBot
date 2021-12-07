namespace RRBot.Entities.Database;
public abstract class DbObject
{
    [FirestoreDocumentId]
    public abstract DocumentReference Reference { get; set; }
}