using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using TimeWaster.XordleBoard;

namespace TimeWaster.XordleBoard.Tests
{
    [TestClass]
    public class UnknownBoardTests
    {
        [TestMethod]
        public void CorrectCPA_RemovesExpected()
        {
            string[] words = new[] { "eerie", "deuce", "ether", "enert" };
            var board = new UnknownBoard(1, words);
            board.AddGuess("zzzzt");

            board.UpdateResult("zzzzt", "aaaac");

            var remainingWords = board.GetWords().ToArray();
            Assert.AreEqual(1, remainingWords.Length);
            Assert.AreEqual("enert", remainingWords[0], true);
        }

        [TestMethod]
        public void PresentCPA_RemovesWordsWithoutLetter()
        {
            string[] words = new[] { "adobe", "panel", "ether", "enert" };
            var board = new UnknownBoard(1, words);
            board.AddGuess("zzzaz");

            board.UpdateResult("zzzaz", "aaapa");

            var remainingWords = board.GetWords().ToArray();
            Assert.AreEqual(2, remainingWords.Length);
            CollectionAssert.Contains(remainingWords, "adobe");
            CollectionAssert.Contains(remainingWords, "panel");
        }

        [TestMethod]
        public void DoublePresentCPA_RemovesWordsWithOnlyOneLetterPresent()
        {
            string[] words = new[] { "haunt", "taunt" };
            var board = new UnknownBoard(1, words);
            board.AddGuess("otter");

            board.UpdateResult("otter", "appaa");

            var remainingWords = board.GetWords().ToArray();
            Assert.AreEqual(1, remainingWords.Length);
            Assert.AreEqual("taunt", remainingWords[0], true);
        }

        [TestMethod]
        public void OnePresentOneAbsentCPA_RemovesWordsWithMoreThanOnePresent()
        {
            string[] words = new[] { "haunt", "taunt" };
            var board = new UnknownBoard(1, words);
            board.AddGuess("otter");

            board.UpdateResult("otter", "apaaa");

            var remainingWords = board.GetWords().ToArray();
            Assert.AreEqual(1, remainingWords.Length);
            Assert.AreEqual("haunt", remainingWords[0], true);
        }

        [TestMethod]
        public void OnePresentCPA_DoesntRemoveWordsWithTwoPresent()
        {
            string[] words = new[] { "haunt", "taunt" };
            var board = new UnknownBoard(1, words);
            board.AddGuess("outer");

            board.UpdateResult("outer", "appaa");

            var remainingWords = board.GetWords().ToArray();
            Assert.AreEqual(2, remainingWords.Length);
        }
    }
}
