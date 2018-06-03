using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using log4net;
using log4net.Config;

namespace SudokuMaster
{

    public interface IForm1
    {
        void SetText(string input);

        void SetStatus(string value, bool beep);

        void SetStatus2(string value, bool beep);

        void SetLabel(int col, int row, string input);

    }

    public partial class Form1 : Form, IForm1
    {

        #region
        //private int Level { get; set; } = 1; 
        #endregion

        private const string saveGame = "Do you want to save current game?";
        private const string saveCurrentGame = "Save current game?";
        public const string LargeFontName = "Verdana";
        public const string SmallFontName = "Consolas";
        public const int SmallFontSize = 6;
        public const int LargeFontSize = 10;
        private const int CellHeight = 32;
        private const int CellWidth = 32;
        private const int XOffset = -20;
        private const int YOffset = 25;


        // number the user selected from the toolStrip
        public int SelectedNumber { get; set; } = 1;

        public string SavedFileName { get; set; }

        public string CurrentGameState { get; set; }

        public static Form1 _Form1;

        public ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void SetStatus(string value, bool beep = false)
        {
            if (beep) Console.Beep();
            toolStripStatusLabel1.Text = value;
        }

        public void SetStatus2(string value, bool beep = false)
        {
            if (beep) Console.Beep();
            toolStripStatusLabel2.Text = value;
        }

        // time that the game starts
        public DateTime StartTime { get; set; }

        public void SetLabel(int col, int row, string input)
        {
            // find the CustomLabel control
            var control = Controls.Find($"{col}{row}", true).FirstOrDefault();
            if (!(control is CustomLabel label)) return;
            label.Font = new Font(LargeFontName, 10, label.Font.Style | FontStyle.Bold);
            label.Text = input;
        }

        public void SetText(string input)
        {
            TxtActivities.AppendText(input + Environment.NewLine);
            TxtActivities.SelectionStart = TxtActivities.TextLength;
            TxtActivities.SelectionStart = 0;
            TxtActivities.ScrollToCaret();
        }

        private readonly Sudoku _sudoku = new Sudoku();

        public Form1()
        {
            InitializeComponent();

            _Form1 = this;

            XmlConfigurator.Configure();

        }

        public bool GameHasStarted { get; set; }

        public bool GameHasEnded { get; set; }

        private void BtnClearTextBox_Click(object sender, EventArgs e)
        {
            TxtActivities.Clear();
            TxtActivities.SelectionStart = 0;
            TxtActivities.ScrollToCaret();

        }

        public void CandidatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _sudoku.CheckCandidates();
        }

        private void Cell_Click(object sender)
        {
            _sudoku.SudokuBoardHandler(sender);
        }

        private void CreateMainMenu()
        {
            // create the File menu
            var fileItem = new ToolStripMenuItem("&File");
            var openSubItem = new ToolStripMenuItem("&Open");
            openSubItem.Click += OpenToolStripMenuItem_Click;
            var saveSubItem = new ToolStripMenuItem("&Save");
            saveSubItem.Click += SaveToolStripMenuItem_Click;
            var saveAsSubItem = new ToolStripMenuItem("Save&As");
            saveAsSubItem.Click += SaveAsToolStripMenuItem_Click;
            var exitSubItem = new ToolStripMenuItem("E&xit");
            exitSubItem.Click += ExitToolStripMenuItem_Click;
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

            var toolsItem = new ToolStripMenuItem("&Tools");
            var CandidatesToolStripMenuItem = new ToolStripMenuItem("Check &Candidates");
            CandidatesToolStripMenuItem.Click += CandidatesToolStripMenuItem_Click;
            toolsItem.DropDownItems.Add(CandidatesToolStripMenuItem);

            // create the Help menu
            var helpItem = new ToolStripMenuItem("&Help");
            var aboutSubItem = new ToolStripMenuItem("&About");
            helpItem.DropDownItems.Add(aboutSubItem);

            menuStrip1.Items.Add(fileItem);
            menuStrip1.Items.Add(editItem);
            //menuStrip1.Items.Add(levelItem);
            menuStrip1.Items.Add(toolsItem);
            menuStrip1.Items.Add(helpItem);
        }

        public void ClearBoard()
        {
            Array.Clear(_sudoku.CellValues, 0, _sudoku.CellValues.Length);
            Array.Clear(_sudoku.Candidates, 0, _sudoku.Candidates.Length);

            // initialize the stacks
            _sudoku.Moves = new Stack<string>();
            _sudoku.RedoMoves = new Stack<string>();

            // initialize the cells in the board
            foreach (int row in Enumerable.Range(1, 9))
            {
                foreach (int col in Enumerable.Range(1, 9))
                {
                    SetCell(col, row, 0);
                }
            }
        }

        public void StartNewGame()
        {
            StartTime = DateTime.Now;
            GameHasStarted = true;
            CurrentGameState = string.Empty;
            timer1.Start();
            //_log.Info(@"New game started");

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

            CreateMainMenu();
            ClearBoard();
            InitializeBoard();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
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

        public void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //GetGameSaveInfo();
            Application.Exit();
        }

        public void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {

            // ask user if they want to save their current game to disk if any
            // if (GetSaveGameResult()) return;

            // initialize arrays
            StartNewGame();

            ClearLabelValues();

            // load the game from disk and reset CurrentGameState
            var contents = CurrentGameState = _sudoku.LoadGameFromDisk();
            _Form1.Text = SavedFileName;

            // set up the board with the saved game
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

            _sudoku.ShowNotes();

        }

