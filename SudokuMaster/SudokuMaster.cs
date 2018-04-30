using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SudokuMaster;

namespace SudokuMaster
{
    public class CustomLabel : Label
    {
        public bool IsEraseable { get; set; }
    }

    public class SudokuPuzzle
    {
        // used to store the state of the grid
        public int[,] Actual = new int[10, 10];
        public Stack<int[,]> ActualStack = new Stack<int[,]>();

        // used to represent the possible values of cells in the grid
        public string[,] Possible = new string[10, 10];
        public Stack<string[,]> PossibleStack = new Stack<string[,]>();

        // backup a copy of the _actual array
        private int[,] _actualBackup = new int[10, 10];

        // store the total score accumulated
        private int _totalscore;

        // indicate if the brute-force subroutine should stop
        public bool BruteForceStop;
        public int CellHeight = 32;

        // dimension of each cell in the grid
        public int CellWidth = 32;

        // has the game started
        public bool GameStarted;

        // stacks to keep track of all the moves
        public Stack<string> Moves;

        public Stack<string> RedoMoves;

        // keep track of file name to save to
        public string SaveFileName = string.Empty;

        // used to keep track of elapsed time
        public int Seconds;

        // offset from the top left corner of the window
        public int XOffset = -20;
        public int YOffset = 25;

        // the number currently selected for insertion
        public int SelectedNumber { get; set; }

        private string CalculatePossibleValues(int col, int row)
        {
            var str = Possible[col, row] == string.Empty ? "123456789" : Possible[col, row];

            int r;
            int c;

            // Step (1) check by column
            for (r = 1; r <= 9; r++)
                if (Actual[col, r] != 0)
                {
                    // that means there is a actual value in it
                    str = str.Replace(Convert.ToString(Actual[col, r].ToString()), string.Empty);
                }

            // Step (2) check by row
            for (c = 1; c <= 9; c++)
                if (Actual[c, row] != 0)
                {
                    // that means there is a actual value in it
                    str = str.Replace(Convert.ToString(Actual[c, row].ToString()), string.Empty);
                }

            // Step (3) check within the minigrid
            var startC = col - (col - 1) % 3;
            var startR = row - (row - 1) % 3;
            for (var rr = startR; rr <= startR + 2; rr++)
                for (var cc = startC; cc <= startC + 2; cc++)
                    if (Actual[cc, rr] != 0)
                    {
                        str = str.Replace(Convert.ToString(Actual[cc, rr].ToString()), string.Empty);
                    }

            // if possible value is string.Empty, then error
            if (str == string.Empty)
            {
                throw new Exception("Invalid Move");
            }

            return str;
        }

        private bool CheckColumnsAndRows()
        {
            var changes = false;

            // check all cells
            for (var row = 1; row <= 9; row++)
            {
                for (var col = 1; col <= 9; col++)
                {
                    if (Actual[col, row] != 0)
                    {
                        continue;
                    }

                    try
                    {
                        Possible[col, row] = CalculatePossibleValues(col, row);
                    }
                    catch (Exception)
                    {
                        throw new Exception("Invalid Move");
                    }

                    if (Possible[col, row].Length != 1)
                    {
                        continue;
                    }

                    // number is confirmed
                    Actual[col, row] = int.Parse(Possible[col, row]);
                    changes = true;

                    // accumulate the total score
                    _totalscore++;
                }
            }

            CheckValues();
            return changes;
        }

