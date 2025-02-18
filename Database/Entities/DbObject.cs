namespace RRBot.Database.Entities;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class BsonCollectionAttribute(string collectionName) : Attribute
{
    public string CollectionName => collectionName;
}

public abstract class DbObject
{
    [BsonId]
    public abstract ObjectId Id { get; set; }
}