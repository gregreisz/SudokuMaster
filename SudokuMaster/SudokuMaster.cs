using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using log4net;
using log4net.Config;

namespace SudokuMaster
{
    public class CustomLabel : Label
    {
        public bool IsEraseable { get; set; }

        public int? Value { get; set; }
    }

    public class Sudoku
    {
        public Sudoku _Sudoku;

        public Sudoku()
        {
            _Sudoku = this;

            XmlConfigurator.Configure();
        }

        public ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        // used to represent the values in the grid
        public int[,] Cell = new int[10, 10];

        public Stack<int[,]> ActualStack = new Stack<int[,]>();

        // stacks to keep track of all the moves
        public Stack<string> Moves;

        public int[,] CellValues
        {
            get => Cell;
            set => Cell = value;
        }

        public string[,] Candidates = new string[10, 10];
        public Stack<string[,]> PossibleStack = new Stack<string[,]>();
        public Stack<string> RedoMoves;

        public const string LargeFontName = "Verdana";
        public const string SmallFontName = "Consolas";
        public const int SmallFontSize = 6;
        public const int LargeFontSize = 10;


        public string TransformPossibleValues(string possibleValues)
        {
            if (possibleValues.Length < 9) return possibleValues;

            var lf = Environment.NewLine;
            var s = possibleValues;

            return string.Format("{0}{1}{2}{1}{3}", s.Substring(0, 3), lf, s.Substring(3, 3), s.Substring(6, 3));
        }

        public int SelectedColumn { get; set; } = 1;

        public int SelectedRow { get; set; } = 1;

        public string FilterFileInput(string input)
        {
            return !string.IsNullOrEmpty(input)
                ? string.Join(string.Empty, Regex.Split(input, @"(?:\r\n|\n|\r)"))
                : string.Empty;
        }

        public bool IsNumeric(string input, out int number)
        {
            return int.TryParse(input, out number);
        }
        public void SudokuBoardHandler(object sender)
        {
            var f = Form1._Form1;

            // check to see if game has started
            if (!f.GameHasStarted)
            {
                f.SetStatus(@"Click File->New to start a new game or File->Open to load an existing game", true);
                return;
            }

            if (sender is CustomLabel label)
            {
                var col = SelectedColumn = int.Parse(label.Name.Substring(0, 1));
                var row = SelectedRow = int.Parse(label.Name.Substring(1, 1));
                var value = f.SelectedNumber;
                if (FilterFileInput(label.Text).Trim().Length == 1 && IsNumeric(label.Text, out _))
                {
                    value = f.SelectedNumber = int.Parse(FilterFileInput(label.Text).Trim());
                }

                // if cell is not eraseable then return
                if (label.IsEraseable == false)
                {
                    f.SetStatus(@"This cell cannot be erased.");

                    return;
                }

                try
                {
                    // if erasing a cell
                    if (value == 0)
                    {
                        // if cell is empty then no need to erase
                        if (CellValues[SelectedColumn, SelectedRow] == 0)
                        {
                            return;
                        }

                        // save the value in the array
                        f.SetCell(col, row, value);
                        f.SetText($@"{value} erased at ({col},{row})");
                    }

                    else
                    {
                        // if move is not valid then return
                        if (!IsMoveValid(col, row, value))
                        {
                            f.SetText($@"Invalid move at ({col},{row})");
                            return;
                        }

                        f.SetCell(col, row, value);
                        f.SetText($"Saved {value} to ({col},{row}) successfully.");

                        // save the value in the array
                        CellValues[col, row] = value;

                        // saves the move into the stack
                        Moves.Push($"{label.Name} {col}{row} pushed onto Moves stack.");

                        ShowMarkups();

                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);

                }
                if (IsPuzzleSolved())
                {
                    f.SetStatus2(@"*****Puzzle Solved*****Game Ended*****", true);
                    f.StartTimer(true);
                    f.GameHasEnded = true;

                }
            }
        }

        public bool IsPuzzleSolved()
        {
            string pattern;

            // check row by row
            for (var r = 1; r <= 9; r++)
            {
                pattern = "123456789";
                for (var c = 1; c <= 9; c++)
                {
                    pattern = pattern.Replace(CellValues[c, r].ToString(), string.Empty);
                }

                if (pattern.Length > 0)
                {
                    return false;
                }
            }

            // check col by col
            for (var c = 1; c <= 9; c++)
            {
                pattern = "123456789";
                for (var r = 1; r <= 9; r++)
                {
                    pattern = pattern.Replace(CellValues[c, r].ToString(), string.Empty);
                }

                if (pattern.Length > 0)
                {
                    return false;
                }
            }

            // check by minigrid
            for (var c = 1; c <= 9; c += 3)
            {
                pattern = "123456789";
                for (var r = 1; r <= 9; r += 3)
                    for (var cc = 0; cc <= 2; cc++)
                        for (var rr = 0; rr <= 2; rr++)
                            pattern = pattern.Replace(CellValues[c + cc, r + rr].ToString(), string.Empty);
                if (pattern.Length > 0)
                {
                    return false;
                }
            }

            return true;
        }

