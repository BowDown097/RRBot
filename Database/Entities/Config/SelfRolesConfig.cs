namespace RRBot.Database.Entities.Config;

public class SelfRolesConfig
{
    public ulong Channel { get; set; }
    public ulong Message { get; set; }
    public Dictionary<string, ulong> SelfRoles { get; set; } = new();
}