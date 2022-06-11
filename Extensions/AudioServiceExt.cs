namespace RRBot.Extensions;
public static class AudioServiceExt
{
    public static async Task<LavalinkTrack> GetYTTrackAsync(this IAudioService service, Uri uri)
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
            return await service.YTDLPGetTrackAsync(uri);
        else
            return await service.RRGetTrackAsync(uri.ToString(), SearchMode.YouTube);
    }

    public static async Task<LavalinkTrack> RRGetTrackAsync(this IAudioService service, string query, SearchMode mode = SearchMode.None)
    {
        LavalinkTrack track = await service.GetTrackAsync(query, mode);
        if (track != null)
            track.Context = new TrackMetadata(track);
        return track;
    }

    public static async Task<LavalinkTrack> YTDLPGetTrackAsync(this IAudioService service, Uri uri)
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
        LavalinkTrack track = await service.GetTrackAsync(obj["url"].ToString());
        if (track != null)
        {
            track.Context = new TrackMetadata(
                obj["thumbnail"]?.ToString(),
                obj["uploader"]?.ToString() ?? obj["channel"]?.ToString(),
                obj["title"]?.ToString());
        }

        return track;
    }
}