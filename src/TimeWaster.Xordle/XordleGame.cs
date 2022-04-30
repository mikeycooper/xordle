using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Linq;
using TimeWaster.XordleBoard;

namespace TimeWaster.Xordle
{
    public class XordleGame
    {
        private const string boardToken = ":board ";

        public static void Octordle(string[] args)
        {
            var words = new List<string>(File.ReadAllLines(
                Path.Combine(
                    Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), 
                    "Octordle.wordlist")));
            var options = ProcessCmdArgs(args);

            if (options.BoardCount == 0)
            {
                Console.Write("How many boards [8]? ");
                var input = Console.ReadLine();
                options.BoardCount = int.Parse(input == "" ? "8" : input);
            }
            var boards = new List<UnknownBoard>(options.BoardCount);

            for (int i = 0; i < options.BoardCount; i++)
            {
                boards.Add(new UnknownBoard(i+1, words));
            }

            AddInitialGuesses(options, words, boards);

            while (true)
            {
                if (boards.All(b => b.IsTerminal)) return;

                // Play the first non-terminal board
                var board = boards.Where(b => !b.IsTerminal).First();

                Play(boards.ToList(), board);
            }
        }

        private static void AddInitialGuesses(XordleOptions options, IEnumerable<string> words, IList<UnknownBoard> boards)
        {
            if (!options.AddManualGuesses)
            {
                foreach (var board in boards)
                {
                    board.AddGuess("ROAST"); // These were a really good starter set in Showdown
                    board.AddGuess("FIELD");
                }
                return;
            }

            while (true)
            {
                Console.Write("Add guess? ");
                var guess = Console.ReadLine();

                if (guess == "") break;

                if (!options.AllowNonWordGuesses && !words.Contains(guess, StringComparer.OrdinalIgnoreCase))
                {
                    Console.Write("Not in the word list. ");
                    ErrorBeep();
                    continue;
                }

                foreach (var board in boards)
                {
                    board.AddGuess(guess);
                }
            }
        }

        static void Play(List<UnknownBoard> allBoards, UnknownBoard board)
        {
            // Keep track of guesses on all other boards besides the current one
            var otherBoards = allBoards.Where(b => b != board).ToList();

            Console.WriteLine();
            Console.WriteLine($"\t\t[BOARD {board.Id}]");
            var evaluatedGuesses = board.GetEvaluatedGuesses();
            foreach (var guess in evaluatedGuesses)
            {
                for (int i = 0; i < guess.Word.Length; i++)
                {
                    if (guess.Result[i] == 'C')
                    {
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.BackgroundColor = ConsoleColor.Green;
                    }
                    else if (guess.Result[i] == 'P')
                    {
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.BackgroundColor = ConsoleColor.Yellow;
                    }
                    Console.Write(guess.Word[i]);
                    Console.ResetColor();
                }
                Console.WriteLine();
            }
            Console.WriteLine();

            while (true)
            {
                if (board.IsSolved)
                {
                    Console.WriteLine();
                    SuccessBeep();
                    return;
                }

                string guess = "";

                // If there's only one possible word, just auto-guess it and assume it's correct
                string cpa = "";
                if (board.RemainingWordCount == 1)
                {
                    guess = board.GetWords().First().ToUpper();
                    board.ClearPendingGuesses();
                    Console.WriteLine($"ANSWER: {guess}.");
                    cpa = "CCCCC";
                }
                else
                {
                    guess = board.GetNextGuess();
                    if (guess == "" || board.RemainingWordCount == 0)
                    {
                        Console.WriteLine("No more words. Board broken.");
                        Console.WriteLine();
                        ErrorBeep();
                        return;
                    }
                    Console.Write($"[{board.RemainingWordCount}] Guess {guess.ToUpper()}. CPA? ");
                    cpa = ReadCPAInput();
                }

                // User can hit enter to get a different random guess
                if (cpa == string.Empty) continue;

                if (cpa == $"{boardToken}:n")
                {
                    // Move to the next live board
                    Console.WriteLine();
                    var liveBoards = allBoards.Where(b => !b.IsTerminal).ToList();
                    if (liveBoards.IndexOf(board) + 1 >= liveBoards.Count)
                    {
                        // If we're at the last board, play the first live board
                        Play(allBoards, liveBoards[0]);
                        return;
                    }
                    else
                    {
                        // Otherwise, play the next board
                        Play(allBoards, liveBoards[liveBoards.IndexOf(board) + 1]);
                        return;
                    }
                }

                if (cpa == $"{boardToken}:p")
                {
                    // Move to the previous live board
                    var liveBoards = allBoards.Where(b => !b.IsTerminal).ToList();
                    if (liveBoards.IndexOf(board) <= 0)
                    {
                        // If we're at the first board, play the last live board
                        Play(allBoards, liveBoards[liveBoards.Count - 1]);
                        return;
                    }
                    else
                    {
                        // Otherwise, play the previous board
                        Play(allBoards, liveBoards[liveBoards.IndexOf(board) - 1]);
                        return;
                    }
                }

                if (cpa == $"{boardToken}:rr")
                {
                    board.ResetBoard();
                    return;
                }

                try
                {
                    board.UpdateResult(guess, cpa);

                    foreach (var otherBoard in otherBoards)
                    {
                        otherBoard.AddGuess(guess);
                    }
                }
                catch (BrokenBoardException)
                {
                    Console.WriteLine("Broken board, invalid CPA discarded.");
                    ErrorBeep();
                }

                if (board.RemainingWordCount <= 5 && board.RemainingWordCount > 1 && board.HasPendingGuesses)
                {
                    //ProcessShortCircuit(board, otherBoards);
                }
            }
        }

