namespace RRBot.Entities;
public readonly struct Achievement(string name, string description, int reward = 0)
{
    public string Name { get; } = name;
    public string Description { get; } = description;
    public int Reward { get; } = reward;
}