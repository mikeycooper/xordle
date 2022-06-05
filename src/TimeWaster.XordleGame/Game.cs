using System.Reflection;
using System.Security.Cryptography;
using TimeWaster.XordleBoard;

namespace TimeWaster.XordleGame;

public static class Game
{
    private static List<string> words = GetWordList();
    private static KnownBoard board;
    private static List<Guess> guesses = new();
    private static List<char> characters = new();

    public static void Play()
    {
        board = new KnownBoard(words[RandomNumberGenerator.GetInt32(words.Count)]);
        characters = new List<char>("ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray());

        while (true)
        {
            guesses.Print();

            if (board.IsSolved) return;

            Console.WriteLine($"\tRemaining Letters: {string.Join(" ", characters)}");
            Console.WriteLine();

            var word = GetWord();
            var result = board.AddGuess(word);

            if (result != string.Empty) guesses.Add(new Guess(word, result));

            // Remove any absent letters from the char array
            for (int i = 0; i < result.Length; i++)
            {
                if (result.ToUpper()[i].Equals('A'))
                {
                    characters.Remove(word.ToUpper()[i]);
                }
            }

            Console.WriteLine();
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
