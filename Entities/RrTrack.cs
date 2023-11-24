namespace RRBot.Entities;

public class RrTrack : ITrackQueueItem
{
    public Uri ArtworkUri { get; }
    public string Author { get; }
    public TrackReference Reference { get; }
    public string Requester { get; }
    public string Title { get; }
    public LavalinkTrack Track => Reference.Track;

    public RrTrack(LavalinkTrack track, IUser requester)
    {
        ArtworkUri = track.ArtworkUri;
        Author = SanitizeOrUnknown(track.Author, "author");
        Reference = new TrackReference(track);
        Requester = requester.Sanitize();
        Title = SanitizeOrUnknown(track.Title, "title");
    }

    public RrTrack(LavalinkTrack track, string artwork, string author, string title, IUser requester)
    {
        ArtworkUri = Uri.TryCreate(artwork, UriKind.Absolute, out Uri uri) ? uri : null;
        Author = SanitizeOrUnknown(author, "author");
        Reference = new TrackReference(track);
        Requester = requester.Sanitize();
        Title = SanitizeOrUnknown(title, "title");
    }

    private static string SanitizeOrUnknown(string str, string classifier)
        => !string.IsNullOrWhiteSpace(str) ? StringCleaner.Sanitize(str) : "Unknown " + classifier;
}