using System.Collections.Generic;

namespace SudokuMaster
{
    public class Cell
    {
        private readonly List<int> _potentialValues = new List<int> {1, 2, 3, 4, 5, 6, 7, 8, 9};

        internal Cell(int row, int column)
        {
            Row = row;
            Column = column;
            PotentialValues = _potentialValues;
        }

        public int Row { get; }

        public int Column { get; }

        internal Blocks Block
        {
            get
            {
                if (Row < 4)
                {
                    if (Column < 4)
                    {
                        return Blocks.UpperLeft;
                    }

                    return Column < 7 ? Blocks.UpperMiddle : Blocks.UpperRight;
                }

                if (Row < 7)
                {
                    if (Column < 4)
                    {
                        return Blocks.MiddleLeft;
                    }

                    return Column < 7 ? Blocks.Middle : Blocks.MiddleRight;
                }

                if (Column < 4)
                {
                    return Blocks.LowerLeft;
                }

                return Column < 7 ? Blocks.LowerMiddle : Blocks.LowerRight;
            }
        }

        public bool IsSolved => Value != null;

        public int? Value { get; set; }
        internal List<int> PotentialValues { get; }

        internal enum Blocks
        {
            UpperLeft,
            UpperMiddle,
            UpperRight,
            MiddleLeft,
            Middle,
            MiddleRight,
            LowerLeft,
            LowerMiddle,
            LowerRight
        }
    }
}