        public string CheckPossibles()
        {
            // print results
            var sb = new StringBuilder();
            foreach (var row in Enumerable.Range(1, 9))
            {
                foreach (var col in Enumerable.Range(1, 9))
                {
                    sb.Append(Possible[col, row] != null ? $"{Possible[col, row]}" : "0");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public string CheckValues()
        {
            // print results
            var sb = new StringBuilder();
            for (var row = 1; row <= 9; row++)
            {
                for (var col = 1; col <= 9; col++)
                {
                    sb.Append(Possible[col, row] != null ? $"{Possible[col, row]} " : $"{Actual[col, row]} ");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
      
        private void CreateEmptyCells(int empty)
        {
            // choose random locations for empty cells
            var emptyCells = new string[empty];

            Random rnd = new Random();

            for (var i = 0; i <= empty / 2; i++)
            {
                bool duplicate;
                do
                {
                    duplicate = false;
                    // get a cell in the first half of the grid
                    int r;
                    int c;
                    do
                    {
                        c = rnd.Next(1, 9);
                        r = rnd.Next(1, 5);
                    } while ((r == 5) & (c > 5));

                    for (var j = 0; j <= i; j++)
                    {
                        // if cell is already selected to be empty
                        if (emptyCells[j] != $"{c}{r}")
                        {
                            continue;
                        }

                        duplicate = true;
                        break;
                    }

                    if (duplicate)
                    {
                        continue;
                    }

                    // set the empty cell
                    emptyCells[i] = $"{c}{r}";
                    Actual[c, r] = 0;
                    Possible[c, r] = string.Empty;
                    // reflect the top half of the grid and make it symmetrical
                    emptyCells[empty - 1 - i] = 10 - c + (10 - r).ToString();
                    Actual[10 - c, 10 - r] = 0;
                    Possible[10 - c, 10 - r] = string.Empty;
                } while (duplicate);
            }
        }

        private void FindCellWithFewestPossibleValues(ref int col, ref int row)
        {
            var min = 10;
            for (var r = 1; r <= 9; r++)
            {
                for (var c = 1; c <= 9; c++)
                {
                    if (Actual[c, r] != 0 || Possible[c, r].Length >= min)
                    {
                        continue;
                    }

                    min = Possible[c, r].Length;
                    col = c;
                    row = r;
                }
            }
        }

        private string GenerateNewPuzzle(int level, ref int score)
        {
            var str = string.Empty;
            var empty = 0;
            int r;
            int c;

            // initialize the entire board
            for (r = 1; r <= 9; r++)
            {
                for (c = 1; c <= 9; c++)
                {
                    Actual[c, r] = 0;
                    Possible[c, r] = string.Empty;
                }
            }

            // clear the stacks
            ActualStack.Clear();
            PossibleStack.Clear();

            // populate the board with numbers by solving an empty grid---
            try
            {
                // use logical methods to setup the grid first
                if (!SolvePuzzle())
                {
                    // then use brute-force
                    SolvePuzzleByBruteForce();
                }
            }
            catch (Exception)
            {
                // just in case an error occured, return an empty string
                return string.Empty;
            }

            // make a backup copy of the _actual array
            _actualBackup = (int[,])Actual.Clone();

            var rnd = new Random();

            // set the number of empty cells based on the level of difficulty
            switch (level)
            {
                case 1:
                    empty = rnd.Next(40, 45);
                    break;
                case 2:
                    empty = rnd.Next(46, 49);
                    break;
                case 3:
                    empty = rnd.Next(50, 53);
                    break;
                case 4:
                    empty = rnd.Next(54, 58);
                    break;
            }

            // clear the stacks that are used in brute-force elimination 
            ActualStack.Clear();
            PossibleStack.Clear();
            BruteForceStop = false;

            // create empty cells
            CreateEmptyCells(empty);

            // convert the values in the actual array to a string
            for (r = 1; r <= 9; r++)
            {
                for (c = 1; c <= 9; c++)
                {
                    str += Convert.ToString(Actual[c, r].ToString());
                }
            }

            // verify the puzzle has only one solution
            var tries = 0;
            do
            {
                _totalscore = 0;
                try
                {
                    if (!SolvePuzzle())
                    {
                        // if puzzle is not solved and this is a level 1 to 3 puzzle
                        if (level < 4)
                        {
                            // choose another pair of cells to empty
                            VacateAnotherPairOfCells(ref str);
                            tries++;
                        }
                        else
                        {
                            // level 4 puzzles does not guarantee single solution and potentially need guessing
                            SolvePuzzleByBruteForce();
                            goto endOfDoLoop;
                        }
                    }
                    else
                    {
                        // puzzle does indeed have 1 solution
                        goto endOfDoLoop;
                    }
                }
                catch (Exception)
                {
                    return string.Empty;
                }

                // if too many tries, exit the loop
                if (tries > 50)
                {
                    return string.Empty;
                }
            } while (true);

            endOfDoLoop:

            // return the score as well as the puzzle as a string
            score = _totalscore;
            return str;
        }

        public string GetPuzzle(int level)
        {
            var score = 0;
            string result;
            do
            {
                result = GenerateNewPuzzle(level, ref score);
                if (result == string.Empty)
                {
                    continue;
                }

                // check if puzzle matches the level of difficult
                switch (level)
                {
                    // average for this level is 44
                    case 1:
                        if ((score >= 42) & (score <= 46))
                        {
                            goto endOfDoLoop;
                        }

                        break;
                    // average for this level is 51
                    case 2:
                        if ((score >= 49) & (score <= 53))
                        {
                            goto endOfDoLoop;
                        }

                        break;
                    // average for this level is 58
                    case 3:
                        if ((score >= 56) & (score <= 60))
                        {
                            goto endOfDoLoop;
                        }

                        break;
                    // average for this level is 114
                    case 4:
                        if ((score >= 112) & (score <= 116))
                        {
                            goto endOfDoLoop;
                        }

                        break;

                    default:
                        goto endOfDoLoop;
                }
            } while (true);

            endOfDoLoop:
            return result;
        }

        public bool IsMoveValid(int col, int row, int value)
        {
            var isValid = true;

            try
            {
                // scan through columns
                foreach (var r in Enumerable.Range(1, 9))
                {
                    if (Actual[col, r] == value) // duplicate
                    {
                        isValid = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            try
            {
                // scan through rows
                foreach (var c in Enumerable.Range(1, 9))
                {
                    if (Actual[c, row] == value)
                    {
                        isValid = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            try
            {
                // scan through mini-grids
                var startC = col - (col - 1) % 3;
                var startR = row - (row - 1) % 3;

                foreach (var rr in Enumerable.Range(0, 2))
                {
                    foreach (var cc in Enumerable.Range(0, 2))
                        if (Actual[startC + cc, startR + rr] == value) // duplicate
                        {
                            isValid = false;
                        }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return isValid;
        }

        public bool IsPuzzleSolved()
        {
            string pattern;
            int r;
            int c;

            // check row by row
            for (r = 1; r <= 9; r++)
            {
                pattern = "123456789";
                for (c = 1; c <= 9; c++)
                {
                    pattern = pattern.Replace(Convert.ToString(Actual[c, r].ToString()), string.Empty);
                }

                if (pattern.Length > 0)
                {
                    return false;
                }
            }

            // check col by col
            for (c = 1; c <= 9; c++)
            {
                pattern = "123456789";
                for (r = 1; r <= 9; r++)
                {
                    pattern = pattern.Replace(Convert.ToString(Actual[c, r].ToString()), string.Empty);
                }

                if (pattern.Length > 0)
                {
                    return false;
                }
            }

            // check by minigrid
            for (c = 1; c <= 9; c += 3)
            {
                pattern = "123456789";
                for (r = 1; r <= 9; r += 3)
                {
                    for (var cc = 0; cc <= 2; cc++)
                    {
                        for (var rr = 0; rr <= 2; rr++)
                        {
                            pattern = pattern.Replace(Convert.ToString(Actual[c + cc, r + rr].ToString()), string.Empty);
                        }
                    }
                }

                if (pattern.Length > 0)
                {
                    return false;
                }
            }

            return true;
        }

        public string SaveGameToDisk(bool saveAs)
        {
            // if saveFileName is empty, means game has not been saved before
            if (SaveFileName == string.Empty || saveAs)
            {
                var saveFileDialog1 = new SaveFileDialog
                {
                    Filter = @"SDO files (*.sdo)|*.sdo|All files (*.*)|*.*",
                    FilterIndex = 1,
                    RestoreDirectory = false
                };

                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    // store the filename first
                    SaveFileName = Convert.ToString(saveFileDialog1.FileName);
                }
                else
                {
                    return null;
                }
            }

            // formulate the string representing the values to store
            var str = new StringBuilder();
            for (var row = 1; row <= 9; row++)
            {
                for (var col = 1; col <= 9; col++)
                {
                    str.Append(Actual[col, row].ToString());
                }
            }

            // save the values to file
            try
            {
                var fileExists = File.Exists(SaveFileName);
                if (fileExists)
                {
                    File.Delete(SaveFileName);
                }

                File.WriteAllText(SaveFileName, str.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
            return $@"Puzzle saved in {SaveFileName}";
        }

        public bool SolvePuzzle()
        {
            var exitLoop = false;
            try
            {
                bool changes;
                do // Look for Triplets in Columns
                {
                    do // Look for Triplets in Rows
                    {
                        do // Look for Triplets in Minigrids
                        {
                            do // Look for Twins in Columns
                            {
                                do // Look for Twins in Rows
                                {
                                    do // Look for Twins in Minigrids
                                    {
                                        do // Look for Lone Rangers in Columns
                                        {
                                            do // Look for Lone Rangers in Rows
                                            {
                                                do // Look for Lone Rangers in Minigrids
                                                {
                                                    do // Perform Col/Row and Minigrid Elimination
                                                    {
                                                        changes = CheckColumnsAndRows();
                                                        if (!IsPuzzleSolved())
                                                        {
                                                            continue;
                                                        }

                                                        exitLoop = true;
                                                        break;
                                                    } while (changes);

                                                    if (exitLoop)
                                                    {
                                                        break;
                                                    }

                                                    // Look for Lone Rangers in Minigrids
                                                    changes = LookForLoneRangersinMinigrids();
                                                    if (!IsPuzzleSolved())
                                                    {
                                                        continue;
                                                    }

                                                    exitLoop = true;
                                                    break;
                                                } while (changes);

                                                if (exitLoop)
                                                {
                                                    break;
                                                }

                                                // Look for Lone Rangers in Rows
                                                changes = LookForLoneRangersinRows();
                                                if (!IsPuzzleSolved())
                                                {
                                                    continue;
                                                }

                                                exitLoop = true;
                                                break;
                                            } while (changes);

                                            if (exitLoop)
                                            {
                                                break;
                                            }

                                            // Look for Lone Rangers in Columns
                                            changes = LookForLoneRangersinColumns();
                                            if (!IsPuzzleSolved())
                                            {
                                                continue;
                                            }

                                            exitLoop = true;
                                            break;
                                        } while (changes);

                                        if (exitLoop)
                                        {
                                            break;
                                        }

                                        // Look for Twins in Minigrids
                                        changes = LookForTwinsinMinigrids();
                                        if (!IsPuzzleSolved())
                                        {
                                            continue;
                                        }

                                        exitLoop = true;
                                        break;
                                    } while (changes);

                                    if (exitLoop)
                                    {
                                        break;
                                    }

                                    // Look for Twins in Rows
                                    changes = LookForTwinsinRows();
                                    if (!IsPuzzleSolved())
                                    {
                                        continue;
                                    }

                                    exitLoop = true;
                                    break;
                                } while (changes);

                                if (exitLoop)
                                {
                                    break;
                                }

                                // Look for Twins in Columns
                                changes = LookForTwinsinColumns();
                                if (!IsPuzzleSolved())
                                {
                                    continue;
                                }

                                exitLoop = true;
                                break;
                            } while (changes);

                            if (exitLoop)
                            {
                                break;
                            }

                            // Look for Triplets in Minigrids
                            changes = LookForTripletsinMinigrids();
                            if (!IsPuzzleSolved())
                            {
                                continue;
                            }

                            exitLoop = true;
                            break;
                        } while (changes);

                        if (exitLoop)
                        {
                            break;
                        }

                        // Look for Triplets in Rows
                        changes = LookForTripletsinRows();
                        if (!IsPuzzleSolved())
                        {
                            continue;
                        }

                        exitLoop = true;
                        break;
                    } while (changes);

                    if (exitLoop)
                    {
                        break;
                    }

                    // Look for Triplets in Columns
                    changes = LookForTripletsinColumns();
                    if (IsPuzzleSolved())
                    {
                        break;
                    }
                } while (changes);
            }
            catch (Exception)
            {
                throw new Exception("Invalid Move");
            }

            return IsPuzzleSolved();
        }

        public void SolvePuzzleByBruteForce()
        {
            int c = 0;
            int r = 0;

            // accumulate the total score
            _totalscore += 5;

            // find out which cell has the smallest number of possible values
            FindCellWithFewestPossibleValues(ref c, ref r);

            // get the possible values for the chosen cell
            string possibleValues = Possible[c, r];

            // randomize the possible values
            RandomizeThePossibleValues(ref possibleValues);

            // push the actual and possible stacks into the stack
            ActualStack.Push((int[,])(Actual.Clone()));
            PossibleStack.Push((string[,])(Possible.Clone()));

            // select one value and try
            for (int i = 0; i <= possibleValues.Length - 1; i++)
            {
                Actual[c, r] = Convert.ToInt32(possibleValues[i].ToString());
                try
                {
                    if (SolvePuzzle())
                    {
                        // if the puzzle is solved, the recursion can stop now
                        BruteForceStop = true;
                        return;
                    }

                    // no problem with current selection, proceed with next cell
                    SolvePuzzleByBruteForce();
                    if (BruteForceStop)
                    {
                        return;
                    }
                }
                catch (Exception)
                {
                    // accumulate the total score
                    _totalscore += 5;
                    Actual = ActualStack.Pop();
                    Possible = PossibleStack.Pop();
                }
            }
        }

        private bool LookForLoneRangersinMinigrids()
        {
            var changes = false;
            var cPos = 0;
            var rPos = 0;

            // check for each number from 1 to 9
            for (var n = 1; n <= 9; n++)
            {
                // check the 9 mini-grids
                for (var r = 1; r <= 9; r += 3)
                {
                    for (var c = 1; c <= 9; c += 3)
                    {
                        var nextMiniGrid = false;

                        // check within the mini-grid
                        var occurrence = 0;
                        for (var rr = 0; rr <= 2; rr++)
                        {
                            for (var cc = 0; cc <= 2; cc++)
                            {
                                if (Actual[c + cc, r + rr] != 0 || !Possible[c + cc, r + rr].Contains(n.ToString()))
                                {
                                    continue;
                                }

                                occurrence++;
                                cPos = c + cc;
                                rPos = r + rr;
                                if (occurrence <= 1)
                                {
                                    continue;
                                }

                                nextMiniGrid = true;
                                break;
                            }

                            if (nextMiniGrid)
                            {
                                break;
                            }
                        }

                        if (nextMiniGrid || occurrence != 1)
                        {
                            continue;
                        }

                        // that means number is confirmed
                        Actual[cPos, rPos] = n;
                        changes = true;
                        // accumulate the total score
                        _totalscore += 2;
                    }
                }
            }

            return changes;
        }

        private bool LookForLoneRangersinRows()
        {
            var changes = false;
            var cPos = 0;
            var rPos = 0;

            // check by row
            for (var r = 1; r <= 9; r++)
            {
                for (var n = 1; n <= 9; n++)
                {
                    var occurrence = 0;
                    for (var c = 1; c <= 9; c++)
                    {
                        if (Actual[c, r] != 0 || !Possible[c, r].Contains(n.ToString()))
                        {
                            continue;
                        }

                        occurrence++;

                        // if multiple occurrence, not a lone ranger anymore
                        if (occurrence > 1)
                        {
                            break;
                        }

                        cPos = c;
                        rPos = r;
                    }

                    if (occurrence != 1)
                    {
                        continue;
                    }

                    // number is confirmed
                    Actual[cPos, rPos] = n;
                    changes = true;

                    // accumulate the total score
                    _totalscore += 2;
                }
            }

            return changes;
        }

        private bool LookForLoneRangersinColumns()
        {
            var changes = false;
            var cPos = 0;
            var rPos = 0;

            // check by column
            for (var c = 1; c <= 9; c++)
            {
                for (var n = 1; n <= 9; n++)
                {
                    var occurrence = 0;
                    for (var r = 1; r <= 9; r++)
                    {
                        if (Actual[c, r] != 0 || !Possible[c, r].Contains(n.ToString()))
                        {
                            continue;
                        }

                        occurrence++;

                        // if multiple occurrence, not a lone ranger anymore
                        if (occurrence > 1)
                        {
                            break;
                        }

                        cPos = c;
                        rPos = r;
                    }

                    if (occurrence != 1)
                    {
                        continue;
                    }

                    // number is confirmed
                    Actual[cPos, rPos] = n;
                    changes = true;

                    // accumulate the total score
                    _totalscore += 2;
                }
            }

            return changes;
        }

        private bool LookForTwinsinMinigrids()
        {
            var changes = false;

            // look for twins in each cell
            for (var r = 1; r <= 9; r++)
            {
                for (var c = 1; c <= 9; c++)
                {
                    // if two possible values, check for twins
                    if (Actual[c, r] != 0 || Possible[c, r].Length != 2)
                    {
                        continue;
                    }

                    // scan by the mini-grid that the current cell is in
                    var startC = c - (c - 1) % 3;
                    var startR = r - (r - 1) % 3;
                    for (var rr = startR; rr <= startR + 2; rr++)
                    {
                        for (var cc = startC; cc <= startC + 2; cc++)
                        {
                            // for cells other than the pair of twins
                            if (cc == c && rr == r || Possible[cc, rr] != Possible[c, r])
                            {
                                continue;
                            }

                            // remove the twins from all the other possible values in the minigrid
                            for (var rrr = startR; rrr <= startR + 2; rrr++)
                            {
                                for (var ccc = startC; ccc <= startC + 2; ccc++)
                                {
                                    if (Actual[ccc, rrr] != 0 || Possible[ccc, rrr] == Possible[c, r])
                                    {
                                        continue;
                                    }

                                    // save a copy of the original possible values (twins)
                                    var originalPossible = Possible[ccc, rrr];

                                    // remove first twin number from possible values
                                    Possible[ccc, rrr] = Convert.ToString(Possible[ccc, rrr]
                                        .Replace(Convert.ToString(Possible[c, r][0]), string.Empty));

                                    // remove second twin number from possible values
                                    Possible[ccc, rrr] = Convert.ToString(Possible[ccc, rrr]
                                        .Replace(Convert.ToString(Possible[c, r][1]), string.Empty));

                                    // if the possible values are modified, then set the changes variable to true to
                                    // indicate that the possible values of cells in the minigrid have been modified
                                    if (originalPossible != Possible[ccc, rrr])
                                    {
                                        changes = true;
                                    }

                                    // if possible value reduces to empty string, then the user has placed a move that
                                    // results in the puzzle not being solvable
                                    if (Possible[ccc, rrr] == string.Empty)
                                    {
                                        throw new Exception("Invalid Move");
                                    }

                                    // if left with 1 possible value  for the current cell, cell is confirmed
                                    if (Possible[ccc, rrr].Length != 1)
                                    {
                                        continue;
                                    }

                                    Actual[ccc, rrr] = int.Parse(Possible[ccc, rrr]);

                                    // accumulate the total score
                                    _totalscore += 3;
                                }
                            }
                        }
                    }
                }
            }

            return changes;
        }

        private bool LookForTwinsinRows()
        {
            var changes = false;

            // for each row, check each column in the row
            for (var r = 1; r <= 9; r++)
            {
                for (var c = 1; c <= 9; c++)
                {
                    // if two possible values, check for twins
                    if (Actual[c, r] != 0 || Possible[c, r].Length != 2)
                    {
                        continue;
                    }

                    //  scan columns in this row
                    for (var cc = c + 1; cc <= 9; cc++)
                    {
                        if (Possible[cc, r] != Possible[c, r])
                        {
                            continue;
                        }

                        // remove the twins from all the other possible values in the row
                        for (var ccc = 1; ccc <= 9; ccc++)
                        {
                            if (Actual[ccc, r] != 0 || ccc == c || ccc == cc)
                            {
                                continue;
                            }

                            // save a copy of the original possible values (twins)
                            var originalPossible = Possible[ccc, r];

                            // remove first twin number from possible
                            // values
                            Possible[ccc, r] = Convert.ToString(Possible[ccc, r]
                                .Replace(Convert.ToString(Possible[c, r][0]), string.Empty));

                            // remove second twin number from possible values
                            Possible[ccc, r] = Convert.ToString(Possible[ccc, r]
                                .Replace(Convert.ToString(Possible[c, r][1]), string.Empty));

                            // if the possible values are modified, then set the changes variable to true to indicate
                            // that the possible values of cells in the minigrid have been modified
                            if (originalPossible != Possible[ccc, r])
                            {
                                changes = true;
                            }

                            // if possible value reduces to empty string, then the user has placed a move that results
                            // in the puzzle not solvable
                            if (Possible[ccc, r] == string.Empty)
                            {
                                throw new Exception("Invalid Move");
                            }

                            // if left with 1 possible value for the cell, cell is confirmed
                            if (Possible[ccc, r].Length != 1)
                            {
                                continue;
                            }

                            Actual[ccc, r] = int.Parse(Possible[ccc, r]);

                            // accumulate the total score
                            _totalscore += 3;
                        }
                    }
                }
            }

            return changes;
        }

        private bool LookForTwinsinColumns()
        {
            var changes = false;

            // for each column, check each row in the column
            for (var c = 1; c <= 9; c++)
            {
                for (var r = 1; r <= 9; r++)
                {
                    // if two possible values, check for twins
                    if (Actual[c, r] != 0 || Possible[c, r].Length != 2)
                    {
                        continue;
                    }

                    //  scan rows in this column
                    for (var rr = r + 1; rr <= 9; rr++)
                    {
                        if (Possible[c, rr] != Possible[c, r])
                        {
                            continue;
                        }

                        // remove the twins from all the other possible values in the row
                        for (var rrr = 1; rrr <= 9; rrr++)
                        {
                            if (Actual[c, rrr] != 0 || rrr == r || rrr == rr)
                            {
                                continue;
                            }

                            // save a copy of the original possible values (twins)
                            var originalPossible = Possible[c, rrr];

                            // remove first twin number from possible values
                            Possible[c, rrr] = Convert.ToString(Possible[c, rrr]
                                .Replace(Convert.ToString(Possible[c, r][0]), string.Empty));

                            // remove second twin number from possible values
                            Possible[c, rrr] = Convert.ToString(Possible[c, rrr]
                                .Replace(Convert.ToString(Possible[c, r][1]), string.Empty));

                            // if the possible values are modified, then set the changes variable to true to indicate
                            // that the possible values of cells in the minigrid have been modified
                            if (originalPossible != Possible[c, rrr])
                            {
                                changes = true;
                            }

                            // if possible value reduces to empty string, then the user has placed a move that results
                            // in the puzzle not being solvable
                            if (Possible[c, rrr] == string.Empty)
                            {
                                throw new Exception("Invalid Move");
                            }

                            // if left with 1 possible value for the current cell, cell is confirmed
                            if (Possible[c, rrr].Length != 1)
                            {
                                continue;
                            }

                            Actual[c, rrr] = int.Parse(Possible[c, rrr]);

                            // accumulate the total score
                            _totalscore += 3;
                        }
                    }
                }
            }

            return changes;
        }

        private bool LookForTripletsinMinigrids()
        {
            var changes = false;

            // check each cell
            for (var r = 1; r <= 9; r++)
            {
                for (var c = 1; c <= 9; c++)
                    //  three possible values; check for triplets
                    if (Actual[c, r] == 0 && Possible[c, r].Length == 3)
                    {
                        // first potential triplet found
                        var tripletsLocation = c + r.ToString();

                        // scan by mini-grid
                        var startC = c - (c - 1) % 3;
                        var startR = r - (r - 1) % 3;
                        for (var rr = startR; rr <= startR + 2; rr++)
                            for (var cc = startC; cc <= startC + 2; cc++)
                                if (!(cc == c && rr == r) && (Possible[cc, rr] == Possible[c, r] ||
                                                              Possible[cc, rr].Length == 2 &&
                                                              Possible[c, r]
                                                                  .Contains(Convert.ToString(Possible[cc, rr][0]
                                                                      .ToString())) && Possible[c, r]
                                                                  .Contains(Convert.ToString(Possible[cc, rr][1].ToString()))))
                                {
                                    // save the coorindates of the triplets
                                    tripletsLocation += cc + rr.ToString();
                                }

                        // found 3 cells as triplets; remove all from the other cells---
                        if (tripletsLocation.Length != 6)
                        {
                            continue;
                        }

                        // remove each cell's possible values containing the triplet
                        for (var rrr = startR; rrr <= startR + 2; rrr++)
                        {
                            for (var ccc = startC; ccc <= startC + 2; ccc++)
                            {
                                // look for the cell that is not part of the 3 cells found
                                if (Actual[ccc, rrr] != 0 || ccc == Convert.ToInt32(tripletsLocation[0].ToString()) ||
                                    rrr == Convert.ToInt32(tripletsLocation[1].ToString()) ||
                                    ccc == Convert.ToInt32(tripletsLocation[2].ToString()) ||
                                    rrr == Convert.ToInt32(tripletsLocation[3].ToString()) ||
                                    ccc == Convert.ToInt32(tripletsLocation[4].ToString()) ||
                                    rrr == Convert.ToInt32(tripletsLocation[5].ToString()))
                                {
                                    continue;
                                }

                                // save the original possible values
                                var originalPossible = Possible[ccc, rrr];

                                // remove first triplet number from possible values
                                Possible[ccc, rrr] = Convert.ToString(Possible[ccc, rrr]
                                    .Replace(Convert.ToString(Possible[c, r][0]), string.Empty));

                                // remove second triplet number from possible values
                                Possible[ccc, rrr] = Convert.ToString(Possible[ccc, rrr]
                                    .Replace(Convert.ToString(Possible[c, r][1]), string.Empty));

                                // remove third triplet number from possible
                                // values---
                                Possible[ccc, rrr] = Convert.ToString(Possible[ccc, rrr]
                                    .Replace(Convert.ToString(Possible[c, r][2]), string.Empty));

                                // if the possible values are modified, then set the changes variable to true to indicate
                                // that the possible values of cells in the  minigrid have been modified
                                if (originalPossible != Possible[ccc, rrr])
                                {
                                    changes = true;
                                }

                                // if possible value reduces to empty string, then the user has placed a move that results
                                // in the puzzle not solvable
                                if (Possible[ccc, rrr] == string.Empty)
                                {
                                    throw new Exception("Invalid Move");
                                }

                                // if left with 1 possible value for the current cell, cell is confirmed
                                if (Possible[ccc, rrr].Length != 1)
                                {
                                    continue;
                                }

                                Actual[ccc, rrr] = int.Parse(Possible[ccc, rrr]);

                                // accumulate the total score---
                                _totalscore += 4;
                            }
                        }
                    }
            }

            return changes;
        }

        private bool LookForTripletsinColumns()
        {
            var changes = false;

            // for each column, check each row in the column
            for (var c = 1; c <= 9; c++)
            {
                for (var r = 1; r <= 9; r++)
                {
                    //  three possible values; check for triplets
                    if (Actual[c, r] != 0 || Possible[c, r].Length != 3)
                    {
                        continue;
                    }

                    // first potential triplet found
                    var tripletsLocation = $"{c}{r}";

                    // scans rows in this column
                    for (var rr = 1; rr <= 9; rr++)
                    {
                        if (rr == r)
                        {
                            continue;
                        }

                        if (Possible[c, rr] == Possible[c, r] || Possible[c, rr].Length == 2 &&
                            Possible[c, r].Contains(Convert.ToString(Possible[c, rr][0].ToString())) &&
                            Possible[c, r].Contains(Convert.ToString(Possible[c, rr][1].ToString())))
                        {
                            // save the coorindates of the triplet
                            tripletsLocation += $"{c}{rr}";
                        }
                    }

                    //  found 3 cells as triplets; remove all from the other cells
                    if (tripletsLocation.Length != 6)
                    {
                        continue;
                    }

                    // remove each cell's possible values containing the triplet
                    for (var rrr = 1; rrr <= 9; rrr++)
                    {
                        if (Actual[c, rrr] != 0 || rrr == Convert.ToInt32(tripletsLocation[1].ToString()) ||
                            rrr == Convert.ToInt32(tripletsLocation[3].ToString()) ||
                            rrr == Convert.ToInt32(tripletsLocation[5].ToString()))
                        {
                            continue;
                        }

                        // save the original possible values
                        var originalPossible = Possible[c, rrr];

                        // remove first triplet number from possible values
                        Possible[c, rrr] = Convert.ToString(Possible[c, rrr]
                            .Replace(Convert.ToString(Possible[c, r][0]), string.Empty));

                        // remove second triplet number from possible values
                        Possible[c, rrr] = Convert.ToString(Possible[c, rrr]
                            .Replace(Convert.ToString(Possible[c, r][1]), string.Empty));

                        // remove third triplet number from possible values
                        Possible[c, rrr] = Convert.ToString(Possible[c, rrr]
                            .Replace(Convert.ToString(Possible[c, r][2]), string.Empty));

                        // if the possible values are modified, then set the changes variable to true to indicate that
                        // the possible values of cells in the minigrid have been modified
                        if (originalPossible != Possible[c, rrr])
                        {
                            changes = true;
                        }

                        // if possible value reduces to empty string, then the user has placed a move that results in the puzzle not being solvable
                        if (Possible[c, rrr] == string.Empty)
                        {
                            throw new Exception("Invalid Move");
                        }

                        // if left with 1 possible value for the current cell, cell is confirmed
                        if (Possible[c, rrr].Length != 1)
                        {
                            continue;
                        }

                        Actual[c, rrr] = int.Parse(Possible[c, rrr]);

                        // accumulate the total score
                        _totalscore += 4;
                    }
                }
            }

            return changes;
        }

        private bool LookForTripletsinRows()
        {
            var changes = false;

            // for each row, check each column in the row
            for (var r = 1; r <= 9; r++)
            {
                for (var c = 1; c <= 9; c++)
                {
                    //  three possible values; check for triplets
                    if (Actual[c, r] != 0 || Possible[c, r].Length != 3)
                    {
                        continue;
                    }

                    // first potential triplet found
                    var tripletsLocation = c + r.ToString();

                    // scans columns in this row
                    for (var cc = 1; cc <= 9; cc++)
                        // look for other triplets
                        if (cc != c && (Possible[cc, r] == Possible[c, r] || Possible[cc, r].Length == 2 &&
                                        Possible[c, r].Contains(Convert.ToString(Possible[cc, r][0].ToString())) &&
                                        Possible[c, r].Contains(Convert.ToString(Possible[cc, r][1].ToString()))))
                        {
                            // save the coorindates of the triplet
                            tripletsLocation += cc + r.ToString();
                        }

                    // found 3 cells as triplets; remove all from the other cells
                    if (tripletsLocation.Length != 6)
                    {
                        continue;
                    }

                    // remove each cell's possible values containing the triplet
                    for (var ccc = 1; ccc <= 9; ccc++)
                        if (Actual[ccc, r] == 0 && ccc != Convert.ToInt32(tripletsLocation[0].ToString()) &&
                            ccc != Convert.ToInt32(tripletsLocation[2].ToString()) &&
                            ccc != Convert.ToInt32(tripletsLocation[4].ToString()))
                        {
                            // save the original possible values
                            var originalPossible = Possible[ccc, r];

                            // remove first triplet number from possible values
                            Possible[ccc, r] = Convert.ToString(Possible[ccc, r]
                                .Replace(Convert.ToString(Possible[c, r][0]), string.Empty));

                            // remove second triplet number from possible values
                            Possible[ccc, r] = Convert.ToString(Possible[ccc, r]
                                .Replace(Convert.ToString(Possible[c, r][1]), string.Empty));

                            // remove third triplet number from possible values
                            Possible[ccc, r] = Convert.ToString(Possible[ccc, r]
                                .Replace(Convert.ToString(Possible[c, r][2]), string.Empty));

                            // if the possible values are modified, then set the changes variable to true to indicate that
                            // the possible values of cells in the minigrid have been modified
                            if (originalPossible != Possible[ccc, r])
                            {
                                changes = true;
                            }

                            // if possible value reduces to empty string, then the user has placed a move that results in the puzzle not solvable
                            if (Possible[ccc, r] == string.Empty)
                            {
                                throw new Exception("Invalid Move");
                            }

                            // if left with 1 _possible value for the current cell, cell is confirmed
                            if (Possible[ccc, r].Length != 1)
                            {
                                continue;
                            }

                            Actual[ccc, r] = int.Parse(Possible[ccc, r]);

                            // increment the total score
                            _totalscore += 4;
                        }
                }
            }

            return changes;
        }

        private static void RandomizeThePossibleValues(ref string str)
        {
            int i;

            var rnd = new Random();

            var s = str.ToCharArray();
            for (i = 0; i <= str.Length - 1; i++)
            {
                var j = Convert.ToInt32(Convert.ToInt32((str.Length - i + 1) * rnd.Next(1, 9) + i) % str.Length);

                s[i] = s[j];

                // swap the chars
                var temp = s[i];
                s[j] = temp;
            }

            str = s.ToString();
        }

        private void VacateAnotherPairOfCells(ref string str)
        {
            int c;
            int r;

            var rnd = new Random();

            // look for a pair of cells to restore
            do
            {
                c = rnd.Next(1, 9);
                r = rnd.Next(1, 9);
            } while (str[c - 1 + (r - 1) * 9].ToString().Length != 0);

            // restore the value of the cell from the actual_backup array
            str = str.Remove(c - 1 + (r - 1) * 9, 1);
            str = str.Insert(c - 1 + (r - 1) * 9, Convert.ToString(_actualBackup[c, r].ToString()));

            // restore the value of the symmetrical cell from the actual_backup array
            str = str.Remove(10 - c - 1 + (10 - r - 1) * 9, 1);
            str = str.Insert(10 - c - 1 + (10 - r - 1) * 9, Convert.ToString(_actualBackup[10 - c, 10 - r].ToString()));

            // look for another pair of cells to vacate
            do
            {
                c = rnd.Next(1, 9);
                r = rnd.Next(1, 9);
            } while (str[c - 1 + (r - 1) * 9].ToString().Length == 0);

            // remove the cell from the str
            str = str.Remove(c - 1 + (r - 1) * 9, 1);
            str = str.Insert(c - 1 + (r - 1) * 9, "0");

            // remove the symmetrical cell from the str
            str = str.Remove(10 - c - 1 + (10 - r - 1) * 9, 1);
            str = str.Insert(10 - c - 1 + (10 - r - 1) * 9, "0");

            // reinitialize the board
            var counter = (short)0;
            for (var row = 1; row <= 9; row++)
                for (var col = 1; col <= 9; col++)
                {
                    if (Convert.ToInt32(str[counter].ToString()) != 0)
                    {
                        Actual[col, row] = Convert.ToInt32(str[counter].ToString());
                        Possible[col, row] = Convert.ToString(str[counter].ToString());
                    }
                    else
                    {
                        Actual[col, row] = 0;
                        Possible[col, row] = string.Empty;
                    }

                    counter++;
                }
        }

    }
}
