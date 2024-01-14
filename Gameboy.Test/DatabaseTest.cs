using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Qkmaxware.Emulators.Gameboy.Test;

[TestClass]
public class GameDatabaseTest {
    [TestMethod]
    public void TestGameDatabaseNonEmpty() {
        Assert.AreEqual(true, GameDatabase.Instance().Any());
    }
}