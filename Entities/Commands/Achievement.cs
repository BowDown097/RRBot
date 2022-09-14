namespace RRBot.Entities.Commands;
public struct Achievement
{
    public string Name;
    public string Description;
    public int Reward;

    public Achievement(string name, string description, int reward = 0)
    {
        this.Name = name;
        this.Description = description;
        this.Reward = reward;
    }
}