using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SudokuMaster
{
    public interface IForm1
    {
        void SetText(string input, bool writeLine);

        string SetStatus { set; }

        void SetLabel(int col, int row, string input);

    }

    public partial class Form1 : Form, IForm1
    {
        public static Form1 _Form1;

        private readonly Sudoku Sdk = new Sudoku();

        public Form1()
        {
            InitializeComponent();
            // This is to allow referencing controls on this form from classes
            _Form1 = this;
        }

        // This stores the file name for a game that will be saved to or loaded from disk.
        public string SaveFileName { get; set; }

        public string SetStatus
        {
            set => toolStripStatusLabel1.Text = value;
        }

        public void SetText(string input, bool writeLine = true)
        {
            if (!writeLine)
            {
                TxtActivities.AppendText(input);
            }
            TxtActivities.AppendText(input + Environment.NewLine);
        }

        public void SetLabel(int col, int row, string input)
        {
            // Locate the particular Label control
            var control = Controls.Find($"{col}{row}", true).FirstOrDefault();
            if (!(control is CustomLabel cellLabel))
            {
                return;
            }
            cellLabel.Font = new Font("Consolas", 9, cellLabel.Font.Style | FontStyle.Regular);
            cellLabel.Tag = cellLabel.Text = input;
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Sdk.GetCandidates();
        }

        private void BtnCheckCandidates_Click(object sender, EventArgs e)
        {
            Sdk.GetCandidates();
        }

        private void BtnClearTextBox_Click(object sender, EventArgs e)
        {
            TxtActivities.Clear();
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

        private void CandidatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var candidate in Sdk.CheckCandidates())
            {
                SetText(candidate);
            }
        }

        public void Cell_Click(object sender, EventArgs e)
        {
            Sdk.SudokuBoardHandler(sender);
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

        private void EasyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = (ToolStripMenuItem)sender;
            if (menuItem == null)
            {
                throw new ArgumentNullException(nameof(menuItem));
            }

            Sdk.SetLevel(menuItem.Name);
            Sdk.SetCheckedOnLevelMenuItems(menuItem);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // initialize the status bar
            toolStripStatusLabel1.Text = string.Empty;
            toolStripStatusLabel2.Text = string.Empty;

            toolStripButton1.Click += ToolStripButton_Click;
            toolStripButton2.Click += ToolStripButton_Click;
            toolStripButton3.Click += ToolStripButton_Click;
            toolStripButton4.Click += ToolStripButton_Click;
            toolStripButton5.Click += ToolStripButton_Click;
            toolStripButton6.Click += ToolStripButton_Click;
            toolStripButton7.Click += ToolStripButton_Click;
            toolStripButton8.Click += ToolStripButton_Click;
            toolStripButton9.Click += ToolStripButton_Click;
            toolStripButton10.Click += ToolStripButton_Click;

            toolTip1.InitialDelay = 100;
            toolTip1.ReshowDelay = 100;
            toolTip1.AutoPopDelay = 5000;

            TxtActivities.TextChanged += TxtActivities_TextChanged;

            Sdk.InitializeToolStripButtons();

            CreateMenu();

            Sdk.DrawBoard();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Sdk.PaintBoard(e);
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
            Sdk.SetCheckedOnLevelMenuItems(menuItem);
        }

        private void HardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = (ToolStripMenuItem)sender;
            if (menuItem == null)
            {
                throw new ArgumentNullException(nameof(menuItem));
            }

            Sdk.SetLevel(menuItem.Name);
            Sdk.SetCheckedOnLevelMenuItems(menuItem);
        }

        public void InitializeBoard(string fileContents)
        {
            // initialize the board
            var contents = string.Join("", Regex.Split(fileContents, @"(?:\r\n|\n|\r)"));
            if (contents.Length != 81)
            {
                throw new Exception("The file contents length was invalid.");
            }

            int counter = 0;
            foreach (var row in Enumerable.Range(1, 9))
            {
                foreach (var col in Enumerable.Range(1, 9))
                {
                    try
                    {
                        if (int.Parse(contents[counter].ToString()) != 0)
                        {
                            Sdk.SetCell(col, row, int.Parse(contents[counter].ToString()), false);
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

        private void MediumToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = (ToolStripMenuItem)sender;
            if (menuItem == null)
            {
                throw new ArgumentNullException(nameof(menuItem));
            }

            Sdk.SetLevel(menuItem.Name);
            Sdk.SetCheckedOnLevelMenuItems(menuItem);
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
            SetStatus = @"Generating new puzzle...";

            var puzzle = Sdk.GetPuzzle(Sdk.Level);

            // change back to the default cursor
            Cursor.Current = Cursors.Default;

            // start new game
            Sdk.StartNewGame();

            InitializeBoard(puzzle);
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
                SetStatus = openFileDialog1.FileName;
                SaveFileName = openFileDialog1.FileName;
            }
            else
            {
                return;
            }

            // start the game
            Sdk.StartNewGame();

            InitializeBoard(fileContents);

        }

        private void PossiblesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Sdk.GetCandidates();
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
            SetText($@"Value reinserted at ({int.Parse(str[0].ToString())},{int.Parse(str[1].ToString())})");
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
  
        public void SetToolTip(int col, int row, string possiblevalues)
        {
            // Locate the particular Label control
            var control = Controls.Find(col.ToString() + row, true).FirstOrDefault();
            var cellLabel = (CustomLabel)control;
            if (cellLabel == null)
            {
                return;
            }

            toolTip1.SetToolTip(cellLabel, possiblevalues);
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            Sdk.DisplayElapsedTime();
        }

        private void ToolStripButton_Click(object sender, EventArgs e)
        {
            // set the selected button to "checked"
            var button = (ToolStripButton)sender;
            button.Checked = true;

            // set the appropriate number selected
            Sdk.SelectedNumber = button.Text == @"Erase" ? 0 : int.Parse(button.Text);
        }

        private void UndoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // if no previous moves, then exit
            if (Sdk.Moves.Count == 0) return;

            // remove from one stack and push into the redo stack
            var s = Sdk.Moves.Pop();
            Sdk.RedoMoves.Push(Sdk.Moves.Pop());

            // save the value in the array
            Sdk.SetCell(int.Parse(s[0].ToString()), int.Parse(s[1].ToString()), 0, true);
            SetText($@"Value removed at ({int.Parse(s[0].ToString())},{int.Parse(s[1].ToString())})");
        }

        private void TxtActivities_TextChanged(object sender, EventArgs e)
        {
            var box = (TextBox)sender;
            box.SelectionStart = 0;
            box.SelectionLength = 1;
            box.ScrollToCaret();
        }
    }
}