using System;
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

        public Form1()
        {
            InitializeComponent();
            _Form1 = this;
        }

        public static Form1 _Form1;

        public bool HintMode { get; set; }

        // has the game started
        public bool GameStarted;

        public string SaveFileName { get; set; }

        // used to keep track of elapsed time
        public int Seconds;

        public string SetText
        {
            set => TxtActivities.AppendText(value + Environment.NewLine);
        }

        private void BtnHint_Click(object sender, EventArgs e)
        {
            // show hints one cell at a time
            HintMode = true;
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
            if (!GameStarted)
            {
                Console.Beep();
                SetText = @"Click File->New to start a new game or File->Open to load an existing game";
                return;
            }

            var cellLabel = (CustomLabel)sender;

            // determine the col and row of the selected cell
            var col = Sdk.SelectedColumn = int.Parse(cellLabel.Name.Substring(0, 1));
            var row = Sdk.SelectedRow = int.Parse(cellLabel.Name.Substring(1, 1));
            SetText = Sdk.CalculatePossibleValues(col, row);


            // if cell is not erasable then exit
            if (cellLabel.IsEraseable == false)
            {
                Console.Beep();
                SetText = @"This cell cannot be erased." + Environment.NewLine;
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
                    Sdk.SetCell(col, row, Sdk.SelectedNumber, true);
                    SetText = $@"Number erased at ({col},{row})";
                }
                else if (cellLabel.Text == string.Empty)
                {
                    // else setting a value; check if move is valid
                    if (!Sdk.IsMoveValid(col, row, Sdk.SelectedNumber))
                    {
                        SetText = $@"Invalid move at ({col},{row})";
                        return;
                    }

                    // save the value in the array
                    Sdk.SetCell(col, row, Sdk.SelectedNumber, true);
                    SetText = $@"Number placed at ({col},{row}) was {Sdk.SelectedNumber}";


                    // saves the move into the stack
                    Sdk.Moves.Push($"{cellLabel.Name}{Sdk.SelectedNumber}");

                    if (!Sdk.IsPuzzleSolved())
                    {
                        return;
                    }

                    timer1.Enabled = false;
                    Console.Beep();
                    toolStripStatusLabel1.Text = @"*****Puzzle Solved*****";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
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

            Sdk.DrawBoard();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            int y1, y2;

            // draw the horizontal lines
            var x1 = 1 * (Sdk.CellWidth + 1) + Sdk.XOffset - 1;
            var x2 = 9 * (Sdk.CellWidth + 1) + Sdk.XOffset + Sdk.CellWidth;
            for (int r = 1; r <= 10; r += 3)
            {
                y1 = r * (Sdk.CellHeight + 1) + Sdk.YOffset - 1;
                y2 = y1;
                e.Graphics.DrawLine(Pens.Black, x1, y1, x2, y2);
            }

            // draw the vertical lines
            y1 = 1 * (Sdk.CellHeight + 1) + Sdk.YOffset - 1;
            y2 = 9 * (Sdk.CellHeight + 1) + Sdk.YOffset + Sdk.CellHeight;
            for (int c = 1; c <= 10; c += 3)
            {
                x1 = c * (Sdk.CellWidth + 1) + Sdk.XOffset - 1;
                x2 = x1;
                e.Graphics.DrawLine(Pens.Black, x1, y1, x2, y2);
            }
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Sdk.CheckPossibles();
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
            if (GameStarted)
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
                        Sdk.SetCell(col, row, int.Parse(puzzle[counter].ToString()), false);
                    }
                    counter += 1;
                }
            }
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (GameStarted)
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
            Seconds = 0;
            Sdk.ClearBoard();
            GameStarted = true;
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
                            Sdk.SetCell(col, row, int.Parse(fileContents[counter].ToString()), false);
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
            Sdk.SetCell(int.Parse(str[0].ToString()), int.Parse(str[1].ToString()), int.Parse(str[2].ToString()), true);
            TxtActivities.Text = TxtActivities.Text + Environment.NewLine + $@"Value reinserted at ({int.Parse(str[0].ToString())},{int.Parse(str[1].ToString())})";
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!GameStarted)
            {
                Console.Beep();
                TxtActivities.Text = @"Game not started yet.";
                return;
            }

            Sdk.SaveGameToDisk(true);
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!GameStarted)
            {
                TxtActivities.Text = @"Game not started yet.";
                return;
            }

            TxtActivities.Text = Sdk.SaveGameToDisk(false);
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
            int ss = Seconds;
            int mm;

            if (Seconds >= 3600 && Seconds < 219600)
            {
                ss = Seconds % 3600;
                mm = Seconds / 3600;
                int hh = Seconds / 219600;
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

                toolStripStatusLabel2.Text = $@"Elapsed time: {elapsedTime}";
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
                toolStripStatusLabel2.Text = $@"Elapsed time: {elapsedTime}";

            }
            else if (Seconds > 0 && Seconds < 60)
            {
                toolStripStatusLabel2.Text = $@"Elapsed time: {ss} seconds";

            }
            Seconds += 1;
        }

        public void StartNewGame()
        {
            SaveFileName = string.Empty;
            TxtActivities.Clear();
            Seconds = 0;
            Sdk.ClearBoard();
            GameStarted = true;
            timer1.Enabled = true;
            toolStripStatusLabel1.Text = @"New game started.";
        }

        private void ToolStripButton_Click(object sender, EventArgs e)
        {
            var selectedButton = (ToolStripButton)sender;

            // uncheck all the button controls in the ToolStrip
            foreach (int i in Enumerable.Range(1, 10))
            {
                ((ToolStripButton)toolStrip1.Items[i]).Checked = false;
            }

            // set the selected button to "checked"
            selectedButton.Checked = true;

            // set the appropriate number selected
            Sdk.SelectedNumber = selectedButton.Text == @"Erase" ? 0 : int.Parse(selectedButton.Text);

        }

        private void UndoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // if no previous moves, then exit
            if (Sdk.Moves.Count == 0)
            {
                return;
            }

            // remove from one stack and push into the redo stack
            var str = Sdk.Moves.Pop();
            Sdk.RedoMoves.Push(str);

            // save the value in the array
            Sdk.SetCell(int.Parse(str[0].ToString()), int.Parse(str[1].ToString()), 0, true);
            SetText = $@"Value removed at ({int.Parse(str[0].ToString())},{int.Parse(str[1].ToString())})";
        }

        private void CandidatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetText = Sdk.CheckCandidates(Sdk.SelectedColumn, Sdk.SelectedRow, Sdk.Possible);
        }

        private void PossiblesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetText = Sdk.CalculatePossibleValues(Sdk.SelectedColumn, Sdk.SelectedRow);
        }

        private void BtnClearTextBox_Click(object sender, EventArgs e)
        {
            TxtActivities.Clear();
        }

    }
}

