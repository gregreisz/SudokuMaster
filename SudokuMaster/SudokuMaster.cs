using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SudokuMaster
{
    public class CustomLabel : Label
    {
        public bool IsEraseable { get; set; }
    }

    public class Sudoku
    {
        public static Sudoku _Sudoku;

        public Sudoku()
        {
            _Sudoku = this;
        }

        #region Arrays for Sudoku Values

        // used to represent the values in the grid
        public int[,] Actual = new int[10, 10];

        public Stack<int[,]> ActualStack = new Stack<int[,]>();

        // stacks to keep track of all the moves
        public Stack<string> Moves;
        public string[,] PossibleValues = new string[10, 10];
        public Stack<string[,]> PossibleStack = new Stack<string[,]>();
        public Stack<string> RedoMoves;

        #endregion

        #region Public Properties

        public int Level { get; set; } = 1;

        public bool HintsMode { get; set; } = true;

        public bool ClueCellsAreLocked { get; set; }

        public string SaveFileName { get; set; }

        public string CurrentGameState { get; set; }

        // number the user selected from the toolStrip on enter into a cell
        public int SelectedNumber { get; set; }

        public int SelectedColumn { get; set; } = 1;

        public int SelectedRow { get; set; } = 1;

        public int CountOfClues { get; set; }

        public int Counter { get; set; }

        // indicate if the brute-force subroutine should stop
        private bool _bruteForceStop;
        private bool _gameStarted;
        private int _seconds;

        // has the game started
        public bool GameStarted
        {
            get => _gameStarted;
            set => _gameStarted = value;
        }

        public bool BruteForceStop
        {
            get => _bruteForceStop;
            set => _bruteForceStop = value;
        }

        public int Seconds
        {
            get => _seconds;
            set => _seconds = value;
        }

        public int[,] ActualValues
        {
            get => Actual;
            set => Actual = value;
        }

        #endregion

        #region Sudoku Board Constants

        // These colors are for cells with hint values are not eraseable.
        public readonly Color _fixedBackcolor = Color.LightSteelBlue;
        public readonly Color _fixedForecolor = Color.Blue;

        // these colors are for user inserted values which are eraseable.
        public readonly Color _userBackcolor = Color.LightYellow;
        public readonly Color _userForeColor = Color.Black;

        public readonly int labelSizeLarge = 10;
        public readonly string labelLargeFontName = "Verdana";

        public readonly string labelSmallFontName = "Consolas";
        public readonly int labelSizeSmall = 6;

        // This is the default back color for empty cells.
        public Color _defaultBackcolor = Color.White;

        // These are dimensions in pixels of each cell of the sudoku board.
        public int CellHeight = 32;
        public int CellWidth = 32;

        // This is the offset in pixels from the top-left corner of the window.
        public int XOffset = -20;
        public int YOffset = 25;

        #endregion

        public string FilterFileInput(string input)
        {
            return !string.IsNullOrEmpty(input)
                ? string.Join(string.Empty, Regex.Split(input, @"(?:\r\n|\n|\r)"))
                : string.Empty;
        }

        public void AddLabelsToBoard()
        {
            SelectedNumber = 1;

            // used to store the location of the cell
            var location = new Point();

            // add the labels
            foreach (var row in Enumerable.Range(1, 9))
                foreach (var col in Enumerable.Range(1, 9))
                {
                    location.X = col * (CellWidth + 1) + XOffset;
                    location.Y = row * (CellHeight + 1) + YOffset;
                    var label = new CustomLabel
                    {
                        Name = col.ToString() + row,
                        BorderStyle = BorderStyle.Fixed3D,
                        Location = location,
                        Width = CellWidth,
                        Height = CellHeight,
                        TextAlign = ContentAlignment.MiddleCenter,
                        IsEraseable = true,
                        BackColor = _userBackcolor,
                        ForeColor = _userForeColor
                    };
                    label.Click += (sender, e) => Form1._Form1.Cell_Click(sender);
                    Form1._Form1.Controls.Add(label);
                }
        }

        public void SudokuBoardHandler(object sender)
        {
            // check to see if game has started
            if (!GameStarted)
            {
                Console.Beep();
                Form1._Form1.SetText(@"Click File->New to start a new game or File->Open to load an existing game");
                return;
            }

            if (sender is CustomLabel label)
            {
                var col = SelectedColumn = int.Parse(label.Name.Substring(0, 1));
                var row = SelectedRow = int.Parse(label.Name.Substring(1, 1));

                var value = SelectedNumber;

                // if cell is not eraseable then return
                if (!label.IsEraseable)
                {
                    Console.Beep();
                    Form1._Form1.SetText(@"This cell cannot be erased.");
                    return;
                }

                try
                {
                    // if erasing a cell
                    if (value == 0)
                    {
                        // if cell is empty then no need to erase
                        if (ActualValues[SelectedColumn, SelectedRow] == 0)
                        {
                            return;
                        }

                        // save the value in the array
                        SetCell(col, row, value);
                        Form1._Form1.SetText($@"{value} erased at ({col},{row})");
                    }

                    else
                    {
                        // if move is not valid then return
                        if (!IsMoveValid(col, row, value))
                        {
                            Console.Beep();
                            Form1._Form1.SetStatus2 = $@"Invalid move at ({col},{row})";
                            return;
                        }

                        SetCell(col, row, value);
                        UpdateCurrentGameState(col, row, value);
                        RefreshGameBoard();

                        // save the value in the array
                        ActualValues[col, row] = value;

                        // saves the move into the stack
                        Moves.Push($"{label.Name}{value}");



                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

            if (IsPuzzleSolved())
            {
                Form1._Form1.SetStatus2 = @"*****Puzzle Solved*****";
            }
        }

        public void UpdateCurrentGameState(int col, int row, int value)
        {
            // do not use Counter here loops break out when they find the match
            var index = 0;
            var sb = new StringBuilder(CurrentGameState);
            Form1._Form1.SetText(CurrentGameState);
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
            Form1._Form1.SetText(CurrentGameState);
        }

        public bool IsPuzzleSolved()
        {
            string pattern;

            // check row by row
            for (var r = 1; r <= 9; r++)
            {
                pattern = "123456789";
                for (var c = 1; c <= 9; c++) pattern = pattern.Replace(ActualValues[c, r].ToString(), string.Empty);
                if (pattern.Length > 0)
                {
                    return false;
                }
            }

            // check col by col
            for (var c = 1; c <= 9; c++)
            {
                pattern = "123456789";
                for (var r = 1; r <= 9; r++) pattern = pattern.Replace(ActualValues[c, r].ToString(), string.Empty);
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
                            pattern = pattern.Replace(ActualValues[c + cc, r + rr].ToString(), string.Empty);
                if (pattern.Length > 0)
                {
                    return false;
                }
            }

            return true;
        }

        public string CalculatePossibleValues(int col, int row)
        {
            var str = string.IsNullOrEmpty(PossibleValues[col, row]) ? "123456789" : PossibleValues[col, row];

            int r;
            int c;
            const string space = " ";
            // Step (1) check by column
            for (r = 1; r <= 9; r++)
                if (ActualValues[col, r] != 0)
                {
                    // that means there is a actual value in it
                    str = str.Replace(ActualValues[col, r].ToString(), space);
                }

            // Step (2) check by row
            for (c = 1; c <= 9; c++)
                if (ActualValues[c, row] != 0)
                {
                    // that means there is a actual value in it
                    str = str.Replace(ActualValues[c, row].ToString(), space);
                }

            // Step (3) check within the minigrid
            var startC = col - (col - 1) % 3;
            var startR = row - (row - 1) % 3;
            for (var rr = startR; rr <= startR + 2; rr++)
                for (var cc = startC; cc <= startC + 2; cc++)
                {
                    if (ActualValues[cc, rr] != 0)
                    {
                        str = str.Replace(ActualValues[cc, rr].ToString(), space);
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
                    if (ActualValues[c, r] == 0)
                    {
                        sb.AppendLine($"({c},{r}) ({ActualValues[c, r]}) {CalculatePossibleValues(c, r)}");
                    }

                    if (ActualValues[c, r] > 0)
                    {
                        sb.AppendLine($"({c},{r}) ({ActualValues[c, r]}) {CalculatePossibleValues(c, r)}");
                    }

                    if (c % 9 == 0)
                    {
                        sb.AppendLine($@"row {r} {shortLine}");
                    }
                }

            sb.AppendLine();
            Form1._Form1.SetText(sb.ToString());
        }

        private void CheckValues()
        {
            var sb = new StringBuilder();
            foreach (var row in Enumerable.Range(1, 9))
            {
                foreach (var col in Enumerable.Range(1, 9))
                    sb.Append(PossibleValues[col, row] != null ? $"{PossibleValues[col, row]} " : $"{ActualValues[col, row]} ");

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
                    if (ActualValues[col, row] != 0)
                    {
                        continue;
                    }

                    try
                    {
                        PossibleValues[col, row] = CalculatePossibleValues(col, row);
                    }
                    catch (Exception)
                    {
                        throw new Exception("Invalid Move");
                    }

                    if (PossibleValues[col, row].Length != 1)
                    {
                        continue;
                    }

                    // number is confirmed
                    ActualValues[col, row] = int.Parse(PossibleValues[col, row]);
                    changes = true;

                    // accumulate the total score
                }

            CheckValues();
            return changes;
        }

        public void InitializeBoard()
        {
            Array.Clear(ActualValues, 0, ActualValues.Length);
            Array.Clear(PossibleValues, 0, PossibleValues.Length);

            // initialize the stacks
            Moves = new Stack<string>();
            RedoMoves = new Stack<string>();

            // initialize the cells in the board
            foreach (var row in Enumerable.Range(1, 9))
                foreach (var col in Enumerable.Range(1, 9))
                {
                    SetCell(col, row, 0);
                    ActualValues[col, row] = 0;
                    PossibleValues[col, row] = string.Empty;
                }
        }

        public void DisplayElapsedTime()
        {
            var ss = Seconds;
            int mm;

            //TODO Time Elapsed 00:00:05.28

            var label = Form1._Form1.toolStripStatusLabel1;

            if (Seconds >= 3600 && Seconds < 219600)
            {
                ss = Seconds % 3600;
                mm = Seconds / 3600;
                var hh = Seconds / 219600;
                string elapsedTime;
                if (mm.ToString().Length == 1)
                {
                    elapsedTime = hh + ":0" + mm;
                }
                else
                {
                    elapsedTime = hh + ":" + mm;
                }

                if (ss.ToString().Length == 1)
                {
                    elapsedTime += ":0" + ss;
                }
                else
                {
                    elapsedTime += ":" + ss + "." + ss / 10;
                }

                label.Text = $@"Time Elapsed {elapsedTime}";
            }

            else if (ss >= 60 && ss < 3600)
            {
                ss = Seconds % 60;
                mm = Seconds / 60;
                string elapsedTime;
                if (ss.ToString().Length == 1 && Seconds >= 60)
                {
                    elapsedTime = mm + ":" + "0" + ss;
                }
                else
                {
                    elapsedTime = mm + ":" + ss;
                }

                label.Text = $@"Time Elapsed {elapsedTime}";
            }
            else if (Seconds > 0 && Seconds < 60)
            {
                label.Text = $@"Time Elapsed 00:00:{ss}";
            }

            Seconds += 1;
        }

        public string ReadInSavedGame()
        {
            var contents = string.Empty;
            var fileDialog = new OpenFileDialog
            {
                Filter = @"SDO files (*.sdo)|*.sdo|All files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = false
            };

            if (fileDialog.ShowDialog() != DialogResult.OK)
            {
                return contents;
            }

            contents = File.ReadAllText(fileDialog.FileName);
            SaveFileName = fileDialog.FileName;

            return contents;
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
                    SetCell(col, row, value);
                }

            }

            for (var r = 1; r <= 9; r++)
            {
                for (var c = 1; c <= 9; c++)
                {
                    if (Actual[c, r] != 0)
                    {
                        continue;
                    }

                    var control = Form1._Form1.Controls.Find($"{c}{r}", true).FirstOrDefault();
                    var label = (Label)control;
                    if (label != null)
                    {
                        label.Text = CalculatePossibleValues(c, r);
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
            _Sudoku.SaveFileName = Form1._Form1.SetStatus = openFileDialog1.FileName;
            _Sudoku.CountOfClues = contents.Length - contents.Count(x => x == matchChar);
            _Sudoku.CurrentGameState = _Sudoku.FilterFileInput(contents);

            return contents;

        }

        public void SetToCurrentGameState()
        {
            var contents = CurrentGameState;
        }

        public bool IsMoveValid(int col, int row, int value)
        {
            var isValid = true;
            try
            {
                // scan through columns
                foreach (var r in Enumerable.Range(1, 9))
                    if (ActualValues[col, r] == value) // duplicate
                    {
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
                    if (ActualValues[c, row] == value)
                    {
                        isValid = false;
                    }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            try
            {
                // scan through mini-grids
                var startC = col - (col - 1) % 3;
                var startR = row - (row - 1) % 3;

                foreach (var rr in Enumerable.Range(0, 2))
                    foreach (var cc in Enumerable.Range(0, 2))
                        if (ActualValues[startC + cc, startR + rr] == value) // duplicate
                        {
                            isValid = false;
                        }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return isValid;
        }

        public void PaintBoard(PaintEventArgs e)
        {
            int y1, y2;

            // draw the horizontal lines
            var x1 = 1 * (CellWidth + 1) + XOffset - 1;
            var x2 = 9 * (CellWidth + 1) + XOffset + CellWidth;
            for (var r = 1; r <= 10; r += 3)
            {
                y1 = r * (CellHeight + 1) + YOffset - 1;
                y2 = y1;
                e.Graphics.DrawLine(Pens.Black, x1, y1, x2, y2);
            }

            // draw the vertical lines
            y1 = 1 * (CellHeight + 1) + YOffset - 1;
            y2 = 9 * (CellHeight + 1) + YOffset + CellHeight;
            for (var c = 1; c <= 10; c += 3)
            {
                x1 = c * (CellWidth + 1) + XOffset - 1;
                x2 = x1;
                e.Graphics.DrawLine(Pens.Black, x1, y1, x2, y2);
            }
        }

        public string SaveGameToDisk(bool saveAs)
        {
            // if saveFileName is empty, means game has not been saved before
            if (SaveFileName == string.Empty || saveAs)
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
                        SaveFileName = saveFileDialog1.FileName;
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
                    stringBuilder.Append(ActualValues[col, row].ToString());

            // save the values to file
            try
            {
                var fileExists = File.Exists(SaveFileName);
                if (fileExists)
                {
                    File.Delete(SaveFileName);
                }

                File.WriteAllText(SaveFileName, stringBuilder.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }

            return $@"Puzzle saved in {SaveFileName}";
        }

        public void SetCell(int col, int row, int value)
        {
            // save the value in the array
            ActualValues[col, row] = value;
            var s = CalculatePossibleValues(col, row);

            var lf = Environment.NewLine;
            var possibleValues = string.Format("{0}{1}{2}{1}{3}", s.Substring(0, 3), lf, s.Substring(3, 3), s.Substring(6, 3));

            // locate the CustomLabel control
            var control = Form1._Form1.Controls.Find($"{col}{row}", true).FirstOrDefault();
            var label = (CustomLabel)control;
            if (label == null)
            {
                return;
            }

            // if erasing a cell, you need to reset the values for all cells
            if (value > 0)
            {
                // this has to show a start clue
                label.IsEraseable = false;
                label.BackColor = _fixedBackcolor;
                label.ForeColor = _fixedForecolor;
                label.Font = new Font(labelLargeFontName, labelSizeLarge, label.Font.Style | FontStyle.Bold);
                label.Text = value.ToString();
                Counter++;
                Form1._Form1.SetText($"{Counter} was from ({col},{row}) value was {value} and condition was value  > 0.".PadLeft(59).PadRight(59));
            }
            else if (value == 0)
            {
                label.BackColor = _userBackcolor;
                label.ForeColor = _userForeColor;
                label.Font = new Font(labelSmallFontName, labelSizeSmall, label.Font.Style | FontStyle.Bold);
                label.Text = possibleValues;
                Counter++;
                Form1._Form1.SetText($"{Counter} was from ({col},{row}) value was {value} and condition was value == 0.".PadLeft(59).PadRight(59));
            }
            else if (value > 0 && label.IsEraseable)
            {
                label.BackColor = _userBackcolor;
                label.ForeColor = _userForeColor;
                label.Font = new Font(labelLargeFontName, labelSizeLarge, label.Font.Style | FontStyle.Bold);
                label.Text = value.ToString();
                Counter++;
                Form1._Form1.SetText($"{Counter} was from ({col},{row}) value was {value} and condition was value == 0.".PadLeft(59).PadRight(59));

            }

        }

        private string RefreshPossibleValues(int col, int row)
        {
            var p = CalculatePossibleValues(col, row);
            FilterFileInput(p);
            var lf = Environment.NewLine;
            p = $"{p.Substring(0, 3)}{lf}{p.Substring(3, 3)}{lf}{p.Substring(6, 3)}";
            return p;
        }

        public void RefreshAllPossiblesValues()
        {
            var line = new string('*', 47);
            var sb = new StringBuilder { Length = 0 };

            sb.AppendLine(line);
            foreach (var col in Enumerable.Range(1, 9))
                foreach (var row in Enumerable.Range(1, 9))
                {
                    var control = Form1._Form1.Controls.Find($"{col}{row}", true).FirstOrDefault();
                    var label = (CustomLabel)control;
                    if (label == null)
                    {
                        throw new Exception($"The value of ({col},{row}) was null.");
                    }


                    if (!label.IsEraseable)
                    {
                        label.BackColor = _userBackcolor;
                        label.ForeColor = _userForeColor;

                        if (ActualValues[col, row] == 0)
                        {
                            label.Font = new Font(labelSmallFontName, labelSizeSmall, label.Font.Style | FontStyle.Bold);
                            var possibles = label.Text = RefreshPossibleValues(col, row);
                            sb.AppendLine($"({col},{row}) ({ActualValues[col, row]}) ({possibles})");
                        }
                        else if (ActualValues[col, row] > 0)
                        {
                            label.Font = new Font(labelLargeFontName, labelSizeLarge, label.Font.Style | FontStyle.Bold);
                            label.Text = ActualValues[col, row].ToString();
                            sb.AppendLine($"({col},{row}) ({ActualValues[col, row]})");
                        }
                    }

                    if (row % 9 == 0)
                    {
                        sb.AppendLine(line);
                    }
                }

            Form1._Form1.SetText(sb.ToString());
        }

        public void SetMenuItemChecked(ToolStripMenuItem menuItem)
        {
            var form = Form1._Form1;
            foreach (ToolStripMenuItem item in form.menuStrip1.Items)
            {
                if (item.Name.Length == 0)
                {
                    continue;
                }

                foreach (ToolStripMenuItem subItem in item.DropDownItems)
                    subItem.Checked = subItem.Name == menuItem.Name;
            }
        }

        public void SetLevel(string menuItemName)
        {
            switch (menuItemName)
            {
                case "EasyToolStripMenuItem":
                    Level = 1;
                    break;
                case "MediumToolStripMenuItem":
                    Level = 2;
                    break;
                case "HardToolStripMenuItem":
                    Level = 3;
                    break;
                case "ExpertToolStripMenuItem":
                    Level = 4;
                    break;
                default:
                    Level = 1;
                    break;
            }
        }

        public void StartNewGame()
        {
            Seconds = 0;
            ClueCellsAreLocked = false;
            CountOfClues = 0;
            Counter = 0;
            GameStarted = true;
            Form1._Form1.timer1.Enabled = true;
            Form1._Form1.SetStatus2 = @"New game started";
        }

    }
}