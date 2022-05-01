namespace RRBot.Entities.Commands;
public struct Achievement
{
    public string name;
    public string description;
    public int reward;

    public Achievement(string name, string description, int reward = 0)
    {
        this.name = name;
        this.description = description;
        this.reward = reward;
    }
}