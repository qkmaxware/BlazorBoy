using Qkmaxware.Vm.LR35902;

namespace Qkmaxware.Emulators.Gameboy.Hardware;

public interface IMbc : IResetable {
    /**
     * Get the offset value to use for ram access
     * @return 
     */
    public int GetRamOffset();
    
    /**
     * Get the offset value to use for rom access
     * @return 
     */
    public int GetRomOffset();
    
    /**
     * Get a reference to the ram array for this controller
     * @return 
     */
    public byte[] GetActiveRam();
    public IEnumerable<byte[]> GetRamBanks();
    
    /**
     * Read a byte from this controller
     * @param addr
     * @return 
     */
    public int ReadByte(int addr);
    
    /**
     * Write a value to this controller (triggers side-effects)
     * @param addr
     * @param value 
     */
    public void WriteByte(int addr, int value);
}