using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.VisualBasic;

namespace SudokuMaster
{
    public class CustomLabel : Label
    {
        public bool IsEraseable { get; set; }
    }

    public class SudokuPuzzle
    {
        public int SelectedNumber { get; set; }

        public int SelectedColumn { get; set; } = 1;

        public int SelectedRow { get; set; } = 1;

        public string CalculatePossibleValues(int col, int row, string[,] possible, int[,] actual)
        {
            var s = string.Empty;
            s = possible[col, row] == string.Empty ? "123456789" : possible[col, row];

            int r;
            int c;

            // Step (1) check by column
            for (r = 1; r <= 9; r++)
                if (actual[col, r] != 0)
                {
                    // that means there is a actual value in it
                    s = s.Replace(actual[col, r].ToString(), string.Empty);
                }

            // Step (2) check by row
            for (c = 1; c <= 9; c++)
                if (actual[c, row] != 0)
                {
                    // that means there is a actual value in it
                    s = s.Replace(actual[c, row].ToString(), string.Empty);
                }

            // Step (3) check within the minigrid
            var startC = col - (col - 1) % 3;
            var startR = row - (row - 1) % 3;
            for (var rr = startR; rr <= startR + 2; rr++)
            {
                for (var cc = startC; cc <= startC + 2; cc++)
                {
                    if (actual[cc, rr] != 0)
                    {
                        s = s.Replace(actual[cc, rr].ToString(), string.Empty);
                    }
                }
            }

            // if possible value is string.Empty, then error
            if (s == string.Empty)
            {
                throw new Exception("Invalid Move");
            }
            return s;
        }

        public string CheckCandidates(int col, int row, string[,] possible)
        {
            string pattern = "123456789";

            int min = 0;
            int max = 0;

            // check row by row in grid
            foreach (var r in Enumerable.Range(1, 9))
            {
                // check column by column in grid
                foreach (var c in Enumerable.Range(1, 9))
                {
                    if (r == row || c == col)
                    {
                        if (possible[c, r].ToString() != string.Empty)
                        {
                            pattern = pattern.Replace(possible[c, r], string.Empty);
                        }
                    }
                    if (row >= 1 && row <= 3 && col >= 1 && col <= 3)
                    {
                        min = 1; max = 3;
                    }
                    else if (row >= 4 && row <= 6 && col >= 4 && col <= 6)
                    {
                        min = 4; max = 6;
                    }
                    else if (row >= 7 && row <= 9 && col >= 7 && col <= 9)
                    {
                        min = 7; max = 9;
                    }
                    else
                    {
                        continue;
                    }
                    // check row by row in minigrids
                    foreach (var rr in Enumerable.Range(min, max).Where(rr => rr >= min && rr <= max))
                    {
                        // check by column by column in minigrids
                        foreach (var cc in Enumerable.Range(min, max).Where(cc => cc >= min && cc <= max))
                        {
                            if (possible[cc, rr].ToString() != string.Empty)
                            {
                                pattern = pattern.Replace(possible[cc, rr], string.Empty);
                            }
                        }
                    }
                }
            }

            return pattern;
        }

        public bool CheckColumnsAndRows()
        {
            var changes = false;
            using (var Form2 = new Form1())
            {
                // check all cells
                for (var row = 1; row <= 9; row++)
                {
                    for (var col = 1; col <= 9; col++)
                    {
                        if (Form2.Actual[col, row] != 0)
                        {
                            continue;
                        }

                        try
                        {
                            Form2.Possible[col, row] = CalculatePossibleValues(col, row, Form2.Possible, Form2.Actual);
                        }
                        catch (Exception)
                        {
                            throw new Exception("Invalid Move");
                        }

                        if (Form2.Possible[col, row].Length != 1)
                        {
                            continue;
                        }

                        // number is confirmed
                        Form2.Actual[col, row] = int.Parse(Form2.Possible[col, row]);
                        changes = true;

                        // accumulate the total score
                    }
                }
            }

            CheckValues();
            return changes;
        }

