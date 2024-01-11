namespace Qkmaxware.Vm.LR35902;

 

public class DataSize {

    public int ByteCount {get; private set;}

 

    protected DataSize(int bytes) {

        this.ByteCount = bytes;

    }

 

    public static DataSize Bytes(int b) => new DataSize(b);

    public static DataSize Kilobytes(int kB) => Bytes(1000 * kB);

    public static DataSize Kibibytes(int kB) => Bytes(1024 * kB);

}

 

public enum Endianness {

    LittleEndian, BigEndian

}

 

public class FlatRam : IMemory {

    private byte[] bytes;

 

    public Endianness Endianness {get; private set;}

    public DataSize Size {get; private set;}

 

    public FlatRam(DataSize size, Endianness endianness = Endianness.LittleEndian) {

        this.Endianness = endianness;

        this.Size = size;

        this.bytes = new byte[size.ByteCount];

    }

 

    public int ReadByte(int address) {

        return bytes[address];

    }

 

    public int ReadShort(int address) {

        if (Endianness == Endianness.LittleEndian) {

            return (ReadByte(address)) | (ReadByte(address + 1) << 8);

        } else {

            return (ReadByte(address) << 8) | (ReadByte(address + 1));

        }

    }

 

    public void WriteByte(int address, int value) {

        bytes[address] = (byte)(value & 0xFF);

    }

 

    public void WriteShort(int address, int value) {

        if (Endianness == Endianness.LittleEndian) {

            WriteByte(address, value & 0xFF);

            WriteByte(address + 1, (value >> 8) & 0xFF);

        } else {

            WriteByte(address, (value >> 8) & 0xFF);

            WriteByte(address + 1, value & 0xFF);

        }

    }

 

    public void Reset() {

        Array.Fill(bytes, (byte)0);

    }

}