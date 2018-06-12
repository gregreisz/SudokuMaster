using System;
using System.Collections.Generic;
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
        private const string LargeFontName = "Verdana";
        private const string SmallFontName = "Consolas";
        private const int SmallFontSize = 6;
        private const int LargeFontSize = 10;

        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly string[,] Candidates = new string[10, 10];

        // stacks to keep track of all the moves
        public Stack<string> Moves;
        public Stack<string> RedoMoves;

        public Sudoku()
        {
            XmlConfigurator.Configure();
        }

        public string SavedFileName { get; private set; }

        public string CurrentGameState { private get; set; }

        private int[,] CellValues { get; } = new int[10, 10];

        public bool GameHasStarted { get; private set; }

        private int SelectedColumn { get; set; } = 1;

        private int SelectedRow { get; set; } = 1;

        // used to represent the values in the grid
        public void ClearBoard()
        {
            Array.Clear(CellValues, 0, CellValues.Length);
            Array.Clear(Candidates, 0, Candidates.Length);

            // initialize the stacks
            Moves = new Stack<string>();
            RedoMoves = new Stack<string>();

            // initialize the cells in the board
            foreach (var row in Enumerable.Range(1, 9))
            {
                foreach (var col in Enumerable.Range(1, 9))
                {
                    SetCell(col, row, 0);
                }
            }
        }

        public void SetCell(int col, int row, int value)
        {
            var f = Form1._Form1;

            var control = f.Controls.Find($"{col}{row}", true).FirstOrDefault();
            var cellLabel = (CustomLabel)control;
            if (cellLabel == null)
            {
                return;
            }

            // save the value in the array
            CellValues[col, row] = value;
            // if erasing a cell, you need to reset the possible values for all cells
            if (value == 0)
            {
                foreach (var r in Enumerable.Range(1, 9))
                    foreach (var c in Enumerable.Range(1, 9))
                    {
                        if (CellValues[c, r] == 0)
                        {
                            Candidates[c, r] = string.Empty;
                        }
                    }
            }
            else
            {
                Candidates[col, row] = value.ToString();
            }

            // set the properties for the label
            if (value == 0 && cellLabel.Value == null)
            {
                cellLabel.Value = value;
                cellLabel.IsEraseable = true;
                cellLabel.BackColor = Color.LightYellow;
                cellLabel.ForeColor = Color.Black;
                cellLabel.Font = new Font(LargeFontName, LargeFontSize, FontStyle.Bold);
            }
            else if (value > 0 && cellLabel.Value == null)
            {
                cellLabel.Value = value;
                cellLabel.IsEraseable = false;
                cellLabel.BackColor = Color.LightSteelBlue;
                cellLabel.ForeColor = Color.Blue;
                cellLabel.Font = new Font(LargeFontName, LargeFontSize, FontStyle.Bold);
            }
            else if (value > 0 && cellLabel.IsEraseable)
            {
                cellLabel.Value = value;
                cellLabel.BackColor = Color.LightYellow;
                cellLabel.ForeColor = Color.Black;
                cellLabel.Font = new Font(LargeFontName, LargeFontSize, FontStyle.Bold);
            }


            cellLabel.Text = value.ToString();
        }

        private static string TransformPossibleValues(string possibleValues)
        {
            if (possibleValues.Length < 9)
            {
                return possibleValues;
            }

            var lf = Environment.NewLine;
            var s = possibleValues;

            var values = string.Format("{0}{1}{2}{1}{3}", s.Substring(0, 3), lf, s.Substring(3, 3), s.Substring(6, 3));
            return values;
        }

        private static string FilterOutReturns(string input)
        {
            return !string.IsNullOrEmpty(input)
                ? string.Join(string.Empty, Regex.Split(input, @"(?:\r\n|\n|\r|[ ])"))
                : string.Empty;
        }

        private static bool IsNumeric(string input, out int number)
        {
            return int.TryParse(input, out number);
        }

        public void SudokuHandler(object sender)
        {
            var f = Form1._Form1;

            // check to see if game has started
            if (!GameHasStarted)
            {
                f.SetStatus(@"Click File->New to start a new game or File->Open to load an existing game", true);
                return;
            }

            if (sender is CustomLabel label)
            {
                var col = SelectedColumn = int.Parse(label.Name.Substring(0, 1));
                var row = SelectedRow = int.Parse(label.Name.Substring(1, 1));
                var value = f.SelectedNumber;
                if (FilterOutReturns(label.Text).Trim().Length == 1 && IsNumeric(label.Text, out _))
                {
                    value = f.SelectedNumber = int.Parse(FilterOutReturns(label.Text).Trim());
                }

                // if cell is not eraseable then return
                if (label.IsEraseable == false)
                {
                    f.SetText(@"This cell cannot be erased.");
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
                            f.SetStatus(@"Cell is empty so no need to erase");
                            return;
                        }

                        // save the value in the array
                        SetCell(col, row, value);
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

                        SetCell(col, row, value);
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
                    f.StopTimer();
                    f.SetStatus2(@"*****Puzzle Solved*****Game Ended*****", true);
                }
            }
        }

        private bool IsPuzzleSolved()
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
                {
                    for (var cc = 0; cc <= 2; cc++)
                    {
                        for (var rr = 0; rr <= 2; rr++)
                        {
                            pattern = pattern.Replace(CellValues[c + cc, r + rr].ToString(), string.Empty);
                        }
                    }
                }

                if (pattern.Length > 0)
                {
                    return false;
                }
            }

            return true;
        }

        private string CalculatePossibleValues(int col, int row, int spaceLength = 1)
        {
            var str = string.IsNullOrEmpty(Candidates[col, row]) ? "123456789" : Candidates[col, row];

            int r;
            int c;
            var space = new string(' ', spaceLength);

            // check by column
            for (r = 1; r <= 9; r++)
            {
                if (CellValues[col, r] != 0)
                {
                    // that means there is a actual value in it
                    str = str.Replace(CellValues[col, r].ToString(), space);
                }
            }

            // check by row
            for (c = 1; c <= 9; c++)
            {
                if (CellValues[c, row] != 0)
                {
                    // that means there is a actual value in it
                    str = str.Replace(CellValues[c, row].ToString(), space);
                }
            }

            // check the regions
            var startC = col - (col - 1) % 3;
            var startR = row - (row - 1) % 3;
            for (var rr = startR; rr <= startR + 2; rr++)
            {
                for (var cc = startC; cc <= startC + 2; cc++)
                {
                    if (CellValues[cc, rr] != 0)
                    {
                        str = str.Replace(CellValues[cc, rr].ToString(), space);
                    }
                }
            }

            return str;
        }

        public void CheckCandidates()
        {
            var line = new string('*', 47);
            var shortLine = new string('*', 40);
            var sb = new StringBuilder();
            var f = Form1._Form1;

            sb.AppendLine(line);
            foreach (var r in Enumerable.Range(1, 9))
            {
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
            }

            sb.AppendLine();
            f.SetText(sb.ToString());
        }

        public void CheckValues()
        {
            var sb = new StringBuilder();
            var f = Form1._Form1;
            foreach (var row in Enumerable.Range(1, 9))
            {
                foreach (var col in Enumerable.Range(1, 9))
                {
                    sb.Append(Candidates[col, row] != null ? $"{Candidates[col, row]} " : $"{CellValues[col, row]} ");
                }

                sb.AppendLine();
            }


            f.SetText(sb.ToString());
        }

        public void ShowMarkups()
        {
            var f = Form1._Form1;

            foreach (var c in Enumerable.Range(1, 9))
            {
                foreach (var r in Enumerable.Range(1, 9))
                {
                    SetMarkups(f, c, r);
                }
            }
        }

        private void SetMarkups(Form1 f, int c, int r)
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
                SavedFileName = openFileDialog1.FileName;
                CurrentGameState = FilterOutReturns(File.ReadAllText(openFileDialog1.FileName));
            }

            f.Text = SavedFileName;

            return CurrentGameState;
        }

        private bool IsMoveValid(int col, int row, int value)
        {

            var isValid = true;

            try
            {
                // scan through columns
                for (int r = 1; r <= 9; r++)
                {
                    if (CellValues[col, r] == value) // duplicate
                    {
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
                for (int c = 1; c <= 9; c++)
                {
                    if (CellValues[c, row] == value)
                    {
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

                for (int rr = 0; rr <= 2; rr++)
                {
                    for (int cc = 0; cc <= 2; cc++)
                    {
                        if (CellValues[startC + cc, startR + rr] == value) //duplicate
                        {
                            isValid = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return isValid;
        }

        public void ViewMarkups()
        {
            var sb = new StringBuilder();

            foreach (var r in Enumerable.Range(1, 9))
            {
                foreach (var c in Enumerable.Range(1, 9))
                {
                    var control = Form1._Form1.Controls.Find($"{c}{r}", true).FirstOrDefault();
                    var label = (CustomLabel)control;
                    if (label == null) return;
                    if (CellValues[c, r] == 0)
                    {
                        label.Font = new Font(SmallFontName, SmallFontSize, FontStyle.Bold);
                        sb.Append($"[{FilterOutReturns(CalculatePossibleValues(c, r).Trim()).Trim()}]");
                    }
                    else if (CellValues[c, r] > 0)
                    {
                        label.Font = new Font(LargeFontName, LargeFontSize, FontStyle.Bold);
                        sb.Append(CellValues[c, r]);
                    }
                }

                sb.AppendLine();
            }

            Form1._Form1.RichTextBox1.Text = sb.ToString();
        }

        public void SaveGameToDisk(bool saveAs)
        {
            // if savedFileName is empty then game has not been saved before
            if (SavedFileName == string.Empty || saveAs)
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
                        SavedFileName = saveFileDialog1.FileName;
                    }
                    else
                    {
                        return;
                    }
                }
            }

            // formulate the string representing the values to store
            var stringBuilder = new StringBuilder();
            foreach (var row in Enumerable.Range(1, 9))
                foreach (var col in Enumerable.Range(1, 9))
                {
                    stringBuilder.Append(CellValues[col, row].ToString());
                }

            // save the values to file
            try
            {
                var fileExists = File.Exists(SavedFileName);
                if (fileExists)
                {
                    File.Delete(SavedFileName);
                }

                File.WriteAllText(SavedFileName, stringBuilder.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void StartNewGame()
        {
            var f = Form1._Form1;
            f.StartTime = DateTime.Now;
            GameHasStarted = true;
            CurrentGameState = string.Empty;
            f.StartTimer();
            f.SetStatus2(@"New game started");
        }
    }
}