using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RRBot.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace RRBot.Modules
{
    [Summary("Commands that don't do anything related to the bot's systems: they just exist for fun (hence the name).")]
    public class Fun : ModuleBase<SocketCommandContext>
    {
        private async Task<RuntimeResult> RandomImg(string apiUrl, string key, bool cat = false)
        {
            using WebClient client = new();
            string response = await client.DownloadStringTaskAsync(apiUrl);
            string image = "";
            if (cat)
            {
                JArray arr = JArray.Parse(response);
                image = arr[0][key].Value<string>();
            }
            else
            {
                JObject json = JObject.Parse(response);
                image = json[key].Value<string>();
            }

            if (string.IsNullOrWhiteSpace(image))
                return CommandResult.FromError("Couldn't find the picture you wanted :(");

            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Found one!")
                .WithImageUrl(image);
            await ReplyAsync(embed: embed.Build());
            return CommandResult.FromSuccess();
        }

        [Alias("gato", "kitty")]
        [Command("cat")]
        [Summary("Random cat picture!")]
        [Remarks("$cat")]
        public async Task<RuntimeResult> Cat() => await RandomImg("https://api.thecatapi.com/v1/images/search", "url", true);

        [Alias("definition")]
        [Command("define")]
        [Summary("Define a term.")]
        [Remarks("$define [term]")]
        public async Task<RuntimeResult> Define([Remainder] string term)
        {
            if (term.Equals("nigger", StringComparison.OrdinalIgnoreCase))
                return CommandResult.FromError("Nope.");

            using WebClient client = new();
            string response = await client.DownloadStringTaskAsync($"https://api.pearson.com/v2/dictionaries/ldoce5/entries?headword={term}");
            DefinitionResponse def = JsonConvert.DeserializeObject<DefinitionResponse>(response);
            if (def.Count == 0)
                return CommandResult.FromError("Couldn't find anything for that term, chief.");

            StringBuilder description = new();
            DefinitionResult[] filteredResults = def.Results.Where(res => res.Headword.Equals(term, StringComparison.OrdinalIgnoreCase)
                && res.Senses != null).ToArray();
            for (int i = 1; i <= filteredResults.Length; i++)
            {
                DefinitionResult result = filteredResults[i - 1];
                description.AppendLine($"**{i}:**\n*{result.PartOfSpeech}*");
                foreach (Sense sense in result.Senses)
                {
                    description.AppendLine($"Definition: {sense.Definition[0]}");
                    if (sense.Examples != null)
                        description.AppendLine($"Example: {sense.Examples[0].Text}");
                }
            }

            if (description.Length == 0)
                return CommandResult.FromError("Couldn't find anything for that term, chief.");

            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(term.ToLower()))
                .WithDescription(description.ToString());
            await ReplyAsync(embed: embed.Build());
            return CommandResult.FromSuccess();
        }

        [Alias("doggo", "heckingchonker")]
        [Command("dog")]
        [Summary("Random dog picture!")]
        [Remarks("$dog")]
        public async Task<RuntimeResult> Dog() => await RandomImg("https://dog.ceo/api/breeds/image/random", "message");

        [Command("flip")]
        [Summary("Flip a coin.")]
        [Remarks("$flip")]
        public async Task Flip()
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Coin");

            if (RandomUtil.Next(0, 2) != 0)
            {
                embed.WithDescription("You flipped.. HEADS!");
                embed.WithImageUrl("https://images.squarespace-cdn.com/content/v1/5786a922cd0f688d44f9cab2/1482515593363-33KPMNHCMDW7G0T12VK9/image-asset.png");
            }
            else
            {
                embed.WithDescription("You flipped.. TAILS!");
                embed.WithImageUrl("https://i.imgur.com/LxajBRS.png");
            }

            await ReplyAsync(embed: embed.Build());
        }

        [Command("gay")]
        [Summary("See how gay a user is.")]
        [Remarks("$gay [user]")]
        public async Task Gay(IGuildUser user)
        {
            int gay = !user.IsBot ? RandomUtil.Next(1, 101) : 0;

            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription($"{user} is {gay}% gay.");

            if (gay <= 10)
                embed.WithTitle("Not Gay");
            else if (gay > 10 && gay < 50)
                embed.WithTitle("Kinda Gay");
            else if (gay >= 50 && gay < 90)
                embed.WithTitle("Gay");
            else
                embed.WithTitle("Hella Gay!");

            await ReplyAsync(embed: embed.Build());
        }

        [Command("penis")]
        [Summary("See how big a user's penis is.")]
        [Remarks("$penis [user]")]
        public async Task Penis(IGuildUser user)
        {
            int equals = !user.IsBot ? RandomUtil.Next(1, 16) : 20;
            string penis = "8" + new string('=', equals) + "D";

            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription($"{user}'s penis: {penis}");

            if (equals <= 3)
                embed.WithTitle("Micropenis LMFAO");
            else if (equals > 3 && equals < 7)
                embed.WithTitle("Ehhh");
            else if (equals >= 7 && equals < 12)
                embed.WithTitle("Not bad at all!");
            else
                embed.WithTitle("God damn, he's packin'!");

            await ReplyAsync(embed: embed.Build());
        }

        [Command("sneed")]
        [Summary("Sneed")]
        [Remarks("$sneed")]
        public async Task Sneed() => await ReplyAsync("https://static.wikia.nocookie.net/simpsons/images/1/14/Al_Sneed.png/revision/latest?cb=20210430000431");

        [Command("trivia")]
        [Summary("Generate a random trivia question.")]
        [Remarks("$trivia")]
        public async Task Trivia()
        {
            // get all the stuff we need
            using WebClient client = new();
            string response = await client.DownloadStringTaskAsync("https://opentdb.com/api.php?amount=1");
            TriviaQuestion trivia = JsonConvert.DeserializeObject<Trivia>(response).Results[0];
            string question = HttpUtility.HtmlDecode(trivia.Question);
            string correctAnswer = HttpUtility.HtmlDecode(trivia.CorrectAnswer);
            IEnumerable<string> incorrectAnswers = trivia.IncorrectAnswers.Select(a => HttpUtility.HtmlDecode(a));

            // set up and randomize answers array
            List<string> answers = new(incorrectAnswers.Append(correctAnswer));
            for (int i = 0; i < answers.Count - 1; i++)
            {
                int j = RandomUtil.Next(i, answers.Count);
                string temp = answers[i];
                answers[i] = answers[j];
                answers[j] = temp;
            }

            ComponentBuilder components = new();
            StringBuilder description = new($"{question}\n\nPress the button with the respective number to submit your answer.\n");
            for (int i = 1; i <= answers.Count; i++)
            {
                string answer = answers[i - 1];
                description.AppendLine($"{i}: {answer}");
                components.WithButton(i.ToString(), $"trivia-{Context.User.Id}-{i}-{answer == correctAnswer}");
            }

            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Trivia!")
                .WithDescription(description.ToString());
            await ReplyAsync(embed: embed.Build(), component: components.Build());
        }

        [Command("verse")]
        [Summary("Random bible verse!")]
        [Remarks("$verse")]
        public async Task Verse()
        {
            using WebClient client = new();
            string response = await client.DownloadStringTaskAsync("https://labs.bible.org/api/?passage=random&type=json");
            dynamic verse = JArray.Parse(response)[0];

            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle($"{verse.bookname} {verse.chapter}:{verse.verse}")
                .WithDescription(verse.text);
            await ReplyAsync(embed: embed.Build());
        }

        [Command("waifu")]
        [Summary("Get yourself a random waifu from our vast and sexy collection of scrumptious waifus.")]
        [Remarks("$waifu")]
        public async Task Waifu()
        {
            List<string> keys = Constants.WAIFUS.Keys.ToList();
            string waifu = keys[RandomUtil.Next(Constants.WAIFUS.Count)];

            EmbedBuilder waifuEmbed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("Say hello to your new waifu!")
                .WithDescription($"Your waifu is **{waifu}**.")
                .WithImageUrl(Constants.WAIFUS[waifu]);
            await ReplyAsync(embed: waifuEmbed.Build());
        }
    }
}
