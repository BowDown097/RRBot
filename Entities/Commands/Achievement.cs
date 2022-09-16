namespace RRBot.Entities.Commands;
public struct Achievement
{
    public readonly string Name;
    public readonly string Description;
    public readonly int Reward;

    public Achievement(string name, string description, int reward = 0)
    {
        Name = name;
        Description = description;
        Reward = reward;
    }
}