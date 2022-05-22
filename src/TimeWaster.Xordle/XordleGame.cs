using System.Reflection;
using System.Security.Cryptography;
using TimeWaster.XordleBoard;

namespace TimeWaster.Xordle
{
    public static class XordleGame
    {
        private static List<string> words = GetWordList();
        private static KnownBoard board;
        private static List<Guess> guesses = new();

        public static void Play()
        {
            board = new KnownBoard(words[RandomNumberGenerator.GetInt32(words.Count)]);

            while (true)
            {
                Console.WriteLine();
                guesses.Print();

                if (board.IsSolved) return;

                var word = GetWord();
                var result = board.AddGuess(word);

                guesses.Add(new Guess(word, result));
            }
        }

        private static string GetWord()
        {
            while (true)
            {
                Console.Write("Guess? ");
                var word = Console.ReadLine().Trim();
                if (words.Contains(word, StringComparer.OrdinalIgnoreCase))
                {
                    return word;
                }

                Console.WriteLine("Not a valid word");
            }
        }

        private static List<string> GetWordList() => new(
            File.ReadAllLines(
                Path.Combine(
                    Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
                    "Octordle.wordlist")));
    }
}