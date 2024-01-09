namespace Qkmaxware.Emulators.Gameboy.Hardware;

public class SerialConnection : IMemorySegment {
    private TextReader? reader;
    private TextWriter? writer;

    public bool EnableWrite = false;
    public bool EnableRead = false;

    

    public SerialConnection(TextReader? reader, TextWriter? writer){
        this.reader = reader;
        this.writer = writer;
    }

    public void SwapReader(TextReader? reader) {
        this.reader = reader;
    }
    public void SwapWriter(TextWriter? writer) {
        this.writer = writer;
    }

    public void Reset() { }
    public void SetMMU(MemoryMap mmu) { }
    public int ReadByte(int addr) {
        try{
            if(EnableRead){
                return (reader?.Read() ?? 0) & 0xFF;
            }
            else{
                return 0;
            }
        }catch(Exception){
            return 0;
        }
    }

    public void WriteByte(int addr, int value) {
        try{
            if(EnableWrite){
                writer?.Write(System.Text.Encoding.ASCII.GetString(new byte[]{(byte)value}));
                writer?.Flush();
            }
        }catch (Exception){}
    }

    public int ReadShort(int address)
    {
        throw new NotImplementedException();
    }

    public void WriteShort(int address, int value)
    {
        throw new NotImplementedException();
    }
}