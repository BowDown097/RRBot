namespace RRBot.Entities;
public struct Achievement
{
    public string Name { get; }
    public string Description { get; }
    public int Reward { get; }

    public Achievement(string name, string description, int reward = 0)
    {
        Name = name;
        Description = description;
        Reward = reward;
    }
}