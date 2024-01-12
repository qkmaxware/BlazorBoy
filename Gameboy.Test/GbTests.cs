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