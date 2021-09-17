#pragma warning disable IDE1006
namespace RRBot.Entities
{
    public class Trivia
    {
        public TriviaQuestion[] results { get; set; }
    }

    public class TriviaQuestion
    {
        public string question { get; set; }
        public string correct_answer { get; set; }
        public string[] incorrect_answers { get; set; }
    }
}