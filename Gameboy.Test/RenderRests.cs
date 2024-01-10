using System.ComponentModel;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qkmaxware.Emulators.Gameboy.Hardware;

namespace Qkmaxware.Emulators.Gameboy.Test;

[TestClass]
public class RenderTests : GbTests {

    [TestMethod]
    public void TestDoesFlushBuffer() {
        var gb = MakeGB(out var results);

        // Read and validate cart
        var name = "01-special";
        var cart = ReadCart(name);
        gb.LoadCartridge(cart);

        // Run test
        int lineNo = 0;
        var flushed = false;
        for (lineNo = 0; lineNo < 1000000; lineNo++) {
            gb.Dispatch();
            if (gb.GPU.HasBufferJustFlushed) {
                flushed = true;
                break;
            }
        }

        Assert.AreNotEqual(0, lineNo);
        Assert.AreEqual(true, flushed);
    }

}