using TimeWaster.XordleBoard;

namespace TimeWaster.Xordle;

internal static class Extensions
{
    public static void Print(this Guess guess)
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
    }

    public static void Print(this IEnumerable<Guess> guesses)
    {
        foreach (Guess guess in guesses)
        {
            guess.Print();
            Console.WriteLine();
        }

        Console.WriteLine();
    }
}
