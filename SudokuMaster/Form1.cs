using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
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

        void StartTimer();

        void StopTimer();

    }

    public partial class Form1 : Form, IForm1
    {
        private const int CellHeight = 32;
        private const int CellWidth = 32;
        private const int XOffset = -20;
        private const int YOffset = 25;
        private const string LinkData = @"http://www.sudokuessentials.com/support-files/blank-sudoku-grid-candidates.pdf";
        private const string LinkData2 = @"C:\Users\greg\Documents\Projects\SudokuMaster\SudokuMaster\Sudoku Games\how-to-solve-sudoku-puzzle.pdf";

        private int linesPrinted;
        private string[] _lines;

        private readonly Sudoku _sudoku = new Sudoku();

        // number the user selected from the toolStrip
        public int SelectedNumber { get; set; } = 1;

        public static Form1 _Form1;

        private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void StartTimer()
        {
            timer1.Start();
            var time = DateTime.Now.ToString(CultureInfo.InvariantCulture);
            _log.Info($"{nameof(timer1)} started at {time}");
        }

        public void StopTimer()
        {
            timer1.Stop();
            var time = DateTime.Now.ToString(CultureInfo.InvariantCulture);
            _log.Info($"{nameof(timer1)} stopped at {time}");
        }

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

        // time that the game started
        public DateTime StartTime { get; set; }

        public void SetText(string input)
        {
            RichTextBox1.AppendText(input + Environment.NewLine);
            RichTextBox1.SelectionStart = RichTextBox1.TextLength;
            RichTextBox1.SelectionStart = 0;
            RichTextBox1.ScrollToCaret();
        }

        public Form1()
        {
            InitializeComponent();

            _Form1 = this;

            XmlConfigurator.Configure();

        }

        private void BtnClearTextBox_Click(object sender, EventArgs e)
        {
            RichTextBox1.Clear();
            RichTextBox1.SelectionStart = 0;
            RichTextBox1.ScrollToCaret();

        }

        private void Cell_Click(object sender)
        {
            _sudoku.SudokuHandler(sender);
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

            // create the Tools menu
            var toolsItem = new ToolStripMenuItem("&Tools");

            var CheckValuesToolStripMenuItem = new ToolStripMenuItem("Check &Values");
            CheckValuesToolStripMenuItem.Click += CheckValuesToolStripMenuItem_Click;
            toolsItem.DropDownItems.Add(CheckValuesToolStripMenuItem);

            var CheckCandidatesToolStripMenuItem = new ToolStripMenuItem("Check Can&didates");
            CheckCandidatesToolStripMenuItem.Click += CheckCandidatesToolStripMenuItem_Click;
            toolsItem.DropDownItems.Add(CheckCandidatesToolStripMenuItem);

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

            printDocument1.PrintPage += PrintDocument1_BeginPrint;
            printDocument1.PrintPage += PrintDocument1_PrintPage;

            //CreateMainMenu();
            //_sudoku.ClearBoard();
            //InitializeBoard();

            //AddLinkLabel();
            //AddLinkLabel2();

            SetLabelProperties();
        }

        private void SetLabelProperties()
        {
            int  maxSize = 292;
            int  minSize = 292;

            tableLayoutPanel1.Padding = new Padding(0);
            tableLayoutPanel1.Margin = new Padding(0);
            tableLayoutPanel1.ColumnCount = 9;
            tableLayoutPanel1.RowCount = 9;
            tableLayoutPanel1.Enabled = true;
            tableLayoutPanel1.Dock = DockStyle.None;
            tableLayoutPanel1.BackColor = Color.Orange;
            tableLayoutPanel1.BorderStyle = BorderStyle.Fixed3D;
            tableLayoutPanel1.MaximumSize = new Size(maxSize, maxSize);
            tableLayoutPanel1.MinimumSize = new Size(minSize, minSize);

            foreach (var cl in tableLayoutPanel1.Controls)
            {
                if (cl is CustomLabel label)
                {
                    label.Padding = new Padding(0);
                    label.Margin = new Padding(0);
                    label.Anchor = Anchor & AnchorStyles.Left & AnchorStyles.Right & AnchorStyles.Top & AnchorStyles.Bottom;
                    label.BackColor = Color.LightYellow;
                    label.ForeColor = Color.Black;
                    label.BorderStyle = BorderStyle.Fixed3D;
                    label.HasNakedSingle = false;
                    var size = new Size(32, 32);
                    label.MaximumSize = size;
                    label.MinimumSize = size;
                    label.Dock = DockStyle.None;

                }
            }
        }

        private void PrintDocument1_BeginPrint(object sender, EventArgs e)
        {
            char[] param = { '\n' };
            _lines = printDialog1.PrinterSettings.PrintRange == PrintRange.Selection ? RichTextBox1.SelectedText.Split(param) : RichTextBox1.Text.Split(param);

            int i = 0;
            char[] trimParam = { '\r' };
            foreach (string s in _lines)
            {
                _lines[i++] = s.TrimEnd(trimParam);
            }

        }

        private void PrintDocument1_PrintPage(object sender, PrintPageEventArgs e)
        {
            int x = e.MarginBounds.Left;
            int y = e.MarginBounds.Top;
            Brush brush = new SolidBrush(RichTextBox1.ForeColor);

            while (linesPrinted < _lines.Length)
            {
                e.Graphics.DrawString(_lines[linesPrinted++], RichTextBox1.Font, brush, x, y);
                y += 15;
                if (y >= e.MarginBounds.Bottom)
                {
                    e.HasMorePages = true;
                    return;
                }
            }
        }

        private void AddLinkLabel()
        {
            var linkLabel = new LinkLabel
            {
                Name = "linkLabel1",
                Text = @"Download blank-sudoku-grid-candidates.pdf",
                Font = new Font("Verdana", 9),
                Location = new Point(325, 6),
                Size = new Size(560, 23),
                BorderStyle = BorderStyle.None,
                LinkVisited = false,
                TextAlign = ContentAlignment.MiddleLeft,
                FlatStyle = FlatStyle.Flat,
                LinkBehavior = LinkBehavior.NeverUnderline,
                BackColor = Color.Transparent

            };

            linkLabel.Links.Add(0, linkLabel.Text.Length, LinkData);
            linkLabel.LinkClicked += LinkLabel1_LinkClicked;

            Controls.Add(linkLabel);
            linkLabel.BringToFront();
        }

        private void AddLinkLabel2()
        {
            var linkLabel = new LinkLabel
            {
                Name = @"linkLabel2",
                Text = @"How to solve sudoku puzzles",
                Font = new Font("Verdana", 9),
                Location = new Point(325, 29),
                Size = new Size(560, 23),
                BorderStyle = BorderStyle.None,
                LinkVisited = false,
                TextAlign = ContentAlignment.MiddleLeft,
                FlatStyle = FlatStyle.Flat,
                LinkBehavior = LinkBehavior.NeverUnderline,
                BackColor = Color.Transparent

            };
            linkLabel.Links.Add(0, linkLabel.Text.Length, LinkData2);
            linkLabel.LinkClicked += LinkLabel2_LinkClicked;

            Controls.Add(linkLabel);
            linkLabel.BringToFront();
        }

        private static void LinkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (sender is LinkLabel linkLabel1)
            {
                linkLabel1.LinkVisited = false;
            }

            Process.Start(LinkData);
        }

        private static void LinkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (sender is LinkLabel linkLabel2)
            {
                linkLabel2.LinkVisited = false;
            }

            Process.Start(LinkData2);
        }

        //private void Form1_Paint(object sender, PaintEventArgs e)
        //{
        //    int y1, y2;

        //    // draw the horizontal _lines
        //    var x1 = 1 * (CellWidth + 1) + XOffset - 1;
        //    var x2 = 9 * (CellWidth + 1) + XOffset + CellWidth;
        //    for (var r = 1; r <= 10; r += 3)
        //    {
        //        y1 = r * (CellHeight + 1) + YOffset - 1;
        //        y2 = y1;
        //        e.Graphics.DrawLine(Pens.Black, x1, y1, x2, y2);
        //    }

        //    // draw the vertical _lines
        //    y1 = 1 * (CellHeight + 1) + YOffset - 1;
        //    y2 = 9 * (CellHeight + 1) + YOffset + CellHeight;
        //    for (var c = 1; c <= 10; c += 3)
        //    {
        //        x1 = c * (CellWidth + 1) + XOffset - 1;
        //        x2 = x1;
        //        e.Graphics.DrawLine(Pens.Black, x1, y1, x2, y2);
        //    }
        //}

        private static void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // initialize arrays
            _sudoku.StartNewGame();

            ClearLabelValues();



            // load the game from disk and reset CurrentGameState
            var contents = _sudoku.CurrentGameState = _sudoku.LoadGameFromDisk();
            _Form1.Text = _sudoku.SavedFileName;

            // set up the board with the saved game
            var counter = 0;
            foreach (var row in Enumerable.Range(1, 9))
            {
                foreach (var col in Enumerable.Range(1, 9))
                {
                    var value = int.Parse(contents[counter].ToString());
                    counter++;
                    _sudoku.SetCell(col, row, value);
                }
            }

            // reset the button controls in the ToolStrip
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

            toolStripButton1.Checked = true;
            //_sudoku.ShowMarkups();


        }

        private void CheckValuesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _sudoku.CheckValues();
        }

        private void InitializeBoard()
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

        private void RedoToolStripMenuItem_Click(object sender, EventArgs e)
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

            _sudoku.SetCell(col, row, value);
            SetText($@"Value reinserted at ({col},{row})");
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!_sudoku.GameHasStarted)
            {
                Console.Beep();
                SetStatus(@"Game not started yet.");
                return;
            }

            const bool saveAs = true;
            _sudoku.SaveGameToDisk(saveAs);
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!_sudoku.GameHasStarted)
            {
                SetStatus(@"Game not started yet.");
                return;
            }

            const bool saveAs = false;
            _sudoku.SaveGameToDisk(saveAs);
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

        private void UndoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // if no previous moves, then exit
            if (_sudoku.Moves.Count == 0) return;

            // remove from one stack and push into the redo stack
            var s = _sudoku.Moves.Pop();
            _sudoku.RedoMoves.Push(_sudoku.Moves.Pop());


            // save the value in the array
            int col = int.Parse(s[0].ToString());
            int row = int.Parse(s[1].ToString());

            _sudoku.SetCell(col, row, 0);
            SetStatus($@"Value removed at ({col},{row}).");
        }

        private void BtnViewCandidates_Click(object sender, EventArgs e)
        {
            _sudoku.DisplayCandidates();
            RichTextBox1.SelectionStart = RichTextBox1.TextLength;
            RichTextBox1.ScrollToCaret();

        }

        private void CheckCandidatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _sudoku.DisplayCandidates();
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

        private void Timer1_Tick(object sender, EventArgs e)
        {
            timer1.Enabled = true;
            timer1.Start();

            var eTime = DateTime.Now - StartTime;
            SetStatus($@"Time Elapsed {eTime.Hours:00}:{eTime.Minutes:00}:{eTime.Seconds:00}");
        }

        private void ButtonViewMarkups_Click(object sender, EventArgs e)
        {
            //_sudoku.ViewMarkups();
            _sudoku.ShowMarkups();
        }


        private void ButtonPrintText_Click(object sender, EventArgs e)
        {
            if (printDialog1.ShowDialog() == DialogResult.OK)
            {
                printDocument1.Print();
            }
        }

    }

}
