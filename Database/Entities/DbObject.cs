namespace RRBot.Database.Entities;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class BsonCollectionAttribute : Attribute
{
    public string CollectionName { get; }
    public BsonCollectionAttribute(string collectionName) => CollectionName = collectionName;
}

public abstract class DbObject
{
    [BsonId]
    public abstract ObjectId Id { get; set; }
}