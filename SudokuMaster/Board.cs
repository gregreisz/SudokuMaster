using System.Collections.Generic;
using System.Linq;

namespace SudokuMaster
{
    public class Board
    {
        public Board()
        {
            Cells = new List<Cell>();
            foreach (var row in Enumerable.Range(1, 9))
            {
                foreach (var column in Enumerable.Range(1, 9))
                {
                    Cells.Add(new Cell(row, column));
                }
            }
        }

        public List<Cell> Cells { get; }

        public void SetCellValue(int row, int column, int value)
        {
            var activeCell = Cells.Single(x => x.Row == row && x.Column == column);
            activeCell.Value = value;

            // Remove value from other squares in the same row
            foreach (var cell in Cells.Where(s => !s.IsSolved && s.Row == row))
            {
                cell.PotentialValues.Remove(value);
            }

            // Remove value from other squares in the same column
            foreach (var cell in Cells.Where(s => !s.IsSolved && s.Column == column))
            {
                cell.PotentialValues.Remove(value);
            }

            // Remove value from other squares in the same quadrant
            foreach (var cell in Cells.Where(s => !s.IsSolved && s.Block == activeCell.Block))
            {
                cell.PotentialValues.Remove(value);
            }

            // Set the Value for any square that only have one remaining PotentialValue
            foreach (var cell in Cells.Where(s => !s.IsSolved && s.PotentialValues.Count == 1))
            {
                SetCellValue(cell.Row, cell.Column, cell.PotentialValues[0]);
            }
        }

    }
}