using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qkmaxware.Vm.LR35902;

namespace Qkmaxware.Emulators.Gameboy.Test;

[TestClass]

public partial class VersionUpdateTests : GbTests {

    [TestMethod]
    public void TestGpuToPpu() {
        var game = ReadCart("bgbtest");
        Gameboy normal = new Gameboy(gpu: new Hardware.Gpu());
        normal.LoadCartridge(game);
        Gameboy newppu = new Gameboy(gpu: new Hardware.Ppu());
        newppu.LoadCartridge(game);

        var test_time = TimeSpan.FromSeconds(30);
        var fps = 60;
        var frames = test_time.TotalSeconds * fps;

        AssertState(normal.GetState(), newppu.GetState(), "Frame " + 0 + " differs between implementations");
        var frame = 0;
        for (; frame <= frames; frame++) {
            // Step both systems
            normal.DispatchUntilBufferFlush();
            newppu.DispatchUntilBufferFlush();

            // Compare system states
            AssertState(normal.GetState(), newppu.GetState(), "Frame " + frame + " differs between implementations");
        }
    }
    
}