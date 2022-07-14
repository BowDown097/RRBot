namespace RRBot.Entities.Commands;
public class TrackMetadata
{
    public Uri Artwork { get; set; }
    public string Author { get; set; }
    public string Requester { get; set; }
    public string Title { get; set; }

    public TrackMetadata(LavalinkTrack track, IUser requester)
    {
        Author = !string.IsNullOrWhiteSpace(track.Author) ? Format.Sanitize(track.Author) : "Unknown author";
        Requester = requester.Sanitize();
        Title = !string.IsNullOrWhiteSpace(track.Title) ? Format.Sanitize(track.Title) : "Unknown title";
    }

    public TrackMetadata(string artwork, string author, string title, IUser requester)
    {
        Artwork = Uri.TryCreate(artwork, UriKind.Absolute, out Uri uri) ? uri : null;
        Author = !string.IsNullOrWhiteSpace(author) ? Format.Sanitize(author) : "Unknown author";
        Requester = requester.Sanitize();
        Title = !string.IsNullOrWhiteSpace(title) ? Format.Sanitize(title) : "Unknown title";
    }
}