        private static void ProcessShortCircuit(UnknownBoard board, IEnumerable<UnknownBoard> otherBoards)
        {
            var words = board.GetWords();
            Console.Write($"SHORT CIRCUIT: {string.Join("  ", words)}? ");
            WarningBeep();

            while (true)
            {
                var input = Console.ReadLine().ToLower();
                if (input == "") break; // Blank just goes back to guesses

                if (words.Contains(input))
                {
                    board.ClearPendingGuesses();

                    board.AddGuess(input);
                    foreach (var otherBoard in otherBoards) otherBoard.AddGuess(input);

                    break;
                }

                // If we got this far, we have an invalid input
                Console.Write($"\"{input}\" is not in the word list. {string.Join("  ", words)}? ");
                ErrorBeep();
            }
        }

        private static string ReadCPAInput()
        {
            while (true)
            {
                var response = Console.ReadLine();

                // p and n go to previous and next boards
                if (response.Equals("p", StringComparison.OrdinalIgnoreCase)) return $"{boardToken}:p";
                if (response.Equals("n", StringComparison.OrdinalIgnoreCase)) return $"{boardToken}:n";
                if (response.Equals("rr", StringComparison.OrdinalIgnoreCase)) return $"{boardToken}:rr";

                // User can hit enter to get a different guess
                if (response == string.Empty) return string.Empty;

                var cpa = response.ToUpper();

                if (cpa.Any(c => c != 'C' && c != 'P' && c != 'A'))
                {
                    Console.Write($"CPA {cpa} contains an invalid CPA identifier. CPA? ");
                    ErrorBeep();
                    continue;
                }
                if (cpa.Length != 5)
                {
                    Console.Write($"CPA {cpa} must be 5 character long. CPA? ");
                    ErrorBeep();
                    continue;
                }
                return cpa;
            }
        }

        private static XordleOptions ProcessCmdArgs(string[] args)
        {
            var options = new XordleOptions
            {
                AddManualGuesses = args.Contains("-addguesses", StringComparer.OrdinalIgnoreCase),
                AllowNonWordGuesses = args.Contains("-nonwords", StringComparer.OrdinalIgnoreCase)
            };

            var boardsArg = args.FirstOrDefault(a => a.IndexOf("-boards=") == 0);
            options.BoardCount =  (boardsArg != null)
                ? int.Parse(boardsArg.Substring("-boards=".Length))
                : 8;

            return options;
        }

        private static void ErrorBeep() => Console.Beep(150, 500);
        private static void WarningBeep() => Console.Beep(300, 500);
        private static void SuccessBeep() => Console.Beep(1000, 500);

        private class XordleOptions
        {
            public int BoardCount { get; set; }
            public bool AddManualGuesses { get; set; }
            public bool AllowNonWordGuesses { get; set; }
        }
    }
}