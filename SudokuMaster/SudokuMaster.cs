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

        public string SavedFileName { get; set; }

        public string CurrentGameState { get; set; }

        public string TransformPossibleValues(string possibleValues)
        {
            var lf = Environment.NewLine;
            var s = possibleValues;
            return string.Format("{0}{1}{2}{1}{3}", s.Substring(0, 3), lf, s.Substring(3, 3), s.Substring(6, 3));
        }

        public int SelectedColumn { get; set; } = 1;

        public int SelectedRow { get; set; } = 1;

        public int CountOfClues { get; set; }

        public int Counter { get; set; }

        public string FilterFileInput(string input)
        {
            return !string.IsNullOrEmpty(input)
                ? string.Join(string.Empty, Regex.Split(input, @"(?:\r\n|\n|\r)"))
                : string.Empty;
        }

        public void SudokuBoardHandler(object sender)
        {
            // check to see if game has started
            if (Form1._Form1.GameStartTime > DateTime.Now)
            {
                Form1._Form1.SetStatus(@"Click File->New to start a new game or File->Open to load an existing game", true);
                return;
            }

            if (sender is CustomLabel label)
            {
                var col = SelectedColumn = int.Parse(label.Name.Substring(0, 1));
                var row = SelectedRow = int.Parse(label.Name.Substring(1, 1));
                var value = Form1._Form1.SelectedNumber;

                // if cell is not eraseable then return
                if (label.IsEraseable == false)
                {
                    Form1._Form1.SetText(@"This cell cannot be erased.");
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
                        Form1._Form1.SetCell(col, row, value);
                        Form1._Form1.SetText($@"{value} erased at ({col},{row})");
                    }

                    else
                    {
                        // if move is not valid then return
                        if (!IsMoveValid(col, row, value))
                        {
                            Form1._Form1.SetText($@"Invalid move at ({col},{row})");
                            return;
                        }

                        Form1._Form1.SetCell(col, row, value);
                        Form1._Form1.SetText($"Saved {value} to ({col},{row}) successfully.");


                        _log.Info($"Saved {value} to ({col},{row}) successfully.");
                        // save the value in the array
                        CellValues[col, row] = value;

                        // saves the move into the stack
                        Moves.Push($"{label.Name} {col}{row} pushed onto Moves stack.");

                        UpdateNotes();

                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

            if (IsPuzzleSolved())
            {
                Form1._Form1.SetStatus2(@"*****Puzzle Solved*****", true);
            }
        }

        public void UpdateCurrentGameState(int col, int row, int value)
        {
            // do not use Counter here loops break out when they find the match
            var index = 0;
            var sb = new StringBuilder(CurrentGameState);
            foreach (var r in Enumerable.Range(1, 9))
            {
                foreach (var c in Enumerable.Range(1, 9))
                {

                    if (c == col && r == row)
                    {
                        sb.Remove(index, 1);
                        sb.Insert(index, value);
                        goto BreakLoops;
                    }
                    index++;
                }
            }

            BreakLoops:
            CurrentGameState = sb.ToString();
        }

        //public bool IsPuzzleSolved()
        //{
        //    string pattern;

        //    // check row by row
        //    for (var r = 1; r <= 9; r++)
        //    {
        //        pattern = "123456789";
        //        for (var c = 1; c <= 9; c++) pattern = pattern.Replace(CellValues[c, r].ToString(), string.Empty);
        //        if (pattern.Length > 0)
        //        {
        //            return false;
        //        }
        //    }

        //    // check col by col
        //    for (var c = 1; c <= 9; c++)
        //    {
        //        pattern = "123456789";
        //        for (var r = 1; r <= 9; r++) pattern = pattern.Replace(CellValues[c, r].ToString(), string.Empty);
        //        if (pattern.Length > 0)
        //        {
        //            return false;
        //        }
        //    }

        //    // check by minigrid
        //    for (var c = 1; c <= 9; c += 3)
        //    {
        //        pattern = "123456789";
        //        for (var r = 1; r <= 9; r += 3)
        //            for (var cc = 0; cc <= 2; cc++)
        //                for (var rr = 0; rr <= 2; rr++)
        //                    pattern = pattern.Replace(CellValues[c + cc, r + rr].ToString(), string.Empty);
        //        if (pattern.Length > 0)
        //        {
        //            return false;
        //        }
        //    }

        //    return true;
        //}

        public string CalculatePossibleValues(int col, int row)
        {
            var str = string.IsNullOrEmpty(Candidates[col, row]) ? "123456789" : Candidates[col, row];

            int r;
            int c;
            const string space = " ";
            // Step (1) check by column
            for (r = 1; r <= 9; r++)
                if (CellValues[col, r] != 0)
                {
                    // that means there is a actual value in it
                    str = str.Replace(CellValues[col, r].ToString(), space);
                }

            // Step (2) check by row
            for (c = 1; c <= 9; c++)
                if (CellValues[c, row] != 0)
                {
                    // that means there is a actual value in it
                    str = str.Replace(CellValues[c, row].ToString(), space);
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

            if (str.Length != 9)
            {
                throw new Exception("Invalid Move");
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

        public bool CheckColumnsAndRows()
        {
            var changes = false;

            // check all cells
            foreach (var row in Enumerable.Range(1, 9))
                foreach (var col in Enumerable.Range(1, 9))
                {
                    if (CellValues[col, row] != 0)
                    {
                        continue;
                    }

                    try
                    {
                        Candidates[col, row] = CalculatePossibleValues(col, row);
                    }
                    catch (Exception)
                    {
                        throw new Exception("Invalid Move");
                    }

                    if (Candidates[col, row].Length != 1)
                    {
                        continue;
                    }

                    // number is confirmed
                    CellValues[col, row] = int.Parse(Candidates[col, row]);
                    changes = true;

                    // accumulate the total score
                }

            CheckValues();
            return changes;
        }

        public void InitializeBoard()
        {
            Array.Clear(CellValues, 0, CellValues.Length);
            Array.Clear(Candidates, 0, Candidates.Length);

            // initialize the stacks
            Moves = new Stack<string>();
            RedoMoves = new Stack<string>();

            // initialize the cells in the board
            foreach (var row in Enumerable.Range(1, 9))
                foreach (var col in Enumerable.Range(1, 9))
                {
                    Form1._Form1.SetCell(col, row, 0);
                    CellValues[col, row] = 0;
                    Candidates[col, row] = string.Empty;
                }
        }

        public void RefreshGameBoard()
        {

            var contents = CurrentGameState;

            // set up the board with the current game state
            Counter = 0;
            var counter = 0;
            foreach (var row in Enumerable.Range(1, 9))
            {
                foreach (var col in Enumerable.Range(1, 9))
                {
                    var value = int.Parse(contents[counter].ToString());
                    counter++;
                    Form1._Form1.SetCell(col, row, value);
                }

            }

            for (var r = 1; r <= 9; r++)
            {
                for (var c = 1; c <= 9; c++)
                {
                    if (Cell[c, r] != 0)
                    {
                        continue;
                    }

                    var control = Form1._Form1.Controls.Find($"{c}{r}", true).FirstOrDefault();
                    var label = (Label)control;
                    if (label != null)
                    {
                        label.Text = TransformPossibleValues(CalculatePossibleValues(c, r));
                    }
                }
            }

        }

        public void UpdateNotes()
        {
            foreach (var r in Enumerable.Range(1, 9))
            {
                foreach (var c in Enumerable.Range(1, 9))
                {
                    var control = Form1._Form1.Controls.Find($"{c}{r}", true).FirstOrDefault();
                    var label = (CustomLabel)control;

                    if (CellValues[c, r] != 0 && label != null)
                    {
                        label.Font = new Font(LargeFontName, LargeFontSize, FontStyle.Bold);
                    }

                    if (CellValues[c, r] == 0 && label != null)
                    {
                        label.Font = new Font(SmallFontName, SmallFontSize, FontStyle.Bold);
                        label.Text = TransformPossibleValues(CalculatePossibleValues(c, r));
                    }
                }
            }

        }

        public string LoadGameFromDisk()
        {
            var openFileDialog1 = new OpenFileDialog
            {
                Filter = @"SDO files (*.sdo)|*.sdo|All files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = false
            };


            var contents = string.Empty;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                contents = _Sudoku.FilterFileInput(File.ReadAllText(openFileDialog1.FileName));
            }

            const char matchChar = '0';
            Form1._Form1.Text = _Sudoku.SavedFileName = openFileDialog1.FileName;
            _Sudoku.CountOfClues = contents.Length - contents.Count(x => x == matchChar);
            _Sudoku.CurrentGameState = _Sudoku.FilterFileInput(contents);

            return contents;

        }

        public bool IsMoveValid(int col, int row, int value)
        {
            var isValid = true;
            try
            {
                // scan through columns
                foreach (var r in Enumerable.Range(1, 9))
                    if (CellValues[col, r] == value) // duplicate
                    {
                        Form1._Form1.SetStatus2($"Move to {col},{r} found to be invalid while scanning columns.", true);
                        isValid = false;

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
                    if (CellValues[c, row] == value)
                    {
                        Form1._Form1.SetStatus2($"Move to {c},{row} found to be invalid while scanning rows.", true);

                        isValid = false;
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
                    foreach (var cc in Enumerable.Range(0, 2))
                        if (CellValues[startC + cc, startR + rr] == value) // duplicate
                        {
                            Form1._Form1.SetStatus2($"Move to {startC + cc},{startR + rr} found to be invalid while scanning boxes.", true);
                            isValid = false;
                        }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return isValid;
        }

        public bool IsPuzzleSolved(int col, int row, int value)
        {

            // scan through columns
            foreach (var r in Enumerable.Range(1, 9))
            {
                if (CellValues[col, r] == value) // duplicate
                {
                    return false;
                }
            }

            // scan through rows
            foreach (var c in Enumerable.Range(1, 9))
            {
                if (CellValues[c, row] == value)
                {
                    return false;
                }
            }

            // scan through regions
            var startC = col - (col - 1) % 3;
            var startR = row - (row - 1) % 3;

            foreach (var rr in Enumerable.Range(0, 2))
            {
                foreach (var cc in Enumerable.Range(0, 2))
                {
                    if (CellValues[startC + cc, startR + rr] == value) // duplicate
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public string SaveGameToDisk(bool saveAs)
        {
            // if saveFileName is empty, means game has not been saved before
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
                        return null;
                    }
                }
            }

            // formulate the string representing the values to store
            var stringBuilder = new StringBuilder();
            foreach (var row in Enumerable.Range(1, 9))
                foreach (var col in Enumerable.Range(1, 9))
                    stringBuilder.Append(CellValues[col, row].ToString());

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
                return null;
            }

            return $@"Puzzle saved in {SavedFileName}";
        }



    }
}

