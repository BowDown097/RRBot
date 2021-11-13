namespace RRBot.Modules
{
    [Summary("Degenerates and retards with no significant other (and probably no friends either) unite!")]
    [RequireNsfwEnabled]
    [RequireNsfw]
    public class NSFW : ModuleBase<SocketCommandContext>
    {
        [Command("neko")]
        [Summary("Some good ol' neko hentai (sometimes just saucy lewds too).")]
        [Remarks("$neko")]
        public async Task Neko()
        {
            using HttpClient client = new();
            string apiUrl = RandomUtil.Next(2) == 0
                ? "https://nekos.life/api/v2/img/nsfw_neko_gif"
                : "https://nekos.life/api/v2/img/lewd";
            string response = await client.GetStringAsync(apiUrl);
            string imgUrl = JObject.Parse(response)["url"].Value<string>();

            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("One neko coming right up!")
                .WithImageUrl(imgUrl);
            await ReplyAsync(embed: embed.Build());
        }

        [Command("nhentai")]
        [Summary("Search for a doujinshi/manga from NHentai, or go for a completely random one! If you provide multiple keywords for a search, separate them with a comma with no spaces.")]
        [Remarks("$nhentai")]
        public async Task<RuntimeResult> NHentai([Remainder] string keyword = "")
        {
            GalleryElement gallery;
            if (string.IsNullOrWhiteSpace(keyword))
            {
                NHentaiSharp.Search.SearchResult result = await NHentaiSharp.Core.SearchClient.SearchAsync();
                gallery = result.elements[RandomUtil.Next(0, result.elements.Length)];
            }
            else
            {
                string[] keywords = keyword.Contains(',') ? keyword.Split(',') : new string[] { keyword };
                try
                {
                    // the search code kinda garbage but according to the README.md of the NHentaiSharp project you have to do this i guess
                    NHentaiSharp.Search.SearchResult result = await NHentaiSharp.Core.SearchClient.SearchWithTagsAsync(keywords);
                    int page = RandomUtil.Next(0, result.numPages) + 1;
                    result = await NHentaiSharp.Core.SearchClient.SearchWithTagsAsync(keywords, page);
                    gallery = result.elements[RandomUtil.Next(0, result.elements.Length)];
                }
                catch (Exception)
                {
                    return CommandResult.FromError("No results were found with the provided tag(s).");
                }
            }

            string englishTitle = string.IsNullOrEmpty(gallery.englishTitle) ? "No English title" : gallery.englishTitle;
            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.Red)
                .WithTitle("One hentai coming right up!")
                .WithDescription($"Well, buddy, I've found you **{gallery.japaneseTitle}** ({englishTitle}).\nIt's at: {gallery.url}")
                .WithImageUrl(gallery.cover.imageUrl.ToString());
            await ReplyAsync(embed: embed.Build());
            return CommandResult.FromSuccess();
        }
    }
}
