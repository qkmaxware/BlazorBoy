namespace Qkmaxware.Vm.LR35902;

public class Operation {
    public int Opcode {get; private set;}
    public string? Name {get; private set;}
    public Action? Action {get; private set;}

    public Operation (int opcode, string name, Action? action = null) {
        this.Opcode = opcode;
        this.Name = name;
        this.Action = action;
    }

    public Operation (int opcode, string name, Operation[]? map, Action? action = null) {
        this.Opcode = opcode;
        this.Name = name;
        this.Action = action;

         if(map is not null) {
            if (this.Opcode >= 0 && this.Opcode < map.Length) {
                map[this.Opcode] = this;
            } else {
                throw new IndexOutOfRangeException($"Cannot auto-insert opcode {opcode:X2} into the provided map.");
            }
        }
    }

    public void Invoke() {
        this.Action?.Invoke();
    }

    public override string ToString() {
        return Name ?? ("0x" + Opcode.ToString());
    }
}