namespace Qkmaxware.Vm.LR35902;

public enum ArgT {
    Byte, Short
}

public class Operation {
    public int Opcode {get; private set;}
    public string? Name {get; private set;}
    private ArgT[] args;
    public int Arity => args.Length;
    public Action<int[]>? Action {get; private set;}

    public Operation (int opcode, string name, Action<int[]>? action = null) : this(opcode, name, null, null, action) {}
    public Operation (int opcode, string name, ArgT[] args, Action<int[]>? action = null) : this(opcode, name, args, null, action) {}
    public Operation (int opcode, string name,  Operation[]? map, Action<int[]>? action = null) : this(opcode, name, null, map, action) {}
    public Operation (int opcode, string name, ArgT[]? args, Operation[]? map, Action<int[]>? action = null) {
        this.Opcode = opcode;
        this.Name = name;
        this.Action = action;
        this.args = args ?? new ArgT[0];

         if(map is not null) {
            if (this.Opcode >= 0 && this.Opcode < map.Length) {
                map[this.Opcode] = this;
            } else {
                throw new IndexOutOfRangeException($"Cannot auto-insert opcode {opcode:X2} into the provided map.");
            }
        }
    }

    public void Invoke(int[] args) {
        this.Action?.Invoke(args);
    }

    public ArgT GetArgumentType(int i) {
        if (i < 0 || i >= args.Length)
            return ArgT.Byte;
        else 
            return args[i];
    }

    public override string ToString() {
        return Name ?? ("0x" + Opcode.ToString());
    }
}