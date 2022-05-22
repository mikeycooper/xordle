using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using TimeWaster.XordleBoard;

namespace TimeWaster.Xordle
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
    public class XordleSolver
    {
        private const string commandToken = ":command:";
        private static XordleOptions options;
        private static List<UnknownBoard> allBoards;
        private static readonly Stopwatch sw = new();

        public static void Solve(string[] args)
        {
            var words = GetWordList();
            options = ProcessCmdArgs(args);

            while (true)
            {
                Start(words);
                Console.WriteLine($"Completed in {sw.ElapsedMilliseconds / 1000}s");
                sw.Reset();
                if (!options.EndlessPlay) return;
            }
        }

        private static void Start(List<string> words)
        {
            if (options.BoardCount == 0)
            {
                Console.Write("How many boards [8]? ");
                var input = Console.ReadLine();
                options.BoardCount = int.Parse(input == "" ? "8" : input);
            }
            allBoards = new List<UnknownBoard>(options.BoardCount);

            for (int i = 0; i < options.BoardCount; i++)
            {
                allBoards.Add(new UnknownBoard(i + 1, words));
            }

            AddInitialGuesses(options, words, allBoards);
            while (true)
            {
                if (allBoards.All(b => b.IsTerminal)) return;

                var remainingBoards = allBoards.Where(b => !b.IsTerminal).ToList();

                var board =
                    // If AutoAdvance is enabled, play the first non-terminal board with pending guesses, if any
                    remainingBoards.Where(b => options.AutoAdvance && b.HasPendingGuesses).FirstOrDefault()
                    // If no boards have pending guesses or autoadvance is disabled, just play the first non-terminal board
                    ?? remainingBoards.Where(b => !b.IsTerminal).First();

                PlayBoards(remainingBoards, board);
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

        static void PlayBoards(List<UnknownBoard> remainingBoards, UnknownBoard board)
        {
            // Keep track of guesses on all remaining boards besides the current one
            var otherBoards = remainingBoards.Where(b => b != board).ToList();

            Console.WriteLine();
            Console.WriteLine($"\t\t[BOARD {board.Id}]");
            var evaluatedGuesses = board.GetEvaluatedGuesses();
            foreach (var guess in evaluatedGuesses)
            {
                guess.Print();
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
                    Console.Write($"[{board.RemainingWordCount.ToString().PadLeft(board.OriginalWordCount.ToString().Length)}] Guess {guess.ToUpper()}. CPA? ");
                    cpa = ReadCPAInput();
                }

                // User can hit enter to get a different random guess
                if (cpa == string.Empty) continue;

                if (cpa == $"{commandToken}:n")
                {
                    // Move to the next remaining board
                    Console.WriteLine();
                    if (remainingBoards.IndexOf(board) + 1 >= remainingBoards.Count)
                    {
                        // If we're at the last board, play the first remaining board
                        PlayBoards(remainingBoards, remainingBoards[0]);
                        return;
                    }
                    else
                    {
                        // Otherwise, play the next board
                        PlayBoards(remainingBoards, remainingBoards[remainingBoards.IndexOf(board) + 1]);
                        return;
                    }
                }

                if (cpa == $"{commandToken}:p")
                {
                    // Move to the previous remaining board
                    if (remainingBoards.IndexOf(board) <= 0)
                    {
                        // If we're at the first board, play the last live board
                        PlayBoards(remainingBoards, remainingBoards[remainingBoards.Count - 1]);
                        return;
                    }
                    else
                    {
                        // Otherwise, play the previous board
                        PlayBoards(remainingBoards, remainingBoards[remainingBoards.IndexOf(board) - 1]);
                        return;
                    }
                }

                if (cpa == $"{commandToken}:rr")
                {
                    board.ResetBoard();
                    return;
                }

                if (cpa == $"{commandToken}:ss")
                {
                    string path = $"{Environment.CurrentDirectory}\\saved-octordle.txt";
                    var payload = allBoards.Select(b => b.ToDto());
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    File.WriteAllText(
                        path,
                        JsonSerializer.Serialize(payload, options));
                    Console.WriteLine($"Game saved to {path}");
                    SuccessBeep();
                    return;
                }

                if (cpa == $"{commandToken}:ll")
                {
                    string path = $"{Environment.CurrentDirectory}\\saved-octordle.txt";
                    var dtos = JsonSerializer.Deserialize<List<UnknownBoardSaveDto>>(File.ReadAllText(path));

                    allBoards = RegenerateBoardsFromSave(dtos);

                    Console.WriteLine($"Game loaded from {path}");
                    SuccessBeep();
                    return;
                }

                try
                {
                    sw.Start();
                    board.UpdateResult(guess, cpa);

                    foreach (var otherBoard in otherBoards)
                    {
                        otherBoard.AddGuess(guess);
                    }

                    if (ShouldAdvance(board, otherBoards))
                    {
                        // Auto-advance for pending guesses if we're done with them on this board
                        AdvanceBeep();
                        return;
                    }
                }
                catch (BrokenBoardException)
                {
                    Console.WriteLine("Broken board, invalid CPA discarded.");
                    ErrorBeep();
                }
            }
        }

        private static List<UnknownBoard> RegenerateBoardsFromSave(List<UnknownBoardSaveDto> dtos)
        {
            var boards = new List<UnknownBoard>(dtos.Count);
            foreach (var dto in dtos)
            {
                var board = new UnknownBoard(dto.Id, GetWordList());
                foreach (var guess in dto.Guesses)
                {
                    board.AddGuess(guess.Word);
                    if (!string.IsNullOrWhiteSpace(guess.Result))
                    {
                        board.UpdateResult(guess.Word, guess.Result);
                    }
                }
                boards.Add(board);
            }

            return boards;
        }

        private static bool ShouldAdvance(UnknownBoard board, IEnumerable<UnknownBoard> otherBoards)
        {
            // If auto-advance is disabled, always return false
            if (!options.AutoAdvance) return false;

            return !board.IsSolved
                && board.RemainingWordCount > 1
                && !board.HasPendingGuesses
                && otherBoards.Any(b => b.HasPendingGuesses);
        }

        private static string ReadCPAInput()
        {
            while (true)
            {
                var response = Console.ReadLine();

                // p and n go to previous and next boards
                if (response.Equals("p", StringComparison.OrdinalIgnoreCase)) return $"{commandToken}:p";
                if (response.Equals("n", StringComparison.OrdinalIgnoreCase)) return $"{commandToken}:n";
                if (response.Equals("rr", StringComparison.OrdinalIgnoreCase)) return $"{commandToken}:rr";
                if (response.Equals("ss", StringComparison.OrdinalIgnoreCase)) return $"{commandToken}:ss";
                if (response.Equals("ll", StringComparison.OrdinalIgnoreCase)) return $"{commandToken}:ll";

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
                AllowNonWordGuesses = args.Contains("-nonwords", StringComparer.OrdinalIgnoreCase),
                AutoAdvance = !args.Contains("-noAutoAdvance"),
                EndlessPlay = args.Contains("-endless", StringComparer.OrdinalIgnoreCase)
            };

            var boardsArg = args.FirstOrDefault(a => a.IndexOf("-boards=") == 0);
            options.BoardCount =  (boardsArg != null)
                ? int.Parse(boardsArg.Substring("-boards=".Length))
                : 8;

            return options;
        }

        private static List<string> GetWordList() => new(
            File.ReadAllLines(
                Path.Combine(
                    Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
                    "Octordle.wordlist")));

        private static void ErrorBeep() => Console.Beep(150, 500);

        private static void WarningBeep() => Console.Beep(300, 500);

        private static void SuccessBeep() => Console.Beep(1000, 500);

        private static void AdvanceBeep()
        {
            Console.Beep(1100, 200);
            Console.Beep(1200, 200);
        }

        private class XordleOptions
        {
            public int BoardCount { get; set; }
            public bool AddManualGuesses { get; set; }
            public bool AllowNonWordGuesses { get; set; }
            public bool AutoAdvance { get; set; }
            public bool EndlessPlay { get; set; }
        }
    }
}