        public string CalculatePossibleValues(int col, int row, int spaceLength = 1)
        {
            var str = string.IsNullOrEmpty(Candidates[col, row]) ? "123456789" : Candidates[col, row];

            int r;
            int c;
            var space = new string(' ', spaceLength);
            // Step (1) check by column
            for (r = 1; r <= 9; r++)
                if (CellValues[col, r] != 0)
                {
                    // that means there is a actual value in it
                    str = str.Replace(CellValues[col, r].ToString(), space);
                }

            // Step (2) check by row
            for (c = 1; c <= 9; c++)
            {
                if (CellValues[c, row] != 0)
                {
                    // that means there is a actual value in it
                    str = str.Replace(CellValues[c, row].ToString(), space);
                }
            }

            // Step (3) check within the minigrid
            var startC = col - (col - 1) % 3;
            var startR = row - (row - 1) % 3;
            for (var rr = startR; rr <= startR + 2; rr++)
                for (var cc = startC; cc <= startC + 2; cc++)
                {
                    if (CellValues[cc, rr] != 0)
                    {
                        str = str.Replace(CellValues[cc, rr].ToString(), space);
                    }
                }

            return str;
        }

        public void CheckCandidates()
        {
            var line = new string('*', 47);
            var shortLine = new string('*', 40);
            var sb = new StringBuilder();

            sb.AppendLine(line);
            foreach (var r in Enumerable.Range(1, 9))
                foreach (var c in Enumerable.Range(1, 9))
                {
                    if (CellValues[c, r] == 0)
                    {
                        sb.AppendLine($"({c},{r}) ({CellValues[c, r]}) {CalculatePossibleValues(c, r)}");
                    }

                    if (CellValues[c, r] > 0)
                    {
                        sb.AppendLine($"({c},{r}) ({CellValues[c, r]}) {CalculatePossibleValues(c, r)}");
                    }

                    if (c % 9 == 0)
                    {
                        sb.AppendLine($@"row {r} {shortLine}");
                    }
                }

            sb.AppendLine();
            Form1._Form1.SetText(sb.ToString());
        }

        public void CheckValues()
        {
            var sb = new StringBuilder();
            foreach (var row in Enumerable.Range(1, 9))
            {
                foreach (var col in Enumerable.Range(1, 9))
                {
                    sb.Append(Candidates[col, row] != null ? $"{Candidates[col, row]} " : $"{CellValues[col, row]} ");
                }

                sb.AppendLine();
            }

            Form1._Form1.SetText(sb.ToString());
        }

        public void ShowMarkups()
        {
            var f = Form1._Form1;

            foreach (var r in Enumerable.Range(1, 9))
            {
                foreach (var c in Enumerable.Range(1, 9))
                {
                    var control = f.Controls.Find($"{c}{r}", true).FirstOrDefault();
                    var label = (CustomLabel)control;

                    if (CellValues[c, r] == 0 && label != null)
                    {
                        label.Font = new Font(SmallFontName, SmallFontSize, FontStyle.Bold);
                        label.Text = TransformPossibleValues(CalculatePossibleValues(c, r));
                    }

                    if (CellValues[c, r] > 0 && label != null)
                    {
                        label.Font = new Font(LargeFontName, LargeFontSize, FontStyle.Bold);
                    }

                }
            }

        }

        public string LoadGameFromDisk()
        {
            var f = Form1._Form1;
            var openFileDialog1 = new OpenFileDialog
            {
                Filter = @"SDO files (*.sdo)|*.sdo|All files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = false
            };

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                f.SavedFileName = openFileDialog1.FileName;
                f.CurrentGameState = _Sudoku.FilterFileInput(File.ReadAllText(openFileDialog1.FileName));
            }

            f.Text = f.SavedFileName;

            return f.CurrentGameState;
        }

        public bool IsMoveValid(int col, int row, int value)
        {
            var isValid = true;
            try
            {
                // scan through columns
                foreach (var r in Enumerable.Range(1, 9))
                {
                    if (CellValues[col, r] == value) // duplicate
                    {
                        Form1._Form1.SetStatus($"Move to {col},{r} found to be invalid while scanning columns.", true);
                        isValid = false;

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            try
            {
                // scan through rows
                foreach (var c in Enumerable.Range(1, 9))
                {
                    if (CellValues != null && CellValues[c, row] == value)
                    {
                        Form1._Form1.SetStatus($"Move to {c},{row} found to be invalid while scanning rows.", true);

                        isValid = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            try
            {
                // scan through regions
                var startC = col - (col - 1) % 3;
                var startR = row - (row - 1) % 3;

                foreach (var rr in Enumerable.Range(0, 2))
                {
                    foreach (var cc in Enumerable.Range(0, 2))
                        if (CellValues != null && CellValues[startC + cc, startR + rr] == value) // duplicate
                        {
                            Form1._Form1.SetStatus($"Move to {startC + cc},{startR + rr} found to be invalid while scanning boxes.", true);
                            isValid = false;
                        }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return isValid;
        }

        public string SaveGameToDisk(bool saveAs)
        {
            var f = Form1._Form1;

            // if saveFileName is empty, means game has not been saved before
            if (f.SavedFileName == string.Empty || saveAs)
            {
                using (var saveFileDialog1 = new SaveFileDialog
                {
                    Filter = @"SDO files (*.sdo)|*.sdo|All files (*.*)|*.*",
                    FilterIndex = 1,
                    RestoreDirectory = false
                })
                {
                    if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                    {
                        // store the filename first
                        f.SavedFileName = saveFileDialog1.FileName;
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            // formulate the string representing the values to store
            var stringBuilder = new StringBuilder();
            foreach (var row in Enumerable.Range(1, 9))
            {
                foreach (var col in Enumerable.Range(1, 9))
                {
                    stringBuilder.Append(CellValues[col, row].ToString());
                }
            }

            // save the values to file
            try
            {
                var fileExists = File.Exists(f.SavedFileName);
                if (fileExists)
                {
                    File.Delete(f.SavedFileName);
                }

                File.WriteAllText(f.SavedFileName, stringBuilder.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }

            return $@"Puzzle saved in {f.SavedFileName}";
        }



    }
}