        public string CheckPossibles()
        {
            var sb = new StringBuilder();
            using (var Form2 = new Form1())
            {
                // print results
                foreach (var row in Enumerable.Range(1, 9))
                {
                    foreach (var col in Enumerable.Range(1, 9))
                    {
                        sb.Append(Form2.Possible[col, row] != null && Form2.Possible[col, row].Length > 0 ? $"{Form2.Possible[col, row]}" : " ");
                    }
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        public string CheckValues()
        {
            var sb = new StringBuilder();
            using (var Form2 = new Form1())
            {
                foreach (int row in Enumerable.Range(1, 9))
                {
                    for (int col = 1; col <= 9; col++)
                    {
                        sb.Append(Form2.Possible[col, row] != null ? $"{Form2.Possible[col, row]} " : $"{Form2.Actual[col, row]} ");
                    }
                    sb.AppendLine();
                }

            }
            return sb.ToString();
        }

        private static void CreateEmptyCells(int empty)
        {
            var random = new Random();

            // choose random locations for empty cells
            var emptyCells = new string[empty];
            for (int i = 1; i <= empty / 2; i++)
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
                        c = random.Next(1, 10);
                        r = random.Next(1, 6);
                    } while (r == 5 & c > 5);

                    for (int j = 0; j <= i; j++)
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
                    var Form2 = new Form1
                    {
                        Actual = { [c, r] = 0 },
                        Possible = { [c, r] = string.Empty }
                    };


                    // reflect the top half of the grid and make it symmetrical
                    emptyCells[empty - 1 - i] = (10 - c).ToString() + (10 - r);
                    Form2.Actual[10 - c, 10 - r] = 0;
                    Form2.Possible[10 - c, 10 - r] = string.Empty;
                } while (duplicate);
            }
        }

        private static void FindCellWithFewestPossibleValues(ref int col, ref int row)
        {
            var min = 10;
            var Form2 = new Form1();
            for (var r = 1; r <= 9; r++)
            {
                for (var c = 1; c <= 9; c++)
                {
                    if (Form2.Actual[c, r] != 0 || Form2.Possible[c, r].Length >= min)
                    {
                        continue;
                    }

                    min = Form2.Possible[c, r].Length;
                    col = c;
                    row = r;
                }
            }
        }

        private string GenerateNewPuzzle(int level, ref int score)
        {
            var str = string.Empty;
            int empty;
            var Form2 = new Form1();
            // initialize the entire board
            foreach (int row in Enumerable.Range(1, 9))
            {
                foreach (int col in Enumerable.Range(1, 9))
                {
                    Form2.Actual[col, row] = 0;
                    Form2.Possible[col, row] = string.Empty;
                }
            }

            // clear the stacks
            Form2.ActualStack.Clear();
            Form2.PossibleStack.Clear();

            // populate the board with numbers by solving an empty grid
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
            Form2.ActualBackup = (int[,])Form2.Actual.Clone();

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
                default:
                    empty = rnd.Next(40, 45);
                    break;
            }

            // clear the stacks that are used in brute-force elimination 
            Form2.ActualStack.Clear();
            Form2.PossibleStack.Clear();
            Form2.BruteForceStop = false;

            // create empty cells
            CreateEmptyCells(empty);

            // convert the values in the actual array to a string
            foreach (var row in Enumerable.Range(1, 9))
            {
                foreach (var col in Enumerable.Range(1, 9))
                {
                    str += Form2.Actual[col, row].ToString();
                }
            }

