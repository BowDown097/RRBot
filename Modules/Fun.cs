namespace RRBot.Modules;
[Summary("Commands that don't do anything related to the bot's systems: they just exist for fun (hence the name).")]
public class Fun : ModuleBase<SocketCommandContext>
{
    private static readonly BoardPos[] adjacents = { (-1, -1), (-1, 0), (-1, 1), (0, -1), (0, 1), (1, -1), (1, 0), (1, 1) };

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
            .WithImageUrl(JArray.Parse(response)[0]["url"].Value<string>());
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
        if (def.Count == 0)
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
            .WithImageUrl(JObject.Parse(response)["message"].Value<string>());
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
    [Summary("See how gay a user is.")]
    [Remarks("$gay luner")]
    public async Task Gay([Remainder] IGuildUser user)
    {
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
            .WithDescription($"{user.Sanitize()} is {gay}% gay.");
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
            .WithImageUrl(Constants.MAGIC_CONCH_IMAGES[RandomUtil.Next(Constants.MAGIC_CONCH_IMAGES.Length)]);
        await ReplyAsync(embed: embed.Build());
    }
    #pragma warning restore IDE0060, RCS1163

    [Command("minesweeper")]
    [Summary("Play a game of Minesweeper. Choose between difficulty 1-3.")]
    [Remarks("$minesweeper 2")]
    public async Task<RuntimeResult> Minesweeper(int difficulty = 1)
    {
        if (difficulty < 1 || difficulty > 3)
            return CommandResult.FromError($"**{difficulty}** is not a valid difficulty!");

        int[,] board = GenerateBoard(difficulty);
        StringBuilder boardBuilder = new();
        for (int x = 0; x < board.GetLength(0); x++)
        {
            for (int y = 0; y < board.GetLength(1); y++)
            {
                string tile = board[x, y] == -1 ? "💥" : Constants.POLL_EMOTES[board[x, y]];
                boardBuilder.Append($"||{tile}||");
            }

            boardBuilder.Append('\n');
        }

        await ReplyAsync(boardBuilder.ToString());
        return CommandResult.FromSuccess();
    }

    [Command("penis")]
    [Summary("See how big a user's penis is.")]
    [Remarks("$penis Arctic Hawk")]
    public async Task Penis([Remainder] IGuildUser user)
    {
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
            .WithDescription($"{user.Sanitize()}'s penis: {"8" + new string('=', equals) + "D"}");
        await ReplyAsync(embed: embed.Build());
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

    [Command("verse")]
    [Summary("Random bible verse!")]
    public async Task Verse()
    {
        using HttpClient client = new();
        string response = await client.GetStringAsync("https://labs.bible.org/api/?passage=random&type=json");
        dynamic verse = JArray.Parse(response)[0];

        EmbedBuilder embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle($"{verse.bookname} {verse.chapter}:{verse.verse}")
            .WithDescription(verse.text);
        await ReplyAsync(embed: embed.Build());
    }

    [Command("waifu")]
    [Summary("Get yourself a random waifu from our vast and sexy collection of scrumptious waifus.")]
    public async Task Waifu()
    {
        string waifu = Constants.WAIFUS.Keys.ElementAt(RandomUtil.Next(Constants.WAIFUS.Count));
        EmbedBuilder waifuEmbed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Say hello to your new waifu!")
            .WithDescription($"Your waifu is **{waifu}**.")
            .WithImageUrl(Constants.WAIFUS[waifu]);
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
            if (pos != BoardPos.ORIGIN && board[pos.x, pos.y] != -1)
            {
                board[pos.x, pos.y] = -1;
                mines.Add(pos);
            }
        }

        foreach (BoardPos mine in mines)
        {
            foreach (BoardPos adjacent in adjacents.Select(adj => mine + adj))
            {
                if (adjacent.x < 0 || adjacent.x >= 8 || adjacent.y < 0 || adjacent.y >= 8 || board[adjacent.x, adjacent.y] == -1)
                    continue;
                board[adjacent.x, adjacent.y]++;
            }
        }

        return board;
    }
    #endregion
}