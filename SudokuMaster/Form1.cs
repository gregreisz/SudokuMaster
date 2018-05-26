using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.ComponentModel;
using System.Threading;
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

    public partial class Form1 : Form, IForm1, INotifyPropertyChanged
    {

        #region
        //private readonly string labelSmallFontName = "Consolas";
        //private readonly int labelSizeSmall = 6;
        //private readonly int labelSizeLarge = 10;
        //private int Level { get; set; } = 1; 
        #endregion

        private const string saveGame = "Do you want to save current game?";
        private const string saveCurrentGame = "Save current game?";
        private const string labelLargeFontName = "Verdana";
        private const int CellHeight = 32;
        private const int CellWidth = 32;
        private const int XOffset = -20;
        private const int YOffset = 25;


        // number the user selected from the toolStrip
        public int SelectedNumber { get; set; } = 1;

        public static Form1 _Form1;

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

        public DateTime StartTime { get; set; }

        public void SetLabel(int col, int row, string input)
        {
            // find the CustomLabel control
            var control = Controls.Find($"{col}{row}", true).FirstOrDefault();
            if (!(control is CustomLabel label)) return;
            label.Font = new Font(labelLargeFontName, 10, label.Font.Style | FontStyle.Bold);
            label.Text = input;
        }

        public void SetText(string input)
        {
            TxtActivities.Clear();
            TxtActivities.AppendText(Environment.NewLine + input);
            TxtActivities.SelectionStart = TxtActivities.TextLength;
            TxtActivities.ScrollToCaret();
        }

        private readonly Sudoku _sudoku = new Sudoku();

        public Form1()
        {
            InitializeComponent();

            _Form1 = this;

            XmlConfigurator.Configure();

            backgroundWorker1.DoWork += BackgroundWorker1_DoWork;
            backgroundWorker1.ProgressChanged += BackgroundWorker1_ProgressChanged;
            backgroundWorker1.RunWorkerCompleted += BackgroundWorker1_RunWorkerCompleted;
            backgroundWorker1.WorkerReportsProgress = backgroundWorker1.WorkerSupportsCancellation = true;
            backgroundWorker1.RunWorkerAsync();
        }

        private bool _cancel;

        private void BackgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null) MessageBox.Show(e.Error.ToString());
            if (_cancel) Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (backgroundWorker1.IsBusy) _cancel = e.Cancel = true;
            backgroundWorker1.CancelAsync();
        }

        private void BackgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            SetStatus(e.UserState as string);
        }

        private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!backgroundWorker1.CancellationPending)
            {
                var elapsed = DateTime.Now.Subtract(StartTime);

                var etime = $"Time Elapsed {elapsed.Hours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
                backgroundWorker1.ReportProgress(0, etime);
                Thread.Sleep(15);
            }
        }

        private void BtnCheckValues_Click(object sender, EventArgs e)
        {
            TxtActivities.Clear();
            _sudoku.CheckValues();
            TxtActivities.SelectionStart = TxtActivities.TextLength;
            TxtActivities.ScrollToCaret();

        }

        public void CandidatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //_Sudoku.CheckCandidates();
        }

        private void Cell_Click(object sender)
        {
            _sudoku.SudokuBoardHandler(sender);
        }

        //private void CheckColumnsAndRowsMenuItem_Click(object sender, EventArgs e)
        //{
        //    _sudoku.CheckColumnsAndRows();
        //}

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

            //// create the Level menu
            //var levelItem = new ToolStripMenuItem("&Level") {Name = "LevelMenuItem"};
            //var easyToolStripMenuItem = new ToolStripMenuItem("&Easy") {Name = "EasyToolStripMenuItem"};
            ////easyToolStripMenuItem.Click += EasyToolStripMenuItem_Click;
            //var mediumToolStripMenuItem = new ToolStripMenuItem("&Medium") {Name = "MediumToolStripMenuItem"};
            //mediumToolStripMenuItem.Click += MediumToolStripMenuItem_Click;
            //var hardToolStripMenuItem = new ToolStripMenuItem("&Hard") {Name = "HardToolStripMenuItem"};
            //hardToolStripMenuItem.Click += HardToolStripMenuItem_Click;
            //var expertToolStripMenuItem = new ToolStripMenuItem("E&xpert") {Name = "ExpertToolStripMenuItem"};
            //expertToolStripMenuItem.Click += ExpertToolStripMenuItem_Click;
            //levelItem.DropDownItems.Add(easyToolStripMenuItem);
            //levelItem.DropDownItems.Add(mediumToolStripMenuItem);
            //levelItem.DropDownItems.Add(hardToolStripMenuItem);
            //levelItem.DropDownItems.Add(expertToolStripMenuItem);

            // create the Tools menu
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

        //public void EasyToolStripMenuItem_Click(object sender, EventArgs e)
        //{
        //    var menuItem = (ToolStripMenuItem) sender;
        //    if (menuItem == null)
        //    {
        //        throw new ArgumentNullException(nameof(menuItem));
        //    }

        //    SetLevel(menuItem.Name);
        //    SetMenuItemChecked(menuItem);
        //}


        public event PropertyChangedEventHandler PropertyChanged;

        private bool _gameHasStarted = true;

        public bool GameHasStarted
        {
            get => _gameHasStarted;
            set
            {
                _gameHasStarted = value;

                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged(nameof(GameHasStarted));
            }
        }

        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(name));
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

            AddLabelsToBoard();

            //SetStatus = "Time Elapsed 00:00:00.00";
        }

        public void GameHasStarted_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            SetText($"sender{sender} {e} {DateTime.Now}");
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
            GetGameSaveInfo();
            Application.Exit();
        }

        //public void ExpertToolStripMenuItem_Click(object sender, EventArgs e)
        //{
        //    var menuItem = (ToolStripMenuItem) sender;
        //    if (menuItem == null)
        //    {
        //        throw new ArgumentNullException(nameof(menuItem));
        //    }

        //    SetLevel(menuItem.Name);
        //    SetMenuItemChecked(menuItem);
        //}

        //public void SetMenuItemChecked(ToolStripMenuItem menuItem)
        //{
        //    foreach (ToolStripMenuItem item in menuStrip1.Items)
        //    {
        //        if (item.Name.Length == 0)
        //        {
        //            continue;
        //        }

        //        foreach (ToolStripMenuItem subItem in item.DropDownItems)
        //            subItem.Checked = subItem.Name == menuItem.Name;
        //    }
        //}

        //public void SetLevel(string menuItemName)
        //{
        //    switch (menuItemName)
        //    {
        //        case "EasyToolStripMenuItem":
        //            Level = 1;
        //            break;
        //        case "MediumToolStripMenuItem":
        //            Level = 2;
        //            break;
        //        case "HardToolStripMenuItem":
        //            Level = 3;
        //            break;
        //        case "ExpertToolStripMenuItem":
        //            Level = 4;
        //            break;
        //        default:
        //            Level = 1;
        //            break;
        //    }
        //}

        //public void HardToolStripMenuItem_Click(object sender, EventArgs e)
        //{
        //    var menuItem = (ToolStripMenuItem) sender;
        //    if (menuItem == null)
        //    {
        //        throw new ArgumentNullException(nameof(menuItem));
        //    }

        //    SetLevel(menuItem.Name);
        //    SetMenuItemChecked(menuItem);
        //}

        //public void MediumToolStripMenuItem_Click(object sender, EventArgs e)
        //{
        //    var menuItem = (ToolStripMenuItem) sender;
        //    if (menuItem == null)
        //    {
        //        throw new ArgumentNullException(nameof(menuItem));
        //    }

        //    SetLevel(menuItem.Name);
        //    SetMenuItemChecked(menuItem);
        //}

        public void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {

            // ask user if they want to save their current game to disk if any
            if (GetGameSaveInfo()) return;

            _sudoku.StartNewGame();


            // load the game from disk
            _sudoku.CurrentGameState = _sudoku.LoadGameFromDisk();

            // set up the board with the saved game
            _sudoku.RefreshGameBoard();


            //Sudoku.Counter = 0;
            //var counter = 0;
            //foreach (var row in Enumerable.Range(1, 9))
            //{
            //    foreach (var col in Enumerable.Range(1, 9))
            //    {
            //        var value = int.Parse(Sudoku.CurrentGameState[counter].ToString());
            //        counter++;
            //        Sudoku.SetCell(col, row, value);
            //    }

            //}



            StartTime = DateTime.Now;
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
                    IsLocked = null
                };
                label.Click += (sender, e) => Cell_Click(sender);
                Controls.Add(label);
            }
        }

        private bool GetGameSaveInfo()
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
                SetStatus2(@"Game not started yet.");
                return;
            }

            _sudoku.SaveGameToDisk(true);
        }

        public void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!GameHasStarted)
            {
                SetStatus2(@"Game not started yet.");
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
            var selectedButton = (ToolStripButton) sender;
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
            SetStatus2($@"Value removed at ({col},{row}).");
        }

        private void BtnViewCandidates_Click(object sender, EventArgs e)
        {
            TxtActivities.Clear();
            _sudoku.CheckCandidates();
            TxtActivities.SelectionStart = TxtActivities.TextLength;
            TxtActivities.ScrollToCaret();

        }

        public void SetCell(int col, int row, int value, bool eraseable = false)
        {
            var control = Controls.Find($"{col}{row}", true).FirstOrDefault();
            var cellLabel = (CustomLabel) control;
            if (cellLabel == null) return;

            // save the value in the array
            _sudoku.CellValues[col, row] = value;

            // if erasing a cell, you need to reset the possible values for all cells
            if (value == 0)
            {
                for (int r = 1; r <= 9; r++)
                {
                    for (int c = 1; c <= 9; c++)
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

            // set the appearance for the Label control
            if (value == 0) // erasing the cell
            {
                cellLabel.Text = string.Empty;
                cellLabel.IsEraseable = eraseable;
                cellLabel.BackColor = Color.LightYellow;
            }
            else
            {
                if (!eraseable) // means default puzzle values
                {
                    cellLabel.BackColor = Color.SteelBlue;
                    cellLabel.ForeColor = Color.Blue;
                }
                else // means user-set value
                {
                    cellLabel.BackColor = Color.LightYellow;
                    cellLabel.ForeColor = Color.Black;
                }

                cellLabel.Text = value.ToString();
            }
        }


    }
}
