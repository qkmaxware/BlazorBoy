namespace Qkmaxware.Emulators.Gameboy.Test;

public class OpcodeTestJsonModel {
    public string? name {get; set;}
    public OpcodeTestStateJsonModel? initial {get; set;}
    public OpcodeTestStateJsonModel? final {get; set;}
}

 
public class OpcodeTestStateJsonModel {
    public int pc {get; set;}
    public int sp {get; set;}
    public int a  {get; set;}
    public int b  {get; set;}
    public int c  {get; set;}
    public int d  {get; set;}
    public int e  {get; set;}
    public int f  {get; set;}
    public int h  {get; set;}
    public int l  {get; set;}
    public int ime  {get; set;}
    public int ie  {get; set;}
    public int[][]? ram  {get; set;}
}