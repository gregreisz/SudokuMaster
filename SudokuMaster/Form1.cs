using System;
using System.Drawing;
using System.Linq;
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
            // find the CustomLabel control
            var control = Controls.Find($"{col}{row}", true).FirstOrDefault();
            if (!(control is CustomLabel label)) return;
            label.Font = new Font("Consolas", 10, label.Font.Style | FontStyle.Regular);
            label.Text = input;
        }

        public void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void BtnCheckCandidates_Click(object sender, EventArgs e)
        {
        }

        private void BtnClearTextBox_Click(object sender, EventArgs e)
        {
            TxtActivities.Clear();
        }

        private void BtnHint_Click(object sender, EventArgs e)
        {
            // show hints one cell at a time
            Sdk.HintsMode = true;
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

        public void CandidatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var candidate in Sdk.CheckCandidates())
            {
                SetText(candidate);
            }
        }

        public void Cell_Click(object sender, EventArgs e)
        {
            Sdk.SudokuBoardHandler(sender, e);
        }

        public void CheckColumnsAndRowsMenuItem_Click(object sender, EventArgs e)
        {
            Sdk.CheckColumnsAndRows();
        }

        public void EasyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = (ToolStripMenuItem)sender;
            if (menuItem == null)
            {
                throw new ArgumentNullException(nameof(menuItem));
            }

            Sdk.SetLevel(menuItem.Name);
            Sdk.SetMenuItemChecked(menuItem);
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

            toolTip1.InitialDelay = 100;
            toolTip1.ReshowDelay = 100;
            toolTip1.AutoPopDelay = 5000;

            TxtActivities.TextChanged += TxtActivities_TextChanged;

            Sdk.CreateMainMenu();

            Sdk.AddCellLabels();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Sdk.PaintBoard(e);
        }

        public void ExitToolStripMenuItem_Click(object sender, EventArgs e)
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

        public void ExpertToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = (ToolStripMenuItem)sender;
            if (menuItem == null)
            {
                throw new ArgumentNullException(nameof(menuItem));
            }

            Sdk.SetLevel(menuItem.Name);
            Sdk.SetMenuItemChecked(menuItem);
        }

        public void HardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = (ToolStripMenuItem)sender;
            if (menuItem == null)
            {
                throw new ArgumentNullException(nameof(menuItem));
            }

            Sdk.SetLevel(menuItem.Name);
            Sdk.SetMenuItemChecked(menuItem);
        }

        public void MediumToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = (ToolStripMenuItem)sender;
            if (menuItem == null)
            {
                throw new ArgumentNullException(nameof(menuItem));
            }

            Sdk.SetLevel(menuItem.Name);
            Sdk.SetMenuItemChecked(menuItem);
        }

        public void NewToolStripMenuItem_Click(object sender, EventArgs e)
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
            Sdk.StartGame();

            Sdk.LoadSavedGame(puzzle);
        }

        public void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // check with the user to see if that want to save a game they may have opened
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

            // read the game file from disk
           var contents = Sdk.ReadInSavedGame();

            // initialize the game
            Sdk.StartGame();

            Sdk.LoadSavedGame(contents);
        }
    
        public void PossiblesToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        public void RedoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // if no more next move, then exit
            if (Sdk.RedoMoves.Count == 0) return;

            // remove from one stack and push into the moves stack
            var str = Sdk.RedoMoves.Pop();
            Sdk.Moves.Push(str);

            // save the value in the array
            Sdk.SetCell(int.Parse(str[0].ToString()), int.Parse(str[1].ToString()), int.Parse(str[2].ToString()));
            SetText($@"Value reinserted at ({int.Parse(str[0].ToString())},{int.Parse(str[1].ToString())})");
        }

        public void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!Sdk.GameStarted)
            {
                Console.Beep();
                TxtActivities.Text = @"Game not started yet.";
                return;
            }

            Sdk.SaveGameToDisk(true);
        }

        public void SaveToolStripMenuItem_Click(object sender, EventArgs e)
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
            // uncheck all the button controls in the ToolStrip
            foreach (var i in Enumerable.Range(1, 10))
            {
                ((ToolStripButton)toolStrip1.Items[i]).Checked = false;
                ((ToolStripButton)toolStrip1.Items[i]).AutoToolTip = false;
                ((ToolStripButton)toolStrip1.Items[i]).ToolTipText = string.Empty;
            }

            // set the selected button to "checked"
            var button = (ToolStripButton)sender;
            button.Checked = true;

            // set the appropriate number selected
            Sdk.SelectedNumber = button.Text == @"Erase" ? 0 : int.Parse(button.Text);

        }

        private void TxtActivities_TextChanged(object sender, EventArgs e)
        {
            var box = (TextBox)sender;
            box.SelectionStart = 0;
            box.SelectionLength = 1;
            box.ScrollToCaret();
        }

        public void UndoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // if no previous moves, then exit
            if (Sdk.Moves.Count == 0) return;

            // remove from one stack and push into the redo stack
            var s = Sdk.Moves.Pop();
            Sdk.RedoMoves.Push(Sdk.Moves.Pop());

            // save the value in the array
            Sdk.SetCell(int.Parse(s[0].ToString()), int.Parse(s[1].ToString()), 0);
            SetText($@"Value removed at ({int.Parse(s[0].ToString())},{int.Parse(s[1].ToString())})");
        }

    }
}