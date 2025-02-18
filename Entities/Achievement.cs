namespace RRBot.Entities;
public class Achievement(string name, string description, int reward = 0)
{
    public string Name => name;
    public string Description => description;
    public int Reward => reward;
}