using Qkmaxware.Vm.LR35902;

namespace Qkmaxware.Emulators.Gameboy.Hardware;

/// <summary>
/// MBC1 controller
/// https://gbdev.io/pandocs/MBC1.html
/// This is the first MBC chip for the Game Boy
/// </summary>
public class Mbc1 : BaseMbc {

    private List<byte[]> eram;

    #region Registers
    private bool ramEnable;
    private int lowerRomBankNumber = 1;
    private int higherRomBankNumber = 0;
    private int romBankIndex => (higherRomBankNumber << 5) + lowerRomBankNumber;
    private int ramBankIndex;
    private BankingModeSelect bankingModeSelect;
    #endregion

    enum BankingModeSelect {
        Simple = 0, Advanced = 1
    }

    public Mbc1(Cartridge cart) : base(cart) {
        this.eram = new List<byte[]>();
        for (var i = 0; i < Math.Max(1, cart.Info.eramClass.BankCount); i++) {
            this.eram.Add(new byte[KiB_8.ByteCount]);
        }
    }

    public override void Reset() {
        ramEnable = false;
        lowerRomBankNumber = 1;
        higherRomBankNumber = 0; 
        ramBankIndex = 0;
        bankingModeSelect = BankingModeSelect.Simple;
        foreach (var bank in eram) {
            Array.Fill(bank, (byte)0);
        }
    }

    public override byte[] GetActiveRam() {
        return this.eram[ramBankIndex];
    }

    public override IEnumerable<byte[]> GetRamBanks() {
        foreach (var bank in this.eram)
            yield return bank;
    }

    public override int ReadByte(int addr) {
        // 0000–3FFF — ROM Bank X0 (Read Only)
        if (between(addr, 0x0000, 0x3FFF)) {
            // This area normally contains the first 16 KiB (bank 00) of the cartridge ROM.
            if (Cart is null)
                return LO;
            return Cart.read(addr);
        }

        // 4000–7FFF — ROM Bank 01-7F
        if (between(addr, 0x4000, 0x7FFF)) {
            if (Cart is null)
                return LO;
            var offset = romBankIndex * KiB_16.ByteCount;
            return Cart.read(offset + (addr - 0x4000));
        }

        // A000–BFFF — RAM Bank 00–03
        if (between(addr, 0xA000, 0xBFFF)) {
            if (!ramEnable) {
                return 0xFF; // Otherwise reads return open bus values (often $FF, but not guaranteed) 
            }
            return eram[ramBankIndex][addr - 0xA000];
        }

        return 0;
    }

    public override void WriteByte(int addr, int value)
    {
        // 0000–1FFF — RAM Enable (Write Only)
        if (between(addr, 0x0000, 0x1FFF)) {
            this.ramEnable = (value & 0xF) == 0xA;
        }
        // 2000–3FFF — ROM Bank Number (Write Only)
        if (between(addr, 0x2000, 0x3FFF)) {
            var desiredBank = value & 0b11111; // Last 5 bits only
            if (desiredBank == 0)
                desiredBank = 1; // cannot duplicate bank $00 into both the 0000–3FFF and 4000–7FFF
            // If the ROM Bank Number is set to a higher value than the number of banks in the cart, 
            // the bank number is masked to the required number of bits
            // TODO 
            this.lowerRomBankNumber = desiredBank;
        }
        // 4000–5FFF — RAM Bank Number (Write Only)
        if (between(addr, 0x4000, 0x5FFF)) {
            if (this.bankingModeSelect == BankingModeSelect.Simple) {
                // This second 2-bit register can be used to select a RAM Bank in range from $00–$03
                this.ramBankIndex = value & 0b11;
            } else {
                // or to specify the upper two bits (bits 5-6) of the ROM Bank number
                this.higherRomBankNumber = (value & 0b110000) >> 5;
            }
        }   
        // 6000–7FFF — Banking Mode Select (Write Only)
        if (between(addr, 0x6000, 0x7FFF)) {
            /*
            This 1-bit register selects between the two MBC1 banking modes, 
            controlling the behaviour of the secondary 2-bit banking register (above). 
            If the cart is not large enough to use the 2-bit register (≤ 8 KiB RAM and ≤ 512 KiB ROM) 
            this mode select has no observable effect. 
            The program may freely switch between the two modes at any time.
            */
            this.bankingModeSelect = (BankingModeSelect)(value & 0b1);
        }

        // A000–BFFF — RAM Bank 00–03
        if (between(addr, 0xA000, 0xBFFF)) {
            if (!ramEnable) {
                eram[0][addr - 0xA000] = (byte)value;
            }
            eram[ramBankIndex][addr - 0xA000] = (byte)value;
        }

    }
}