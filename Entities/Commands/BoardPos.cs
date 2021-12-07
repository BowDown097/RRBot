namespace RRBot.Entities.Commands;
public struct BoardPos
{
    public static readonly BoardPos ORIGIN = (0, 0);
    public int x, y;

    public BoardPos(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public static implicit operator BoardPos((int x, int y) point) => new(point.x, point.y);

    public override bool Equals(object obj) => obj is BoardPos pos && this == pos;
    public override int GetHashCode() => x.GetHashCode() ^ y.GetHashCode();

    public static bool operator ==(BoardPos pos1, BoardPos pos2) => pos1.x == pos2.x && pos1.y == pos2.y;
    public static bool operator !=(BoardPos pos1, BoardPos pos2) => !(pos1 == pos2);
    public static BoardPos operator +(BoardPos pos1, BoardPos pos2) => new(pos1.x + pos2.x, pos1.y + pos2.y);
}