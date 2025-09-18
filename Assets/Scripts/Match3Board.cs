using System;
using System.Collections.Generic;

namespace Match3
{
    // Basic Match-3 board
    public class Match3Board
    {
        public int Rows;        // number of rows
        public int Cols;        // number of columns
        public int ColorsCount; // how many different tile colors

        private int[,] grid;    //the grid is the board itself, each tile placement - each cell contains an int which is a "color" 
        private Random rng;     // random generator
        private const int EMPTY = -1; //empty tiles will have -1 (no color assigned)

        public Match3Board(int rows, int cols, int colorsCount, int? seed = null)//constructor //seed is the starting value that a RNG uses to begin its sequence //int? means an int or null
        {
            Rows = rows;
            Cols = cols;
            ColorsCount = colorsCount;

            grid = new int[Rows, Cols];
            rng = seed.HasValue ? new Random(seed.Value) : new Random();

            FillStartBoard();
        }

        // return the board state
        public int[,] GetBoard() => (int[,])grid.Clone(); //when we call the board we get as a return a copy of it not the original board

        // try swapping two cells
        public bool TrySwap(Coord a, Coord b, out int cleared, out int cascades)//coord is a struct we define in the bottom of the code so a describes 2 nums and b too
        { //out int saves the variable value for use outside the func (its not erased)
            cleared = 0;  //cleared = the total number of tiles removed because of the swap (including cascades)
            cascades = 0; //A cascade happens when you make a match, tiles clear, new tiles fall, and then that falling creates another match automatically.-So cascades tells you how many chain reactions happened from that single swap

            if (!AreAdjacent(a, b)) return false; //if tile a and tile b are not adjacent then we cant even try to swap them

            Swap(a, b); //they are adjacent so we swap their places

            var matches = FindMatches(); //findmatches is a func later - 
            if (matches.Count == 0)
            {
                // no match -> revert
                Swap(a, b);
                return false;
            }

            // keep clearing until stable
            while (matches.Count > 0)
            {
                cleared += Clear(matches);
                ApplyGravity();
                Refill();
                cascades++;

                matches = FindMatches();
            }

            return true;
        }

        // --------- PLAYABILITY CHECKS & RESHUFFLE ----------

