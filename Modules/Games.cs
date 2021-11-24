namespace RRBot.Modules
{
    [Summary("Play some games!")]
    public class Games : ModuleBase<SocketCommandContext>
    {
        private static readonly BoardPos[] adjacents = { (-1, -1), (-1, 0), (-1, 1), (0, -1), (0, 1), (1, -1), (1, 0), (1, 1) };

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
                if (pos != BoardPos.ORIGIN && board[pos.x, pos.y] != -1)
                {
                    board[pos.x, pos.y] = -1;
                    mines.Add(pos);
                }
            }

            foreach (BoardPos mine in mines)
            {
                foreach (BoardPos adjacent in adjacents.Select(adj => mine + adj))
                {
                    if (adjacent.x < 0 || adjacent.x >= 8 || adjacent.y < 0 || adjacent.y >= 8 || board[adjacent.x, adjacent.y] == -1)
                        continue;
                    board[adjacent.x, adjacent.y]++;
                }
            }

            return board;
        }

        [Command("minesweeper")]
        [Summary("Play a game of Minesweeper. Choose between difficulty 1-3.")]
        [Remarks("$minesweeper <difficulty>")]
        public async Task<RuntimeResult> Minesweeper(int difficulty = 1)
        {
            if (difficulty < 1 || difficulty > 3)
                return CommandResult.FromError($"**{difficulty}** is not a valid difficulty!");

            int[,] board = GenerateBoard(difficulty);
            StringBuilder boardBuilder = new();
            for (int x = 0; x < board.GetLength(0); x++)
            {
                for (int y = 0; y < board.GetLength(1); y++)
                {
                    string tile = board[x, y] == -1 ? "💥" : Constants.POLL_EMOTES[board[x, y]];
                    boardBuilder.Append($"||{tile}||");
                }

                boardBuilder.Append('\n');
            }

            await ReplyAsync(boardBuilder.ToString());
            return CommandResult.FromSuccess();
        }
    }
}
