namespace RRBot.Entities.Commands
{
    public class Trivia
    {
        [JsonProperty("results")]
        public TriviaQuestion[] Results { get; set; }
    }

    public class TriviaQuestion
    {
        [JsonProperty("question")]
        public string Question { get; set; }
        [JsonProperty("correct_answer")]
        public string CorrectAnswer { get; set; }
        [JsonProperty("incorrect_answers")]
        public string[] IncorrectAnswers { get; set; }

        public void DecodeMembers()
        {
            Question = HttpUtility.HtmlDecode(Question);
            CorrectAnswer = HttpUtility.HtmlDecode(CorrectAnswer);
            IncorrectAnswers = IncorrectAnswers.Select(a => HttpUtility.HtmlDecode(a)).ToArray();
        }
    }
}