        public bool HasAnyValidSwap()
        {
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    if (grid[r, c] == EMPTY) continue;

                    // try swap RIGHT
                    if (c + 1 < Cols && grid[r, c + 1] != EMPTY)
                    {
                        SwapInGrid(r, c, r, c + 1);
                        bool ok = CreatesMatchAt(r, c) || CreatesMatchAt(r, c + 1);
                        SwapInGrid(r, c, r, c + 1);
                        if (ok) return true;
                    }
                    // try swap DOWN
                    if (r + 1 < Rows && grid[r + 1, c] != EMPTY)
                    {
                        SwapInGrid(r, c, r + 1, c);
                        bool ok = CreatesMatchAt(r, c) || CreatesMatchAt(r + 1, c);
                        SwapInGrid(r, c, r + 1, c);
                        if (ok) return true;
                    }
                }
            }
            return false;
        }

        private void SwapInGrid(int r1, int c1, int r2, int c2)
        {
            int tmp = grid[r1, c1];
            grid[r1, c1] = grid[r2, c2];
            grid[r2, c2] = tmp;
        }

        private bool CreatesMatchAt(int r, int c)
        {
            int color = grid[r, c];
            if (color == EMPTY) return false;

            // horizontal run length through (r,c)
            int run = 1;
            int cc = c - 1; while (cc >= 0 && grid[r, cc] == color) { run++; cc--; }
            cc = c + 1; while (cc < Cols && grid[r, cc] == color) { run++; cc++; }
            if (run >= 3) return true;

            // vertical run length through (r,c)
            run = 1;
            int rr = r - 1; while (rr >= 0 && grid[rr, c] == color) { run++; rr--; }
            rr = r + 1; while (rr < Rows && grid[rr, c] == color) { run++; rr++; }
            return run >= 3;
        }

        private bool HasAnyImmediateMatch()
        {
            // reuse existing FindMatches() behavior
            return FindMatches().Count > 0;
        }

        public void ReshuffleBoard()
        {
            // collect current colors (stable board should have no EMPTY)
            var cells = new List<(int r, int c)>(Rows * Cols);
            var colors = new List<int>(Rows * Cols);
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                {
                    cells.Add((r, c));
                    colors.Add(grid[r, c] == EMPTY ? rng.Next(ColorsCount) : grid[r, c]);
                }

            // try many random permutations until: no immediate matches AND at least one valid move
            for (int attempt = 0; attempt < 500; attempt++)
            {
                // Fisher–Yates
                for (int i = colors.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    (colors[i], colors[j]) = (colors[j], colors[i]);
                }

                // place row-major
                int k = 0;
                for (int i = 0; i < cells.Count; i++)
                {
                    var (r, c) = cells[i];
                    grid[r, c] = colors[k++];
                }

                if (!HasAnyImmediateMatch() && HasAnyValidSwap())
                    return;
            }

            // Fallback: rebuild fresh (no 3-in-a-row) until we get a playable board
            for (int tries = 0; tries < 200; tries++)
            {
                FillStartBoard();                  // avoids instant matches
                if (HasAnyValidSwap()) return;     // ensure at least one move exists
            }
        }


        // --- Board creation and refill ---

        private void FillStartBoard() //Fills every cell using PickSafeColor(r,c) which avoids starting with matches (no 3-in-a-row horizontally/vertically at spawn).
        {
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    grid[r, c] = PickSafeColor(r, c);
                }
            }
        }

        private void Refill()
        {
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    if (grid[r, c] == EMPTY)
                        grid[r, c] = rng.Next(ColorsCount);
        }

        // --- Matching / clearing / gravity ---

        private List<Coord> FindMatches()
        {
            var toClear = new List<Coord>();

            // horizontal
            for (int r = 0; r < Rows; r++)
            {
                int run = 1;
                for (int c = 1; c < Cols; c++)
                {
                    if (grid[r, c] != EMPTY && grid[r, c] == grid[r, c - 1])
                    {
                        run++;
                        if (c == Cols - 1 && run >= 3)
                            for (int k = c - run + 1; k <= c; k++) toClear.Add(new Coord(r, k));
                    }
                    else
                    {
                        if (run >= 3)
                            for (int k = c - run; k < c; k++) toClear.Add(new Coord(r, k));
                        run = 1;
                    }
                }
            }

            // vertical
            for (int c = 0; c < Cols; c++)
            {
                int run = 1;
                for (int r = 1; r < Rows; r++)
                {
                    if (grid[r, c] != EMPTY && grid[r, c] == grid[r - 1, c])
                    {
                        run++;
                        if (r == Rows - 1 && run >= 3)
                            for (int k = r - run + 1; k <= r; k++) toClear.Add(new Coord(k, c));
                    }
                    else
                    {
                        if (run >= 3)
                            for (int k = r - run; k < r; k++) toClear.Add(new Coord(k, c));
                        run = 1;
                    }
                }
            }

            return toClear;
        }

        private int Clear(List<Coord> matches)
        {
            int count = 0;
            foreach (var pos in matches)
            {
                if (grid[pos.R, pos.C] != EMPTY)
                {
                    grid[pos.R, pos.C] = EMPTY;
                    count++;
                }
            }
            return count;
        }

        private void ApplyGravity()
        {
            for (int c = 0; c < Cols; c++)
            {
                int writeRow = Rows - 1;
                for (int r = Rows - 1; r >= 0; r--)
                {
                    if (grid[r, c] != EMPTY)
                    {
                        grid[writeRow, c] = grid[r, c];
                        if (writeRow != r) grid[r, c] = EMPTY;
                        writeRow--;
                    }
                }
            }
        }

        // --- Helpers ---

        private int PickSafeColor(int r, int c) //avoids starting with matches (no 3-in-a-row horizontally/vertically at spawn)
        {
            while (true)//Tries random colors until the color won’t create a horizontal or vertical triple at that location.
            {
                int color = rng.Next(ColorsCount);

                // avoid starting with a 3 in a row
                if (c >= 2 && grid[r, c - 1] == color && grid[r, c - 2] == color)
                    continue;
                if (r >= 2 && grid[r - 1, c] == color && grid[r - 2, c] == color)
                    continue;

                return color;
            }
        }

        private bool AreAdjacent(Coord a, Coord b) =>
            Math.Abs(a.R - b.R) + Math.Abs(a.C - b.C) == 1;

        private void Swap(Coord a, Coord b)
        {
            int tmp = grid[a.R, a.C];
            grid[a.R, a.C] = grid[b.R, b.C];
            grid[b.R, b.C] = tmp;
        }


    }

    // simple coordinate struct
    public struct Coord
    {
        public int R, C; //r is row index and c is col index
        public Coord(int r, int c) { R = r; C = c; } //constructor
        public override string ToString() => $"({R},{C})";
    }
}

