#pragma warning disable IDE1006
namespace RRBot.Entities
{
    public class DefinitionResponse
    {
        public int count { get; set; }
        public DefinitionResult[] results { get; set; }
    }

    public class DefinitionResult
    {
        public string headword { get; set; }
        public string part_of_speech { get; set; }
        public Sense[] senses { get; set; }
    }

    public class Sense
    {
        public string[] definition { get; set; }
        public Example[] examples { get; set; }
    }

    public class Example
    {
        public string text { get; set; }
    }
}