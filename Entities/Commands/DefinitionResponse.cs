namespace RRBot.Entities.Commands
{
    public class DefinitionResponse
    {
        [JsonProperty("count")]
        public int Count { get; set; }
        [JsonProperty("results")]
        public Definition[] Results { get; set; }
    }

    public class Definition
    {
        [JsonProperty("headword")]
        public string Headword { get; set; }
        [JsonProperty("part_of_speech")]
        public string PartOfSpeech { get; set; }
        [JsonProperty("senses")]
        public Sense[] Senses { get; set; }
    }

    public class Sense
    {
        [JsonProperty("definition")]
        public string[] Definition { get; set; }
        [JsonProperty("examples")]
        public Example[] Examples { get; set; }
    }

    public class Example
    {
        [JsonProperty("text")]
        public string Text { get; set; }
    }
}