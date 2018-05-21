﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.ComponentModel;
using System.Threading;

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
        #region Private Constants
        private const string saveGame = "Do you want to save current game?";
        private const string saveCurrentGame = "Save current game?";
        #endregion

        public static Form1 _Form1;

        private Sudoku sudoku = new Sudoku();

        public Sudoku Sudoku { get => sudoku; set => sudoku = value; }

        #region Public Methods for Referencing Form1 Controls

        public void SetStatus(string value, bool beep = false)
        {
            if(beep) Console.Beep();
            toolStripStatusLabel1.Text = value;
        }

        public void SetStatus2(string value, bool beep = false)
        {
            if (beep) Console.Beep();
            toolStripStatusLabel2.Text = value;
        }

        public DateTime StartTime { get => startTime; set => startTime = value; }

        public void SetLabel(int col, int row, string input)
        {
            // find the CustomLabel control
            var control = Controls.Find($"{col}{row}", true).FirstOrDefault();
            if (!(control is CustomLabel label)) return;
            label.Font = new Font(Sudoku.labelLargeFontName, 10, label.Font.Style | FontStyle.Bold);
            label.Text = input;
        }

        public void SetText(string input)
        {
            TxtActivities.Clear();
            TxtActivities.AppendText(Environment.NewLine + input);
            TxtActivities.SelectionStart = TxtActivities.TextLength;
            TxtActivities.ScrollToCaret();
        }

        #endregion

        public Form1()
        {
            InitializeComponent();

            _Form1 = this;

            backgroundWorker1.DoWork += BackgroundWorker1_DoWork;
            backgroundWorker1.ProgressChanged += BackgroundWorker1_ProgressChanged;
            backgroundWorker1.RunWorkerCompleted += BackgroundWorker1_RunWorkerCompleted;
            backgroundWorker1.WorkerReportsProgress = backgroundWorker1.WorkerSupportsCancellation = true;
            backgroundWorker1.RunWorkerAsync();
        }

        private DateTime startTime;

        private bool mCancel;

        private void BackgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null) MessageBox.Show(e.Error.ToString());
            if (mCancel) Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (backgroundWorker1.IsBusy) mCancel = e.Cancel = true;
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
                var elapsed = DateTime.Now.Subtract(startTime);

                var etime = $"Time Elapsed {elapsed.Hours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
                backgroundWorker1.ReportProgress(0, etime);
                Thread.Sleep(15);
            }
        }

        private void BtnCheckValues_Click(object sender, EventArgs e)
        {
            TxtActivities.Clear();
            Sudoku.CheckValues();
            TxtActivities.SelectionStart = TxtActivities.TextLength;
            TxtActivities.ScrollToCaret();

        }

        public void CandidatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //_Sudoku.CheckCandidates();
        }

        public void Cell_Click(object sender)
        {
            Sudoku.SudokuBoardHandler(sender);
        }

        public void CheckColumnsAndRowsMenuItem_Click(object sender, EventArgs e)
        {
            Sudoku.CheckColumnsAndRows();
        }

        public void CreateMainMenu()
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
            toolsItem.DropDownItems.Add(CandidatesToolStripMenuItem);

            // create the Help menu
            var helpItem = new ToolStripMenuItem("&Help");
            var aboutSubItem = new ToolStripMenuItem("&About");
            helpItem.DropDownItems.Add(aboutSubItem);

            menuStrip1.Items.Add(fileItem);
            menuStrip1.Items.Add(editItem);
            menuStrip1.Items.Add(levelItem);
            menuStrip1.Items.Add(toolsItem);
            menuStrip1.Items.Add(helpItem);
        }

        public void EasyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = (ToolStripMenuItem)sender;
            if (menuItem == null)
            {
                throw new ArgumentNullException(nameof(menuItem));
            }

            Sudoku.SetLevel(menuItem.Name);
            Sudoku.SetMenuItemChecked(menuItem);
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

            Sudoku.AddLabelsToBoard();

            //SetStatus = "Time Elapsed 00:00:00.00";
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Sudoku.PaintBoard(e);
        }

        public void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GetGameSaveInfo();
            Application.Exit();
        }

        public void ExpertToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = (ToolStripMenuItem)sender;
            if (menuItem == null)
            {
                throw new ArgumentNullException(nameof(menuItem));
            }

            Sudoku.SetLevel(menuItem.Name);
            Sudoku.SetMenuItemChecked(menuItem);
        }

        public void HardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = (ToolStripMenuItem)sender;
            if (menuItem == null)
            {
                throw new ArgumentNullException(nameof(menuItem));
            }

            Sudoku.SetLevel(menuItem.Name);
            Sudoku.SetMenuItemChecked(menuItem);
        }

        public void MediumToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = (ToolStripMenuItem)sender;
            if (menuItem == null)
            {
                throw new ArgumentNullException(nameof(menuItem));
            }

            Sudoku.SetLevel(menuItem.Name);
            Sudoku.SetMenuItemChecked(menuItem);
        }

        public void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // ask user if they want to save their current game to disk if any
            if (GetGameSaveInfo()) return;

            StartNewGame();


            // load the game from disk
            Sudoku.CurrentGameState = Sudoku.LoadGameFromDisk();

            // set up the board with the saved game
            Sudoku.RefreshGameBoard();


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

            //for (var r = 1; r <= 9; r++)
            //{
            //    for (var c = 1; c <= 9; c++)
            //    {
            //        if (Sudoku.Actual[c, r] != 0)
            //        {
            //            continue;
            //        }

            //        var control = Controls.Find($"{c}{r}", true).FirstOrDefault();
            //        var label = (Label)control;
            //        if (label != null)
            //        {
            //            label.Text = Sudoku.FixupPossibleValues(Sudoku.CalculatePossibleValues(c, r));
            //        }
            //    }
            //}

            startTime = DateTime.Now;
        }

        public void StartNewGame()
        {
            Array.Clear(Sudoku.ActualValues, 0, Sudoku.ActualValues.Length);
            Array.Clear(Sudoku.PossibleValues, 0, Sudoku.PossibleValues.Length);

            // initialize the stacks
            Sudoku.Moves = new Stack<string>();
            Sudoku.RedoMoves = new Stack<string>();

            Sudoku.Counter = 0;
            Sudoku.GameStarted = true;
            SetStatus2(@"New game started");
        }

        private bool GetGameSaveInfo()
        {
            if (!Sudoku.GameStarted)
            {
                return false;
            }

            var response = MessageBox.Show(saveGame, saveCurrentGame, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

            if (response == DialogResult.Yes)
            {
                Sudoku.SaveGameToDisk(false);
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
            if (Sudoku.RedoMoves.Count == 0) return;

            // remove from one stack and push into the moves stack
            var str = Sudoku.RedoMoves.Pop();
            Sudoku.Moves.Push(str);

            // save the value in the array
            int col = int.Parse(str[0].ToString());
            int row = int.Parse(str[1].ToString());
            int value = int.Parse(str[2].ToString());

            Sudoku.SetCell(col, row, value);
            SetText($@"Value reinserted at ({col},{row})");
        }

        public void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!Sudoku.GameStarted)
            {
                Console.Beep();
                SetStatus2(@"Game not started yet.");
                return;
            }

            Sudoku.SaveGameToDisk(true);
        }

        public void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!Sudoku.GameStarted)
            {
                SetStatus2(@"Game not started yet.");
                return;
            }

            Sudoku.SaveGameToDisk(false);
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
            Sudoku.SelectedNumber = selectedButton.Text == @"Erase" ? 0 : int.Parse(selectedButton.Text);

        }

        public void UndoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // if no previous moves, then exit
            if (Sudoku.Moves.Count == 0) return;

            // remove from one stack and push into the redo stack
            var s = Sudoku.Moves.Pop();
            Sudoku.RedoMoves.Push(Sudoku.Moves.Pop());


            // save the value in the array
            int col = int.Parse(s[0].ToString());
            int row = int.Parse(s[1].ToString());

            Sudoku.SetCell(col, row, 0);
            SetStatus2($@"Value removed at ({col},{row}).");
        }

        private void BtnViewCandidates_Click(object sender, EventArgs e)
        {
            TxtActivities.Clear();
            Sudoku.CheckCandidates();
            TxtActivities.SelectionStart = TxtActivities.TextLength;
            TxtActivities.ScrollToCaret();

        }

        private void TxtActivities_VisibleChanged(object sender, EventArgs e)
        {
            if (!TxtActivities.Visible)
            {
                return;
            }

            TxtActivities.SelectionStart = TxtActivities.TextLength;
            TxtActivities.ScrollToCaret();
        }
    }
}