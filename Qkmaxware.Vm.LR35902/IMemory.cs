namespace Qkmaxware.Vm.LR35902;

/// <summary>
/// Memory interface
/// </summary>
public interface IMemory : IResetable {
    /// <summary>
    /// Read a byte from memory
    /// </summary>
    /// <param name="address">address of the byte</param>
    /// <returns>byte</returns>
    public int ReadByte(int address);

    /// <summary>
    /// Read a short (16 bits) from memory
    /// </summary>
    /// <param name="address">start address</param>
    /// <returns>short</returns>
    public int ReadShort(int address);

    /// <summary>
    /// Write a byte to memory
    /// </summary>
    /// <param name="address">address of the byte</param>
    /// <param name="value">value to store</param>
    public void WriteByte(int address, int value);

    /// <summary>
    /// Write a short (16bits) to memory
    /// </summary>
    /// <param name="address">start address</param>
    /// <param name="value">value to store</param>
    public void WriteShort(int address, int value);
}