using System.Collections.Generic;
using System.Linq;

namespace TimeWaster.XordleBoard
{
    public abstract class Board
    {
        protected List<Guess> Guesses = new();

        public bool IsSolved => Guesses.Any(g => g.IsSolved);

        public virtual bool IsTerminal => IsSolved;

        public string AddGuess(string guess)
        {
            if (this.IsTerminal)
            {
                // This board is complete. Ignore the guess.
                return "";
            }

            guess = guess.ToLower();

            if (Guesses.Any(g => g.Word == guess))
            {
                // We've already added this guess. Ignore it.
                return "";
            }

            var cpa = EvaluateGuess(guess);
            Guesses.Add(new Guess(guess, cpa));
            return cpa;
        }

        protected virtual string EvaluateGuess(string guess) => "";
    }
}