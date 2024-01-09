using Qkmaxware.Vm.LR35902;

namespace Qkmaxware.Emulators.Gameboy;

public class OperationTrace {
    public Operation Operation {get; private set;}
    public int StartAddress {get; private set;}
    public OperationTrace(int address, Operation op) {
        this.Operation = op;
        this.StartAddress = address;
    }
}

public interface ITrace {
    public OperationTrace? Last {get;}
    public void Add(int address, Operation op);
}

public class InstructionHistoryTrace : ITrace {
    public int MaxHistory {get; private set;}
    public bool TrackPerformance {get; private set;} = false;
    private LinkedList<OperationTrace> operations = new LinkedList<OperationTrace>();

    public OperationTrace? Last => operations.Count == 0 ? null : operations.Last?.Value;

    public IEnumerable<OperationTrace> History {
        get {
            var el = operations.Last;
            while (el is not null) {
                yield return el.Value;
                el = el.Previous;
            }
        }
    }

    public InstructionHistoryTrace() : this(25) {}
    public InstructionHistoryTrace(int max) {
        this.MaxHistory = Math.Max(1, max);
    }

    public void Add(int address, Operation op) {
        operations.AddLast(new OperationTrace(address, op));
        while (operations.Count > MaxHistory) {
            operations.RemoveFirst();
        }
    }
}

public class TerminalTrace : ITrace {
    public OperationTrace? Last {get; private set;}

    public void Add(int address, Operation op) {
        this.Last = new OperationTrace(address, op);
        Console.WriteLine(op);
    }
}

public class FileTrace : ITrace {
    
    public string FileName {get; private set;}
    public OperationTrace? Last {get; private set;}

    public Registry? DumpRegistry {get; set;}

    private TextWriter writer;

    public FileTrace(string filename, Registry? dumpRegistry = null) {
        this.FileName = filename;
        this.DumpRegistry = dumpRegistry;
        writer = File.AppendText(FileName);
    }   

    ~FileTrace() {
        writer?.Dispose();
    }

    public void Add(int address, Operation op) {
        this.Last = new OperationTrace(address, op);
        writer.Write($"0x{address:X4}: {op.Name}".PadRight(25));
        if (DumpRegistry is not null) {
            writer.Write(" -> ");
            writer.Write(DumpRegistry.ToString());
        }
        writer.WriteLine();
        writer.Flush();
    }
}