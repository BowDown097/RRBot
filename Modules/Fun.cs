namespace RRBot.Modules;
[Summary("Commands that don't do anything related to the bot's systems: they just exist for fun (hence the name).")]
public class Fun : ModuleBase<SocketCommandContext>
{
    private static readonly BoardPos[] Adjacents = { (-1, -1), (-1, 0), (-1, 1), (0, -1), (0, 1), (1, -1), (1, 0), (1, 1) };

    #region Commands
    [Alias("gato", "kitty")]
    [Command("cat")]
    [Summary("Random cat picture!")]
    public async Task Cat()
    {
        using HttpClient client = new();
        string response = await client.GetStringAsync("https://api.thecatapi.com/v1/images/search");
        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Found one!")
            .WithImageUrl(JArray.Parse(response)[0]["url"]?.Value<string>());
        await ReplyAsync(embed: embed.Build());
    }

    [Alias("definition")]
    [Command("define")]
    [Summary("Define a term.")]
    [Remarks("$define penis")]
    public async Task<RuntimeResult> Define([Remainder] string term)
    {
        if (await FilterSystem.ContainsFilteredWord(Context.Guild, term))
            return CommandResult.FromError("Nope.");

        using HttpClient client = new();
        string response = await client.GetStringAsync($"https://api.pearson.com/v2/dictionaries/ldoce5/entries?headword={term}");
        DefinitionResponse def = JsonConvert.DeserializeObject<DefinitionResponse>(response);
        if (def is null || def.Count == 0)
            return CommandResult.FromError("Couldn't find anything for that term, chief.");

        StringBuilder description = new();
        Definition[] filtered = def.Results.Where(res => res.Headword.Equals(term, StringComparison.OrdinalIgnoreCase)
            && res.Senses != null).ToArray();
        for (int i = 1; i <= filtered.Length; i++)
        {
            Definition definition = filtered[i - 1];
            description.AppendLine($"**{i}:**\n*{definition.PartOfSpeech}*");
            foreach (Sense sense in definition.Senses)
            {
                if (await FilterSystem.ContainsFilteredWord(Context.Guild, sense.Definition[0]))
                    return CommandResult.FromError("Nope.");
                description.AppendLine($"Definition: {sense.Definition[0]}");
                if (sense.Examples != null)
                    description.AppendLine($"Example: {sense.Examples[0].Text}");
            }
        }

        if (description.Length == 0)
            return CommandResult.FromError("Couldn't find anything for that term, chief.");

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle(term.ToTitleCase())
            .WithDescription(description.ToString());
        await ReplyAsync(embed: embed.Build());
        return CommandResult.FromSuccess();
    }

    [Alias("doggo", "heckingchonker")]
    [Command("dog")]
    [Summary("Random dog picture!")]
    public async Task Dog()
    {
        using HttpClient client = new();
        string response = await client.GetStringAsync("https://dog.ceo/api/breeds/image/random");
        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Found one!")
            .WithImageUrl(JObject.Parse(response)["message"]?.Value<string>());
        await ReplyAsync(embed: embed.Build());
    }

    [Command("flip")]
    [Summary("Flip a coin.")]
    public async Task Flip()
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Coin");

        if (RandomUtil.Next(0, 2) != 0)
        {
            embed.WithDescription("You flipped.. HEADS!");
            embed.WithImageUrl("https://i.imgur.com/Y77AMLp.png");
        }
        else
        {
            embed.WithDescription("You flipped.. TAILS!");
            embed.WithImageUrl("https://i.imgur.com/O3ULvhg.png");
        }

