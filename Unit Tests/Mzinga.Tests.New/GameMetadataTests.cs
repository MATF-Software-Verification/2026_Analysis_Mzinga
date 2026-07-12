using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mzinga.Core;

namespace Mzinga.Tests.New
{
    [TestClass]
    public class GameMetadataTests
    {
        [TestMethod]
        public void GameMetaData_SetTag()
        {
            var meta = new GameMetadata();
            
            meta.SetTag("Event", "");
            Assert.AreEqual("", meta.Event);

            meta.SetTag("Site", "  City ");
            Assert.AreEqual("City", meta.Site);
            
            meta.SetTag("Result", "WhiteWins");
            Assert.AreEqual(BoardState.WhiteWins, meta.Result);
            
            meta.SetTag("Result", "Draw");
            Assert.AreEqual(BoardState.Draw, meta.Result);
            
            meta.SetTag("Result", "InProgress");
            Assert.AreEqual(BoardState.InProgress, meta.Result);

            meta.SetTag("UnknownTag", "ExtraData\"\" ");
            Assert.AreEqual("ExtraData", meta.OptionalTags["UnknownTag"]);
        }

        [TestMethod]
        public void GameMetaData_GetTag()
        {
            var meta = new GameMetadata();
            meta.SetTag("White", "Alice");
            meta.SetTag("Black", "Bob");
            meta.SetTag("Round", "5");
            meta.SetTag("GameType", "Base+MLP");

            Assert.AreEqual("Alice", meta.GetTag("White"));
            Assert.AreEqual("Bob", meta.GetTag("Black"));
            Assert.AreEqual("5", meta.GetTag("Round"));
            Assert.IsNull(meta.GetTag("UnknownTag"));
            Assert.AreEqual("Base+MLP", meta.GetTag("GameType"));
        }
        
        [TestMethod]
        public void GameMetaData_SetTag_ArgumentNullException()
        {
            var meta = new GameMetadata();
            
            bool exceptionThrown = false;
            try
            {
                meta.SetTag("", "Test");
            }
            catch (System.ArgumentNullException)
            {
                exceptionThrown = true;
            }
            
            Assert.IsTrue(exceptionThrown);
        }

        [TestMethod]
        public void GameMetaData_MoveCommentary()
        {
            var meta = new GameMetadata();

            meta.SetMoveCommentary(0, "First move");
            meta.SetMoveCommentary(1, "Second move");
            meta.SetMoveCommentary(0, "First updated");

            Assert.HasCount(2, meta.MoveCommentary);
            Assert.AreEqual("First updated", meta.GetMoveCommentary(0));
            Assert.AreEqual("Second move", meta.GetMoveCommentary(1));
            Assert.IsNull(meta.GetMoveCommentary(10));
        }

        [TestMethod]
        public void GameMetaData_Clone()
        {
            var meta = new GameMetadata();
            meta.SetTag("Event", "TestEvent");
            meta.SetTag("Result", "BlackWins");
            meta.SetTag("GameType", "Base");
            meta.SetMoveCommentary(5, "Blunder");

            var clone = meta.Clone();

            Assert.AreEqual("TestEvent", clone.Event);
            Assert.AreEqual(BoardState.BlackWins, clone.Result);
            Assert.AreEqual(GameType.Base, clone.GameType);
            Assert.IsTrue(clone.MoveCommentary.ContainsKey(5));
            Assert.AreEqual("Blunder", clone.MoveCommentary[5]);
        }
    }
}
