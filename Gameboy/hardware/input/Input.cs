using Qkmaxware.Vm.LR35902;

namespace Qkmaxware.Emulators.Gameboy.Hardware;

public enum KeyCodes {
    Up, Down, Left, Right, Select, Start, A, B
}

public class Input : IMemorySegment {
    private int[] rows = new int[]{
        0x0F,                       // buttons
        0x0F                        // directional keys
    };
    private int colidx = 0;
    private Timer timer = new Timer(); 

    public void Reset(){
        Array.Fill(rows, 0x0F);
        timer.Reset();
        colidx = 0;
    }

    public int ReadByte(int addr){
        if(addr == 0xFF00){
            switch(colidx){
                case 0x10:
                    return rows[0];
                case 0x20:
                    return rows[1];
            }
        }
        return 0;
    }
    
    public void WriteByte(int addr, int value){
        if(addr == 0xFF00){
            colidx = value & 0x30;
        }
    }

    public bool IsKeyDown(KeyCodes key){
        switch(key){
            case KeyCodes.Up:
                return (rows[1] &= 0x4) == 0;
            case KeyCodes.Down:
                return (rows[1] &= 0x8) == 0;
            case KeyCodes.Left:
                return (rows[1] &= 0x2) == 0;
            case KeyCodes.Right:
                return (rows[1] &= 0x1) == 0;
            case KeyCodes.Select:
                return (rows[0] &= 0x8) == 0;
            case KeyCodes.Start:
                return (rows[0] &= 0x4) == 0;
            case KeyCodes.A:
                return (rows[0] &= 0x1) == 0;
            case KeyCodes.B:
                return (rows[0] &= 0x2) == 0;
        }
        return false;
    }

    public void KeyDown(KeyCodes keycode){
        if(keycode == KeyCodes.Up){
            rows[1] &= 0xB;
        }
        else if(keycode == KeyCodes.Down){
            rows[1] &= 0x7;
        }
        else if(keycode == KeyCodes.Left){
            rows[1] &= 0xD;
        }
        else if(keycode == KeyCodes.Right){
            rows[1] &= 0xE;
        }
        else if(keycode == KeyCodes.Start){
            rows[0] &= 0x7;
        }
        else if(keycode == KeyCodes.Select){
            rows[0] &= 0xB;
        }
        else if(keycode == KeyCodes.A){
            rows[0] &= 0xE;
        }
        else if(keycode == KeyCodes.B){
            rows[0] &= 0xD;
        }
    }
    
    public void KeyUp(KeyCodes keycode){
        if(keycode == KeyCodes.Up){
            rows[1] |= 0x4;
        }
        else if(keycode == KeyCodes.Down){
            rows[1] |= 0x8;
        }
        else if(keycode == KeyCodes.Left){
            rows[1] |= 0x2;
        }
        else if(keycode == KeyCodes.Right){
            rows[1] |= 0x1;
        }
        else if(keycode == KeyCodes.Start){
            rows[0] |= 0x8;
        }
        else if(keycode == KeyCodes.Select){
            rows[0] |= 0x4;
        }
        else if(keycode == KeyCodes.A){
            rows[0] |= 0x1;
        }
        else if(keycode == KeyCodes.B){
            rows[0] |= 0x2;
        }
    }

    public void SetMMU(MemoryMap mmu) {  }

    

    
}