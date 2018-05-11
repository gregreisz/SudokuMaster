using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SudokuMaster
{
    public interface IForm1
    {
        string SetText { set; }

        string SaveFileName { get; set; }

    }

    public partial class Form1 : Form, IForm1
    {
        private readonly SudokuPuzzle Sdk = new SudokuPuzzle();

        // back color for empty cells
        public Color _defaultBackcolor = Color.White;

        // colors for cells with hint values
        // these cells are not eraseable
        public readonly Color _fixedForecolor = Color.Blue;
        public readonly Color _fixedBackcolor = Color.LightSteelBlue;

        // these colors are for user inserted values which can be erased
        public readonly Color _userBackcolor = Color.LightYellow;
        public readonly Color _userForecolor = Color.Black;

        // dimension of each cell of the grid
        public int CellWidth = 32;
        public int CellHeight = 32;

        // offset from the top-left corner of the window
        public int XOffset = -20;
        public int YOffset = 25;

        public Form1()
        {
            InitializeComponent();
            _Form1 = this;
        }

        public static Form1 _Form1;

        public string SaveFileName { get; set; }

        public string SetText
        {
            set => TxtActivities.AppendText(value + Environment.NewLine);
        }

        private void BtnHint_Click(object sender, EventArgs e)
        {
            // show hints one cell at a time
            Sdk.HintMode = true;
            try
            {
                Sdk.CheckColumnsAndRows();

            }
            catch (Exception ex)
            {
                MessageBox.Show(@"Please undo your move", @"Invalid Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MessageBox.Show(ex.Message);
            }
        }

        private void BtnSolvePuzzle_Click(object sender, EventArgs e)
        {
            Sdk.ActualStack.Clear();
            Sdk.PossibleStack.Clear();

            Sdk.BruteForceStop = false;

            // solve the puzzle; no need to stop
            try
            {
                if (!Sdk.SolvePuzzle())
                {
                    Sdk.SolvePuzzleByBruteForce();

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                throw;
            }

        }

        public void Cell_Click(object sender, EventArgs e)
        {
            // check to see if game has even started or not
            if (!Sdk.GameStarted)
            {
                Console.Beep();
                SetText = @"Click File->New to start a new game or File->Open to load an existing game";
                return;
            }

            var cellLabel = (CustomLabel)sender;

            // determine the col and row of the selected cell
            var col = Sdk.SelectedColumn = int.Parse(cellLabel.Name.Substring(0, 1));
            var row = Sdk.SelectedRow = int.Parse(cellLabel.Name.Substring(1, 1));

            // if cell is not erasable then exit
            if (cellLabel.IsEraseable == false)
            {
                Console.Beep();
                SetText = @"This cell cannot be erased.";
                return;
            }

            try
            {
                // If erasing a cell
                if (Sdk.SelectedNumber == 0)
                {
                    // if cell is empty then no need to erase
                    if (Sdk.Actual[Sdk.SelectedColumn, Sdk.SelectedRow] == 0)
                    {
                        return;
                    }

                    // save the value in the array
                    SetCell(col, row, Sdk.SelectedNumber, true);
                    SetText = $@"Number erased at ({col},{row})";
                }
                else if (cellLabel.Text == string.Empty)
                {
                    // else setting a value; check if move is valid
                    if (!Sdk.IsMoveValid(col, row, Sdk.SelectedNumber))
                    {
                        Console.Beep();
                        SetText = $@"Invalid move at ({col},{row})";
                        return;
                    }

                    // save the value in the array
                    SetCell(col, row, Sdk.SelectedNumber, true);
                    SetText = $@"The number entered at ({col},{row}) was {Sdk.SelectedNumber}.";

                    // saves the move into the stack
                    Sdk.Moves.Push($"{cellLabel.Name}{Sdk.SelectedNumber}");

                    if (!Sdk.IsPuzzleSolved())
                    {
                        return;
                    }

                    timer1.Enabled = false;
                    Console.Beep();
                    SetText = @"*****Puzzle Solved*****";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void ClearBoard()
        {
            // initialize the stacks
            Sdk.Moves = new Stack<string>();
            Sdk.RedoMoves = new Stack<string>();

            // initialize the cells in the board
            foreach (var row in Enumerable.Range(1, 9))
            {
                foreach (var col in Enumerable.Range(1, 9))
                {
                    SetCell(col, row, 0, true);
                }
            }
        }

        protected void CheckColumnsAndRowsMenuItem_Click(object sender, EventArgs e)
        {
            Sdk.CheckColumnsAndRows();
        }

        private void CreateMenu()
        {
            // create the File menu
            var fileItem = new ToolStripMenuItem("&File");

            var newSubItem = new ToolStripMenuItem("&New");
            newSubItem.Click += NewToolStripMenuItem_Click;

            var openSubItem = new ToolStripMenuItem("&Open");
            openSubItem.Click += OpenToolStripMenuItem_Click;

            var saveSubItem = new ToolStripMenuItem("&Save");
            saveSubItem.Click += SaveToolStripMenuItem_Click;

            var saveAsSubItem = new ToolStripMenuItem("Save&As");
            saveAsSubItem.Click += SaveAsToolStripMenuItem_Click;

            var exitSubItem = new ToolStripMenuItem("E&xit");
            exitSubItem.Click += ExitToolStripMenuItem_Click;

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
            undoSubItem.Click += UndoToolStripMenuItem_Click;

            var redoSubItem = new ToolStripMenuItem("&Redo");
            redoSubItem.Click += RedoToolStripMenuItem_Click;

            editItem.DropDownItems.Add(undoSubItem);
            editItem.DropDownItems.Add(redoSubItem);

            // create the Level menu
            var levelItem = new ToolStripMenuItem("&Level") { Name = "LevelMenuItem" };
            var easyToolStripMenuItem = new ToolStripMenuItem("&Easy") { Name = "EasyToolStripMenuItem" };
            easyToolStripMenuItem.Click += EasyToolStripMenuItem_Click;

            var mediumToolStripMenuItem = new ToolStripMenuItem("&Medium") { Name = "MediumToolStripMenuItem" };
            mediumToolStripMenuItem.Click += MediumToolStripMenuItem_Click;

            var hardToolStripMenuItem = new ToolStripMenuItem("&Hard") { Name = "HardToolStripMenuItem" };
            hardToolStripMenuItem.Click += HardToolStripMenuItem_Click;

            var expertToolStripMenuItem = new ToolStripMenuItem("E&xpert") { Name = "ExpertToolStripMenuItem" };
            expertToolStripMenuItem.Click += ExpertToolStripMenuItem_Click;

            levelItem.DropDownItems.Add(easyToolStripMenuItem);
            levelItem.DropDownItems.Add(mediumToolStripMenuItem);
            levelItem.DropDownItems.Add(hardToolStripMenuItem);
            levelItem.DropDownItems.Add(expertToolStripMenuItem);

            // create the Tools menu
            var toolsItem = new ToolStripMenuItem("&Tools");

            var CandidatesToolStripMenuItem = new ToolStripMenuItem("Check &Candidates");
            CandidatesToolStripMenuItem.Click += CandidatesToolStripMenuItem_Click;

            var PossiblesToolStripMenuItem = new ToolStripMenuItem("Check &Possibles");
            PossiblesToolStripMenuItem.Click += PossiblesToolStripMenuItem_Click;

            var CheckColumnsAndRowsMenuItem = new ToolStripMenuItem("Check Co&lumns and Rows");
            CheckColumnsAndRowsMenuItem.Click += CheckColumnsAndRowsMenuItem_Click;

            toolsItem.DropDownItems.Add(CandidatesToolStripMenuItem);
            toolsItem.DropDownItems.Add(PossiblesToolStripMenuItem);
            toolsItem.DropDownItems.Add(CheckColumnsAndRowsMenuItem);

            // create the Help menu
            var helpItem = new ToolStripMenuItem("&Help");
            var aboutSubItem = new ToolStripMenuItem("&About");
            aboutSubItem.Click += AboutToolStripMenuItem_Click;
            helpItem.DropDownItems.Add(aboutSubItem);

            menuStrip1.Items.Add(fileItem);
            menuStrip1.Items.Add(editItem);
            menuStrip1.Items.Add(levelItem);
            menuStrip1.Items.Add(toolsItem);
            menuStrip1.Items.Add(helpItem);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            // initialize the status bar
            toolStripStatusLabel1.Text = string.Empty;
            toolStripStatusLabel2.Text = string.Empty;

            toolStripButton1.Click += ToolStripButton_Click;
            toolStripButton1.Checked = true;
            toolStripButton2.Click += ToolStripButton_Click;
            toolStripButton3.Click += ToolStripButton_Click;
            toolStripButton4.Click += ToolStripButton_Click;
            toolStripButton5.Click += ToolStripButton_Click;
            toolStripButton6.Click += ToolStripButton_Click;
            toolStripButton7.Click += ToolStripButton_Click;
            toolStripButton8.Click += ToolStripButton_Click;
            toolStripButton9.Click += ToolStripButton_Click;
            toolStripButton10.Click += ToolStripButton_Click;

            CreateMenu();

            DrawBoard();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            int y1, y2;

            // draw the horizontal lines
            var x1 = 1 * (CellWidth + 1) + XOffset - 1;
            var x2 = 9 * (CellWidth + 1) + XOffset + CellWidth;
            for (int r = 1; r <= 10; r += 3)
            {
                y1 = r * (CellHeight + 1) + YOffset - 1;
                y2 = y1;
                e.Graphics.DrawLine(Pens.Black, x1, y1, x2, y2);
            }

            // draw the vertical lines
            y1 = 1 * (CellHeight + 1) + YOffset - 1;
            y2 = 9 * (CellHeight + 1) + YOffset + CellHeight;
            for (int c = 1; c <= 10; c += 3)
            {
                x1 = c * (CellWidth + 1) + XOffset - 1;
                x2 = x1;
                e.Graphics.DrawLine(Pens.Black, x1, y1, x2, y2);
            }
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Sdk.GetPossiblesAndValues();
        }

        public void DrawBoard()
        {
            Sdk.SelectedNumber = 1;

            // used to store the location of the cell
            var location = new Point();

            // draws the cells
            foreach (var row in Enumerable.Range(1, 9))
            {
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
                    label.Font = new Font(label.Font, label.Font.Style | FontStyle.Bold);
                    label.IsEraseable = true;
                    label.Click += Cell_Click;
                    Controls.Add(label);
                }
            }
        }

        private void EasyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = (ToolStripMenuItem)sender;
            if (menuItem == null)
            {
                throw new ArgumentNullException(nameof(menuItem));
            }

            Sdk.SetLevel(menuItem.Name);
            SetCheckedOrNotChecked(menuItem);
        }

        private void HardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = (ToolStripMenuItem)sender;
            if (menuItem == null)
            {
                throw new ArgumentNullException(nameof(menuItem));
            }

            Sdk.SetLevel(menuItem.Name);
            SetCheckedOrNotChecked(menuItem);
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            const string message = "Do you want to save the current game?";
            const string caption = "Save current game";
            const MessageBoxButtons buttons = MessageBoxButtons.YesNoCancel;

            var result = MessageBox.Show(message, caption, buttons);
            if (result == DialogResult.Yes)
            {
                Sdk.SaveGameToDisk(false);
            }
            else if (result == DialogResult.Cancel)
            {
                return;
            }

            Application.Exit();

        }

        private void ExpertToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = (ToolStripMenuItem)sender;
            if (menuItem == null)
            {
                throw new ArgumentNullException(nameof(menuItem));
            }

            Sdk.SetLevel(menuItem.Name);
            SetCheckedOrNotChecked(menuItem);
        }

        private void MediumToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = (ToolStripMenuItem)sender;
            if (menuItem == null)
            {
                throw new ArgumentNullException(nameof(menuItem));
            }

            Sdk.SetLevel(menuItem.Name);
            SetCheckedOrNotChecked(menuItem);
        }

        private void NewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Sdk.GameStarted)
            {
                const string message = "Do you want to save current game?";
                const string caption = "Save current game";

                var result = MessageBox.Show(message, caption, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    Sdk.SaveGameToDisk(false);

                }
                else if (result == DialogResult.Cancel)
                {
                    return;
                }
            }

            // change to the hourglass cursor
            Cursor.Current = Cursors.WaitCursor;
            toolStripStatusLabel1.Text = @"Generating new puzzle...";

            var puzzle = Sdk.GetPuzzle(Sdk.Level);

            // change back to the default cursor
            Cursor.Current = Cursors.Default;

            // start new game
            StartNewGame();

            // initialize the board
            var counter = 0;
            foreach (int row in Enumerable.Range(1, 9))
            {
                foreach (int col in Enumerable.Range(1, 9))
                {
                    if (puzzle[counter].ToString() != "0")
                    {
                        SetCell(col, row, int.Parse(puzzle[counter].ToString()), false);
                    }
                    counter += 1;
                }
            }
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Sdk.GameStarted)
            {
                const string message = "Do you want to save current game?";
                const string caption = "Save current game";
                const MessageBoxButtons buttons = MessageBoxButtons.YesNoCancel;

                var result = MessageBox.Show(message, caption, buttons);
                if (result == DialogResult.Yes)
                {
                    Sdk.SaveGameToDisk(false);
                }
                else
                {
                    return;
                }
            }

            // load the game from disk
            string fileContents;
            var openFileDialog1 = new OpenFileDialog
            {
                Filter = @"SDO files (*.sdo)|*.sdo|All files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = false
            };
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                fileContents = File.ReadAllText(openFileDialog1.FileName);
                toolStripStatusLabel1.Text = openFileDialog1.FileName;
                SaveFileName = openFileDialog1.FileName;

            }
            else
            {
                return;
            }

            // start the game
            SaveFileName = string.Empty;
            TxtActivities.Text = string.Empty;
            Sdk.Seconds = 0;
            ClearBoard();
            Sdk.GameStarted = true;
            timer1.Enabled = true;
            toolStripStatusLabel1.Text = @"New game started";
            toolTip1.RemoveAll();

            // initialize the board
            int counter = 0;
            foreach (int row in Enumerable.Range(1, 9))
            {
                foreach (int col in Enumerable.Range(1, 9))
                {
                    try
                    {
                        if (int.Parse(fileContents[counter].ToString()) != 0)
                        {
                            SetCell(col, row, int.Parse(fileContents[counter].ToString()), false);
                        }
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show(exception.Message);
                        return;
                    }
                    counter += 1;
                }
            }

        }

        private void RedoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // if no more next move, then exit
            if (Sdk.RedoMoves.Count == 0) return;

            // remove from one stack and push into the moves stack
            var str = Sdk.RedoMoves.Pop();
            Sdk.Moves.Push(str);

            // save the value in the array
            SetCell(int.Parse(str[0].ToString()), int.Parse(str[1].ToString()), int.Parse(str[2].ToString()), true);
            TxtActivities.Text = TxtActivities.Text + Environment.NewLine + $@"Value reinserted at ({int.Parse(str[0].ToString())},{int.Parse(str[1].ToString())})";
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!Sdk.GameStarted)
            {
                Console.Beep();
                TxtActivities.Text = @"Game not started yet.";
                return;
            }

            Sdk.SaveGameToDisk(true);
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!Sdk.GameStarted)
            {
                TxtActivities.Text = @"Game not started yet.";
                return;
            }

            TxtActivities.Text = Sdk.SaveGameToDisk(false);
        }

        public void SetCell(int col, int row, int value, bool eraseable)
        {
            // Locate the particular Label control
            var control = Controls.Find($"{col}{row}", true).FirstOrDefault();
            var cellLabel = (CustomLabel)control;
            if (cellLabel == null)
            {
                return;
            }

            // save the value in the array
            Sdk.Actual[col, row] = value;

            // if erasing a cell, you need to reset the possible values for all cells
            if (value == 0)
            {
                foreach (var r in Enumerable.Range(1, 9))
                {
                    foreach (var c in Enumerable.Range(1, 9))
                    {
                        if (Sdk.Actual[c, r] == 0)
                        {
                            Sdk.Possible[c, r] = string.Empty;
                        }
                    }
                }
            }
            else
            {
                Sdk.Possible[col, row] = value.ToString();
            }

            // set the appearance for the Label control
            if (value == 0) // erasing the cell
            {
                cellLabel.Text = string.Empty;
                cellLabel.IsEraseable = eraseable;
                cellLabel.BackColor = _defaultBackcolor;
            }
            // means default puzzle values
            else
            {
                if (eraseable == false)
                {
                    cellLabel.BackColor = _fixedBackcolor;
                    cellLabel.ForeColor = _fixedForecolor;
                }
                // means user-set value
                else
                {
                    cellLabel.BackColor = _userBackcolor;
                    cellLabel.ForeColor = _userForecolor;
                }

                cellLabel.Text = value.ToString();
                cellLabel.IsEraseable = eraseable;
            }
        }

        private void SetCheckedOrNotChecked(ToolStripMenuItem menuItem)
        {
            foreach (ToolStripMenuItem item in menuStrip1.Items)
            {
                if (item.Name.Length == 0) continue;

                foreach (ToolStripMenuItem subItem in item.DropDownItems)
                {
                    subItem.Checked = subItem.Name == menuItem.Name;
                }
            }
        }

        public void SetToolTip(int col, int row, string possiblevalues)
        {
            // Locate the particular Label control
            var control = Controls.Find(col.ToString() + row, true).FirstOrDefault();
            var cellLabel = (CustomLabel)control;
            if (cellLabel == null)
            {
                return;
            }

            toolTip1.SetToolTip((CustomLabel)control, possiblevalues);
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            Sdk.DisplayElapsedTime();
        }

        public void StartNewGame()
        {
            SaveFileName = string.Empty;
            TxtActivities.Clear();
            Sdk.Seconds = 0;
            ClearBoard();
            Sdk.GameStarted = true;
            timer1.Enabled = true;
            toolStripStatusLabel1.Text = @"New game started.";
        }

        private void ToolStripButton_Click(object sender, EventArgs e)
        {
            var button = (ToolStripButton)sender;
            // uncheck all the button controls in the ToolStrip
            foreach (int i in Enumerable.Range(1, 10))
            {
                ((ToolStripButton)toolStrip1.Items[i]).Checked = false;
            }

            // set the selected button to "checked"
            button.Checked = true;
            // set the appropriate number selected
            Sdk.SelectedNumber = button.Text == @"Erase" ? 0 : int.Parse(button.Text);

        }

        private void UndoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // if no previous moves, then exit
            if (Sdk.Moves.Count == 0) return;

            // remove from one stack and push into the redo stack
            string s = Sdk.Moves.Pop();
            Sdk.RedoMoves.Push(s);

            // save the value in the array
            SetCell(int.Parse(s[0].ToString()), int.Parse(s[1].ToString()), 0, true);
            SetText = $@"Value removed at ({int.Parse(s[0].ToString())},{int.Parse(s[1].ToString())})";

        }

        private void CandidatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var possibleValues = Sdk.CheckCandidates();
            foreach (var item in possibleValues)
            {
                SetText = item;
            }
        }

        private void PossiblesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Sdk.GetPossiblesAndValues();
        }

        private void BtnClearTextBox_Click(object sender, EventArgs e)
        {
            TxtActivities.Clear();
        }

        private void BtnCheckCandidates_Click(object sender, EventArgs e)
        {
            var possibleValues = Sdk.CheckCandidates();
            var s = string.Empty;
            foreach (int col in Enumerable.Range(1, 9))
            {
                foreach (int row in Enumerable.Range(1, 9))
                {
                    s += possibleValues[col, row];
                    if (col == 9)
                    {
                        s += Environment.NewLine;
                        SetText = s;
                    }
                    s = string.Empty;
                }

            }
        }
    }
}

