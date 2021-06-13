using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using NHentaiSharp.Search;
using RRBot.Preconditions;

namespace RRBot.Modules
{
    [RequireNsfwEnabled]
    [RequireNsfw]
    public class NSFW : ModuleBase<SocketCommandContext>
    {
        [Command("nhentai")]
        [Summary("Search for a doujinshi/manga from NHentai, or go for a completely random one! If you provide multiple keywords for a search, separate them with a comma with no spaces.")]
        [Remarks("``$nhentai``")]
        public async Task<RuntimeResult> NHentai([Remainder] string keyword = "")
        {
            GalleryElement funny = new GalleryElement();
            Random random = new Random();
            if (string.IsNullOrWhiteSpace(keyword)) // generate random funny if no keyword(s) is given
            {
                NHentaiSharp.Search.SearchResult result = await NHentaiSharp.Core.SearchClient.SearchAsync();
                funny = result.elements[random.Next(0, result.elements.Length)];
            }
            else
            {
                string[] keywords = keyword.Contains(',') ? keyword.Split(',') : new string[] { keyword };
                try
                {
                    // the search code kinda garbage but according to the README.md of the NHentaiSharp project you have to do this i guess
                    NHentaiSharp.Search.SearchResult result = await NHentaiSharp.Core.SearchClient.SearchWithTagsAsync(keywords);
                    int page = random.Next(0, result.numPages) + 1;
                    result = await NHentaiSharp.Core.SearchClient.SearchWithTagsAsync(keywords, page);
                    funny = result.elements[random.Next(0, result.elements.Length)];
                }
                catch (Exception)
                {
                    return CommandResult.FromError($"{Context.User.Mention}, I could not find a doujinshi/manga given the provided tag(s).");
                }
            }

            string englishTitle = string.IsNullOrEmpty(funny.englishTitle) ? "no English title" : funny.englishTitle;
            EmbedBuilder embed = new EmbedBuilder
            {
                Color = Color.Red,
                Title = "One hentai coming right up!",
                Description = $"Well, buddy, I've found you **{funny.japaneseTitle}** ({englishTitle}).\nIt's at: {funny.url.ToString()}",
                ImageUrl = funny.cover.imageUrl.ToString()
            };
            await ReplyAsync(embed: embed.Build());
            return CommandResult.FromSuccess();
        }
    }
}
