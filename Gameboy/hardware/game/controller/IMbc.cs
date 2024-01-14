using Qkmaxware.Vm.LR35902;

namespace Qkmaxware.Emulators.Gameboy.Hardware;

public interface IMbc : IResetable {
    /// <summary>
    /// Get a reference to the ram array for this controller
    /// </summary>
    /// <returns>active ram array</returns>
    public byte[] GetActiveRam();
    
    /// <summary>
    /// Get all RAM banks managed by this controller
    /// </summary>
    /// <returns>banks of RAM</returns>
    public IEnumerable<byte[]> GetRamBanks();

    /// <summary>
    /// Update the value of each ram bank to those provided
    /// </summary>
    /// <param name="banks">banks of RAM</param>
    public void UpdateRamBanks(IEnumerable<byte[]> banks);
    
    /// <summary>
    /// Read a byte from this controller
    /// </summary>
    /// <param name="addr">address</param>
    /// <returns>byte</returns>
    public int ReadByte(int addr);
    
    /// <summary>
    /// Write a value to this controller (triggers side-effects)
    /// </summary>
    /// <param name="addr">address to write to</param>
    /// <param name="value">byte to write</param>
    public void WriteByte(int addr, int value);
}