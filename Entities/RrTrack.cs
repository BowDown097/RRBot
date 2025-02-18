namespace RRBot.Entities;
public class RrTrack : ITrackQueueItem
{
    public Uri? ArtworkUri { get; }
    public string Author { get; }
    public TrackReference Reference { get; }
    public string Requester { get; }
    public string Title { get; }
    public LavalinkTrack Track => Reference.Track!;

    public RrTrack(LavalinkTrack track, IUser requester)
    {
        ArtworkUri = track.ArtworkUri;
        Author = SanitizeOr(track.Author, "Unknown author");
        Reference = new TrackReference(track);
        Requester = requester.Sanitize();
        Title = SanitizeOr(track.Title, "Unknown title");
    }

    public RrTrack(LavalinkTrack track, string artwork, string author, string title, IUser requester)
    {
        ArtworkUri = Uri.TryCreate(artwork, UriKind.Absolute, out Uri? uri) ? uri : null;
        Author = SanitizeOr(author, "Unknown author");
        Reference = new TrackReference(track);
        Requester = requester.Sanitize();
        Title = SanitizeOr(title, "Unknown title");
    }

    private static string SanitizeOr(string str, string fallback)
        => !string.IsNullOrWhiteSpace(str) ? StringCleaner.Sanitize(str) : fallback;
}