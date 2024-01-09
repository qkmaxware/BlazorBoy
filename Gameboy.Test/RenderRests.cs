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

    [TestMethod]
    public void TestDoesVBlank() {
        // Get GB
        var gb = MakeGB(out var results);
        int lineNo = 0;
        int? vblankLineNo = null;
        gb.OnVBlank += (bmp) => { 
            if (lineNo != 0 && !vblankLineNo.HasValue) {
                if (bmp.Flatten().Where(pixel => pixel != ColourPallet.BackgroundWhite).Any()) {
                    vblankLineNo = lineNo; // Have we actually drawn a pixel
                }
            } 
        };

        // Read and validate cart
        var name = "01-special";
        var cart = ReadCart(name);
        gb.LoadCartridge(cart);

        // Run test
        for (lineNo = 0; lineNo < 1000000; lineNo++) {
            gb.Dispatch();
        }

        // Validate results
        //Assert.Fail("VBLANK AFTER + " + (vblankLineNo.HasValue ? vblankLineNo.Value : 0).ToString());
        Assert.AreEqual(true, vblankLineNo.HasValue);
        Assert.AreNotEqual(0, vblankLineNo.HasValue ? vblankLineNo.Value : 0);
        Assert.AreNotEqual(1, vblankLineNo.HasValue ? vblankLineNo.Value : 1);
    }

}