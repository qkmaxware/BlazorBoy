using Qkmaxware.Vm.LR35902;

namespace Qkmaxware.Emulators.Gameboy.Hardware;


/// <summary>
/// Base class for MBC controllers
/// </summary>
public abstract class BaseMbc : IMbc {

    protected static readonly DataSize KiB_16 = DataSize.Kibibytes(16);
    protected static readonly DataSize KiB_8 = DataSize.Kibibytes(8);

    protected static readonly int LO = 0;
    protected static readonly int HI = 0xFF;

    protected Cartridge Cart {get; private set;}

    public BaseMbc(Cartridge cart) {
        this.Cart = cart;
    }
    protected static bool between(int x, int lower, int upper) {
        return lower <= x && x <= upper;
    }

    public abstract void Reset();

    public abstract byte[] GetActiveRam();
    public abstract IEnumerable<byte[]> GetRamBanks();
    public virtual void UpdateRamBanks(IEnumerable<byte[]> banks) {
        // Reset the banks
        foreach (var bank in this.GetRamBanks()) {
            Array.Fill(bank, (byte)0);
        }

        // For each bank with a cooresponding update bank provided, copy bytes from the update into the original
        foreach (var pair in this.GetRamBanks().Zip(banks)) {
            Array.Copy(pair.Second, pair.First, Math.Min(pair.First.Length, pair.Second.Length));
        }
    }


    public abstract int ReadByte(int addr);
    public abstract void WriteByte(int addr, int value);

}
