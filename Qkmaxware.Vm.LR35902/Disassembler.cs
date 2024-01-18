namespace Qkmaxware.Vm.LR35902;

/// <summary>
/// Disassembled instruction information
/// </summary>
public class InstructionInfo {
    public uint Address {get; set;}
    public Operation? Operation {get; set;}
    public byte Opcode {get; set;}
    public int[] Arguments {get; set;}

    public InstructionInfo(uint address, byte value) {
        this.Address = address;
        this.Arguments = new int[0];    
        this.Opcode = value;
    }
    public InstructionInfo(uint address, Operation op, int[] args) {
        this.Address = address;
        this.Opcode = (byte)op.Opcode;
        this.Operation = op;
        this.Arguments = args;
    }
}

/// <summary>
/// Disassemble compiled binary into assembly instructions
/// </summary>
public class Disassembler {

    /// <summary>
    /// The endianness of the system, used for multi-byte instruction arguments
    /// </summary>
    private Endianness Endianness {get; set;}

    /// <summary>
    /// Create a new disassembler with the given endianness
    /// </summary>
    /// <param name="endianness">endianness used for multi-byte instruction arguments</param>
    public Disassembler(Endianness endianness) {
        this.Endianness = endianness;
    }

    /// <summary>
    /// Disassemble a sequence of compiled bytes in a program
    /// </summary>
    /// <param name="bytes">bytes to disassemble</param>
    /// <returns>list of assembly instructions with arguments</returns>
    public IEnumerable<InstructionInfo> Disassemble(IEnumerable<byte> bytes) {
        Cpu cpu = new Cpu(new FlatRam(DataSize.Bytes(0), this.Endianness));

        uint addr = 0u;
        var enumerator = bytes.GetEnumerator();
        while (enumerator.MoveNext()) {
            var opcode = enumerator.Current;
            Operation? operation;
            if (!cpu.TryFetchOperation(opcode, out operation)) {
                yield return new InstructionInfo(addr, opcode);
                addr++;
                continue;
            }
            addr++;

            int[] args = new int[operation.Arity];
            for (var i = 0; i < args.Length; i++) {
                switch (operation.GetArgumentType(i)) {
                    case ArgT.Byte: {
                            enumerator.MoveNext();
                            var value = enumerator.Current;
                            addr++;
                            args[i] = value;
                        }
                        break;
                    case ArgT.Short:{
                            enumerator.MoveNext();
                            var value1 = (int)enumerator.Current;
                            addr++;
                            enumerator.MoveNext();
                            var value2 = (int)enumerator.Current;
                            addr++;
                            if (Endianness == Endianness.LittleEndian) {
                                args[i] = (value2 << 8) | value1; // Little endian
                            } else {
                                args[i] = (value1 << 8) | value2; // Big endian
                            }
                        }
                        break;
                    default:
                        throw new ArgumentException("Disassembler doesn't know how to read arguments of this type");
                }
            }

            yield return new InstructionInfo(addr, operation, args);
        }
    }
}