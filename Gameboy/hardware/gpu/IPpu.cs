namespace Qkmaxware.Emulators.Gameboy.Hardware;

/// <summary>
/// Picture processing unit interface
/// </summary>
public interface IPpu : IMemorySegment {
    /// <summary>
    /// Canvas containing the rendered image
    /// </summary>
    public Bitmap Canvas {get;}

    #region Flags
    /// <summary>
    /// Test if the PPU's buffer has flushed last step
    /// </summary>
    public bool HasBufferJustFlushed {get;}
    #endregion

    /// <summary>
    /// Perform a single GPU step
    /// </summary>
    /// <param name="step">cpu cycles</param>
    public void Step(int step);

    /// <summary>
    /// Get the current state of the PPU
    /// </summary>
    /// <returns>state</returns>
    public PpuState GetState();
}