namespace RRBot.Database.Entities.Config;

public abstract class DbConfig : DbObject
{
    public abstract ulong GuildId { get; init; }
}