        public void InitializeBoard()
        {
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
                        BorderStyle = BorderStyle.FixedSingle,
                        Location = location,
                        Width = CellWidth,
                        Height = CellHeight,
                        TextAlign = ContentAlignment.MiddleCenter
                    };

                    label.Click += (sender, e) => Cell_Click(sender);
                    Controls.Add(label);
                }
        }

        private bool GetSaveGameResult()
        {
            if (!GameHasStarted)
            {
                return false;
            }

            var response = MessageBox.Show(saveGame, saveCurrentGame, MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (response == DialogResult.Yes)
            {
                _sudoku.SaveGameToDisk(false);
            }
            else if (response == DialogResult.Cancel)
            {
                return true;
            }

            return false;
        }

        public void RedoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // if no more next move, then exit
            if (_sudoku.RedoMoves.Count == 0) return;

            // remove from one stack and push into the moves stack
            var str = _sudoku.RedoMoves.Pop();
            _sudoku.Moves.Push(str);

            // save the value in the array
            int col = int.Parse(str[0].ToString());
            int row = int.Parse(str[1].ToString());
            int value = int.Parse(str[2].ToString());

            SetCell(col, row, value);
            SetText($@"Value reinserted at ({col},{row})");
        }

        public void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!GameHasStarted)
            {
                Console.Beep();
                SetStatus(@"Game not started yet.");
                return;
            }

            _sudoku.SaveGameToDisk(true);
        }

        public void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!GameHasStarted)
            {
                SetStatus(@"Game not started yet.");
                return;
            }

            _sudoku.SaveGameToDisk(false);
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
            SelectedNumber = selectedButton.Text == @"Erase" ? 0 : int.Parse(selectedButton.Text);

        }

        public void UndoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // if no previous moves, then exit
            if (_sudoku.Moves.Count == 0) return;

            // remove from one stack and push into the redo stack
            var s = _sudoku.Moves.Pop();
            _sudoku.RedoMoves.Push(_sudoku.Moves.Pop());


            // save the value in the array
            int col = int.Parse(s[0].ToString());
            int row = int.Parse(s[1].ToString());

            SetCell(col, row, 0);
            SetStatus($@"Value removed at ({col},{row}).");
        }

        private void BtnViewCandidates_Click(object sender, EventArgs e)
        {
            _sudoku.CheckCandidates();
            TxtActivities.SelectionStart = TxtActivities.TextLength;
            TxtActivities.ScrollToCaret();

        }

        public void SetCell(int col, int row, int value)
        {
            var control = Controls.Find($"{col}{row}", true).FirstOrDefault();
            var cellLabel = (CustomLabel)control;
            if (cellLabel == null) return;

            // save the value in the array
            _sudoku.CellValues[col, row] = value;
            var counter1 = 0;
            var counter2 = 0;
            var counter3 = 0;
            // if erasing a cell, you need to reset the possible values for all cells
            if (value == 0)
            {
                foreach (int r in Enumerable.Range(1, 9))
                {
                    foreach (int c in Enumerable.Range(1, 9))
                    {
                        if (_sudoku.CellValues[c, r] == 0)
                        {
                            _sudoku.Candidates[c, r] = string.Empty;
                        }
                    }
                }
            }
            else
            {
                _sudoku.Candidates[col, row] = value.ToString();
            }

            // set the properties for the label
            if (value == 0 && cellLabel.Value == null)
            {

                cellLabel.Value = value;
                cellLabel.IsEraseable = true;
                cellLabel.BackColor = Color.LightYellow;
                cellLabel.ForeColor = Color.Black;
                cellLabel.Font = new Font(LargeFontName, LargeFontSize, FontStyle.Bold);
                counter1++;
                _log.Info($"Counter 1 = {counter1}{col},{row}({value} > 0 && {cellLabel.Value} != null)");
            }
            else if (value > 0 && cellLabel.Value == null)
            {
                cellLabel.Value = value;
                cellLabel.IsEraseable = false;
                cellLabel.BackColor = Color.LightSteelBlue;
                cellLabel.ForeColor = Color.Blue;
                cellLabel.Font = new Font(LargeFontName, LargeFontSize, FontStyle.Bold);
                counter2++;
                _log.Info($"Counter 2 = {counter2}{col},{row}({value} > 0 && {cellLabel.Value} != null)");
            }
            else if (value > 0 && cellLabel.IsEraseable)
            {
                cellLabel.Value = value;
                cellLabel.BackColor = Color.LightYellow;
                cellLabel.ForeColor = Color.Black;
                cellLabel.Font = new Font(LargeFontName, LargeFontSize, FontStyle.Bold);
                counter3++;
                _log.Info($"Counter 3 = {counter3}{col},{row}({value} > 0 && {cellLabel.Value} != null)");
            }


            cellLabel.Text = value.ToString();
        }

        private void ClearLabelValues()
        {
            foreach (var control in Controls)
            {
                if (control is CustomLabel label)
                {
                    label.Value = null;
                }

            }
        }

        private void BtnShowNotes_Click(object sender, EventArgs e)
        {
            _sudoku.ShowNotes();
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            timer1.Enabled = true;
            timer1.Start();

            var eTime = DateTime.Now - StartTime;
            SetStatus($@"Time Elapsed {eTime.Hours:00}:{eTime.Minutes:00}:{eTime.Seconds:00}");
        }

        private void BtnCheckValues_Click(object sender, EventArgs e)
        {
            _sudoku.CheckValues();
        }

        private void TxtActivities_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