        await ReplyAsync(embed: embed.Build());
    }

    [Command("gay")]
    [Summary("See how gay you or another user is.")]
    [Remarks("$gay luner")]
    public async Task Gay([Remainder] IGuildUser user = null)
    {
        user ??= Context.User as IGuildUser;
        int gay = !user.IsBot ? RandomUtil.Next(1, 101) : 0;
        string title = gay switch
        {
            <= 10 => "Not Gay",
            > 10 and < 50 => "Kinda Gay",
            >= 50 and < 90 => "Gay",
            _ => "Hella Gay!"
        };

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle(title)
            .WithDescription(user.Id == Context.User.Id
                ? $"You are {gay}% gay."
                : $"{user.Sanitize()} is {gay}% gay.");
        await ReplyAsync(embed: embed.Build());
    }

    #pragma warning disable IDE0060, RCS1163
    [Alias("conch")]
    [Command("magicconch")]
    [Summary("Ask the Magic Conch ANYTHING!")]
    [Remarks("$magicconch Will I get bitches?")]
    public async Task MagicConch([Remainder] string question) // not discarded for $help
    {
        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("The Magic Conch Shell says...")
            .WithImageUrl(Constants.MagicConchImages[RandomUtil.Next(Constants.MagicConchImages.Length)]);
        await ReplyAsync(embed: embed.Build());
    }
    #pragma warning restore IDE0060, RCS1163

    [Command("minesweeper")]
    [Summary("Play a game of Minesweeper. Choose between difficulty 1-3.")]
    [Remarks("$minesweeper 2")]
    public async Task<RuntimeResult> Minesweeper(int difficulty = 1)
    {
        if (difficulty is < 1 or > 3)
            return CommandResult.FromError($"**{difficulty}** is not a valid difficulty!");

        int[,] board = GenerateBoard(difficulty);
        StringBuilder boardBuilder = new();
        for (int x = 0; x < board.GetLength(0); x++)
        {
            for (int y = 0; y < board.GetLength(1); y++)
            {
                string tile = board[x, y] == -1 ? "💥" : Constants.PollEmotes[board[x, y]];
                boardBuilder.Append($"||{tile}||");
            }

            boardBuilder.Append('\n');
        }

        await ReplyAsync(boardBuilder.ToString());
        return CommandResult.FromSuccess();
    }

    [Command("penis")]
    [Summary("See how big a user's penis is, or your own.")]
    [Remarks("$penis Arctic Hawk")]
    public async Task Penis([Remainder] IGuildUser user = null)
    {
        user ??= Context.User as IGuildUser;
        int equals = !user.IsBot ? RandomUtil.Next(1, 16) : 20;
        string title = equals switch
        {
            <= 3 => "Micropenis LMFAO",
            > 3 and < 7 => "Ehhh",
            >= 7 and < 12 => "Not bad at all!",
            _ => "God damn, he's packin'!"
        };

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle(title)
            .WithDescription(user.Id == Context.User.Id
                ? $"Your penis: {"8" + new string('=', equals) + "D"}"
                : $"{user.Sanitize()}'s penis: {"8" + new string('=', equals) + "D"}");
        await ReplyAsync(embed: embed.Build());
    }

    [Command("prefertranslation")]
    [Summary("Set a preferred translation of The Holy Bible.")]
    public async Task<RuntimeResult> PreferTranslation(string translation)
    {
        string tLower = translation.ToLower();
        if (tLower is not ("cherokee" or "bbe" or "kjv" or "web" or "oeb-us" or "clementine" or "almeida" or "rccv"))
        {
            return CommandResult.FromError("That translation is not supported: Supported translations are " +
                                           "Cherokee, BBE, KJV, WEB, OEB-US, Clementine, Almeida, and RCCV.");
        }

        DbUser user = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        user.PreferredBibleTranslation = tLower;
        await MongoManager.UpdateObjectAsync(user);

        await Context.User.NotifyAsync(Context.Channel, "Successfully updated your preferred Bible translation.");
        return CommandResult.FromSuccess();
    }

    [Command("sneed")]
    [Summary("Sneed")]
    public async Task Sneed() => await ReplyAsync("https://static.wikia.nocookie.net/simpsons/images/1/14/Al_Sneed.png/revision/latest?cb=20210430000431");

    [Command("trivia")]
    [Summary("Generate a random trivia question.")]
    public async Task Trivia()
    {
        // get all the stuff we need
        using HttpClient client = new();
        string response = await client.GetStringAsync("https://opentdb.com/api.php?amount=1");
        TriviaQuestion trivia = JsonConvert.DeserializeObject<Trivia>(response).Results[0];
        trivia.DecodeMembers();
        string[] answers = trivia.IncorrectAnswers.Append(trivia.CorrectAnswer).ToArray();

        // set up and randomize answers array
        for (int i = 0; i < answers.Length - 1; i++)
        {
            int j = RandomUtil.Next(i, answers.Length);
            (answers[i], answers[j]) = (answers[j], answers[i]);
        }

        ComponentBuilder components = new();
        StringBuilder description = new($"{trivia.Question}\n\nPress the button with the respective number to submit your answer.\n");
        for (int i = 1; i <= answers.Length; i++)
        {
            string answer = answers[i - 1];
            description.AppendLine($"{i}: {answer}");
            components.WithButton(i.ToString(), $"trivia-{i}-{answer == trivia.CorrectAnswer}");
        }

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Trivia!")
            .WithDescription(description.ToString());
        await ReplyAsync(embed: embed.Build(), components: components.Build());
    }
    
    [Alias("bible")]
    [Command("verse")]
    [Summary("Get a verse or a range of verses from The Holy Bible.")]
    public async Task<RuntimeResult> Verse([Remainder] string verse)
    {
        DbUser user = await MongoManager.FetchUserAsync(Context.User.Id, Context.Guild.Id);
        string translation = !string.IsNullOrEmpty(user.PreferredBibleTranslation)
            ? user.PreferredBibleTranslation : "kjv"; // KJV best translation don't @ me

        using HttpClient client = new();
        string response = await client.GetStringAsync($"https://bible-api.com/{verse}?translation={translation}");
        if (response.Contains("not found"))
            return CommandResult.FromError("Invalid verse input! Here's an example to help you out: ``John 3:16-19``");

        JObject responseObj = JObject.Parse(response);
        string reference = responseObj["reference"].ToString();
        string translationName = responseObj["translation_name"].ToString();
        JArray verses = responseObj["verses"] as JArray;

        StringBuilder description = new();
        foreach (JToken verseObj in verses)
        {
            string text = verseObj["text"].ToString().Trim().ReplaceLineEndings("") + " ";
            int verseNum = verseObj["verse"]?.Value<int>() ?? 0;
            if (verses.Count > 1)
                text = $"**[{verseNum}]** " + text;
            description.Append(text);
        }

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Gold)
            .WithTitle(reference)
            .WithDescription(description.ToString())
            .WithFooter($"Translation: {translationName}");
        await ReplyAsync(embed: embed.Build());

        return CommandResult.FromSuccess();
    }

    [Command("waifu")]
    [Summary("Get yourself a random waifu from our vast and sexy collection of scrumptious waifus.")]
    public async Task Waifu()
    {
        string waifu = Constants.Waifus.Keys.ElementAt(RandomUtil.Next(Constants.Waifus.Count));
        EmbedBuilder waifuEmbed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Say hello to your new waifu!")
            .WithDescription($"Your waifu is **{waifu}**.")
            .WithImageUrl(Constants.Waifus[waifu]);
        await ReplyAsync(embed: waifuEmbed.Build());
    }
    #endregion

    #region Helpers
    private static int[,] GenerateBoard(int difficulty)
    {
        double density = difficulty switch
        {
            1 => 0.146,
            2 => 0.201,
            3 => 0.246,
            _ => 0.201
        };

        int totalMines = (int)Math.Floor(64.0 * density);
        int[,] board = new int[8, 8];
        List<BoardPos> mines = new(totalMines);
        while (mines.Count < totalMines)
        {
            BoardPos pos = (RandomUtil.Next(8), RandomUtil.Next(8));
            if (pos == BoardPos.Origin || board[pos.X, pos.Y] == -1) continue;
            board[pos.X, pos.Y] = -1;
            mines.Add(pos);
        }

        foreach (BoardPos mine in mines)
        {
            foreach (BoardPos adjacent in Adjacents.Select(adj => mine + adj))
            {
                if (adjacent.X is < 0 or >= 8 || adjacent.Y is < 0 or >= 8 || board[adjacent.X, adjacent.Y] == -1)
                    continue;
                board[adjacent.X, adjacent.Y]++;
            }
        }

        return board;
    }
    #endregion
}