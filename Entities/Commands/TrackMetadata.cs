namespace RRBot.Entities.Commands;
public class TrackMetadata
{
    public string Author { get; set; }
    public string Title { get; set; }

    public TrackMetadata(LavalinkTrack track)
    {
        Author = !string.IsNullOrWhiteSpace(track.Author) ? Format.Sanitize(track.Author) : "Unknown author";
        Title = !string.IsNullOrWhiteSpace(track.Author) ? Format.Sanitize(track.Title) : "Unknown title";
    }

    public TrackMetadata(string author, string title)
    {
        Author = Format.Sanitize(author);
        Title = Format.Sanitize(title);
    }
}