using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using TimeWaster.XordleBoard;

namespace TimeWaster.XordleBoard.Tests
{
    [TestClass]
    public class UnknownBoardTests
    {
        public string[] words = new []{"eerie", "deuce", "ether", "enert"};

        [TestMethod]
        public void CorrectCPA_RemovesExpected()
        {
            var board = new UnknownBoard(1, words);
            board.AddGuess("zzzzt");

            board.UpdateResult("zzzzt", "aaaac");

            var remainingWords = board.GetWords().ToArray();
            Assert.AreEqual(1, remainingWords.Length);
        }
    }
}