            // verify the puzzle has only one solution
            var tries = 0;
            do
            {
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
            var Form2 = new Form1();
            try
            {
                // scan through columns
                foreach (var r in Enumerable.Range(1, 9))
                {
                    if (Form2.Actual[col, r] == value) // duplicate
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
                    if (Form2.Actual[c, row] == value)
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
                        if (Form2.Actual[startC + cc, startR + rr] == value) // duplicate
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

            var Form2 = new Form1();

            // check row by row
            for (r = 1; r <= 9; r++)
            {
                pattern = "123456789";
                for (c = 1; c <= 9; c++)
                {
                    pattern = pattern.Replace(Convert.ToString(Form2.Actual[c, r].ToString()), string.Empty);
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
                    pattern = pattern.Replace(Convert.ToString(Form2.Actual[c, r].ToString()), string.Empty);
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
                    for (int cc = 0; cc <= 2; cc++)
                    {
                        for (int rr = 0; rr <= 2; rr++)
                        {
                            pattern = pattern.Replace(Convert.ToString(Form2.Actual[c + cc, r + rr].ToString()), string.Empty);
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
            var Form2 = new Form1();
            // if saveFileName is empty, means game has not been saved before
            if (Form2.SaveFileName == string.Empty || saveAs)
            {
                using (var saveFileDialog1 = new SaveFileDialog
                {
                    Filter = @"SDO files (*.sdo)|*.sdo|All files (*.*)|*.*",
                    FilterIndex = 1,
                    RestoreDirectory = false
                })
                {
                    if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                    {
                        // store the filename first
                        Form2.SaveFileName = saveFileDialog1.FileName;
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            // formulate the string representing the values to store
            var str = new StringBuilder();
            foreach (var row in Enumerable.Range(1, 9))
            {
                foreach (var col in Enumerable.Range(1, 9))
                {
                    str.Append(Form2.Actual[col, row].ToString());
                }
            }

            // save the values to file
            try
            {
                var fileExists = File.Exists(Form2.SaveFileName);
                if (fileExists)
                {
                    File.Delete(Form2.SaveFileName);
                }

                File.WriteAllText(Form2.SaveFileName, str.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
            return $@"Puzzle saved in {Form2.SaveFileName}";
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
            int c = 1;
            int r = 1;

            var Form2 = new Form1();
            // find out which cell has the smallest number of possible values
            FindCellWithFewestPossibleValues(ref c, ref r);

            // get the possible values for the chosen cell
            string possibleValues = Form2.Possible[c, r];

            // randomize the possible values
            RandomizeThePossibleValues(ref possibleValues);

            // push the actual and possible stacks into the stack
            Form2.ActualStack.Push((int[,])Form2.Actual.Clone());
            Form2.PossibleStack.Push((string[,])Form2.Possible.Clone());

            // select one value and try
            for (int i = 0; i <= possibleValues.Length - 1; i++)
            {
                Form2.Actual[c, r] = int.Parse(possibleValues[i].ToString());
                try
                {
                    if (SolvePuzzle())
                    {
                        // if the puzzle is solved, the recursion can stop now
                        Form2.BruteForceStop = true;
                        return;
                    }

                    // no problem with current selection, proceed with next cell
                    SolvePuzzleByBruteForce();
                    if (Form2.BruteForceStop)
                    {
                        return;
                    }
                }
                catch (Exception)
                {
                    // accumulate the total score
                    Form2.Actual = Form2.ActualStack.Pop();
                    Form2.Possible = Form2.PossibleStack.Pop();
                }
            }
        }

        private static bool LookForLoneRangersinMinigrids()
        {
            var changes = false;
            var cPos = 0;
            var rPos = 0;

            var Form2 = new Form1();
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
                                if (Form2.Actual[c + cc, r + rr] != 0 || !Form2.Possible[c + cc, r + rr].Contains(n.ToString()))
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
                        Form2.Actual[cPos, rPos] = n;
                        changes = true;
                    }
                }
            }

            return changes;
        }

        private static bool LookForLoneRangersinRows()
        {
            var changes = false;
            var cPos = 0;
            var rPos = 0;
            var Form2 = new Form1();
            // check by row
            for (var r = 1; r <= 9; r++)
            {
                for (var n = 1; n <= 9; n++)
                {
                    var occurrence = 0;
                    for (var c = 1; c <= 9; c++)
                    {
                        if (Form2.Actual[c, r] != 0 || !Form2.Possible[c, r].Contains(n.ToString()))
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
                    Form2.Actual[cPos, rPos] = n;
                    changes = true;

                }
            }

            return changes;
        }

        private static bool LookForLoneRangersinColumns()
        {
            var changes = false;
            var cPos = 0;
            var rPos = 0;
            var Form2 = new Form1();
            // check by column
            for (var c = 1; c <= 9; c++)
            {
                for (var n = 1; n <= 9; n++)
                {
                    var occurrence = 0;
                    for (var r = 1; r <= 9; r++)
                    {
                        if (Form2.Actual[c, r] != 0 || !Form2.Possible[c, r].Contains(n.ToString()))
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
                    Form2.Actual[cPos, rPos] = n;
                    changes = true;

                }
            }

            return changes;
        }

        private static bool LookForTwinsinMinigrids()
        {
            var changes = false;
            var Form2 = new Form1();
            // look for twins in each cell
            for (var r = 1; r <= 9; r++)
            {
                for (var c = 1; c <= 9; c++)
                {
                    // if two possible values, check for twins
                    if (Form2.Actual[c, r] != 0 || Form2.Possible[c, r].Length != 2)
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
                            if (cc == c && rr == r || Form2.Possible[cc, rr] != Form2.Possible[c, r])
                            {
                                continue;
                            }

                            // remove the twins from all the other possible values in the minigrid
                            for (var rrr = startR; rrr <= startR + 2; rrr++)
                            {
                                for (var ccc = startC; ccc <= startC + 2; ccc++)
                                {
                                    if (Form2.Actual[ccc, rrr] != 0 || Form2.Possible[ccc, rrr] == Form2.Possible[c, r])
                                    {
                                        continue;
                                    }

                                    // save a copy of the original possible values (twins)
                                    var originalPossible = Form2.Possible[ccc, rrr];

                                    // remove first twin number from possible values
                                    Form2.Possible[ccc, rrr] = Form2.Possible[ccc, rrr].Replace(Form2.Possible[c, r][0].ToString(), string.Empty);

                                    // remove second twin number from possible values
                                    Form2.Possible[ccc, rrr] = Form2.Possible[ccc, rrr].Replace(Form2.Possible[c, r][1].ToString(), string.Empty);

                                    // if the possible values are modified, then set the changes variable to true to
                                    // indicate that the possible values of cells in the minigrid have been modified
                                    if (originalPossible != Form2.Possible[ccc, rrr])
                                    {
                                        changes = true;
                                    }

                                    // if possible value reduces to empty string, then the user has placed a move that
                                    // results in the puzzle not being solvable
                                    if (Form2.Possible[ccc, rrr] == string.Empty)
                                    {
                                        throw new Exception("Invalid Move");
                                    }

                                    // if left with 1 possible value  for the current cell, cell is confirmed
                                    if (Form2.Possible[ccc, rrr].Length != 1)
                                    {
                                        continue;
                                    }

                                    Form2.Actual[ccc, rrr] = int.Parse(Form2.Possible[ccc, rrr]);

                                }
                            }
                        }
                    }
                }
            }

            return changes;
        }

        private static bool LookForTwinsinRows()
        {
            var changes = false;
            var Form2 = new Form1();
            // for each row, check each column in the row
            for (var r = 1; r <= 9; r++)
            {
                for (var c = 1; c <= 9; c++)
                {
                    // if two possible values, check for twins
                    if (Form2.Actual[c, r] != 0 || Form2.Possible[c, r].Length != 2)
                    {
                        continue;
                    }

                    //  scan columns in this row
                    for (var cc = c + 1; cc <= 9; cc++)
                    {
                        if (Form2.Possible[cc, r] != Form2.Possible[c, r])
                        {
                            continue;
                        }

                        // remove the twins from all the other possible values in the row
                        for (var ccc = 1; ccc <= 9; ccc++)
                        {
                            if (Form2.Actual[ccc, r] != 0 || ccc == c || ccc == cc)
                            {
                                continue;
                            }

                            // save a copy of the original possible values (twins)
                            var originalPossible = Form2.Possible[ccc, r];

                            // remove first twin number from possible
                            // values
                            Form2.Possible[ccc, r] = Convert.ToString(Form2.Possible[ccc, r]
                                .Replace(Convert.ToString(Form2.Possible[c, r][0]), string.Empty));

                            // remove second twin number from possible values
                            Form2.Possible[ccc, r] = Convert.ToString(Form2.Possible[ccc, r]
                                .Replace(Convert.ToString(Form2.Possible[c, r][1]), string.Empty));

                            // if the possible values are modified, then set the changes variable to true to indicate
                            // that the possible values of cells in the minigrid have been modified
                            if (originalPossible != Form2.Possible[ccc, r])
                            {
                                changes = true;
                            }

                            // if possible value reduces to empty string, then the user has placed a move that results
                            // in the puzzle not solvable
                            if (Form2.Possible[ccc, r] == string.Empty)
                            {
                                throw new Exception("Invalid Move");
                            }

                            // if left with 1 possible value for the cell, cell is confirmed
                            if (Form2.Possible[ccc, r].Length != 1)
                            {
                                continue;
                            }

                            Form2.Actual[ccc, r] = int.Parse(Form2.Possible[ccc, r]);

                        }
                    }
                }
            }

            return changes;
        }

        private static bool LookForTwinsinColumns()
        {
            var changes = false;
            var Form2 = new Form1();
            // for each column, check each row in the column
            for (var c = 1; c <= 9; c++)
            {
                for (var r = 1; r <= 9; r++)
                {
                    // if two possible values, check for twins
                    if (Form2.Actual[c, r] != 0 || Form2.Possible[c, r].Length != 2)
                    {
                        continue;
                    }

                    //  scan rows in this column
                    for (var rr = r + 1; rr <= 9; rr++)
                    {
                        if (Form2.Possible[c, rr] != Form2.Possible[c, r])
                        {
                            continue;
                        }

                        // remove the twins from all the other possible values in the row
                        for (var rrr = 1; rrr <= 9; rrr++)
                        {
                            if (Form2.Actual[c, rrr] != 0 || rrr == r || rrr == rr)
                            {
                                continue;
                            }

                            // save a copy of the original possible values (twins)
                            var originalPossible = Form2.Possible[c, rrr];

                            // remove first twin number from possible values
                            Form2.Possible[c, rrr] = Convert.ToString(Form2.Possible[c, rrr]
                                .Replace(Convert.ToString(Form2.Possible[c, r][0]), string.Empty));

                            // remove second twin number from possible values
                            Form2.Possible[c, rrr] = Convert.ToString(Form2.Possible[c, rrr]
                                .Replace(Convert.ToString(Form2.Possible[c, r][1]), string.Empty));

                            // if the possible values are modified, then set the changes variable to true to indicate
                            // that the possible values of cells in the minigrid have been modified
                            if (originalPossible != Form2.Possible[c, rrr])
                            {
                                changes = true;
                            }

                            // if possible value reduces to empty string, then the user has placed a move that results
                            // in the puzzle not being solvable
                            if (Form2.Possible[c, rrr] == string.Empty)
                            {
                                throw new Exception("Invalid Move");
                            }

                            // if left with 1 possible value for the current cell, cell is confirmed
                            if (Form2.Possible[c, rrr].Length != 1)
                            {
                                continue;
                            }

                            Form2.Actual[c, rrr] = int.Parse(Form2.Possible[c, rrr]);

                        }
                    }
                }
            }

            return changes;
        }

        private static bool LookForTripletsinMinigrids()
        {
            var changes = false;
            var Form2 = new Form1();
            // check each cell
            for (var r = 1; r <= 9; r++)
            {
                for (var c = 1; c <= 9; c++)
                    //  three possible values; check for triplets
                    if (Form2.Actual[c, r] == 0 && Form2.Possible[c, r].Length == 3)
                    {
                        // first potential triplet found
                        var tripletsLocation = c + r.ToString();

                        // scan by mini-grid
                        var startC = c - (c - 1) % 3;
                        var startR = r - (r - 1) % 3;
                        for (var rr = startR; rr <= startR + 2; rr++)
                            for (var cc = startC; cc <= startC + 2; cc++)
                                if (!(cc == c && rr == r) && (Form2.Possible[cc, rr] == Form2.Possible[c, r] ||
                                                              Form2.Possible[cc, rr].Length == 2 &&
                                                              Form2.Possible[c, r]
                                                                  .Contains(Convert.ToString(Form2.Possible[cc, rr][0]
                                                                      .ToString())) && Form2.Possible[c, r]
                                                                  .Contains(Convert.ToString(Form2.Possible[cc, rr][1].ToString()))))
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
                                if (Form2.Actual[ccc, rrr] != 0 || ccc == Convert.ToInt32(tripletsLocation[0].ToString()) ||
                                    rrr == Convert.ToInt32(tripletsLocation[1].ToString()) ||
                                    ccc == Convert.ToInt32(tripletsLocation[2].ToString()) ||
                                    rrr == Convert.ToInt32(tripletsLocation[3].ToString()) ||
                                    ccc == Convert.ToInt32(tripletsLocation[4].ToString()) ||
                                    rrr == Convert.ToInt32(tripletsLocation[5].ToString()))
                                {
                                    continue;
                                }

                                // save the original possible values
                                var originalPossible = Form2.Possible[ccc, rrr];

                                // remove first triplet number from possible values
                                Form2.Possible[ccc, rrr] = Convert.ToString(Form2.Possible[ccc, rrr]
                                    .Replace(Convert.ToString(Form2.Possible[c, r][0]), string.Empty));

                                // remove second triplet number from possible values
                                Form2.Possible[ccc, rrr] = Convert.ToString(Form2.Possible[ccc, rrr]
                                    .Replace(Convert.ToString(Form2.Possible[c, r][1]), string.Empty));

                                // remove third triplet number from possible
                                // values---
                                Form2.Possible[ccc, rrr] = Convert.ToString(Form2.Possible[ccc, rrr]
                                    .Replace(Convert.ToString(Form2.Possible[c, r][2]), string.Empty));

                                // if the possible values are modified, then set the changes variable to true to indicate
                                // that the possible values of cells in the  minigrid have been modified
                                if (originalPossible != Form2.Possible[ccc, rrr])
                                {
                                    changes = true;
                                }

                                // if possible value reduces to empty string, then the user has placed a move that results
                                // in the puzzle not solvable
                                if (Form2.Possible[ccc, rrr] == string.Empty)
                                {
                                    throw new Exception("Invalid Move");
                                }

                                // if left with 1 possible value for the current cell, cell is confirmed
                                if (Form2.Possible[ccc, rrr].Length != 1)
                                {
                                    continue;
                                }

                                Form2.Actual[ccc, rrr] = int.Parse(Form2.Possible[ccc, rrr]);
                            }
                        }
                    }
            }

            return changes;
        }

        private static bool LookForTripletsinColumns()
        {
            var changes = false;
            var Form2 = new Form1();
            // for each column, check each row in the column
            for (var c = 1; c <= 9; c++)
            {
                for (var r = 1; r <= 9; r++)
                {
                    //  three possible values; check for triplets
                    if (Form2.Actual[c, r] != 0 || Form2.Possible[c, r].Length != 3)
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

                        if (Form2.Possible[c, rr] == Form2.Possible[c, r] || Form2.Possible[c, rr].Length == 2 &&
                            Form2.Possible[c, r].Contains(Form2.Possible[c, rr][0].ToString()) &&
                            Form2.Possible[c, r].Contains(Form2.Possible[c, rr][1].ToString()))
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
                        if (Form2.Actual[c, rrr] != 0 || rrr == Convert.ToInt32(tripletsLocation[1].ToString()) ||
                            rrr == Convert.ToInt32(tripletsLocation[3].ToString()) ||
                            rrr == Convert.ToInt32(tripletsLocation[5].ToString()))
                        {
                            continue;
                        }

                        // save the original possible values
                        var originalPossible = Form2.Possible[c, rrr];

                        // remove first triplet number from possible values
                        Form2.Possible[c, rrr] = Form2.Possible[c, rrr].Replace(Form2.Possible[c, r][0].ToString(), string.Empty);

                        // remove second triplet number from possible values
                        Form2.Possible[c, rrr] = Form2.Possible[c, rrr].Replace(Form2.Possible[c, r][1].ToString(), string.Empty);

                        // remove third triplet number from possible values
                        Form2.Possible[c, rrr] = Form2.Possible[c, rrr].Replace(Form2.Possible[c, r][2].ToString(), string.Empty);

                        // if the possible values are modified, then set the changes variable to true to indicate that
                        // the possible values of cells in the minigrid have been modified
                        if (originalPossible != Form2.Possible[c, rrr])
                        {
                            changes = true;
                        }

                        // if possible value reduces to empty string, then the user has placed a move that results in the puzzle not being solvable
                        if (Form2.Possible[c, rrr] == string.Empty)
                        {
                            throw new Exception("Invalid Move");
                        }

                        // if left with 1 possible value for the current cell, cell is confirmed
                        if (Form2.Possible[c, rrr].Length != 1)
                        {
                            continue;
                        }

                        Form2.Actual[c, rrr] = int.Parse(Form2.Possible[c, rrr]);
                    }
                }
            }

            return changes;
        }

        private static bool LookForTripletsinRows()
        {
            var changes = false;
            var Form2 = new Form1();
            // for each row, check each column in the row
            for (var r = 1; r <= 9; r++)
            {
                for (var c = 1; c <= 9; c++)
                {
                    //  three possible values; check for triplets
                    if (Form2.Actual[c, r] != 0 || Form2.Possible[c, r].Length != 3)
                    {
                        continue;
                    }

                    // first potential triplet found
                    var tripletsLocation = c + r.ToString();

                    // scans columns in this row
                    for (var cc = 1; cc <= 9; cc++)
                        // look for other triplets
                        if (cc != c && (Form2.Possible[cc, r] == Form2.Possible[c, r] ||
                                        Form2.Possible[cc, r].Length == 2 &&
                                        Form2.Possible[c, r].Contains(Form2.Possible[cc, r][0].ToString())) &&
                                        Form2.Possible[c, r].Contains(Form2.Possible[cc, r][1].ToString()))
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
                        if (Form2.Actual[ccc, r] == 0 && ccc != tripletsLocation[0] && ccc != tripletsLocation[2] && ccc != tripletsLocation[4])
                        {
                            // save the original possible values
                            var originalPossible = Form2.Possible[ccc, r];

                            // remove first triplet number from possible values
                            Form2.Possible[ccc, r] = Form2.Possible[ccc, r].Replace(Form2.Possible[c, r][0].ToString(), string.Empty);

                            // remove second triplet number from possible values
                            Form2.Possible[ccc, r] = Form2.Possible[ccc, r].Replace(Form2.Possible[c, r][1].ToString(), string.Empty);

                            // remove third triplet number from possible values
                            Form2.Possible[ccc, r] = Form2.Possible[ccc, r].Replace(Form2.Possible[c, r][2].ToString(), string.Empty);

                            // if the possible values are modified, then set the changes variable to true to indicate that
                            // the possible values of cells in the minigrid have been modified
                            if (originalPossible != Form2.Possible[ccc, r])
                            {
                                changes = true;
                            }

                            // if possible value reduces to empty string, then the user has placed a move that results in the puzzle not solvable
                            if (Form2.Possible[ccc, r] == string.Empty)
                            {
                                throw new Exception("Invalid Move");
                            }

                            // if left with 1 _possible value for the current cell, cell is confirmed
                            if (Form2.Possible[ccc, r].Length != 1)
                            {
                                continue;
                            }

                            Form2.Actual[ccc, r] = int.Parse(Form2.Possible[ccc, r]);

                        }
                }
            }

            return changes;
        }

        private static void RandomizeThePossibleValues(ref string str)
        {
            VBMath.Randomize();
            var array = str.ToCharArray();
            int i;
            for (i = 0; i <= str.Length - 1; i++)
            {
                var j = Convert.ToInt32(Convert.ToInt32((str.Length - i + 1) * VBMath.Rnd() + i) % str.Length);
                // swap the chars
                var temp = array[i];
                array[i] = array[j];
                array[j] = temp;
            }
            str = new string(array);
        }

        public void SetLevel(string itemName)
        {
            var Form2 = new Form1();
            switch (itemName)
            {
                case "EasyToolStripMenuItem":
                    Form2.Level = 1;
                    break;
                case "MediumToolStripMenuItem":
                    Form2.Level = 2;
                    break;
                case "HardToolStripMenuItem":
                    Form2.Level = 3;
                    break;
                case "ExpertToolStripMenuItem":
                    Form2.Level = 4;
                    break;
                default:
                    Form2.Level = 1;
                    break;
            }
        }

        private static void VacateAnotherPairOfCells(ref string str)
        {
            int c;
            int r;

            var Form2 = new Form1();
            var rnd = new Random();

            // look for a pair of cells to restore
            do
            {
                c = rnd.Next(1, 9);
                r = rnd.Next(1, 9);
            } while (str[c - 1 + (r - 1) * 9].ToString().Length != 0);

            // restore the value of the cell from the actual_backup array
            str = str.Remove(c - 1 + (r - 1) * 9, 1);
            str = str.Insert(c - 1 + (r - 1) * 9, Form2.ActualBackup[c, r].ToString());

            // restore the value of the symmetrical cell from the actual_backup array
            str = str.Remove(10 - c - 1 + (10 - r - 1) * 9, 1);
            str = str.Insert(10 - c - 1 + (10 - r - 1) * 9, Form2.ActualBackup[10 - c, 10 - r].ToString());

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
                    if (int.Parse(str[counter].ToString()) != 0)
                    {
                        Form2.Actual[col, row] = int.Parse(str[counter].ToString());
                        Form2.Possible[col, row] = str[counter].ToString();
                    }
                    else
                    {
                        Form2.Actual[col, row] = 0;
                        Form2.Possible[col, row] = string.Empty;
                    }

                    counter++;
                }
        }

    }
}
