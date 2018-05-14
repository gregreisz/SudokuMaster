﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.VisualBasic;

namespace SudokuMaster
{
    public class CustomLabel : Label
    {
        public bool IsEraseable { get; set; }

        public string AlternateText { get; set; }
    }

    public class Sudoku
    {

        #region Arrays for Sudoku Grid Values

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

        public bool HintsMode { get; set; } = false;

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

        public void AddCellLabels()
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
                    label.Font = new Font("Consolas", labelSizeLarge, label.Font.Style | FontStyle.Bold);
                    label.IsEraseable = true;
                    label.Click += form.Cell_Click;
                    form.Controls.Add(label);
                }
        }

        public void SudokuBoardHandler(object sender, EventArgs e)
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

            // if cell is not eraseable then exit
            if (label.IsEraseable == false)
            {
                Console.Beep();
                form.SetText(@"This cell cannot be erased.");
                return;
            }

            try
            {
                // if erasing a cell
                if (SelectedNumber == 0)
                {
                    // if cell is empty then no need to erase
                    if (Actual[SelectedColumn, SelectedRow] == 0)
                    {
                        return;
                    }

                    // save the value in the array
                    SetCell(col, row, SelectedNumber);
                    form.SetText($@"{SelectedNumber} erased at ({col},{row})");
                }
                else if (label.Text == string.Empty)
                {
                    // else setting a value; check if move is valid
                    if (!IsMoveValid(col, row, SelectedNumber))
                    {
                        Console.Beep();
                        form.SetText($@"Invalid move at ({col},{row})");
                        return;
                    }

                    // save the value in the array
                    SetCell(col, row, SelectedNumber);

                    // saves the move into the stack
                    Moves.Push($"{label.Name}{SelectedNumber}");

                    if (!IsPuzzleSolved())
                    {
                        return;
                    }

                    form.timer1.Enabled = false;
                    Console.Beep();
                    form.SetText(@"*****Puzzle Solved*****");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
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

            return s;
        }

        public string[,] CheckCandidates()
        {
            var possibleValues = new string[10, 10];

            // check row by row in grid
            foreach (var r in Enumerable.Range(1, 9))
                // check column by column in grid
                foreach (var c in Enumerable.Range(1, 9))
                    possibleValues[c, r] = CalculatePossibles(c, r, true);

            return possibleValues;
        }

        private void CheckValues()
        {
            // print results
            var sb = new StringBuilder();
            foreach (var row in Enumerable.Range(1, 9))
            {
                foreach (var col in Enumerable.Range(1, 9))
                    sb.Append(Possible[col, row] != null ? $"{Possible[col, row]} " : $"{Actual[col, row]} ");

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
                foreach (var col in Enumerable.Range(1, 9))
                    SetCell(col, row, 0);
        }

        private void CreateEmptyCells(int empty)
        {
            var random = new Random();

            // choose random locations for empty cells
            var emptyCells = new string[empty];
            for (var i = 1; i <= empty / 2; i++)
            {
                bool duplicate;
                do
                {
                    duplicate = false;
                    // get a cell in the first half of the grid
                    int r;
                    int c;
                    do
                    {
                        c = random.Next(1, 10);
                        r = random.Next(1, 6);
                    } while ((r == 5) & (c > 5));

                    for (var j = 0; j <= i; j++)
                    {
                        // if cell is already selected to be empty
                        if (emptyCells[j] != $"{c}{r}")
                        {
                            continue;
                        }

                        duplicate = true;
                        break;
                    }

                    if (duplicate)
                    {
                        continue;
                    }

                    // set the empty cell
                    emptyCells[i] = $"{c}{r}";
                    Actual[c, r] = 0;
                    Possible[c, r] = string.Empty;


                    // reflect the top half of the grid and make it symmetrical
                    emptyCells[empty - 1 - i] = (10 - c).ToString() + (10 - r);
                    Actual[10 - c, 10 - r] = 0;
                    Possible[10 - c, 10 - r] = string.Empty;
                } while (duplicate);
            }
        }

        public void CreateMainMenu()
        {
            var form = Form1._Form1;
            // create the File menu
            var fileItem = new ToolStripMenuItem("&File");

            var newSubItem = new ToolStripMenuItem("&New");
            newSubItem.Click += form.NewToolStripMenuItem_Click;

            var openSubItem = new ToolStripMenuItem("&Open");
            openSubItem.Click += form.OpenToolStripMenuItem_Click;

            var saveSubItem = new ToolStripMenuItem("&Save");
            saveSubItem.Click += form.SaveToolStripMenuItem_Click;

            var saveAsSubItem = new ToolStripMenuItem("Save&As");
            saveAsSubItem.Click += form.SaveAsToolStripMenuItem_Click;

            var exitSubItem = new ToolStripMenuItem("E&xit");
            exitSubItem.Click += form.ExitToolStripMenuItem_Click;

            fileItem.DropDownItems.Add(newSubItem);
            fileItem.DropDownItems.Add(openSubItem);
            fileItem.DropDownItems.Add(new ToolStripSeparator());
            fileItem.DropDownItems.Add(saveSubItem);
            fileItem.DropDownItems.Add(saveAsSubItem);
            fileItem.DropDownItems.Add(new ToolStripSeparator());
            fileItem.DropDownItems.Add(exitSubItem);

            // create the Edit menu
            var editItem = new ToolStripMenuItem("&Edit");
            var undoSubItem = new ToolStripMenuItem("&Undo");
            undoSubItem.Click += form.UndoToolStripMenuItem_Click;

            var redoSubItem = new ToolStripMenuItem("&Redo");
            redoSubItem.Click += form.RedoToolStripMenuItem_Click;

            editItem.DropDownItems.Add(undoSubItem);
            editItem.DropDownItems.Add(redoSubItem);

            // create the Level menu
            var levelItem = new ToolStripMenuItem("&Level") { Name = "LevelMenuItem" };
            var easyToolStripMenuItem = new ToolStripMenuItem("&Easy") { Name = "EasyToolStripMenuItem" };
            easyToolStripMenuItem.Click += form.EasyToolStripMenuItem_Click;

            var mediumToolStripMenuItem = new ToolStripMenuItem("&Medium") { Name = "MediumToolStripMenuItem" };
            mediumToolStripMenuItem.Click += form.MediumToolStripMenuItem_Click;

            var hardToolStripMenuItem = new ToolStripMenuItem("&Hard") { Name = "HardToolStripMenuItem" };
            hardToolStripMenuItem.Click += form.HardToolStripMenuItem_Click;

            var expertToolStripMenuItem = new ToolStripMenuItem("E&xpert") { Name = "ExpertToolStripMenuItem" };
            expertToolStripMenuItem.Click += form.ExpertToolStripMenuItem_Click;

            levelItem.DropDownItems.Add(easyToolStripMenuItem);
            levelItem.DropDownItems.Add(mediumToolStripMenuItem);
            levelItem.DropDownItems.Add(hardToolStripMenuItem);
            levelItem.DropDownItems.Add(expertToolStripMenuItem);

            // create the Tools menu
            var toolsItem = new ToolStripMenuItem("&Tools");

            //var CandidatesToolStripMenuItem = new ToolStripMenuItem("Check &Candidates");
            //CandidatesToolStripMenuItem.Click += form.CandidatesToolStripMenuItem_Click;

            var PossiblesToolStripMenuItem = new ToolStripMenuItem("Check &Possibles");
            PossiblesToolStripMenuItem.Click += form.PossiblesToolStripMenuItem_Click;

            //var CheckColumnsAndRowsMenuItem = new ToolStripMenuItem("Check Co&lumns and Rows");
            //CheckColumnsAndRowsMenuItem.Click += form.CheckColumnsAndRowsMenuItem_Click;

            //toolsItem.DropDownItems.Add(CandidatesToolStripMenuItem);
            toolsItem.DropDownItems.Add(PossiblesToolStripMenuItem);
            //toolsItem.DropDownItems.Add(CheckColumnsAndRowsMenuItem);

            // create the Help menu
            var helpItem = new ToolStripMenuItem("&Help");
            var aboutSubItem = new ToolStripMenuItem("&About");
            aboutSubItem.Click += form.AboutToolStripMenuItem_Click;
            helpItem.DropDownItems.Add(aboutSubItem);

            form.menuStrip1.Items.Add(fileItem);
            form.menuStrip1.Items.Add(editItem);
            form.menuStrip1.Items.Add(levelItem);
            form.menuStrip1.Items.Add(toolsItem);
            form.menuStrip1.Items.Add(helpItem);
        }

        public void DisplayElapsedTime()
        {
            var ss = Seconds;
            int mm;

            var label = Form1._Form1.toolStripStatusLabel2;

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
                    elapsedTime += ":" + ss;
                }

                label.Text = $@"Elapsed time: {elapsedTime}";
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

                label.Text = $@"Elapsed time: {elapsedTime}";
            }
            else if (Seconds > 0 && Seconds < 60)
            {
                label.Text = $@"Elapsed time: {ss} seconds";
            }

            Seconds += 1;
        }

        private void FindCellWithFewestPossibles(ref int col, ref int row)
        {
            var min = 10;
            foreach (var r in Enumerable.Range(1, 9))
                foreach (var c in Enumerable.Range(1, 9))
                {
                    if (Actual[c, r] != 0 || Possible[c, r].Length >= min)
                    {
                        continue;
                    }

                    min = Possible[c, r].Length;
                    col = c;
                    row = r;
                }
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

        public void LoadSavedGame(string fileContents)
        {
            // initialize the board
            var contents = string.Join(string.Empty, Regex.Split(fileContents, @"(?:\r\n|\n|\r)"));
            var counter = 0;
            foreach (var row in Enumerable.Range(1, 9))
                foreach (var col in Enumerable.Range(1, 9))
                {
                    SetCell(col, row, int.Parse(contents[counter].ToString()));
                    counter++;
                }
        }

        private string GenerateNewPuzzle(int level, ref int score)
        {
            var str = string.Empty;
            int empty;

            // initialize the entire board
            foreach (var row in Enumerable.Range(1, 9))
                foreach (var col in Enumerable.Range(1, 9))
                {
                    Actual[col, row] = 0;
                    Possible[col, row] = string.Empty;
                }

            // clear the stacks
            ActualStack.Clear();
            PossibleStack.Clear();

            // populate the board with numbers by solving an empty grid
            try
            {
                // use logical methods to setup the grid first
                if (!SolvePuzzle())
                {
                    // then use brute-force
                    SolvePuzzleByBruteForce();
                }
            }
            catch (Exception)
            {
                // just in case an error occured, return an empty string
                return string.Empty;
            }

            // make a backup copy of the _actual array
            ActualBackup = (int[,])Actual.Clone();

            var rnd = new Random();

            // set the number of empty cells based on the level of difficulty
            switch (level)
            {
                case 1:
                    empty = rnd.Next(40, 45);
                    break;
                case 2:
                    empty = rnd.Next(46, 49);
                    break;
                case 3:
                    empty = rnd.Next(50, 53);
                    break;
                case 4:
                    empty = rnd.Next(54, 58);
                    break;
                default:
                    empty = rnd.Next(40, 45);
                    break;
            }

            // clear the stacks that are used in brute-force elimination 
            ActualStack.Clear();
            PossibleStack.Clear();
            BruteForceStop = false;

            // create empty cells
            CreateEmptyCells(empty);

            // convert the values in the actual array to a string
            foreach (var row in Enumerable.Range(1, 9))
                foreach (var col in Enumerable.Range(1, 9))
                    str += Actual[col, row].ToString();

            // verify the puzzle has only one solution
            var tries = 0;
            do
            {
                try
                {
                    if (!SolvePuzzle())
                    {
                        // if puzzle is not solved and this is a level 1 to 3 puzzle
                        if (level < 4)
                        {
                            // choose another pair of cells to empty
                            VacatePairOfCells(ref str);
                            tries++;
                        }
                        else
                        {
                            // level 4 puzzles does not guarantee single solution and potentially need guessing
                            SolvePuzzleByBruteForce();
                            goto endOfDoLoop;
                        }
                    }
                    else
                    {
                        // puzzle does indeed have 1 solution
                        goto endOfDoLoop;
                    }
                }
                catch (Exception)
                {
                    return string.Empty;
                }

                // if too many tries, exit the loop
                if (tries > 50)
                {
                    return string.Empty;
                }
            } while (true);

            endOfDoLoop:

            // return the score as well as the puzzle as a string
            return str;
        }

        public string GetPuzzle(int level)
        {
            var score = 0;
            string result;
            do
            {
                result = GenerateNewPuzzle(level, ref score);
                if (result == string.Empty)
                {
                    continue;
                }

                // check if puzzle matches the level of difficult
                switch (level)
                {
                    // average for this level is 44
                    case 1:
                        if ((score >= 42) & (score <= 46))
                        {
                            goto endOfDoLoop;
                        }

                        break;
                    // average for this level is 51
                    case 2:
                        if ((score >= 49) & (score <= 53))
                        {
                            goto endOfDoLoop;
                        }

                        break;
                    // average for this level is 58
                    case 3:
                        if ((score >= 56) & (score <= 60))
                        {
                            goto endOfDoLoop;
                        }

                        break;
                    // average for this level is 114
                    case 4:
                        if ((score >= 112) & (score <= 116))
                        {
                            goto endOfDoLoop;
                        }

                        break;

                    default:
                        goto endOfDoLoop;
                }
            } while (true);

            endOfDoLoop:
            return result;
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
                    foreach (var cc in Enumerable.Range(0, 2))
                        if (Actual[startC + cc, startR + rr] == value) // duplicate
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

        public bool IsPuzzleSolved()
        {
            string pattern;
            int r;
            int c;

            // check row by row
            for (r = 1; r <= 9; r++)
            {
                pattern = "123456789";
                for (c = 1; c <= 9; c++)
                    pattern = pattern.Replace(Convert.ToString(Actual[c, r].ToString()), string.Empty);
                if (pattern.Length > 0)
                {
                    return false;
                }
            }

            // check col by col
            for (c = 1; c <= 9; c++)
            {
                pattern = "123456789";
                for (r = 1; r <= 9; r++)
                    pattern = pattern.Replace(Convert.ToString(Actual[c, r].ToString()), string.Empty);
                if (pattern.Length > 0)
                {
                    return false;
                }
            }

            // check by minigrid
            for (c = 1; c <= 9; c += 3)
            {
                pattern = "123456789";
                for (r = 1; r <= 9; r += 3)
                    for (var cc = 0; cc <= 2; cc++)
                        for (var rr = 0; rr <= 2; rr++)
                            pattern = pattern.Replace(Convert.ToString(Actual[c + cc, r + rr].ToString()), string.Empty);
                if (pattern.Length > 0)
                {
                    return false;
                }
            }

            return true;
        }

        private bool LookForLoneRangersinMinigrids()
        {
            var changes = false;
            var cPos = 0;
            var rPos = 0;

            // check for each number from 1 to 9
            for (var n = 1; n <= 9; n++)
                // check the 9 mini-grids
                for (var r = 1; r <= 9; r += 3)
                    for (var c = 1; c <= 9; c += 3)
                    {
                        var nextMiniGrid = false;

                        // check within the mini-grid
                        var occurrence = 0;
                        for (var rr = 0; rr <= 2; rr++)
                        {
                            for (var cc = 0; cc <= 2; cc++)
                            {
                                if (Actual[c + cc, r + rr] != 0 || !Possible[c + cc, r + rr].Contains(n.ToString()))
                                {
                                    continue;
                                }

                                occurrence++;
                                cPos = c + cc;
                                rPos = r + rr;
                                if (occurrence <= 1)
                                {
                                    continue;
                                }

                                nextMiniGrid = true;
                                break;
                            }

                            if (nextMiniGrid)
                            {
                                break;
                            }
                        }

                        if (nextMiniGrid || occurrence != 1)
                        {
                            continue;
                        }

                        // that means number is confirmed
                        Actual[cPos, rPos] = n;
                        changes = true;
                    }

            return changes;
        }

        private bool LookForLoneRangersinRows()
        {
            var changes = false;
            var cPos = 0;
            var rPos = 0;
            for (var r = 1; r <= 9; r++)
                for (var n = 1; n <= 9; n++)
                {
                    var occurrence = 0;
                    for (var c = 1; c <= 9; c++)
                    {
                        if (Actual[c, r] != 0 || !Possible[c, r].Contains(n.ToString()))
                        {
                            continue;
                        }

                        occurrence++;

                        // if multiple occurrence, not a lone ranger anymore
                        if (occurrence > 1)
                        {
                            break;
                        }

                        cPos = c;
                        rPos = r;
                    }

                    if (occurrence != 1)
                    {
                        continue;
                    }

                    // number is confirmed
                    Actual[cPos, rPos] = n;
                    changes = true;
                }

            return changes;
        }

        private bool LookForLoneRangersinColumns()
        {
            var changes = false;
            var cPos = 0;
            var rPos = 0;
            // check by column
            for (var c = 1; c <= 9; c++)
                for (var n = 1; n <= 9; n++)
                {
                    var occurrence = 0;
                    for (var r = 1; r <= 9; r++)
                    {
                        if (Actual[c, r] != 0 || !Possible[c, r].Contains(n.ToString()))
                        {
                            continue;
                        }

                        occurrence++;

                        // if multiple occurrence, not a lone ranger anymore
                        if (occurrence > 1)
                        {
                            break;
                        }

                        cPos = c;
                        rPos = r;
                    }

                    if (occurrence != 1)
                    {
                        continue;
                    }

                    // number is confirmed
                    Actual[cPos, rPos] = n;
                    changes = true;
                }

            return changes;
        }

        private bool LookForTwinsinMinigrids()
        {
            var changes = false;
            // look for twins in each cell
            for (var r = 1; r <= 9; r++)
                for (var c = 1; c <= 9; c++)
                {
                    // if two possible values, check for twins
                    if (Actual[c, r] != 0 || Possible[c, r].Length != 2)
                    {
                        continue;
                    }

                    // scan by the mini-grid that the current cell is in
                    var startC = c - (c - 1) % 3;
                    var startR = r - (r - 1) % 3;
                    for (var rr = startR; rr <= startR + 2; rr++)
                        for (var cc = startC; cc <= startC + 2; cc++)
                        {
                            // for cells other than the pair of twins
                            if (cc == c && rr == r || Possible[cc, rr] != Possible[c, r])
                            {
                                continue;
                            }

                            // remove the twins from all the other possible values in the minigrid
                            for (var rrr = startR; rrr <= startR + 2; rrr++)
                                for (var ccc = startC; ccc <= startC + 2; ccc++)
                                {
                                    if (Actual[ccc, rrr] != 0 || Possible[ccc, rrr] == Possible[c, r])
                                    {
                                        continue;
                                    }

                                    // save a copy of the original possible values (twins)
                                    var originalPossible = Possible[ccc, rrr];

                                    // remove first twin number from possible values
                                    Possible[ccc, rrr] = Possible[ccc, rrr].Replace(Possible[c, r][0].ToString(), string.Empty);

                                    // remove second twin number from possible values
                                    Possible[ccc, rrr] = Possible[ccc, rrr].Replace(Possible[c, r][1].ToString(), string.Empty);

                                    // if the possible values are modified, then set the changes variable to true to
                                    // indicate that the possible values of cells in the minigrid have been modified
                                    if (originalPossible != Possible[ccc, rrr])
                                    {
                                        changes = true;
                                    }

                                    // if possible value reduces to empty string, then the user has placed a move that
                                    // results in the puzzle not being solvable
                                    if (Possible[ccc, rrr] == string.Empty)
                                    {
                                        throw new Exception("Invalid Move");
                                    }

                                    // if left with 1 possible value  for the current cell, cell is confirmed
                                    if (Possible[ccc, rrr].Length != 1)
                                    {
                                        continue;
                                    }

                                    Actual[ccc, rrr] = int.Parse(Possible[ccc, rrr]);
                                }
                        }
                }

            return changes;
        }

        private bool LookForTwinsinRows()
        {
            var changes = false;
            // for each row, check each column in the row
            for (var r = 1; r <= 9; r++)
                for (var c = 1; c <= 9; c++)
                {
                    // if two possible values, check for twins
                    if (Actual[c, r] != 0 || Possible[c, r].Length != 2)
                    {
                        continue;
                    }

                    //  scan columns in this row
                    for (var cc = c + 1; cc <= 9; cc++)
                    {
                        if (Possible[cc, r] != Possible[c, r])
                        {
                            continue;
                        }

                        // remove the twins from all the other possible values in the row
                        for (var ccc = 1; ccc <= 9; ccc++)
                        {
                            if (Actual[ccc, r] != 0 || ccc == c || ccc == cc)
                            {
                                continue;
                            }

                            // save a copy of the original possible values (twins)
                            var originalPossible = Possible[ccc, r];

                            // remove first twin number from possible
                            // values
                            Possible[ccc, r] = Convert.ToString(Possible[ccc, r]
                                .Replace(Convert.ToString(Possible[c, r][0]), string.Empty));

                            // remove second twin number from possible values
                            Possible[ccc, r] = Convert.ToString(Possible[ccc, r]
                                .Replace(Convert.ToString(Possible[c, r][1]), string.Empty));

                            // if the possible values are modified, then set the changes variable to true to indicate
                            // that the possible values of cells in the minigrid have been modified
                            if (originalPossible != Possible[ccc, r])
                            {
                                changes = true;
                            }

                            // if possible value reduces to empty string, then the user has placed a move that results
                            // in the puzzle not solvable
                            if (Possible[ccc, r] == string.Empty)
                            {
                                throw new Exception("Invalid Move");
                            }

                            // if left with 1 possible value for the cell, cell is confirmed
                            if (Possible[ccc, r].Length != 1)
                            {
                                continue;
                            }

                            Actual[ccc, r] = int.Parse(Possible[ccc, r]);
                        }
                    }
                }

            return changes;
        }

        private bool LookForTwinsinColumns()
        {
            var changes = false;
            // for each column, check each row in the column
            for (var c = 1; c <= 9; c++)
                for (var r = 1; r <= 9; r++)
                {
                    // if two possible values, check for twins
                    if (Actual[c, r] != 0 || Possible[c, r].Length != 2)
                    {
                        continue;
                    }

                    //  scan rows in this column
                    for (var rr = r + 1; rr <= 9; rr++)
                    {
                        if (Possible[c, rr] != Possible[c, r])
                        {
                            continue;
                        }

                        // remove the twins from all the other possible values in the row
                        for (var rrr = 1; rrr <= 9; rrr++)
                        {
                            if (Actual[c, rrr] != 0 || rrr == r || rrr == rr)
                            {
                                continue;
                            }

                            // save a copy of the original possible values (twins)
                            var originalPossible = Possible[c, rrr];

                            // remove first twin number from possible values
                            Possible[c, rrr] = Convert.ToString(Possible[c, rrr]
                                .Replace(Convert.ToString(Possible[c, r][0]), string.Empty));

                            // remove second twin number from possible values
                            Possible[c, rrr] = Convert.ToString(Possible[c, rrr]
                                .Replace(Convert.ToString(Possible[c, r][1]), string.Empty));

                            // if the possible values are modified, then set the changes variable to true to indicate
                            // that the possible values of cells in the minigrid have been modified
                            if (originalPossible != Possible[c, rrr])
                            {
                                changes = true;
                            }

                            // if possible value reduces to empty string, then the user has placed a move that results
                            // in the puzzle not being solvable
                            if (Possible[c, rrr] == string.Empty)
                            {
                                throw new Exception("Invalid Move");
                            }

                            // if left with 1 possible value for the current cell, cell is confirmed
                            if (Possible[c, rrr].Length != 1)
                            {
                                continue;
                            }

                            Actual[c, rrr] = int.Parse(Possible[c, rrr]);
                        }
                    }
                }

            return changes;
        }

        private bool LookForTripletsinMinigrids()
        {
            var changes = false;
            // check each cell
            for (var r = 1; r <= 9; r++)
                for (var c = 1; c <= 9; c++)
                    //  three possible values; check for triplets
                    if (Actual[c, r] == 0 && Possible[c, r].Length == 3)
                    {
                        // first potential triplet found
                        var tripletsLocation = c + r.ToString();

                        // scan by mini-grid
                        var startC = c - (c - 1) % 3;
                        var startR = r - (r - 1) % 3;
                        for (var rr = startR; rr <= startR + 2; rr++)
                            for (var cc = startC; cc <= startC + 2; cc++)
                                if (!(cc == c && rr == r) && (Possible[cc, rr] == Possible[c, r] ||
                                                              Possible[cc, rr].Length == 2 &&
                                                              Possible[c, r]
                                                                  .Contains(Convert.ToString(Possible[cc, rr][0]
                                                                      .ToString())) && Possible[c, r]
                                                                  .Contains(Convert.ToString(Possible[cc, rr][1].ToString()))))
                                {
                                    // save the coorindates of the triplets
                                    tripletsLocation += cc + rr.ToString();
                                }

                        // found 3 cells as triplets; remove all from the other cells---
                        if (tripletsLocation.Length != 6)
                        {
                            continue;
                        }

                        // remove each cell's possible values containing the triplet
                        for (var rrr = startR; rrr <= startR + 2; rrr++)
                            for (var ccc = startC; ccc <= startC + 2; ccc++)
                            {
                                // look for the cell that is not part of the 3 cells found
                                if (Actual[ccc, rrr] != 0 || ccc == Convert.ToInt32(tripletsLocation[0].ToString()) ||
                                    rrr == Convert.ToInt32(tripletsLocation[1].ToString()) ||
                                    ccc == Convert.ToInt32(tripletsLocation[2].ToString()) ||
                                    rrr == Convert.ToInt32(tripletsLocation[3].ToString()) ||
                                    ccc == Convert.ToInt32(tripletsLocation[4].ToString()) ||
                                    rrr == Convert.ToInt32(tripletsLocation[5].ToString()))
                                {
                                    continue;
                                }

                                // save the original possible values
                                var originalPossible = Possible[ccc, rrr];

                                // remove first triplet number from possible values
                                Possible[ccc, rrr] = Convert.ToString(Possible[ccc, rrr]
                                    .Replace(Convert.ToString(Possible[c, r][0]), string.Empty));

                                // remove second triplet number from possible values
                                Possible[ccc, rrr] = Convert.ToString(Possible[ccc, rrr]
                                    .Replace(Convert.ToString(Possible[c, r][1]), string.Empty));

                                // remove third triplet number from possible
                                // values---
                                Possible[ccc, rrr] = Convert.ToString(Possible[ccc, rrr]
                                    .Replace(Convert.ToString(Possible[c, r][2]), string.Empty));

                                // if the possible values are modified, then set the changes variable to true to indicate
                                // that the possible values of cells in the  minigrid have been modified
                                if (originalPossible != Possible[ccc, rrr])
                                {
                                    changes = true;
                                }

                                // if possible value reduces to empty string, then the user has placed a move that results
                                // in the puzzle not solvable
                                if (Possible[ccc, rrr] == string.Empty)
                                {
                                    throw new Exception("Invalid Move");
                                }

                                // if left with 1 possible value for the current cell, cell is confirmed
                                if (Possible[ccc, rrr].Length != 1)
                                {
                                    continue;
                                }

                                Actual[ccc, rrr] = int.Parse(Possible[ccc, rrr]);
                            }
                    }

            return changes;
        }

        private bool LookForTripletsinColumns()
        {
            var changes = false;
            // for each column, check each row in the column
            for (var c = 1; c <= 9; c++)
                for (var r = 1; r <= 9; r++)
                {
                    //  three possible values; check for triplets
                    if (Actual[c, r] != 0 || Possible[c, r].Length != 3)
                    {
                        continue;
                    }

                    // first potential triplet found
                    var tripletsLocation = $"{c}{r}";

                    // scans rows in this column
                    for (var rr = 1; rr <= 9; rr++)
                    {
                        if (rr == r)
                        {
                            continue;
                        }

                        if (Possible[c, rr] == Possible[c, r] || Possible[c, rr].Length == 2 &&
                            Possible[c, r].Contains(Possible[c, rr][0].ToString()) &&
                            Possible[c, r].Contains(Possible[c, rr][1].ToString()))
                        {
                            // save the coorindates of the triplet
                            tripletsLocation += $"{c}{rr}";
                        }
                    }

                    //  found 3 cells as triplets; remove all from the other cells
                    if (tripletsLocation.Length != 6)
                    {
                        continue;
                    }

                    // remove each cell's possible values containing the triplet
                    for (var rrr = 1; rrr <= 9; rrr++)
                    {
                        if (Actual[c, rrr] != 0 || rrr == Convert.ToInt32(tripletsLocation[1].ToString()) ||
                            rrr == Convert.ToInt32(tripletsLocation[3].ToString()) ||
                            rrr == Convert.ToInt32(tripletsLocation[5].ToString()))
                        {
                            continue;
                        }

                        // save the original possible values
                        var originalPossible = Possible[c, rrr];

                        // remove first triplet number from possible values
                        Possible[c, rrr] = Possible[c, rrr].Replace(Possible[c, r][0].ToString(), string.Empty);

                        // remove second triplet number from possible values
                        Possible[c, rrr] = Possible[c, rrr].Replace(Possible[c, r][1].ToString(), string.Empty);

                        // remove third triplet number from possible values
                        Possible[c, rrr] = Possible[c, rrr].Replace(Possible[c, r][2].ToString(), string.Empty);

                        // if the possible values are modified, then set the changes variable to true to indicate that
                        // the possible values of cells in the minigrid have been modified
                        if (originalPossible != Possible[c, rrr])
                        {
                            changes = true;
                        }

                        // if possible value reduces to empty string, then the user has placed a move that results in the puzzle not being solvable
                        if (Possible[c, rrr] == string.Empty)
                        {
                            throw new Exception("Invalid Move");
                        }

                        // if left with 1 possible value for the current cell, cell is confirmed
                        if (Possible[c, rrr].Length != 1)
                        {
                            continue;
                        }

                        Actual[c, rrr] = int.Parse(Possible[c, rrr]);
                    }
                }

            return changes;
        }

        private bool LookForTripletsinRows()
        {
            var changes = false;
            // for each row, check each column in the row
            for (var r = 1; r <= 9; r++)
                for (var c = 1; c <= 9; c++)
                {
                    //  three possible values; check for triplets
                    if (Actual[c, r] != 0 || Possible[c, r].Length != 3)
                    {
                        continue;
                    }

                    // first potential triplet found
                    var tripletsLocation = c + r.ToString();

                    // scans columns in this row
                    for (var cc = 1; cc <= 9; cc++)
                        // look for other triplets
                        if (cc != c && (Possible[cc, r] == Possible[c, r] ||
                                        Possible[cc, r].Length == 2 &&
                                        Possible[c, r].Contains(Possible[cc, r][0].ToString())) &&
                            Possible[c, r].Contains(Possible[cc, r][1].ToString()))
                        {
                            // save the coorindates of the triplet
                            tripletsLocation += cc + r.ToString();
                        }

                    // found 3 cells as triplets; remove all from the other cells
                    if (tripletsLocation.Length != 6)
                    {
                        continue;
                    }

                    // remove each cell's possible values containing the triplet
                    for (var ccc = 1; ccc <= 9; ccc++)
                        if (Actual[ccc, r] == 0 && ccc != tripletsLocation[0] && ccc != tripletsLocation[2] &&
                            ccc != tripletsLocation[4])
                        {
                            // save the original possible values
                            var originalPossible = Possible[ccc, r];

                            // remove first triplet number from possible values
                            Possible[ccc, r] = Possible[ccc, r].Replace(Possible[c, r][0].ToString(), string.Empty);

                            // remove second triplet number from possible values
                            Possible[ccc, r] = Possible[ccc, r].Replace(Possible[c, r][1].ToString(), string.Empty);

                            // remove third triplet number from possible values
                            Possible[ccc, r] = Possible[ccc, r].Replace(Possible[c, r][2].ToString(), string.Empty);

                            // if the possible values are modified, then set the changes variable to true to indicate that
                            // the possible values of cells in the minigrid have been modified
                            if (originalPossible != Possible[ccc, r])
                            {
                                changes = true;
                            }

                            // if possible value reduces to empty string, then the user has placed a move that results in the puzzle not solvable
                            if (Possible[ccc, r] == string.Empty)
                            {
                                throw new Exception("Invalid Move");
                            }

                            // if left with 1 _possible value for the current cell, cell is confirmed
                            if (Possible[ccc, r].Length != 1)
                            {
                                continue;
                            }

                            Actual[ccc, r] = int.Parse(Possible[ccc, r]);
                        }
                }

            return changes;
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

        private static void RandomizeThePossibles(ref string str)
        {
            int i;
            VBMath.Randomize();
            var s = str.ToCharArray();
            for (i = 0; i <= str.Length - 1; i++)
            {
                var j = Convert.ToInt32((str.Length - i + 1) * VBMath.Rnd() + i) % str.Length;
                // swap the chars
                var temp = s[i];
                s[i] = s[j];
                s[j] = temp;
            }

            str = new string(s);
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

            label.Font = new Font("Consolas", labelSizeLarge, label.Font.Style | FontStyle.Bold);
            // if erasing a cell, you need to reset the values for all cells
            // set the appearance for the CustomLabel control
            if (value == 0)
            {
                // this cell can show the candidates for the value
                label.IsEraseable = true;
                label.BackColor = _userBackcolor;
                label.ForeColor = _userForecolor;
                label.Font = new Font("Consolas", labelSizeSmall, label.Font.Style | FontStyle.Regular);
                var possibles = CalculatePossibles(col, row, true);
                var lf = Environment.NewLine;
                possibles = $"{possibles.Substring(0, 3)}{lf}{possibles.Substring(3, 3)}{lf}{possibles.Substring(6, 3)}";
                label.Text = possibles;
            }
            else if (value > 0)
            {
                // this cell has to show a start clue
                label.BackColor = _fixedBackcolor;
                label.ForeColor = _fixedForecolor;
                label.Text = value.ToString();
            }
            else if (value != 0 && !isEraseable)
            {
                // this has to show a start clue
                label.BackColor = _fixedBackcolor;
                label.ForeColor = _fixedForecolor;
                label.Text = value.ToString();
            }

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

        public bool SolvePuzzle()
        {
            var exitLoop = false;
            try
            {
                bool changes;
                do // Look for Triplets in Columns
                {
                    do // Look for Triplets in Rows
                    {
                        do // Look for Triplets in Minigrids
                        {
                            do // Look for Twins in Columns
                            {
                                do // Look for Twins in Rows
                                {
                                    do // Look for Twins in Minigrids
                                    {
                                        do // Look for Lone Rangers in Columns
                                        {
                                            do // Look for Lone Rangers in Rows
                                            {
                                                do // Look for Lone Rangers in Minigrids
                                                {
                                                    do // Perform Col/Row and Minigrid Elimination
                                                    {
                                                        changes = CheckColumnsAndRows();
                                                        if (!IsPuzzleSolved())
                                                        {
                                                            continue;
                                                        }

                                                        exitLoop = true;
                                                        break;
                                                    } while (changes);

                                                    if (exitLoop)
                                                    {
                                                        break;
                                                    }

                                                    // Look for Lone Rangers in Minigrids
                                                    changes = LookForLoneRangersinMinigrids();
                                                    if (!IsPuzzleSolved())
                                                    {
                                                        continue;
                                                    }

                                                    exitLoop = true;
                                                    break;
                                                } while (changes);

                                                if (exitLoop)
                                                {
                                                    break;
                                                }

                                                // Look for Lone Rangers in Rows
                                                changes = LookForLoneRangersinRows();
                                                if (!IsPuzzleSolved())
                                                {
                                                    continue;
                                                }

                                                exitLoop = true;
                                                break;
                                            } while (changes);

                                            if (exitLoop)
                                            {
                                                break;
                                            }

                                            // Look for Lone Rangers in Columns
                                            changes = LookForLoneRangersinColumns();
                                            if (!IsPuzzleSolved())
                                            {
                                                continue;
                                            }

                                            exitLoop = true;
                                            break;
                                        } while (changes);

                                        if (exitLoop)
                                        {
                                            break;
                                        }

                                        // Look for Twins in Minigrids
                                        changes = LookForTwinsinMinigrids();
                                        if (!IsPuzzleSolved())
                                        {
                                            continue;
                                        }

                                        exitLoop = true;
                                        break;
                                    } while (changes);

                                    if (exitLoop)
                                    {
                                        break;
                                    }

                                    // Look for Twins in Rows
                                    changes = LookForTwinsinRows();
                                    if (!IsPuzzleSolved())
                                    {
                                        continue;
                                    }

                                    exitLoop = true;
                                    break;
                                } while (changes);

                                if (exitLoop)
                                {
                                    break;
                                }

                                // Look for Twins in Columns
                                changes = LookForTwinsinColumns();
                                if (!IsPuzzleSolved())
                                {
                                    continue;
                                }

                                exitLoop = true;
                                break;
                            } while (changes);

                            if (exitLoop)
                            {
                                break;
                            }

                            // Look for Triplets in Minigrids
                            changes = LookForTripletsinMinigrids();
                            if (!IsPuzzleSolved())
                            {
                                continue;
                            }

                            exitLoop = true;
                            break;
                        } while (changes);

                        if (exitLoop)
                        {
                            break;
                        }

                        // Look for Triplets in Rows
                        changes = LookForTripletsinRows();
                        if (!IsPuzzleSolved())
                        {
                            continue;
                        }

                        exitLoop = true;
                        break;
                    } while (changes);

                    if (exitLoop)
                    {
                        break;
                    }

                    // Look for Triplets in Columns
                    changes = LookForTripletsinColumns();
                    if (IsPuzzleSolved())
                    {
                        break;
                    }
                } while (changes);
            }
            catch (Exception)
            {
                throw new Exception("Invalid Move");
            }

            return IsPuzzleSolved();
        }

        public void SolvePuzzleByBruteForce()
        {
            var c = 1;
            var r = 1;

            // find out which cell has the smallest number of possible values
            FindCellWithFewestPossibles(ref c, ref r);

            // get the possible values for the chosen cell
            var possibleValues = Possible[c, r];

            // randomize the possible values
            RandomizeThePossibles(ref possibleValues);

            // push the actual and possible stacks into the stack
            ActualStack.Push((int[,])Actual.Clone());
            PossibleStack.Push((string[,])Possible.Clone());

            // select one value and try
            for (var i = 0; i <= possibleValues.Length - 1; i++)
            {
                Actual[c, r] = int.Parse(possibleValues);
                try
                {
                    if (SolvePuzzle())
                    {
                        // if the puzzle is solved, the recursion can stop now
                        BruteForceStop = true;
                        return;
                    }

                    // no problem with current selection, proceed with next cell
                    if (!BruteForceStop)
                    {
                        continue;
                    }

                    SolvePuzzleByBruteForce();
                    return;
                }
                catch (Exception)
                {
                    // accumulate the total score
                    Actual = ActualStack.Pop();
                    Possible = PossibleStack.Pop();
                }
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

        private void VacatePairOfCells(ref string str)
        {
            int c;
            int r;

            var rnd = new Random();

            // look for a pair of cells to restore
            do
            {
                c = rnd.Next(1, 9);
                r = rnd.Next(1, 9);
            } while (str[c - 1 + (r - 1) * 9].ToString().Length != 0);

            // restore the value of the cell from the actual_backup array
            str = str.Remove(c - 1 + (r - 1) * 9, 1);
            str = str.Insert(c - 1 + (r - 1) * 9, ActualBackup[c, r].ToString());

            // restore the value of the symmetrical cell from the actual_backup array
            str = str.Remove(10 - c - 1 + (10 - r - 1) * 9, 1);
            str = str.Insert(10 - c - 1 + (10 - r - 1) * 9, ActualBackup[10 - c, 10 - r].ToString());

            // look for another pair of cells to vacate
            do
            {
                c = rnd.Next(1, 9);
                r = rnd.Next(1, 9);
            } while (str[c - 1 + (r - 1) * 9].ToString().Length == 0);

            // remove the cell from the str
            str = str.Remove(c - 1 + (r - 1) * 9, 1);
            str = str.Insert(c - 1 + (r - 1) * 9, "0");

            // remove the symmetrical cell from the str
            str = str.Remove(10 - c - 1 + (10 - r - 1) * 9, 1);
            str = str.Insert(10 - c - 1 + (10 - r - 1) * 9, "0");

            // reinitialize the board
            var counter = 0;
            foreach (var row in Enumerable.Range(1, 9))
                foreach (var col in Enumerable.Range(1, 9))
                {
                    if (int.Parse(str[counter].ToString()) != 0)
                    {
                        Actual[col, row] = int.Parse(str[counter].ToString());
                        Possible[col, row] = str[counter].ToString();
                    }
                    else
                    {
                        Actual[col, row] = 0;
                        Possible[col, row] = string.Empty;
                    }

                    counter++;
                }
        }


    }
}