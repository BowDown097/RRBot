namespace RRBot.Extensions;
public static class AudioServiceExt
{
    public static async Task<RrTrack> GetYtTrackAsync(this IAudioService service, Uri uri,
        SocketGuild guild, IUser requester)
    {
        using HttpClient client = new();
        NameValueCollection query = HttpUtility.ParseQueryString(uri.Query);

        var ctx = new
        {
            videoId = query.TryGetValue("v", out string videoId) ? videoId : uri.Segments.LastOrDefault(),
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
        if (JObject.Parse(response)["playabilityStatus"]?["status"]?.ToString() != "LOGIN_REQUIRED")
            return await service.RrGetTrackAsync(uri.ToString(), requester, TrackSearchMode.YouTube);

        DbConfigMisc misc = await MongoManager.FetchConfigAsync<DbConfigMisc>(guild.Id);
        return misc.NsfwEnabled
            ? await service.YtDlpGetTrackAsync(uri, requester)
            : new RrTrack(new LavalinkTrack { Author = "", Identifier = "restricted", Title = "" }, requester);
    }

    public static async Task<RrTrack> RrGetTrackAsync(this IAudioService service, string query,
        IUser requester, TrackSearchMode mode, string filename = null)
    {
        LavalinkTrack track = await service.Tracks.LoadTrackAsync(query, mode);
        return track is null ? null : new RrTrack(track, track.ArtworkUri?.ToString(), track.Author, filename ?? track.Title, requester);
    }

    public static async Task<RrTrack> YtDlpGetTrackAsync(this IAudioService service, Uri uri, IUser requester)
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
        if (!Uri.TryCreate(obj["uri"]?.ToString(), UriKind.Absolute, out Uri directUri))
            return null;

        LavalinkTrack track = await service.Tracks.LoadTrackAsync(directUri.ToString(), TrackSearchMode.None);
        string author = obj.TryGetValue("uploader", out JToken value)
            ? value.ToString() : obj["channel"]?.ToString();
        string thumbnail = obj["thumbnail"]?.ToString();
        string title = obj.TryGetValue("title", out JToken titleToken)
            ? titleToken.ToString() : Path.GetFileName(directUri.LocalPath);

        return new RrTrack(track, thumbnail, author, title, requester);
    }
}