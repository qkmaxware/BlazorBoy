using Qkmaxware.Vm.LR35902;

namespace Qkmaxware.Emulators.Gameboy;

public interface IMemorySegment : IResetable {
    public void SetMMU(MemoryMap mmu);

    public int ReadByte(int addr);
    public void WriteByte(int addr, int value);
}