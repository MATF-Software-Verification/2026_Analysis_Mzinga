using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mzinga.Core;
using System.Reflection;

namespace Mzinga.Tests.New
{
    [TestClass]
    public class MoveSetTests
    {
        [TestMethod]
        public void MoveSet_Add()
        {
            var moveSet = new MoveSet();
            var move = new Move(PieceName.wA1, Position.OriginPosition, new Position(0, -1, 0));
            
            Assert.HasCount(0, moveSet);
            
            var addMethod = typeof(MoveSet).GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic, null, [typeof(Move).MakeByRefType()], null);
            addMethod!.Invoke(moveSet, [move]);

            Assert.HasCount(1, moveSet);
            Assert.IsTrue(moveSet.Contains(PieceName.wA1));
            Assert.IsFalse(moveSet.Contains(PieceName.wA2));
        }

        [TestMethod]
        public void MoveSet_Clear()
        {
            var moveSet = new MoveSet();
            var move = new Move(PieceName.wA1, Position.OriginPosition, new Position(0, -1, 0));

            var addMethod = typeof(MoveSet).GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic, null, [typeof(Move).MakeByRefType()], null);
            addMethod!.Invoke(moveSet, [move]);
            
            var clearMethod = typeof(MoveSet).GetMethod("Clear", BindingFlags.Instance | BindingFlags.NonPublic);
            clearMethod!.Invoke(moveSet, null);

            Assert.HasCount(0, moveSet);
        }
    }
}
