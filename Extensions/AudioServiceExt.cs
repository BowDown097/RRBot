namespace RRBot.Extensions;
public static class AudioServiceExt
{
    private static async Task<string> GetFirstSearchResultIdAsync(string query)
    {
        using HttpClient client = new();
        var ctx = new
        {
            query,
            context = new
            {
                client = new
                {
                    clientName = "WEB",
                    clientVersion = "2.20240312.01.00"
                }
            }
        };

        using HttpRequestMessage reqMsg = new(HttpMethod.Post,
            "https://www.youtube.com/youtubei/v1/search?key=AIzaSyAO_FJ2SlqU8Q4STEHLGCilw_Y9_11qcW8");
        reqMsg.Content = new StringContent(JsonConvert.SerializeObject(ctx), Encoding.UTF8, "application/json");
        
        using HttpResponseMessage resMsg = await client.SendAsync(reqMsg, HttpCompletionOption.ResponseHeadersRead);
        string response = await resMsg.Content.ReadAsStringAsync();

        if (!JObjectExt.TryParse(response, out JObject? responseObj))
            return "";

        return responseObj?["contents"]?["twoColumnSearchResultsRenderer"]?["primaryContents"]?
            ["sectionListRenderer"]?["contents"]?[0]?["itemSectionRenderer"]?["contents"]?[0]?
            ["videoRenderer"]?["videoId"]?.ToString() ?? "";
    }

    private static async Task<bool> IsAgeRestrictedAsync(string videoId)
    {
        // XBOXONEGUIDE is used because it's fast
        using HttpClient client = new();
        var ctx = new
        {
            videoId,
            context = new
            {
                client = new
                {
                    clientName = "XBOXONEGUIDE",
                    clientVersion = "1.0"
                }
            }
        };
        
        using HttpRequestMessage reqMsg = new(HttpMethod.Post,
            "https://www.youtube.com/youtubei/v1/player?key=AIzaSyAO_FJ2SlqU8Q4STEHLGCilw_Y9_11qcW8");
        reqMsg.Content = new StringContent(JsonConvert.SerializeObject(ctx), Encoding.UTF8, "application/json");
        
        using HttpResponseMessage resMsg = await client.SendAsync(reqMsg, HttpCompletionOption.ResponseHeadersRead);
        string response = await resMsg.Content.ReadAsStringAsync();

        return !JObjectExt.TryParse(response, out JObject? responseObj) ||
               responseObj?["playabilityStatus"]?["status"]?.ToString() == "LOGIN_REQUIRED";
    }

    public static async Task<RrTrack?> GetYtTrackAsync(this IAudioService service, Uri uri,
        SocketGuild guild, IUser requester)
    {
        NameValueCollection query = HttpUtility.ParseQueryString(uri.Query);
        string? videoId = query.Get("v") ?? uri.Segments.LastOrDefault();
        if (videoId is null)
            return null;

        if (await IsAgeRestrictedAsync(videoId))
        {
            DbConfigMisc misc = await MongoManager.FetchConfigAsync<DbConfigMisc>(guild.Id);
            return misc.NsfwEnabled
                ? await service.YtDlpGetTrackAsync(uri, requester)
                : new RrTrack(new LavalinkTrack { Author = "", Identifier = "restricted", Title = "" }, requester);
        }

        RrTrack? lavalinkTrack = await service.RrGetTrackAsync(uri.ToString(), requester, TrackSearchMode.YouTube);
        if (lavalinkTrack is not null)
            return lavalinkTrack;

        Console.WriteLine($"RrGetTrackAsync failed for YouTube video {videoId} - falling back to yt-dlp");
        return await service.YtDlpGetTrackAsync(uri, requester);
    }

    public static async Task<RrTrack?> RrGetTrackAsync(this IAudioService service, string query,
        IUser requester, TrackSearchMode mode, string? filename = null)
    {
        LavalinkTrack? track = await service.Tracks.LoadTrackAsync(query, mode);
        if (track is null)
            return null;

        return new RrTrack(track, track.ArtworkUri?.ToString() ?? "", track.Author,
            HttpUtility.UrlDecode(filename) ?? track.Title, requester);
    }

    public static async Task<RrTrack?> SearchYtTrackAsync(this IAudioService service, string query,
        SocketGuild guild, IUser requester)
    {
        LavalinkTrack? lavalinkTrack = await service.Tracks.LoadTrackAsync(query, TrackSearchMode.YouTube);
        if (lavalinkTrack is not null)
            return new RrTrack(lavalinkTrack, requester);
        
        Console.WriteLine($"Lavalink YouTube search failed for query \"{query}\" - making manual search request");
        string videoId = await GetFirstSearchResultIdAsync(query);
        if (videoId is null)
            return null;

        return await service.GetYtTrackAsync(new Uri($"https://www.youtube.com/watch?v={videoId}"), guild, requester);
    }

    public static async Task<RrTrack?> YtDlpGetTrackAsync(this IAudioService service, Uri uri, IUser requester)
    {
        using Process proc = new();
        proc.StartInfo.FileName = new FileInfo("yt-dlp").GetFullPath();
        proc.StartInfo.Arguments = $"-xj --no-warnings {uri}";
        proc.StartInfo.CreateNoWindow = true;
        proc.StartInfo.RedirectStandardOutput = true;
        proc.StartInfo.UseShellExecute = false;
        proc.Start();

        string output = await proc.StandardOutput.ReadToEndAsync();
        await proc.WaitForExitAsync();

        if (!JObjectExt.TryParse(output.Split('\n')[0], out JObject? obj) ||
            !Uri.TryCreate(obj?["url"]?.ToString(), UriKind.Absolute, out Uri? directUri))
        {
            return null;
        }

        LavalinkTrack? track = await service.Tracks.LoadTrackAsync(directUri.ToString(), TrackSearchMode.None);
        if (track is null)
            return null;

        string author = obj.TryGetValue("uploader", out JToken? value)
            ? value.ToString() ?? "" : obj["channel"]?.ToString() ?? "";
        string thumbnail = obj["thumbnail"]?.ToString() ?? "";
        string title = obj.TryGetValue("title", out JToken? titleToken)
            ? titleToken.ToString() : Path.GetFileName(directUri.LocalPath);

        return new RrTrack(track, thumbnail, author, title, requester);
    }
}