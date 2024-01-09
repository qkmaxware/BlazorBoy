namespace Qkmaxware.Emulators.Gameboy.Hardware;

public class Sprite {
    public enum Priority: byte {
        AboveBackground = (0), BelowBackground = (1)
    }
    public enum XOrientation : byte {
        Normal=(0), Flipped=(1)
    }
    public enum YOrientation: byte {
        Normal=(0), Flipped=(1)
    }
    public enum Palette : byte {
        Zero=(0), One=(1)
    }

    public int id;
    
    //Position
    public int y = -16;
    public int x = -8;
    
    //Data tile number
    public int tile = 0;
    
    //Options
    public Priority priority = Priority.AboveBackground;
    public XOrientation xflip = XOrientation.Normal;
    public YOrientation yflip = YOrientation.Normal;
    public Palette objPalette = Palette.One;
    
    public Sprite(int id){
        this.id = id;
    }
    
    public void Reset(){
        y = -16;
        x = -8;
        tile = 0;
        priority = Priority.AboveBackground;
        xflip = XOrientation.Normal;
        yflip = YOrientation.Normal;
        objPalette = Palette.One; 
    }
}