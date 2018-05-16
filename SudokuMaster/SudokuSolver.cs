using System;
using System.Collections.Generic;

namespace SudokuMaster
{
    public class SudokoSolver
    {
        private readonly Grid _grid;

        public SudokoSolver(Grid grid)
        {
            _grid = grid;
            _grid.Validate();
        }

        public int?[,] Data => _grid.Data;

        public int?[,] SolvePuzzle()
        {
            Solve();
            Console.WriteLine($@"{_grid.Assigns} tries total.");
            return _grid.Data;
        }

        private bool Solve()
        {
            if (!_grid.FindUnassignedLoc(out var row, out var col))
            {
                return true;
            }

            for (var num = 1; num <= 9; num++)
            {
                if (!_grid.NoConflicts(row, col, num))
                {
                    continue;
                }

                _grid.Assign(row, col, num);
                if (Solve())
                {
                    return true;
                }

                _grid.Unassign(row, col);
            }

            return false;
        }
    }

    public class Grid
    {
        private int _curC;
        private int _curR;

        public Grid(int?[,] data)
        {
            Data = data ?? new int?[9, 9];
        }

        public int?[,] Data { get; }

        public int Assigns { get; private set; }

        public bool FindUnassignedLoc(out int row, out int col)
        {
            while (Data[_curR, _curC].HasValue)
            {
                _curC++;
                if (_curC == 9)
                {
                    _curR++;
                    _curC = 0;
                }

                if (_curR != 9)
                {
                    continue;
                }

                row = -1;
                col = -1;
                return false;
            }

            row = _curR;
            col = _curC;
            return true;
        }

        public bool NoConflicts(int row, int col, int num)
        {
            for (var r = 0; r < 9; ++r)
            {
                if (Data[r, col] == num) return false;
            }

            for (var c = 0; c < 9; c++)
                if (Data[row, c] == num) return false;

            var fromC = 3 * (col / 3);
            var fromR = 3 * (row / 3);

            for (var c = fromC; c < fromC + 3; c++)
            {
                for (var r = fromR; r < fromR + 3; r++)
                {
                    if (Data[r, c] == num) return false;
                }
            }

            return true;
        }

        public void Assign(int row, int col, int num)
        {
            Assigns++;
            Data[row, col] = num;
        }

        public void Unassign(int row, int col)
        {
            Data[row, col] = null;
            _curC = col;
            _curR = row;
        }

        public void Validate()
        {
            if (Data.Length != 81)
            {
                throw new Exception("Invalid dimensions!");
            }

            if (!IsLegal())
            {
                throw new Exception("Illegal numbers populated!");
            }
        }

        public bool IsLegal()
        {
            var container = new HashSet<int>();

            //vertical check 
            for (var c = 0; c < 9; ++c)
            {
                container.Clear();
                for (var r = 0; r < 9; ++r)
                {
                    if (!Data[r, c].HasValue) continue;

                    if (container.Contains(Data[r, c].Value)) return false;

                    container.Add(Data[r, c].Value);
                }
            }

            // horizontal check
            for (var r = 0; r < 9; ++r)
            {
                container.Clear();
                for (var c = 0; c < 9; ++c)
                {
                    if (!Data[r, c].HasValue) continue;

                    if (container.Contains(Data[r, c].Value)) return false;

                    container.Add(Data[r, c].Value);
                }
            }

            // square check
            var topLeftCorners = new List<Tuple<int, int>>
            {
                new Tuple<int, int>(0, 0),
                new Tuple<int, int>(0, 3),
                new Tuple<int, int>(0, 6),
                new Tuple<int, int>(3, 0),
                new Tuple<int, int>(3, 3),
                new Tuple<int, int>(3, 6),
                new Tuple<int, int>(6, 0),
                new Tuple<int, int>(6, 3),
                new Tuple<int, int>(6, 6)
            };

            foreach (var topLeftCorner in topLeftCorners)
            {
                var fromC = topLeftCorner.Item2;
                var fromR = topLeftCorner.Item1;
                container.Clear();

                for (var c = fromC; c < fromC + 3; c++)
                {
                    for (var r = fromR; r < fromR + 3; r++)
                    {
                        if (!Data[r, c].HasValue) continue;

                        if (container.Contains(Data[r, c].Value)) return false;

                        container.Add(Data[r, c].Value);
                    }
                }
            }

            return true;
        }
    }
}