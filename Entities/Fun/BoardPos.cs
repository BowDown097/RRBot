namespace RRBot.Entities.Fun;
public readonly struct BoardPos
{
    public static readonly BoardPos Origin = (0, 0);
    public readonly int X;
    public readonly int Y;

    private BoardPos(int x, int y)
    {
        X = x;
        Y = y;
    }

    public static implicit operator BoardPos((int x, int y) point) => new(point.x, point.y);

    public override bool Equals(object obj) => obj is BoardPos pos && this == pos;
    public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode();

    public static bool operator ==(BoardPos pos1, BoardPos pos2) => pos1.X == pos2.X && pos1.Y == pos2.Y;
    public static bool operator !=(BoardPos pos1, BoardPos pos2) => !(pos1 == pos2);
    public static BoardPos operator +(BoardPos pos1, BoardPos pos2) => new(pos1.X + pos2.X, pos1.Y + pos2.Y);
}