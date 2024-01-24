namespace RRBot.Modules;
public partial class Fun
{
    private static int[,] GenerateBoard(int difficulty)
    {
        double density = difficulty switch
        {
            1 => 0.146,
            2 => 0.201,
            3 => 0.246,
            _ => 0.201
        };

        int totalMines = (int)Math.Floor(64.0 * density);
        int[,] board = new int[8, 8];
        List<BoardPos> mines = new(totalMines);
        while (mines.Count < totalMines)
        {
            BoardPos pos = (RandomUtil.Next(8), RandomUtil.Next(8));
            if (pos == BoardPos.Origin || board[pos.X, pos.Y] == -1) continue;
            board[pos.X, pos.Y] = -1;
            mines.Add(pos);
        }

        foreach (BoardPos mine in mines)
        {
            foreach (BoardPos adjacent in Adjacents.Select(adj => mine + adj))
            {
                if (adjacent.X is < 0 or >= 8 || adjacent.Y is < 0 or >= 8 || board[adjacent.X, adjacent.Y] == -1)
                    continue;
                board[adjacent.X, adjacent.Y]++;
            }
        }

        return board;
    }
}