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

        public string AlternateText { get; set; }
    }

    public class Sudoku
    {
        public static Sudoku _Sudoku;

        public Sudoku()
        {
            _Sudoku = this;
        }

        //public Board board = new Board();


        #region Arrays for Sudoku Values

        // used to represent the values in the grid
        public int[,] Actual = new int[10, 10];
        public int[,] ActualBackup = new int[10, 10];

        // used to store the state of the grid
        public Stack<int[,]> ActualStack = new Stack<int[,]>();

        // stacks to keep track of all the moves
        public Stack<string> Moves;
        public string[,] Possible = new string[10, 10];
        public Stack<string[,]> PossibleStack = new Stack<string[,]>();
        public Stack<string> RedoMoves;

        #endregion

        #region Public Properties

        public int Level { get; set; } = 1;

        public bool HintsMode { get; set; } = true;

        public string SaveFileName { get; set; }

        // number the user selected from the toolStrip on enter into a cell
        public int SelectedNumber { get; set; }

        public int SelectedColumn { get; set; } = 1;

        public int SelectedRow { get; set; } = 1;

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

        #endregion

        #region Sudoku Board Constants

        // These colors are for cells with hint values are not eraseable.
        public readonly Color _fixedBackcolor = Color.LightSteelBlue;
        public readonly Color _fixedForecolor = Color.Blue;

        // these colors are for user inserted values which are eraseable.
        public readonly Color _userBackcolor = Color.LightYellow;
        public readonly Color _userForecolor = Color.Black;

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

        public void AddCellLabelsToBoard()
        {
            SelectedNumber = 1;
            var form = Form1._Form1;

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
                        BackColor = _defaultBackcolor
                    };
                    label.Font = new Font(labelLargeFontName, labelSizeLarge, label.Font.Style | FontStyle.Bold);
                    label.IsEraseable = true;
                    label.Click += form.Cell_Click;
                    form.Controls.Add(label);
                }
        }

        public void SudokuBoardHandler(object sender)
        {
            var form = Form1._Form1;

            // check to see if game has started
            if (!GameStarted)
            {
                Console.Beep();
                form.SetText(@"Click File->New to start a new game or File->Open to load an existing game");
                return;
            }

            var label = (CustomLabel)sender;
            var col = SelectedColumn = int.Parse(label.Name.Substring(0, 1));
            var row = SelectedRow = int.Parse(label.Name.Substring(1, 1));
            var value = SelectedNumber;

            RefreshAllPossiblesValues();


            // if cell is not eraseable then exit
            if (!label.IsEraseable)
            {
                Console.Beep();
                form.SetText(@"This cell cannot be erased.");
                return;
            }

            try
            {
                // if erasing a cell
                if (value == 0)
                {
                    // if cell is empty then no need to erase
                    if (Actual[SelectedColumn, SelectedRow] == 0)
                    {
                        return;
                    }

                    // save the value in the array
                    SetCell(col, row, value);

                    form.SetText($@"{value} erased at ({col},{row})");
                }
                else if (label.AlternateText == string.Empty)
                {
                    // else setting a value; check if move is valid
                    if (!IsMoveValid(col, row, value))
                    {
                        Console.Beep();
                        form.SetText($@"Invalid move at ({col},{row})");
                        return;
                    }

                    SetCell(col, row, value);

                    // save the value in the array
                    Actual[col, row] = value;

                    // saves the move into the stack
                    Moves.Push($"{label.Name}{value}");


                    form.timer1.Enabled = false;
                    Console.Beep();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            form.SetStatus2 = string.Empty;

            if(IsPuzzleSolved())
            {
               form.SetStatus2 = @"*****Puzzle Solved*****";
            }

            form.TxtActivities.Clear();
            form.TxtActivities.SelectionLength = 0;
            _Sudoku.RefreshAllPossiblesValues();
        }

        public bool IsPuzzleSolved()
        {
            string pattern;

            // check row by row
            for (int r = 1; r <= 9; r++)
            {
                pattern = "123456789";
                for (int c = 1; c <= 9; c++)
                {
                    pattern = pattern.Replace(Actual[c, r].ToString(), string.Empty);
                }
                if (pattern.Length > 0)
                {
                    return false;
                }
            }

            // check col by col
            for (int c = 1; c <= 9; c++)
            {
                pattern = "123456789";
                for (int r = 1; r <= 9; r++)
                {
                    pattern = pattern.Replace(Actual[c, r].ToString(), string.Empty);
                }
                if (pattern.Length > 0)
                {
                    return false;
                }
            }

            // check by minigrid
            for (int c = 1; c <= 9; c += 3)
            {
                pattern = "123456789";
                for (int r = 1; r <= 9; r += 3)
                {
                    for (int cc = 0; cc <= 2; cc++)
                    {
                        for (int rr = 0; rr <= 2; rr++)
                        {
                            pattern = pattern.Replace(Actual[c + cc, r + rr].ToString(), string.Empty);
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

        public string CalculatePossibles(int col, int row, bool allowEmptyString = false)
        {
            var s = "123456789";
            const string spacer = " ";
            // Step (1) check by column
            foreach (var r in Enumerable.Range(1, 9))
                if (Actual[col, r] != 0)
                {
                    // that means there is a actual value in it
                    s = s.Replace(Actual[col, r].ToString(), spacer);
                }

            // Step (2) check by row
            foreach (var c in Enumerable.Range(1, 9))
                if (Actual[c, row] != 0)
                {
                    // that means there is a actual value in it
                    s = s.Replace(Actual[c, row].ToString(), spacer);
                }

            // Step (3) check within the minigrid
            var startC = col - (col - 1) % 3;
            var startR = row - (row - 1) % 3;
            for (var rr = startR; rr <= startR + 2; rr++)
            {
                for (var cc = startC; cc <= startC + 2; cc++)
                {
                    if (Actual[cc, rr] != 0)
                    {
                        s = s.Replace(Actual[cc, rr].ToString(), spacer);
                    }
                }
            }

            // if possible value is string.Empty, then error
            if (s == string.Empty && !allowEmptyString)
            {
                throw new Exception("Invalid Move");
            }

            Possible[col, row] = s;
            return s;
        }

        public void CheckCandidates()
        {
            var possibleValues = new string[10, 10];
            var form = Form1._Form1;
            var line = new string('*', 47);
            var shortLine = new string('*', 38);
            var sb = new StringBuilder();
            sb.AppendLine(line);
            for (int r = 1; r <= 9; r++)
            {
                for (int c = 1; c <= 9; c++)
                {
                    possibleValues[c, r] = CalculatePossibles(c, r, true);
                    sb.AppendLine($"({c},{r}) ({Actual[c, r]}) {possibleValues[c, r].Replace(" ", string.Empty)}");
                    if (c % 9 == 0) sb.AppendLine($@"row {r} {shortLine}");
                }
            }

            sb.AppendLine();
            form.SetText(sb.ToString());
        }

        private void CheckValues()
        {
            var form = Form1._Form1;
            var sb = new StringBuilder();
            foreach (var row in Enumerable.Range(1, 9))
            {
                foreach (var col in Enumerable.Range(1, 9))
                {
                    sb.Append(Possible[col, row] != null ? $"{Possible[col, row]} " : $"{Actual[col, row]} ");
                }

                sb.AppendLine();
            }

            form.SetText(sb.ToString());
        }

        public bool CheckColumnsAndRows()
        {
            var changes = false;

            // check all cells
            foreach (var row in Enumerable.Range(1, 9))
                foreach (var col in Enumerable.Range(1, 9))
                {
                    if (Actual[col, row] != 0)
                    {
                        continue;
                    }

                    try
                    {
                        Possible[col, row] = CalculatePossibles(col, row);
                    }
                    catch (Exception)
                    {
                        throw new Exception("Invalid Move");
                    }

                    if (Possible[col, row].Length != 1)
                    {
                        continue;
                    }

                    // number is confirmed
                    Actual[col, row] = int.Parse(Possible[col, row]);
                    changes = true;

                    // accumulate the total score
                }

            CheckValues();
            return changes;
        }

        public void ClearBoard()
        {
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

        public void DisplayElapsedTime()
        {
            int ss = Seconds;
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
                    elapsedTime += ":" + ss + "." + ss/10;
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
            string contents = string.Empty;
            var form = Form1._Form1;
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
            form.SetStatus = fileDialog.FileName;
            SaveFileName = fileDialog.FileName;
            return contents;
        }

        public bool LoadSavedGame(string fileContents)
        {
            var contents = string.Join(string.Empty, Regex.Split(fileContents, @"(?:\r\n|\n|\r)"));
            int counter = 0;
            var isLoaded = false;
            foreach (var row in Enumerable.Range(1, 9))
            {
                foreach (var col in Enumerable.Range(1, 9))
                {
                    var value = int.Parse(contents[counter].ToString());
                    SetCell(col, row, value);
                    counter++;
                }
            }

            if (counter == 81)
            {
                isLoaded = true;
            }
            RefreshAllPossiblesValues();
            return isLoaded;
        }

        public bool IsMoveValid(int col, int row, int value)
        {
            var isValid = true;
            try
            {
                // scan through columns
                foreach (var r in Enumerable.Range(1, 9))
                    if (Actual[col, r] == value) // duplicate
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
                    if (Actual[c, row] == value)
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
                {
                    foreach (var cc in Enumerable.Range(0, 2))
                    {
                        if (Actual[startC + cc, startR + rr] == value) // duplicate
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
            var str = new StringBuilder();
            foreach (var row in Enumerable.Range(1, 9))
                foreach (var col in Enumerable.Range(1, 9))
                    str.Append(Actual[col, row].ToString());

            // save the values to file
            try
            {
                var fileExists = File.Exists(SaveFileName);
                if (fileExists)
                {
                    File.Delete(SaveFileName);
                }

                File.WriteAllText(SaveFileName, str.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }

            return $@"Puzzle saved in {SaveFileName}";
        }

        public void SetCell(int col, int row, int value, bool isEraseable = false)
        {
            var form = Form1._Form1;

            // save the value in the array
            Actual[col, row] = value;

            // locate the CustomLabel control
            var control = form.Controls.Find($"{col}{row}", true).FirstOrDefault();
            var label = (CustomLabel)control;

            if (label == null) return;
                label.Text = RefreshPossibleValues(col, row);

            label.Font = new Font(labelLargeFontName, labelSizeLarge, label.Font.Style | FontStyle.Bold);
            // if erasing a cell, you need to reset the values for all cells
            // set the appearance for the CustomLabel control
            if (value == 0)
            {
                // this cell can show the potential values for the cell
                label.IsEraseable = true;
                label.BackColor = _userBackcolor;
                label.ForeColor = _userForecolor;
                label.Font = new Font(labelSmallFontName, labelSizeSmall, label.Font.Style | FontStyle.Bold);
                label.AlternateText = string.Empty;
            }
            else if (value > 0 && !isEraseable)
            {
                // this has to show a start clue
                label.BackColor = _fixedBackcolor;
                label.ForeColor = _fixedForecolor;
                label.Font = new Font(labelLargeFontName, labelSizeLarge, label.Font.Style | FontStyle.Bold);

                label.Text = value.ToString();
                label.AlternateText = null;
            }

        }

        private string RefreshPossibleValues(int col, int row)
        {
            var p = CalculatePossibles(col, row, true);
            var lf = Environment.NewLine;
            p = $"{p.Substring(0, 3)}{lf}{p.Substring(3, 3)}{lf}{p.Substring(6, 3)}";
            return p;
        }

        public void RefreshAllPossiblesValues()
        {
            var form = Form1._Form1;
            var lf = Environment.NewLine;
            var line = new string('*', 47);
            var sb = new StringBuilder {Length = 0};

            sb.AppendLine(line);
            foreach (var col in Enumerable.Range(1, 9))
            {
                foreach (var row in Enumerable.Range(1, 9))
                {
                    var control = form.Controls.Find($"{col}{row}", true).FirstOrDefault();
                    var label = (CustomLabel)control;
                    if (label == null) throw new Exception($"The value of ({col},{row}) was null.");

                    if (Actual[col, row] == 0 && label.IsEraseable)
                    {
                        label.Font = new Font(labelSmallFontName, labelSizeSmall, label.Font.Style | FontStyle.Bold);
                        label.Text = string.Empty;
                        var p = CalculatePossibles(col, row, true);
                        label.Text = $@"{p.Substring(0, 3)}{lf}{p.Substring(3, 3)}{lf}{p.Substring(6, 3)}";
                        sb.AppendLine($"({col},{row}) ({Actual[col, row]}) ({p})");
                    }
                    else if (Actual[col, row] > 0 && !label.IsEraseable)
                    {
                        label.Font = new Font(labelLargeFontName, labelSizeLarge, label.Font.Style | FontStyle.Bold);
                        label.Text = Actual[col, row].ToString();
                        sb.AppendLine($"({col},{row}) ({Actual[col, row]})");

                    }
                    if (row % 9 == 0) sb.AppendLine(line);
                }
            }

            form.SetText(sb.ToString());
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


        public void StartGame()
        {
            var form = Form1._Form1;
            SaveFileName = string.Empty;
            form.TxtActivities.Clear();
            Seconds = 0;
            ClearBoard();
            GameStarted = true;
            form.timer1.Enabled = true;
            form.toolStripStatusLabel1.Text = @"New game started";
            form.toolTip1.RemoveAll();
        }

    }
}
