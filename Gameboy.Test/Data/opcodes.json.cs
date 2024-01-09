namespace Qkmaxware.Emulators.Gameboy.Test;

public class OpcodesJsonModel {
    public Dictionary<string, OpcodeJsonModel>? unprefixed {get; set;}
    public Dictionary<string, OpcodeJsonModel>? cbprefixed {get; set;}
}

public class OpcodeJsonModel {
    public string? mnemonic {get; set;}
    public int length {get; set;}
    public int[]? cycles {get; set;}
    public string[]? flags {get; set;}
    public string? addr {get; set;}
    public string? group {get; set;}
}