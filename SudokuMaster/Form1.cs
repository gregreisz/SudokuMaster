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

        string SetStatus2 { set; }

        void SetLabel(int col, int row, string input);

    }

    public partial class Form1 : Form, IForm1
    {
        public static Form1 _Form1;

        public Sudoku _Sudoku = new Sudoku();

        public Form1()
        {
            InitializeComponent();

            _Form1 = this;
        }

        public string SetStatus
        {
            set => toolStripStatusLabel1.Text = value;
        }

        public string SetStatus2
        {
            set => toolStripStatusLabel2.Text = value;
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
            label.Font = new Font(_Sudoku.labelLargeFontName, 10, label.Font.Style | FontStyle.Bold);
            label.Text = input;
        }

        private void BtnClearTextBox_Click(object sender, EventArgs e)
        {
            TxtActivities.Clear();
            toolStripStatusLabel2.Text = string.Empty;
        }

        private void BtnCheckCandidates_Click(object sender, EventArgs e)
        {
            TxtActivities.Clear();
            TxtActivities.SelectionLength = 0;
            _Sudoku.RefreshAllPossiblesValues();
        }

        private void BtnHint_Click(object sender, EventArgs e)
        {
            // show hints one cell at a time
            _Sudoku.HintsMode = true;
            try
            {
                _Sudoku.CheckColumnsAndRows();
            }
            catch (Exception ex)
            {
                MessageBox.Show(@"Please undo your move", @"Invalid Move", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MessageBox.Show(ex.Message);
            }
        }

        public void CandidatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _Sudoku.CheckCandidates();
        }

        public void Cell_Click(object sender, EventArgs e)
        {
            _Sudoku.SudokuBoardHandler(sender);
        }

        public void CheckColumnsAndRowsMenuItem_Click(object sender, EventArgs e)
        {
            _Sudoku.CheckColumnsAndRows();
        }

        public void CreateMainMenu()
        {
            var form = Form1._Form1;

            // create the File menu
            var fileItem = new ToolStripMenuItem("&File");

            var openSubItem = new ToolStripMenuItem("&Open");
            openSubItem.Click += form.OpenToolStripMenuItem_Click;

            var saveSubItem = new ToolStripMenuItem("&Save");
            saveSubItem.Click += form.SaveToolStripMenuItem_Click;

            var saveAsSubItem = new ToolStripMenuItem("Save&As");
            saveAsSubItem.Click += form.SaveAsToolStripMenuItem_Click;

            var exitSubItem = new ToolStripMenuItem("E&xit");
            exitSubItem.Click += form.ExitToolStripMenuItem_Click;

            //fileItem.DropDownItems.Add(newSubItem);
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

            var CandidatesToolStripMenuItem = new ToolStripMenuItem("Check &Candidates");
            CandidatesToolStripMenuItem.Click += form.CandidatesToolStripMenuItem_Click;

            //var PossiblesToolStripMenuItem = new ToolStripMenuItem("Check &Possibles");

            toolsItem.DropDownItems.Add(CandidatesToolStripMenuItem);
            //toolsItem.DropDownItems.Add(PossiblesToolStripMenuItem);

            // create the Help menu
            var helpItem = new ToolStripMenuItem("&Help");
            var aboutSubItem = new ToolStripMenuItem("&About");
            helpItem.DropDownItems.Add(aboutSubItem);

            form.menuStrip1.Items.Add(fileItem);
            form.menuStrip1.Items.Add(editItem);
            form.menuStrip1.Items.Add(levelItem);
            form.menuStrip1.Items.Add(toolsItem);
            form.menuStrip1.Items.Add(helpItem);
        }

        public void EasyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = (ToolStripMenuItem)sender;
            if (menuItem == null)
            {
                throw new ArgumentNullException(nameof(menuItem));
            }

            _Sudoku.SetLevel(menuItem.Name);
            _Sudoku.SetMenuItemChecked(menuItem);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
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

            TxtActivities.TextChanged += TxtActivities_TextChanged;

            toolTip1.InitialDelay = 100;
            toolTip1.ReshowDelay = 100;
            toolTip1.AutoPopDelay = 5000;


            CreateMainMenu();

            _Sudoku.AddCellLabelsToBoard();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            _Sudoku.PaintBoard(e);
        }

        public void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            const string message = "Do you want to save the current game?";
            const string caption = "Save current game";
            const MessageBoxButtons buttons = MessageBoxButtons.YesNoCancel;

            var result = MessageBox.Show(message, caption, buttons);
            if (result == DialogResult.Yes)
            {
                _Sudoku.SaveGameToDisk(false);
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

            _Sudoku.SetLevel(menuItem.Name);
            _Sudoku.SetMenuItemChecked(menuItem);
        }

        public void HardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = (ToolStripMenuItem)sender;
            if (menuItem == null)
            {
                throw new ArgumentNullException(nameof(menuItem));
            }

            _Sudoku.SetLevel(menuItem.Name);
            _Sudoku.SetMenuItemChecked(menuItem);
        }

        public void MediumToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = (ToolStripMenuItem)sender;
            if (menuItem == null)
            {
                throw new ArgumentNullException(nameof(menuItem));
            }

            _Sudoku.SetLevel(menuItem.Name);
            _Sudoku.SetMenuItemChecked(menuItem);
        }

        public void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // check with the user to see if that want to save a game they may have opened
            if (_Sudoku.GameStarted)
            {
                const string message = "Do you want to save current game?";
                const string caption = "Save current game";
                const MessageBoxButtons buttons = MessageBoxButtons.YesNoCancel;

                var result = MessageBox.Show(message, caption, buttons);
                if (result == DialogResult.Yes)
                {
                    _Sudoku.SaveGameToDisk(false);
                }
            }

            // read the game file from disk
            var contents = _Sudoku.ReadInSavedGame();

            // initialize the game
            _Sudoku.StartGame();

            SetStatus2 = _Sudoku.LoadSavedGame(contents) ? @"Game loaded successfully" : "Game failed to load successfully";

        }

        public void RedoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // if no more next move, then exit
            if (_Sudoku.RedoMoves.Count == 0) return;

            // remove from one stack and push into the moves stack
            var str = _Sudoku.RedoMoves.Pop();
            _Sudoku.Moves.Push(str);

            // save the value in the array
            int col = int.Parse(str[0].ToString());
            int row = int.Parse(str[1].ToString());
            int value = int.Parse(str[2].ToString());

            _Sudoku.SetCell(col, row, value);
            SetText($@"Value reinserted at ({col},{row})");
        }

        public void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!_Sudoku.GameStarted)
            {
                Console.Beep();
                TxtActivities.Text = @"Game not started yet.";
                return;
            }

            _Sudoku.SaveGameToDisk(true);
        }

        public void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!_Sudoku.GameStarted)
            {
                TxtActivities.Text = @"Game not started yet.";
                return;
            }

            TxtActivities.Text = _Sudoku.SaveGameToDisk(false);
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
            _Sudoku.DisplayElapsedTime();
        }

        private void ToolStripButton_Click(object sender, EventArgs e)
        {
            // uncheck all the button controls in the ToolStrip
            foreach (var item in toolStrip1.Items)
            {
                if (!(item is ToolStripButton button))
                {
                    continue;
                }

                button.Checked = false;
                button.AutoToolTip = false;
                button.ToolTipText = string.Empty;
            }

            // set the selected button to "checked"
            var selectedButton = (ToolStripButton)sender;
            selectedButton.Checked = true;

            // set the appropriate number selected
            _Sudoku.SelectedNumber = selectedButton.Text == @"Erase" ? 0 : int.Parse(selectedButton.Text);

        }

        private void TxtActivities_TextChanged(object sender, EventArgs e)
        {
            var box = (TextBox)sender;
            box.SelectionStart = 0;
            box.SelectionLength = 0;
            box.ScrollToCaret();
        }

        public void UndoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // if no previous moves, then exit
            if (_Sudoku.Moves.Count == 0) return;

            // remove from one stack and push into the redo stack
            var s = _Sudoku.Moves.Pop();
            _Sudoku.RedoMoves.Push(_Sudoku.Moves.Pop());


            // save the value in the array
            int col = int.Parse(s[0].ToString());
            int row = int.Parse(s[1].ToString());

            //const int value = 0;
            _Sudoku.SetCell(col, row, 0);
            //_Sudoku.board.SetCellValue(row, col, value);
            SetText($@"Value removed at ({col},{row}).");
        }

    }
}