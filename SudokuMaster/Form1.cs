using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Windows.Forms;

namespace SudokuMaster
{
    public partial class Form1 : Form
    {
        public SudokuPuzzle Sdk = new SudokuPuzzle();

        // back color for empty cells
        private readonly Color _defaultBackcolor = Color.White;

        // colors for cells with hint values
        // these cells are not eraseable
        private readonly Color _fixedForecolor = Color.Blue;
        private readonly Color _fixedBackcolor = Color.LightSteelBlue;

        // these colors are for user inserted values which can be erased
        private readonly Color _userBackcolor = Color.LightYellow;
        private readonly Color _userForecolor = Color.Black;

        // dimension of each cell of the grid
        private const int CellWidth = 32;
        private const int CellHeight = 32;

        // offset from the top-left corner of the window
        private const int XOffset = -20;
        private const int YOffset = 25;

        public bool HintMode { get; set; }

        public int SelectedNumber { get; set; }

        public int Level { get; set; }

        public int Seconds { get; set; }

        public Form1()
        {
            InitializeComponent();

        }

        private void BtnHint_Click(object sender, EventArgs e)
        {
            // show hints one cell at a time
            HintMode = true;
            try
            {
                Sdk.SolvePuzzle();

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
                DisplayActivity(@"Click File->New to start a new game or File->Open to load an existing game", true);
                return;
            }

            var cellLabel = (CustomLabel)sender;

            // if cell is not erasable then exit
            if (cellLabel.IsEraseable == false)
            {
                DisplayActivity("Selected cell is not empty", true);
                return;
            }

            // determine the col and row of the selected cell
            int col = Convert.ToInt32(cellLabel.Name.Substring(0, 1));
            int row = Convert.ToInt32(cellLabel.Name.Substring(1, 1));

            try
            {
                // If erasing a cell
                if (SelectedNumber == 0)
                {
                    // if cell is empty then no need to erase
                    if (Sdk.Actual[col, row] == 0)
                    {
                        return;
                    }

                    // save the value in the array
                    SetCell(col, row, SelectedNumber, true);
                    DisplayActivity($"Number erased at ({col},{row})", false);
                }
                else if (cellLabel.Text == string.Empty)
                {
                    // else setting a value; check if move is valid
                    if (!Sdk.IsMoveValid(col, row, SelectedNumber))
                    {
                        DisplayActivity($"Invalid move at ({col},{row})", true);
                        return;
                    }

                    // save the value in the array
                    SetCell(col, row, SelectedNumber, true);
                    DisplayActivity($"Number placed at ({col},{row})", false);

                    // saves the move into the stack
                    Sdk.Moves.Push($"{cellLabel.Name}{SelectedNumber}");

                    //Dim possible = CalculatePossibleValues(col, row)

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

        private void ClearBoard()
        {
            // initialize the stacks
            Sdk.Moves = new Stack<string>();
            Sdk.RedoMoves = new Stack<string>();

            // initialize the cells in the board
            foreach (var row in Enumerable.Range(1,9))
            {
                foreach (var col in Enumerable.Range(1,9))
                {
                    SetCell(col, row, 0, true);
                }
            }
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
            var ValuesToolStripMenuItem = new ToolStripMenuItem("Check &Values");
            ValuesToolStripMenuItem.Click += ValuesToolStripMenuItem_Click;

            var PossiblesToolStripMenuItem = new ToolStripMenuItem("Check &Possibles");
            PossiblesToolStripMenuItem.Click += PossiblesToolStripMenuItem_Click;

            toolsItem.DropDownItems.Add(ValuesToolStripMenuItem);
            toolsItem.DropDownItems.Add(PossiblesToolStripMenuItem);

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

        public void DisplayActivity(string str, bool soundBeep)
        {
            if (soundBeep)
            {
                Console.Beep();
            }

            TxtActivities.Text += $@"{str}{Environment.NewLine}";
        }

        public void DrawBoard()
        {
            // default selected number is 1
            toolStripButton1.Checked = true;
            SelectedNumber = 1;

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

        private void Form1_Load(object sender, EventArgs e)
        {
            //TopMost = true;

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


            // creates the main menu
            CreateMenu();


            // draw the board
            DrawBoard();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            int y1, y2;

            // draw the horizontal lines
            var x1 = 1 * (CellWidth + 1) + XOffset - 1;
            var x2 = 9 * (CellWidth + 1) + XOffset + CellWidth;
            foreach (int r in Enumerable.Range(1, 10).Where(r => r % 3 == 0))
            {
                y1 = r * (CellHeight + 1) + YOffset - 1;
                y2 = y1;
                e.Graphics.DrawLine(Pens.Red, x1, y1, x2, y2);
            }

            // draw the vertical lines
            y1 = 1 * (CellHeight + 1) + YOffset - 1;
            y2 = 9 * (CellHeight + 1) + YOffset + CellHeight;
            foreach (int c in Enumerable.Range(1, 10).Where(c => c % 3 == 0))
            {
                x1 = c * (CellWidth + 1) + XOffset - 1;
                x2 = x1;
                e.Graphics.DrawLine(Pens.Red, x1, y1, x2, y2);
            }
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Sdk.CheckPossibles();
        }

        private void SetLevel(string itemName)
        {
            switch (itemName)
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
                    Level = 0;
                    break;
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

        private void EasyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = (ToolStripMenuItem)sender;
            if (menuItem == null)
            {
                throw new ArgumentNullException(nameof(menuItem));
            }

            SetLevel(menuItem.Name);
            SetCheckedOrNotChecked(menuItem);
        }

        private void HardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = (ToolStripMenuItem)sender;
            if (menuItem == null)
            {
                throw new ArgumentNullException(nameof(menuItem));
            }

            SetLevel(menuItem.Name);
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

            SetLevel(menuItem.Name);
            SetCheckedOrNotChecked(menuItem);
        }

        private void MediumToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = (ToolStripMenuItem)sender;
            if (menuItem == null)
            {
                throw new ArgumentNullException(nameof(menuItem));
            }

            SetLevel(menuItem.Name);
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

            // Change to the hourglass cursor
            Cursor.Current = Cursors.WaitCursor;
            toolStripStatusLabel1.Text = @"Generating new puzzle...";

            // create an instance of the SudokuPuzzle class
            var puzzle = Sdk.GetPuzzle(Level);

            // Change back to the default cursor
            Cursor.Current = Cursors.Default;

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
                Sdk.SaveFileName = openFileDialog1.FileName;

            }
            else
            {
                return;
            }

            StartNewGame();

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
            DisplayActivity($"Value reinserted at ({int.Parse(str[0].ToString())},{int.Parse(str[1].ToString())})", false);

        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!Sdk.GameStarted)
            {
                DisplayActivity(@"Game not started yet.", false);
                return;
            }

            TxtActivities.Text = Sdk.SaveGameToDisk(false);
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!Sdk.GameStarted)
            {
                DisplayActivity("Game not started yet.", true);
                return;
            }

            Sdk.SaveGameToDisk(true);
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

        public void StartNewGame()
        {
            Sdk.SaveFileName = string.Empty;
            TxtActivities.Text = string.Empty;
            Seconds = 0;
            ClearBoard();
            Sdk.GameStarted = true;
            timer1.Enabled = true;
            toolStripStatusLabel1.Text = @"New game started";
            toolTip1.RemoveAll();
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            int ss = Seconds;
            var mm = ss % 60;

            if (ss >= 3600)
            {
                ss = ss % 60;
                mm = ss % 3600;
                int hh = ss % 219600;

                toolStripStatusLabel2.Text = $@"Elapsed time: {hh} hour(s) {mm} minute(s) {ss} second(s)";
            }

            else if (ss >= 60 && ss < 3600)
            {
                ss = ss % 60;

                toolStripStatusLabel2.Text = $@"Elapsed time: {mm} minute(s) {ss} second(s)";

            }
            else if (ss > 0 && ss < 60)
            {
                toolStripStatusLabel2.Text = $@"Elapsed time: {ss} seconds";

            }
            Seconds += 1;
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
            SetCell(int.Parse(str[0].ToString()), int.Parse(str[1].ToString()), 0, true);
            DisplayActivity($"Value removed at ({int.Parse(str[0].ToString())},{int.Parse(str[1].ToString())})", false);
        }

        private void ValuesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TxtActivities.Text = Sdk.CheckValues();
        }

        private void PossiblesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TxtActivities.Text = Sdk.CheckPossibles();
        }
    }

}

