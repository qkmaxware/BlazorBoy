using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Qkmaxware.Emulators.Gameboy.Test;

public class GbTests {
    public Gameboy MakeGB(out StringWriter stdout) {
        // Configure GB
        Gameboy gb = new Gameboy();
        StringReader input = new StringReader("");
        StringWriter output = new StringWriter();
        stdout = output;
        gb.Serial.EnableRead = true;
        gb.Serial.SwapReader(input);
        gb.Serial.EnableWrite = true;
        gb.Serial.SwapWriter(output);
        return gb;
    }

    public void AssertState(ConsoleState expected, ConsoleState actual, string? message = null) {
        try {
            // Compare CPU state
            if (expected.Cpu is not null && actual.Cpu is not null) {
                Assert.AreEqual(expected.Cpu.Pc, actual.Cpu.Pc);
                Assert.AreEqual(expected.Cpu.Sp, actual.Cpu.Sp);
                Assert.AreEqual(expected.Cpu.A, actual.Cpu.A);
                Assert.AreEqual(expected.Cpu.B, actual.Cpu.B);
                Assert.AreEqual(expected.Cpu.C, actual.Cpu.C);
                Assert.AreEqual(expected.Cpu.D, actual.Cpu.D);
                Assert.AreEqual(expected.Cpu.E, actual.Cpu.E);
                Assert.AreEqual(expected.Cpu.F, actual.Cpu.F);
                Assert.AreEqual(expected.Cpu.Hi, actual.Cpu.Hi);
                Assert.AreEqual(expected.Cpu.Lo, actual.Cpu.Lo);
                Assert.AreEqual(expected.Cpu.Ime, actual.Cpu.Ime);
            } else if (expected.Cpu is not null && actual.Cpu is null) {
                Assert.Fail("Expecting a CPU state, but actual state is null.");
            } else if (expected.Cpu is null && actual.Cpu is not null) {
                Assert.Fail("Expecting a null CPU state, but actual contains CPU state info.");
            }

            // Compare GPU state
            if (expected.Ppu is not null && actual.Ppu is not null) {
                Assert.AreEqual(expected.Ppu.Lcdc, actual.Ppu.Lcdc);
                Assert.AreEqual(expected.Ppu.Stat, actual.Ppu.Stat);
                Assert.AreEqual(expected.Ppu.ViewportX, actual.Ppu.ViewportX);
                Assert.AreEqual(expected.Ppu.ViewportY, actual.Ppu.ViewportY);
                Assert.AreEqual(expected.Ppu.WindowX, actual.Ppu.WindowX);
                Assert.AreEqual(expected.Ppu.WindowY, actual.Ppu.WindowY);
                Assert.AreEqual(expected.Ppu.Lyc, actual.Ppu.Lyc);
                Assert.AreEqual(expected.Ppu.Scanline, actual.Ppu.Scanline);

                Assert.AreEqual(expected.Ppu.OamBytes, actual.Ppu.OamBytes);
                Assert.AreEqual(expected.Ppu.VramBytes, actual.Ppu.VramBytes);
            } else if (expected.Ppu is not null && actual.Ppu is null) {
                Assert.Fail("Expecting a GPU state, but actual state is null.");
            } else if (expected.Ppu is null && actual.Ppu is not null) {
                Assert.Fail("Expecting a null GPU state, but actual contains GPU state info.");
            }

            // Compare Cart RAM state
            if (expected.Cart is not null && actual.Cart is not null) {
                Assert.AreEqual(expected.Cart.RamBanks?.Length ?? 0, actual.Cart.RamBanks?.Length ?? 0);
                if (expected.Cart.RamBanks is not null && actual.Cart.RamBanks is not null) {
                    for (var i = 0; i < expected.Cart.RamBanks.Length; i++) {
                        Assert.AreEqual(expected.Cart.RamBanks[i], actual.Cart.RamBanks[i], "Ram Bank " + i + " doesn't match");
                    }
                }
            } else if (expected.Cart is not null && actual.Cart is null) {
                Assert.Fail("Expecting a Cart state, but actual state is null.");
            } else if (expected.Cart is null && actual.Cart is not null) {
                Assert.Fail("Expecting a null Cart state, but actual contains Cart state info.");
            }
        } catch (UnitTestAssertException outcome) {
            if (message is null)
                throw;
            throw new Exception(message, outcome);
        }
    }

    public Hardware.Cartridge ReadCart(string filename) {
        var assembly = typeof(IsaTests).GetTypeInfo().Assembly;
        foreach (var name in assembly.GetManifestResourceNames()) {
            if (name.EndsWith(filename + ".gb")) {
                Stream? resource = assembly.GetManifestResourceStream(name);
                if (resource is null)
                    continue;

                using (MemoryStream ms = new MemoryStream()) {
                    resource.CopyTo(ms);
                    var cart = new Hardware.Cartridge(ms.ToArray());
                    Assert.AreNotEqual(cart, cart.Info);
                    Assert.IsNotNull(cart.Info.title);
                    return cart;
                }
            }
        }
        throw new ArgumentException("Cartridge with pattern *" + filename + ".gb not found");
    }
    public T ReadJson<T>(string filename) {
        var assembly = typeof(IsaTests).GetTypeInfo().Assembly;
        foreach (var name in assembly.GetManifestResourceNames()) {
            if (name.EndsWith(filename + ".json")) {
                Stream? resource = assembly.GetManifestResourceStream(name);
                if (resource is null)
                    continue;

                var obj = System.Text.Json.JsonSerializer.Deserialize<T>(resource);
                if (obj is null)
                    throw new ArgumentException("Json with pattern *" + filename + ".json is null or empty");
                return obj;
            }
        }
        throw new FileNotFoundException("Json with pattern *" + filename + ".json not found");
    }
}