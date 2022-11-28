namespace RRBot.Extensions;
public static class AudioServiceExt
{
    public static async Task<LavalinkTrack> GetYtTrackAsync(this IAudioService service, Uri uri, SocketGuild guild, IUser requester)
    {
        using HttpClient client = new();
        var ctx = new
        {
            videoId = HttpUtility.ParseQueryString(uri.Query)["v"],
            context = new
            {
                client = new
                {
                    clientName = "WEB",
                    clientVersion = "2.20220609.00.00",
                    hl = "en",
                    gl = "US"
                }
            }
        };

        using HttpRequestMessage reqMsg = new(HttpMethod.Post, "https://www.youtube.com/youtubei/v1/player?key=AIzaSyAO_FJ2SlqU8Q4STEHLGCilw_Y9_11qcW8")
        {
            Content = new StringContent(JsonConvert.SerializeObject(ctx), Encoding.UTF8, "application/json")
        };

        using HttpResponseMessage resMsg = await client.SendAsync(reqMsg, HttpCompletionOption.ResponseHeadersRead);
        string response = await resMsg.Content.ReadAsStringAsync();
        if (JObject.Parse(response)["playabilityStatus"]?["reason"]?.ToString() == "Sign in to confirm your age")
        {
            DbConfigMisc misc = await MongoManager.FetchConfigAsync<DbConfigMisc>(guild.Id);
            if (!misc.NsfwEnabled)
                return new LavalinkTrack("restricted", "", TimeSpan.Zero, false, false, null, "", TimeSpan.Zero, "", "");
            return await service.YtdlpGetTrackAsync(uri, requester);
        }
        else
        {
            return await service.RrGetTrackAsync(uri.ToString(), requester, SearchMode.YouTube);
        }
    }

    public static async Task<LavalinkTrack> RrGetTrackAsync(this IAudioService service, string query, IUser requester, SearchMode mode = SearchMode.None)
    {
        LavalinkTrack track = await service.GetTrackAsync(query, mode);
        if (track != null)
            track.Context = new TrackMetadata(track, requester);
        return track;
    }

    public static async Task<LavalinkTrack> YtdlpGetTrackAsync(this IAudioService service, Uri uri, IUser requester)
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

        JObject obj = JObject.Parse(output.Split('\n')[0]);
        LavalinkTrack track = await service.GetTrackAsync(obj["url"]?.ToString() ?? string.Empty);
        if (track != null)
        {
            track.Context = new TrackMetadata(
                obj["thumbnail"]?.ToString(),
                obj["uploader"]?.ToString() ?? obj["channel"]?.ToString(),
                obj["title"]?.ToString(),
                requester);
        }

        return track;
    }
}