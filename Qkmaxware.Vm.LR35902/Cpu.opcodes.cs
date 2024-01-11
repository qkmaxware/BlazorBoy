using System.Diagnostics.CodeAnalysis;

namespace Qkmaxware.Vm.LR35902;

/// <summary>
/// LR35902 (Z80) emulated CPU
/// </summary>
public partial class Cpu {

    // https://github.com/qkmaxware/GBemu/blob/master/src/gameboy/cpu/Opcodes.java

    public bool IsValidOpcode(int opcode){
        if(this.map is null){
            return false;
        }
        if(opcode < 0 || opcode >= this.map.Length){
            return false;
        }
        return true;
    }

    public Operation FetchOperation(int opcode) {
        if (!IsValidOpcode(opcode)) {
            throw new ArgumentException($"Opcode 0x{opcode:X2} out of range");
        }
        var op = this.map[opcode];
        if (op is null) {
            throw new ArgumentException($"Opcode 0x{opcode:X2} is not defined by the LR35902's instruction set");
        }
        return op;
    }

    public bool TryFetchOperation(int opcode, [NotNullWhen(true) ]out Operation? operation) {
        try {
            operation = FetchOperation(opcode);
            return true;
        } catch {
            operation = null;
            return false;
        }
    }

    public bool IsValidCbOpcode(int opcode){
        if(this.cbmap is null){
            return false;
        }
        if(opcode < 0 || opcode >= this.cbmap.Length){
            return false;
        }
        return true;
    }
    public Operation FetchCbPrefixedOperation(int cbprefixedopcode) {
        if (!IsValidCbOpcode(cbprefixedopcode)) {
            throw new ArgumentException($"CB-Prefixed Opcode 0x{cbprefixedopcode:X2} out of range");
        }
        var op = this.cbmap[cbprefixedopcode];
        if (op is null) {
            throw new ArgumentException($"CB-Prefixed Opcode 0x{cbprefixedopcode:X2} is not defined by the LR35902's instruction set");
        }
        return op;
    }

    public bool TryFetchCbPrefixedOperation(int cbprefixedopcode, [NotNullWhen(true) ]out Operation? operation) {
        try {
            operation = FetchCbPrefixedOperation(cbprefixedopcode);
            return true;
        } catch {
            operation = null;
            return false;
        }
    }

    #region utility methods

    protected bool isHalfCarry(int a, int b){
        //Borrow
        if(b < 0){
            return (((a & 0xF) - (Math.Abs(b) & 0xF)) < 0);
        }
        //Carry
        return (((a & 0xF) + (b & 0xF))) > 0xF;
    }
    
    protected bool isHalfCarry16(int a, int b){
        //Borrow
        if(b < 0){
            return (((a & 0xFFF) - (Math.Abs(b) & 0xFFF)) < 0);
        }
        //Carry
        return(((a & 0xFFF) + (b & 0xFFF))) > 0x0FFF;
    }
    
    protected bool isCarry16(int i){
        return i > 0xFFFF || i < 0;
    }
    
    protected bool isCarry(int i){
        return i > 0xFF || i < 0;
    }
    
    protected bool isZero(int i, int max){
        return (i & max) == 0;
    }
    
    protected bool isZero(int i){
        return isZero(i, BitWidth.BIT8);
    }
    
    //Compare two values and set the flags accordingly
    protected void compare(int a, int b){
        int c = a - b;
        
        reg.zero(isZero(c));                //If result is zero, set the zero flag
        reg.subtract(true);                 //Subtract is always set to true
        reg.halfcarry(isHalfCarry(a, -b));  //Set the half carry flag
        reg.carry(c < 0);                   //Set the carry flag if c < 0
    }
    
    //Check if a particular bit is set
    protected void testBit(int v, int bit){
        int mask = 1 << bit;
        //Set is bit 'b' of register r is 0;
        reg.zero((v & mask) == 0);
        reg.subtract(false);
        reg.halfcarry(true);
    }
    
    //Set a bit to 0
    protected int resetBit(int i, int bit){
        int mask = ~(1 << bit);
        return i & mask;
    }
    
    //Set a bit to 1
    protected int setBit(int i, int bit){
        int mask = 1 << bit;
        return i | mask;
    }
    
    //Rotate right 9Bit with carry
    protected int rotateRight(int value){
        //9Bit rotation to the right, the carry is copied into bit 7
        bool carry = reg.carry();
        bool leaving = (value & 0x1) != 0;
        int shift = ((value >> 1) | (carry ? 0x80 : 0)) & 0xFF;
        
        reg.carry(leaving);
        reg.subtract(false);
        reg.halfcarry(false);
        reg.zero(isZero(shift));
        
        return shift;
    }
    
    //Rotate left 9Bit with carry
    protected int rotateLeft(int value){
        //9Bit rotation to the right, the carry is copied into bit 7
        bool carry = reg.carry();
        bool leaving = (value & 0x80) != 0;
        int shift = ((value << 1) | (carry ? 0x1 : 0)) & 0xFF;
        
        reg.carry(leaving);
        reg.subtract(false);
        reg.halfcarry(false);
        reg.zero(isZero(shift));
        
        return shift;
    }
    
    //Rotate right 8Bit without carry
    protected int rotateRightCarry(int value){
        bool leaving = (value & 0x1) != 0;
        int shift = ((value >> 1) | (leaving ? 0x80 : 0)) & 0xFF;
        
        reg.zero(isZero(shift));
        reg.subtract(false);
        reg.halfcarry(false);
        reg.carry(leaving);
        
        return shift;
    }
    
    //Rotate left 8Bit without carry
    protected int rotateLeftCarry(int value){
        bool leaving = (value & 0x80) != 0;
        int shift = ((value << 1) | (leaving ? 1 : 0)) & 0xFF;
        
        reg.zero(isZero(shift));
        reg.subtract(false);
        reg.halfcarry(false);
        reg.carry(leaving);
        
        return shift;
    }
    
    //Shift to the Left filling 0's in the lower bit
    protected int shiftLeft(int value){
        int shift = (value << 1) & 0xFF;
        bool bit7 = (value & 0x80) != 0;
        
        reg.zero(isZero(shift));
        reg.subtract(false);
        reg.halfcarry(false);
        reg.carry(bit7);
        
        return shift;
    }
    
    //Shift to the right, 0's in the upper bit
    protected int shiftRight0(int value){
        int shift = (value >> 1) & 0b01111111;
        bool bit0 = (value & 0x1) != 0;
        
        reg.zero(isZero(shift));
        reg.subtract(false);
        reg.halfcarry(false);
        reg.carry(bit0);
    
        return shift;
    }
    
    //Shift to the right, bit 7 preserved
    protected int shiftRightExtend(int value){
        int shift = (value >> 1) & 0xFF;
        bool bit0 = (value & 0x1) != 0;
        int bit7 = value & 0x80;    //Preserve bit 7 value
        shift = shift | bit7;
        
        reg.zero(isZero(shift));
        reg.subtract(false);
        reg.halfcarry(false);
        reg.carry(bit0);
    
        return shift;
    }
    
    protected int signedByteToUnsigned(int s8){
        if(s8 < 0)
            s8 = 256 + s8;
        return s8;
    }
    
    protected int unsignedByteToSigned(int u8){
        if(u8 > 127)
            u8 = -((~u8+1)&255);
        return u8;
    }
    
    protected int addCarry8(int a, int b){
        int r = (a & 0xF) + (b & 0xF) + (reg.carry() ? 1 : 0);
        int r2 = a + b + (reg.carry() ? 1 : 0);
        
        reg.zero(isZero(r2));
        reg.subtract(false);
        reg.halfcarry(r > 0xF);
        reg.carry(r2 > 0xFF);
        
        return r2;
    }
    
    protected int subCarry8(int a, int b){
        int r2 = (a) - (b) - (reg.carry() ? 1 : 0);
        
        reg.zero(isZero(r2));
        reg.subtract(true);
        reg.halfcarry(((r2 ^ b ^ a) & 0x10) != 0);
        reg.carry(r2 > 0xFF || r2 < 0);
        
        return r2;
    }
    
    protected void push(int a){
        reg.sppp(-1);
        mem.WriteByte(reg.sp(), a >> 8);
        reg.sppp(-1);
        mem.WriteByte(reg.sp(), a & 0xFF);
    }
    
    protected int pop(){
        int l = mem.ReadByte(reg.sp());
        reg.sppp(1);
        int h = mem.ReadByte(reg.sp());
        reg.sppp(1);
        return h << 8 | l;
    }

    protected void rst(int loc, bool autoPushPc = true){
        if (autoPushPc) {
            push(reg.pc());
        }
        reg.pc(loc);
        
        clock.m(3); //Maybe 8 and 32 or 4 or something
        clock.t(12);
    }

    #endregion

    private static readonly int[] noArgs = new int[0];

    /// <summary>
    /// Build all Opcodes
    /// </summary>
    private void rebuildOperationMap() {
        //No Operation
        Operation NOP = new Operation(0x00, "NOP", map, (args) => {
            clock.m(1); //Actual machine time
            clock.t(4); //Number of cycles taken
        });

        ///
        // 8 Bit Loads
        ///
        
        //Load an 8bit immediate value into registry B
        Operation LD_B_n = new Operation(0x06, "LD B,n", new ArgT[]{ ArgT.Byte }, map, (args) => {
            int v = args[0];
            reg.b(v);
            clock.m(2);
            clock.t(8);
        });
        
        //Load an 8bit immediate value into registry C
        Operation LD_C_n = new Operation(0x0E, "LD C,n", new ArgT[]{ ArgT.Byte }, map, (args) => {
            int v = args[0];
            reg.c(v);
            clock.m(2);
            clock.t(8);
        });
        
        //Load an 8bit immediate value into registry D
        Operation LD_D_n = new Operation(0x16, "LD D,n", new ArgT[]{ ArgT.Byte }, map, (args) => {
            int v = args[0];
            reg.d(v);
            clock.m(2);
            clock.t(8);
        });
        
        //Load an 8bit immediate value into registry E
        Operation LD_E_n = new Operation(0x1E, "LD E,n", new ArgT[]{ ArgT.Byte }, map, (args) => {
            int v = args[0];
            reg.e(v);
            clock.m(2);
            clock.t(8);
        });
        
        //Load an 8bit immediate value into registry H
        Operation LD_H_n = new Operation(0x26, "LD H,n", new ArgT[]{ ArgT.Byte }, map, (args) => {
            int v = args[0];
            reg.h(v);
            clock.m(2);
            clock.t(8);
        });
        
        //Load an 8bit immediate value into registry H
        Operation LD_L_n = new Operation(0x2E, "LD L,n", new ArgT[]{ ArgT.Byte }, map, (args) => {
            int v = args[0];
            reg.l(v);
            clock.m(2);
            clock.t(8);
        });
        
        //Load into register A from register A
        Operation LD_rr_aa = new Operation(0x7F, "LD A,A", map, (args) => {
            int v = reg.a();
            reg.a(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register A from register B
        Operation LD_rr_ab = new Operation(0x78, "LD A,B", map, (args) => {
            int v = reg.b();
            reg.a(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register A from register C
        Operation LD_rr_ac = new Operation(0x79, "LD A,C", map, (args) => {
            int v = reg.c();
            reg.a(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register A from register D
        Operation LD_rr_ad = new Operation(0x7A, "LD A,D", map, (args) => {
            int v = reg.d();
            reg.a(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register A from register E
        Operation LD_rr_ae = new Operation(0x7B, "LD A,E", map, (args) => {
            int v = reg.e();
            reg.a(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register A from register H
        Operation LD_rr_ah = new Operation(0x7C, "LD A,H", map, (args) => {
            int v = reg.h();
            reg.a(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register A from register L
        Operation LD_rr_al = new Operation(0x7D, "LD A,L", map, (args) => {
            int v = reg.l();
            reg.a(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register A from memory HL
        Operation LD_rr_ahl = new Operation(0x7E, "LD A,(HL)", map, (args) => {
            int v = mem.ReadByte(reg.hl());
            reg.a(v);
            clock.m(2);
            clock.t(8);
        });
        
        //Load into register B from register B
        Operation LD_rr_bb = new Operation(0x40, "LD B,B", map, (args) => {
            int v = reg.b();
            reg.b(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register B from register C
        Operation LD_rr_bc = new Operation(0x41, "LD B,C", map, (args) => {
            int v = reg.c();
            reg.b(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register B from register D
        Operation LD_rr_bd = new Operation(0x42, "LD B,D", map, (args) => {
            int v = reg.d();
            reg.b(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register B from register E
        Operation LD_rr_be = new Operation(0x43, "LD B,E", map, (args) => {
            int v = reg.e();
            reg.b(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register B from register H
        Operation LD_rr_bh = new Operation(0x44, "LD B,H", map, (args) => {
            int v = reg.h();
            reg.b(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register B from register L
        Operation LD_rr_bl = new Operation(0x45, "LD B,L", map, (args) => {
            int v = reg.l();
            reg.b(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register B from memory HL
        Operation LD_rr_bhl = new Operation(0x46, "LD B,(HL)", map, (args) => {
            int v = mem.ReadByte(reg.hl());
            reg.b(v);
            clock.m(2);
            clock.t(8);
        });
        
        //Load into register C from register B
        Operation LD_rr_cb = new Operation(0x48, "LD C,B", map, (args) => {
            int v = reg.b();
            reg.c(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register C from register C
        Operation LD_rr_cc = new Operation(0x49, "LD C,C", map, (args) => {
            int v = reg.c();
            reg.c(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register C from register D
        Operation LD_rr_cd = new Operation(0x4A, "LD C,D", map, (args) => {
            int v = reg.d();
            reg.c(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register C from register E
        Operation LD_rr_ce = new Operation(0x4B, "LD C,E", map, (args) => {
            int v = reg.e();
            reg.c(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register C from register H
        Operation LD_rr_ch = new Operation(0x4C, "LD C,H", map, (args) => {
            int v = reg.h();
            reg.c(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register C from register L
        Operation LD_rr_cl = new Operation(0x4D, "LD C,L", map, (args) => {
            int v = reg.l();
            reg.c(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register C from memory HL
        Operation LD_rr_chl = new Operation(0x4E, "LD C,(HL)", map, (args) => {
            int v = mem.ReadByte(reg.hl());
            reg.c(v);
            clock.m(2);
            clock.t(8);
        });
        
        //Load into register D from register B
        Operation LD_rr_db = new Operation(0x50, "LD D,B", map, (args) => {
            int v = reg.b();
            reg.d(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register D from register C
        Operation LD_rr_dc = new Operation(0x51, "LD D,C", map, (args) => {
            int v = reg.c();
            reg.d(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register D from register D
        Operation LD_rr_dd = new Operation(0x52, "LD D,D", map, (args) => {
            int v = reg.d();
            reg.d(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register D from register E
        Operation LD_rr_de = new Operation(0x53, "LD D,E", map, (args) => {
            int v = reg.e();
            reg.d(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register D from register H
        Operation LD_rr_dh = new Operation(0x54, "LD D,H", map, (args) => {
            int v = reg.h();
            reg.d(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register D from register L
        Operation LD_rr_dl = new Operation(0x55, "LD D,L", map, (args) => {
            int v = reg.l();
            reg.d(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register D from memory HL
        Operation LD_rr_dhl = new Operation(0x56, "LD D,(HL)", map, (args) => {
            int v = mem.ReadByte(reg.hl());
            reg.d(v);
            clock.m(2);
            clock.t(8);
        });
        
        //Load into register E from register B
        Operation LD_rr_eb = new Operation(0x58, "LD E,B", map, (args) => {
            int v = reg.b();
            reg.e(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register E from register C
        Operation LD_rr_ec = new Operation(0x59, "LD E,C", map, (args) => {
            int v = reg.c();
            reg.e(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register E from register D
        Operation LD_rr_ed = new Operation(0x5A, "LD E,D", map, (args) => {
            int v = reg.d();
            reg.e(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register E from register E
        Operation LD_rr_ee = new Operation(0x5B, "LD E,E", map, (args) => {
            int v = reg.e();
            reg.e(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register E from register H
        Operation LD_rr_eh = new Operation(0x5C, "LD E,H", map, (args) => {
            int v = reg.h();
            reg.e(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register E from register L
        Operation LD_rr_el = new Operation(0x5D, "LD E,L", map, (args) => {
            int v = reg.l();
            reg.e(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register E from memory HL
        Operation LD_rr_ehl = new Operation(0x5E, "LD E,(HL)", map, (args) => {
            int v = mem.ReadByte(reg.hl());
            reg.e(v);
            clock.m(2);
            clock.t(8);
        });
        
        //Load into register H from register B
        Operation LD_rr_hb = new Operation(0x60, "LD H,B", map, (args) => {
            int v = reg.b();
            reg.h(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register H from register C
        Operation LD_rr_hc = new Operation(0x61, "LD H,C", map, (args) => {
            int v = reg.c();
            reg.h(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register H from register D
        Operation LD_rr_hd = new Operation(0x62, "LD H,D", map, (args) => {
            int v = reg.d();
            reg.h(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register H from register E
        Operation LD_rr_he = new Operation(0x63, "LD H,E", map, (args) => {
            int v = reg.e();
            reg.h(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register H from register H
        Operation LD_rr_hh = new Operation(0x64, "LD H,H", map, (args) => {
            int v = reg.h();
            reg.h(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register H from register L
        Operation LD_rr_hl = new Operation(0x65, "LD H,L", map, (args) => {
            int v = reg.l();
            reg.h(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register H from register HL
        Operation LD_rr_hhl = new Operation(0x66, "LD H,(HL)", map, (args) => {
            int v = mem.ReadByte(reg.hl());
            reg.h(v);
            clock.m(2);
            clock.t(8);
        });
        
        //Load into register L from register B
        Operation LD_rr_lb = new Operation(0x68, "LD L,B", map, (args) => {
            int v = reg.b();
            reg.l(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register L from register C
        Operation LD_rr_lc = new Operation(0x69, "LD L,C", map, (args) => {
            int v = reg.c();
            reg.l(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register L from register D
        Operation LD_rr_ld = new Operation(0x6A, "LD L,D", map, (args) => {
            int v = reg.d();
            reg.l(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register L from register E
        Operation LD_rr_le = new Operation(0x6B, "LD L,E", map, (args) => {
            int v = reg.e();
            reg.l(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register L from register H
        Operation LD_rr_lh = new Operation(0x6C, "LD L,H", map, (args) => {
            int v = reg.h();
            reg.l(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register L from register L
        Operation LD_rr_ll = new Operation(0x6D, "LD L,L", map, (args) => {
            int v = reg.l();
            reg.l(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register L from memory HL
        Operation LD_rr_lhl = new Operation(0x6E, "LD L,(HL)", map, (args) => {
            int v = mem.ReadByte(reg.hl());
            reg.l(v);
            clock.m(2);
            clock.t(8);
        });
        
        //Load into memory HL from register B
        Operation LD_rr_hlb = new Operation(0x70, "LD (HL),B", map, (args) => {
            mem.WriteByte(reg.hl(), reg.b());
            clock.m(2);
            clock.t(8);
        });
        
        //Load into memory HL from register C
        Operation LD_rr_hlc = new Operation(0x71, "LD (HL),C", map, (args) => {
            mem.WriteByte(reg.hl(), reg.c());
            clock.m(2);
            clock.t(8);
        });
        
        //Load into memory HL from register D
        Operation LD_rr_hld = new Operation(0x72, "LD (HL),D", map, (args) => {
            mem.WriteByte(reg.hl(), reg.d());
            clock.m(2);
            clock.t(8);
        });
        
        //Load into memory HL from register E
        Operation LD_rr_hle = new Operation(0x73, "LD (HL),E", map, (args) => {
            mem.WriteByte(reg.hl(), reg.e());
            clock.m(2);
            clock.t(8);
        });
        
        //Load into memory HL from register H
        Operation LD_rr_hlh = new Operation(0x74, "LD (HL),H", map, (args) => {
            mem.WriteByte(reg.hl(), reg.h());
            clock.m(2);
            clock.t(8);
        });
        
        //Load into memory HL from register L
        Operation LD_rr_hll = new Operation(0x75, "LD (HL),L", map, (args) => {
            mem.WriteByte(reg.hl(), reg.l());
            clock.m(2);
            clock.t(8);
        });
        
        //Load into memory HL from immediate value n
        Operation LD_rr_hln = new Operation(0x36, "LD (HL),n", new ArgT[]{ ArgT.Byte }, map, (args) => {
            mem.WriteByte(reg.hl(), args[0]);
            clock.m(3);
            clock.t(12);
        });
        
        //Load into register A from memory BC
        Operation LD_abc = new Operation(0x0A, "LD A,(BC)", map, (args) => {
            int v = mem.ReadByte(reg.bc());
            reg.a(v);
            clock.m(2);
            clock.t(8);
        });
        
        //Load into register A from memory DE
        Operation LD_ade = new Operation(0x1A, "LD A,(DE)", map, (args) => {
            int v = mem.ReadByte(reg.de());
            reg.a(v);
            clock.m(2);
            clock.t(8);
        });

        //Load into register A from memory nn
        Operation LD_ann = new Operation(0xFA, "LD A,(nn)", new ArgT[]{ ArgT.Short }, map, (args) => {
            int v = mem.ReadByte(args[0]);
            reg.a(v);
            clock.m(4);
            clock.t(16);
        });
        
        //Load into register A from immediate n
        Operation LD_an = new Operation(0x3E, "LD A,n", new ArgT[]{ ArgT.Byte }, map, (args) => {
            int v = args[0];
            reg.a(v);
            clock.m(2);
            clock.t(8);
        });
        
        //Load into register B the value in register A
        Operation LD_ba = new Operation(0x47, "LD B,A", map, (args) => {
            int v = reg.a();
            reg.b(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register C the value in register A
        Operation LD_ca = new Operation(0x4F, "LD C,A", map, (args) => {
            int v = reg.a();
            reg.c(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register D the value in register A
        Operation LD_da = new Operation(0x57, "LD D,A", map, (args) => {
            int v = reg.a();
            reg.d(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register E the value in register A
        Operation LD_ea = new Operation(0x5F, "LD E,A", map, (args) => {
            int v = reg.a();
            reg.e(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register H the value in register A
        Operation LD_ha = new Operation(0x67, "LD H,A", map, (args) => {
            int v = reg.a();
            reg.h(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into register L the value in register A
        Operation LD_la = new Operation(0x6F, "LD L,A", map, (args) => {
            int v = reg.a();
            reg.l(v);
            clock.m(1);
            clock.t(4);
        });
        
        //Load into memory BC the value in register A
        Operation LD_bca = new Operation(0x02, "LD (BC),A", map, (args) => {
            mem.WriteByte(reg.bc(), reg.a());
            clock.m(2);
            clock.t(8);
        });
        
        //Load into memory DE the value in register A
        Operation LD_dea = new Operation(0x12, "LD (DE),A", map, (args) => {
            mem.WriteByte(reg.de(), reg.a());
            clock.m(2);
            clock.t(8);
        });
        
        //Load into memory HL the value in register A
        Operation LD_hla = new Operation(0x77, "LD (HL),A", map, (args) => {
            mem.WriteByte(reg.hl(), reg.a());
            clock.m(2);
            clock.t(8);
        });
        
        //Load into memory nn the value in register A
        Operation LD_nna = new Operation(0xEA, "LD (nn),A", new ArgT[]{ ArgT.Short }, map, (args) => {
            mem.WriteByte(args[0], reg.a());
            clock.m(4);
            clock.t(16);
        });
        
        //PAGE 70
        
        //Load into register A the value in 0xFF00 + register C
        Operation LD_A_c = new Operation(0xF2, "LD A,(FF00 + C)", map, (args) => {
            int v = mem.ReadByte(0xFF00 + reg.c());
            reg.a(v);
            clock.m(2);
            clock.t(8);
        });
        
        //Load into memory 0xFF00 + register C the value in register A
        Operation LD_c_A = new Operation(0xE2, "LD (FF00 + C),A", map, (args) => {
            mem.WriteByte(0xFF00 + reg.c(), reg.a());
            clock.m(2);
            clock.t(8);
        });
        
        //Put value at address HL into A, Decrement HL
        //Same as LD A,(HL) => DEC HL
        Operation LDD_A_HL = new Operation(0x3A, "LDD A,(HL)", map, (args) => {
            int v = mem.ReadByte(reg.hl());
            reg.a(v);
            reg.hl(reg.hl() - 1); //TODO
            clock.m(2);
            clock.t(8);
        });
        
        //Put register A into memory address at HL
        //Same as LD (HL),A => DEC HL
        Operation LDD_HL_A = new Operation(0x32, "LDD (HL),A", map, (args) =>{
            mem.WriteByte(reg.hl(), reg.a());
            reg.hl(reg.hl() - 1); //TODO
            clock.m(2);
            clock.t(8);
        });
        
        //Put the value at address HL into A and increment HL
        Operation LDI_A_HL = new Operation(0x2A, "LDI A,(HL)", map, (args) => {
            int v = mem.ReadByte(reg.hl());
            reg.a(v);
            reg.hl(reg.hl() + 1);
            clock.m(2);
            clock.t(8);
        });
        
        //Put into memory at HL the value in register A, increment HL
        Operation LDI_HL_A = new Operation(0x22, "LDI (HL),A", map, (args) => {
            mem.WriteByte(reg.hl(), reg.a());
            reg.hl(reg.hl() + 1);
            clock.m(2);
            clock.t(8);
        });
        
        //Put A into memory address 0xFF00 + n
        Operation LDH_n_A = new Operation(0xE0, "LDH (FF00 + n),A", new ArgT[]{ ArgT.Byte }, map, (args) => {
            mem.WriteByte(0xFF00 + args[0], reg.a());
            clock.m(3);
            clock.t(12);
        });
        
        //Put memory address 0xFF00 + n into register A
        Operation LDH_A_n = new Operation(0xF0, "LDH A,(FF00 + n)", new ArgT[]{ ArgT.Byte }, map, (args) => {
            int v = mem.ReadByte(0xFF00 + args[0]);
            reg.a(v);
            clock.m(3);
            clock.t(12);
        });
        
        //PAGE 76
        
        ///
        // 16 Bit Loads
        ///
        
        //Put 16bit immediate value into register BC
        Operation LD_BC_nn = new Operation(0x01, "LD BC,nn", new ArgT[]{ ArgT.Short }, map, (args) => {
            int v = args[0];
            reg.bc(v);
            clock.m(3);
            clock.t(12);
        });
        
        //Put 16bit immediate value into register DE
        Operation LD_DE_nn = new Operation(0x11, "LD DE,nn", new ArgT[]{ ArgT.Short }, map, (args) => {
            int v = args[0];
            reg.de(v);
            clock.m(3);
            clock.t(12);
        });
        
        //Put 16bit immediate value into register HL
        Operation LD_HL_nn = new Operation(0x21, "LD HL,nn", new ArgT[]{ ArgT.Short }, map, (args) => {
            int v = args[0];
            reg.hl(v);
            clock.m(3);
            clock.t(12);
        });
        
        //Put 16bit immediate value into register HL
        Operation LD_SP_nn = new Operation(0x31, "LD SP,nn", new ArgT[]{ ArgT.Short }, map, (args) => {
            int v = args[0];
            reg.sp(v);
            clock.m(3);
            clock.t(12);
        });
        
        //Put HL into stack pointer register SP
        Operation LD_SP_HL = new Operation(0xF9, "LD SP,HL", map, (args) => {
            int v = reg.hl();
            reg.sp(v);
            clock.m(2);
            clock.t(8);
        });
        
        //Put SP + n into HL, (n is a 8bit signed value)
        //Flags Z-Reset, N-Reset, H-Set or Reset, C-Set or Reset
        Operation LDHL_SP_n = new Operation(0xF8, "LDHL SP,n", new ArgT[]{ ArgT.Byte }, map, (args) => {
            var un = args[0];
            int n =  unsignedByteToSigned(un);
            int sp = reg.sp();
            reg.hl(sp + n);
            clock.m(3);
            clock.t(12);
            
            reg.zero(false);
            reg.subtract(false);
            reg.halfcarry( ( sp & 0x0F ) + ( n & 0x0F ) > 0x0F);
            reg.carry(( sp & 0xFF ) + (un & 0xFF) > 0xFF);
        });
        
        //Put stack pointer into memory at nn //TODO
        Operation LD_nn_SP = new Operation(0x08, "LD (nn),SP", new ArgT[]{ ArgT.Short }, map, (args) => {
            int v = reg.sp();
            mem.WriteShort(args[0], v);
            clock.m(5);
            clock.t(20);
        });
        
        //Push register pair AF onto the stack, Decrement Stack Pointer Twice
        Operation PUSH_AF = new Operation(0xF5, "PUSH AF", map, (args) => {
            push(reg.af());
            clock.m(4);
            clock.t(16);
        });
        
        //Push register pair BC onto the stack, Decrement Stack Pointer Twice
        Operation PUSH_BC = new Operation(0xC5, "PUSH BC", map, (args) => {
            push(reg.bc());
            clock.m(4);
            clock.t(16);
        });
        
        //Push register pair DE onto the stack, Decrement Stack Pointer Twice
        Operation PUSH_DE = new Operation(0xD5, "PUSH DE", map, (args) => {
            push(reg.de());
            clock.m(4);
            clock.t(16);
        });
        
        //Push register pair HL onto the stack, Decrement Stack Pointer Twice
        Operation PUSH_HL = new Operation(0xE5, "PUSH HL", map, (args) => {
            push(reg.hl());
            clock.m(4); //TODO 3 and 12 or 4 and 16? different sources say different things
            clock.t(16);
        });
        
        //Pop value off stack into register AF
        Operation POP_AF = new Operation(0xF1, "POP AF", map, (args) => {
            reg.af(pop());
                    
            clock.m(3);
            clock.t(12);
        });
        
        //Pop value off stack into register BC
        Operation POP_BC = new Operation(0xC1, "POP BC", map, (args) => {
            reg.bc(pop());
            clock.m(3);
            clock.t(12);
        });
        
        //Pop value off stack into register DE
        Operation POP_DE = new Operation(0xD1, "POP DE", map, (args) => {
            reg.de(pop());
            clock.m(3);
            clock.t(12);
        });
        
        //Pop value off stack into register HL
        Operation POP_HL = new Operation(0xE1, "POP HL", map, (args) => {
            reg.hl(pop());
            clock.m(3);
            clock.t(12);
        });
        
        //PAGE 80
        
        ///
        // 8 Bit ALU
        ///
        
        //Add register A and register A into register A
        Operation ADD_A_A = new Operation(0x87, "ADD A,A", map, (args) => {
            int a = reg.a();
            int b = reg.a();
            int v = a + b;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(isHalfCarry(a, b));
            reg.carry(isCarry(v));
            
            clock.m(1);
            clock.t(4);
        });
        
        //Add register A and register B into register A
        Operation ADD_A_B = new Operation(0x80, "ADD A,B", map, (args) => {
            int a = reg.a();
            int b = reg.b();
            int v = a + b;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(isHalfCarry(a, b));
            reg.carry(isCarry(v));
            
            clock.m(1);
            clock.t(4);
        });
        
        //Add register A and register C into register A
        Operation ADD_A_C = new Operation(0x81, "ADD A,C", map, (args) => {
            int a = reg.a();
            int b = reg.c();
            int v = a + b;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(isHalfCarry(a, b));
            reg.carry(isCarry(v));
            
            clock.m(1);
            clock.t(4);
        });
        
        //Add register A and register D into register A
        Operation ADD_A_D = new Operation(0x82, "ADD A,D", map, (args) => {
            int a = reg.a();
            int b = reg.d();
            int v = a + b;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(isHalfCarry(a, b));
            reg.carry(isCarry(v));
            
            clock.m(1);
            clock.t(4);
        });
        
        //Add register A and register E into register A
        Operation ADD_A_E = new Operation(0x83, "ADD A,E", map, (args) => {
            int a = reg.a();
            int b = reg.e();
            int v = a + b;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(isHalfCarry(a, b));
            reg.carry(isCarry(v));
            
            clock.m(1);
            clock.t(4);
        });
        
        //Add register A and register H into register A
        Operation ADD_A_H = new Operation(0x84, "ADD A,H", map, (args) => {
            int a = reg.a();
            int b = reg.h();
            int v = a + b;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(isHalfCarry(a, b));
            reg.carry(isCarry(v));
            
            clock.m(1);
            clock.t(4);
        });
        
        //Add register A and register L into register A
        Operation ADD_A_L = new Operation(0x85, "ADD A,L", map, (args) => {
            int a = reg.a();
            int b = reg.l();
            int v = a + b;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(isHalfCarry(a, b));
            reg.carry(isCarry(v));
            
            clock.m(1);
            clock.t(4);
        });
        
        //Add register A and memory at HL into register A
        Operation ADD_A_HL = new Operation(0x86, "ADD A,(HL)", map, (args) => {
            int a = reg.a();
            int b = mem.ReadByte(reg.hl());
            int v = a + b;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(isHalfCarry(a, b));
            reg.carry(isCarry(v));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Add register A and immediate value 'n' into register A
        Operation ADD_A_n = new Operation(0xC6, "ADD A,n", new ArgT[]{ ArgT.Byte }, map, (args) => {
            int a = reg.a();
            int b = args[0];
            int v = a + b;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(isHalfCarry(a, b));
            reg.carry(isCarry(v));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Add A + Carry Flag into registry A
        //int v = addCarry(reg.a(), reg.a());
        Operation ADC_A_A = new Operation(0x8F, "ADC A,A", map, (args) => {
            int v = addCarry8(reg.a(), reg.a());
            reg.a(v);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Add B + Carry Flag into registry A
        Operation ADC_A_B = new Operation(0x88, "ADC A,B", map, (args) => {
            int v = addCarry8(reg.a(), reg.b());
            reg.a(v);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Add C + Carry Flag into registry A
        Operation ADC_A_C = new Operation(0x89, "ADC A,C", map, (args) => {
            int v = addCarry8(reg.a(), reg.c());
            reg.a(v);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Add D + Carry Flag into registry A
        Operation ADC_A_D = new Operation(0x8A, "ADC A,D", map, (args) => {
            int v = addCarry8(reg.a(), reg.d());
            reg.a(v);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Add E + Carry Flag into registry A
        Operation ADC_A_E = new Operation(0x8B, "ADC A,E", map, (args) => {
            int v = addCarry8(reg.a(), reg.e());
            reg.a(v);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Add H + Carry Flag into registry A
        Operation ADC_A_H = new Operation(0x8C, "ADC A,H", map, (args) => {
            int v = addCarry8(reg.a(), reg.h());
            reg.a(v);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Add L + Carry Flag into registry A
        Operation ADC_A_L = new Operation(0x8D, "ADC A,L", map, (args) => {
            int v = addCarry8(reg.a(), reg.l());
            reg.a(v);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Add memory at HL + Carry Flag into registry A
        Operation ADC_A_HL = new Operation(0x8E, "ADC A,(HL)", map, (args) => {
            int v = addCarry8(reg.a(), mem.ReadByte(reg.hl()));
            reg.a(v);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Add immediate value n + Carry Flag into registry A
        Operation ADC_A_n = new Operation(0xCE, "ADC A,n", new ArgT[]{ ArgT.Byte }, map, (args) => {
            int v = addCarry8(reg.a(), args[0]);
            reg.a(v);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Subtract register A from register A
        Operation SUB_A_A = new Operation(0x97, "SUB A,A", map, (args) => {
            int n = reg.a(); //Change me
            int a = reg.a();
            int v = a - n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(true);
            reg.halfcarry(isHalfCarry(a, -n)); //WATCH THIS
            reg.carry(isCarry(v));
            
            clock.m(1);
            clock.t(4);
        });
        
        //Subtract register B from register A
        Operation SUB_A_B = new Operation(0x90, "SUB A,B", map, (args) => {
            int n = reg.b(); //Change me
            int a = reg.a();
            int v = a - n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(true);
            reg.halfcarry(isHalfCarry(a, -n)); //WATCH THIS
            reg.carry(isCarry(v));
            
            clock.m(1);
            clock.t(4);
        });
        
        //Subtract register C from register A
        Operation SUB_A_C = new Operation(0x91, "SUB A,C", map, (args) => {
            int n = reg.c(); //Change me
            int a = reg.a();
            int v = a - n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(true);
            reg.halfcarry(isHalfCarry(a, -n)); //WATCH THIS
            reg.carry(isCarry(v));
            
            clock.m(1);
            clock.t(4);
        });
        
        //Subtract register D from register A
        Operation SUB_A_D = new Operation(0x92, "SUB A,D", map, (args) => {
            int n = reg.d(); //Change me
            int a = reg.a();
            int v = a - n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(true);
            reg.halfcarry(isHalfCarry(a, -n)); //WATCH THIS
            reg.carry(isCarry(v));
            
            clock.m(1);
            clock.t(4);
        });
        
        //Subtract register E from register A
        Operation SUB_A_E = new Operation(0x93, "SUB A,E", map, (args) => {
            int n = reg.e(); //Change me
            int a = reg.a();
            int v = a - n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(true);
            reg.halfcarry(isHalfCarry(a, -n)); //WATCH THIS
            reg.carry(isCarry(v));
            
            clock.m(1);
            clock.t(4);
        });
        
        //Subtract register CHfrom register A
        Operation SUB_A_H = new Operation(0x94, "SUB A,H", map, (args) => {
            int n = reg.h(); //Change me
            int a = reg.a();
            int v = a - n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(true);
            reg.halfcarry(isHalfCarry(a, -n)); //WATCH THIS
            reg.carry(isCarry(v));
            
            clock.m(1);
            clock.t(4);
        });
        
        //Subtract register L from register A
        Operation SUB_A_L = new Operation(0x95, "SUB A,L", map, (args) => {
            int n = reg.l(); //Change me
            int a = reg.a();
            int v = a - n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(true);
            reg.halfcarry(isHalfCarry(a, -n)); //WATCH THIS
            reg.carry(isCarry(v));
            
            clock.m(1);
            clock.t(4);
        });
        
        //Subtract memory at HL from register A
        Operation SUB_A_HL = new Operation(0x96, "SUB A,(HL)", map, (args) => {
            int n = mem.ReadByte(reg.hl()); //Change me
            int a = reg.a();
            int v = a - n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(true);
            reg.halfcarry(isHalfCarry(a, -n)); //WATCH THIS
            reg.carry(isCarry(v));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Subtract immediate value n from register A
        Operation SUB_A_n = new Operation(0xD6, "SUB A,n", new ArgT[]{ ArgT.Byte }, map, (args) => {
            int n = args[0]; //Change me
            int a = reg.a();
            int v = a - n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(true);
            reg.halfcarry(isHalfCarry(a, -n)); //WATCH THIS
            reg.carry(isCarry(v));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Subtract A + Carry flag from register A
        //int v = subCarry(reg.a(), reg.a());
        Operation SBC_A_A = new Operation(0x9F, "SBC A,A", map, (args) => {
            int v = subCarry8(reg.a(), reg.a());
            reg.a(v);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Subtract B + Carry flag from register A
        Operation SBC_A_B = new Operation(0x98, "SBC A,B", map, (args) => {
            int v = subCarry8(reg.a(), reg.b());
            reg.a(v);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Subtract C + Carry flag from register A
        Operation SBC_A_C = new Operation(0x99, "SBC A,C", map, (args) => {
            int v = subCarry8(reg.a(), reg.c());
            reg.a(v);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Subtract D + Carry flag from register A
        Operation SBC_A_D = new Operation(0x9A, "SBC A,D", map, (args) => {
            int v = subCarry8(reg.a(), reg.d());
            reg.a(v);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Subtract E + Carry flag from register A
        Operation SBC_A_E = new Operation(0x9B, "SBC A,E", map, (args) => {
            int v = subCarry8(reg.a(), reg.e());
            reg.a(v);
            
            clock.m(1);
            clock.t(4);
        });
        
        
        //Subtract H + Carry flag from register A
        Operation SBC_A_H = new Operation(0x9C, "SBC A,H", map, (args) => {
            int v = subCarry8(reg.a(), reg.h());
            reg.a(v);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Subtract L + Carry flag from register A
        Operation SBC_A_L = new Operation(0x9D, "SBC A,L", map, (args) => {
            int v = subCarry8(reg.a(), reg.l());
            reg.a(v);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Subtract memory at HL + Carry flag from register A
        Operation SBC_A_HL = new Operation(0x9E, "SBC A,(HL)", map, (args) => {
            int v = subCarry8(reg.a(), mem.ReadByte(reg.hl()));
            reg.a(v);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Subtract immediate value n + Carry flag from register A
        Operation SBC_A_n = new Operation(0xDE, "SBC A,n", new ArgT[]{ ArgT.Byte }, map, (args) => {
            int v = subCarry8(reg.a(), args[0]);
            reg.a(v);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Logically and register A with register A, result in A
        Operation AND_A_A = new Operation(0xA7, "AND A,A", map, (args) => {
            int n = reg.a();
            int a = reg.a();
            int v = a & n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(true);
            reg.carry(false);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Logically and register A with register B, result in A
        Operation AND_A_B = new Operation(0xA0, "AND A,B", map, (args) => {
            int n = reg.b();
            int a = reg.a();
            int v = a & n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(true);
            reg.carry(false);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Logically and register A with register C, result in A
        Operation AND_A_C = new Operation(0xA1, "AND A,C", map, (args) => {
            int n = reg.c();
            int a = reg.a();
            int v = a & n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(true);
            reg.carry(false);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Logically and register A with register D, result in A
        Operation AND_A_D = new Operation(0xA2, "AND A,D", map, (args) => {
            int n = reg.d();
            int a = reg.a();
            int v = a & n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(true);
            reg.carry(false);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Logically and register A with register E, result in A
        Operation AND_A_E = new Operation(0xA3, "AND A,E", map, (args) => {
            int n = reg.e();
            int a = reg.a();
            int v = a & n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(true);
            reg.carry(false);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Logically and register A with register H, result in A
        Operation AND_A_H = new Operation(0xA4, "AND A,H", map, (args) => {
            int n = reg.h();
            int a = reg.a();
            int v = a & n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(true);
            reg.carry(false);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Logically and register A with register L, result in A
        Operation AND_A_L = new Operation(0xA5, "AND A,L", map, (args) => {
            int n = reg.l();
            int a = reg.a();
            int v = a & n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(true);
            reg.carry(false);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Logically and register A with memory at HL, result in A
        Operation AND_A_HL = new Operation(0xA6, "AND A,(HL)", map, (args) => {
            int n = mem.ReadByte(reg.hl());
            int a = reg.a();
            int v = a & n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(true);
            reg.carry(false);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Logically and register A with immediate value n, result in A
        Operation AND_A_n = new Operation(0xE6, "AND A,n", new ArgT[]{ ArgT.Byte }, map, (args) => {
            int n = args[0];
            int a = reg.a();
            int v = a & n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(true);
            reg.carry(false);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Logically or register A with register A, result in A
        Operation OR_A_A = new Operation(0xB7, "OR A,A", map, (args) => {
            int n = reg.a();
            int a = reg.a();
            int v = a | n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(false);
            reg.carry(false);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Logically or register A with register B, result in A
        Operation OR_A_B = new Operation(0xB0, "OR A,B", map, (args) => {
            int n = reg.b();
            int a = reg.a();
            int v = a | n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(false);
            reg.carry(false);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Logically or register A with register C, result in A
        Operation OR_A_C = new Operation(0xB1, "OR A,C", map, (args) => {
            int n = reg.c();
            int a = reg.a();
            int v = a | n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(false);
            reg.carry(false);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Logically or register A with register D, result in A
        Operation OR_A_D = new Operation(0xB2, "OR A,D", map, (args) => {
            int n = reg.d();
            int a = reg.a();
            int v = a | n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(false);
            reg.carry(false);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Logically or register A with register E, result in A
        Operation OR_A_E = new Operation(0xB3, "OR A,E", map, (args) => {
            int n = reg.e();
            int a = reg.a();
            int v = a | n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(false);
            reg.carry(false);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Logically or register A with register H, result in A
        Operation OR_A_H = new Operation(0xB4, "OR A,H", map, (args) => {
            int n = reg.h();
            int a = reg.a();
            int v = a | n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(false);
            reg.carry(false);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Logically or register A with register L, result in A
        Operation OR_A_L = new Operation(0xB5, "OR A,L", map, (args) => {
            int n = reg.l();
            int a = reg.a();
            int v = a | n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(false);
            reg.carry(false);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Logically or register A with memory at HL, result in A
        Operation OR_A_HL = new Operation(0xB6, "OR A,(HL)", map, (args) => {
            int n = mem.ReadByte(reg.hl());
            int a = reg.a();
            int v = a | n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(false);
            reg.carry(false);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Logically or register A with immediate value n, result in A
        Operation OR_A_n = new Operation(0xF6, "OR A,n", new ArgT[]{ ArgT.Byte }, map, (args) => {
            int n = args[0];
            int a = reg.a();
            int v = a | n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(false);
            reg.carry(false);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Logically exclusive or register A with register A, result in A
        Operation XOR_A_A = new Operation(0xAF, "XOR A,A", map, (args) => {
            int n = reg.a();
            int a = reg.a();
            int v = a ^ n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(false);
            reg.carry(false);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Logically exclusive or register A with register B, result in A
        Operation XOR_A_B = new Operation(0xA8, "XOR A,B", map, (args) => {
            int n = reg.b();
            int a = reg.a();
            int v = a ^ n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(false);
            reg.carry(false);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Logically exclusive or register A with register C, result in A
        Operation XOR_A_C = new Operation(0xA9, "XOR A,C", map, (args) => {
            int n = reg.c();
            int a = reg.a();
            int v = a ^ n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(false);
            reg.carry(false);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Logically exclusive or register A with register D, result in A
        Operation XOR_A_D = new Operation(0xAA, "XOR A,D", map, (args) => {
            int n = reg.d();
            int a = reg.a();
            int v = a ^ n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(false);
            reg.carry(false);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Logically exclusive or register A with register E, result in A
        Operation XOR_A_E = new Operation(0xAB, "XOR A,E", map, (args) => {
            int n = reg.e();
            int a = reg.a();
            int v = a ^ n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(false);
            reg.carry(false);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Logically exclusive or register A with register H, result in A
        Operation XOR_A_H = new Operation(0xAC, "XOR A,H", map, (args) => {
            int n = reg.h();
            int a = reg.a();
            int v = a ^ n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(false);
            reg.carry(false);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Logically exclusive or register A with register L, result in A
        Operation XOR_A_L = new Operation(0xAD, "XOR A,L", map, (args) => {
            int n = reg.l();
            int a = reg.a();
            int v = a ^ n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(false);
            reg.carry(false);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Logically exclusive or register A with memory at HL, result in A
        Operation XOR_A_HL = new Operation(0xAE, "XOR A,(HL)", map, (args) => {
            int n = mem.ReadByte(reg.hl());
            int a = reg.a();
            int v = a ^ n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(false);
            reg.carry(false);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Logically exclusive or register A with immedate value n, result in A
        Operation XOR_A_n = new Operation(0xEE, "XOR A,n", new ArgT[]{ ArgT.Byte }, map, (args) => {
            int n = args[0];
            int a = reg.a();
            int v = a ^ n;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(false);
            reg.carry(false);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Compare register A with register A
        Operation CP_A_A = new Operation(0xBF, "CP A,A", map, (args) => {
            int n = reg.a();
            int a = reg.a();
                
            compare(a,n);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Compare register A with register B
        Operation CP_A_B = new Operation(0xB8, "CP A,B", map, (args) => {
            int n = reg.b();
            int a = reg.a();
                
            compare(a,n);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Compare register A with register C
        Operation CP_A_C = new Operation(0xB9, "CP A,C", map, (args) => {
            int n = reg.c();
            int a = reg.a();
                
            compare(a,n);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Compare register A with register D
        Operation CP_A_D = new Operation(0xBA, "CP A,D", map, (args) => {
            int n = reg.d();
            int a = reg.a();
                
            compare(a,n);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Compare register A with register E
        Operation CP_A_E = new Operation(0xBB, "CP A,E", map, (args) => {
            int n = reg.e();
            int a = reg.a();
                
            compare(a,n);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Compare register A with register H
        Operation CP_A_H = new Operation(0xBC, "CP A,H", map, (args) => {
            int n = reg.h();
            int a = reg.a();
                
            compare(a,n);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Compare register A with register L
        Operation CP_A_L = new Operation(0xBD, "CP A,L", map, (args) => {
            int n = reg.l();
            int a = reg.a();
                
            compare(a,n);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Compare register A with memory at HL
        Operation CP_A_HL = new Operation(0xBE, "CP A,(HL)", map, (args) => {
            int n = mem.ReadByte(reg.hl());
            int a = reg.a();
                
            compare(a,n);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Compare register A with immediate value n
        Operation CP_A_n = new Operation(0xFE, "CP A,n", new ArgT[]{ ArgT.Byte }, map, (args) => {
            int n = args[0];
            int a = reg.a();   
            
            compare(a,n);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Increment register A
        Operation INC_A = new Operation(0x3C, "INC A", map, (args) => {
            int n = reg.a();
            int v = n + 1;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(isHalfCarry(n, 1));
            
            clock.m(1);
            clock.t(4);
        });
        
        //Increment register B
        Operation INC_B = new Operation(0x04, "INC B", map, (args) => {
            int n = reg.b();
            int v = n + 1;
            reg.b(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(isHalfCarry(n, 1));
            
            clock.m(1);
            clock.t(4);
        });
        
        //Increment register C
        Operation INC_C = new Operation(0x0C, "INC C", map, (args) => {
            int n = reg.c();
            int v = n + 1;
            reg.c(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(isHalfCarry(n, 1));
            
            clock.m(1);
            clock.t(4);
        });
        
        //Increment register D
        Operation INC_D = new Operation(0x14, "INC D", map, (args) => {
            int n = reg.d();
            int v = n + 1;
            reg.d(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(isHalfCarry(n, 1));
            
            clock.m(1);
            clock.t(4);
        });
        
        //Increment register E
        Operation INC_E = new Operation(0x1C, "INC E", map, (args) => {
            int n = reg.e();
            int v = n + 1;
            reg.e(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(isHalfCarry(n, 1));
            
            clock.m(1);
            clock.t(4);
        });
        
        //Increment register H
        Operation INC_H = new Operation(0x24, "INC H", map, (args) => {
            int n = reg.h();
            int v = n + 1;
            reg.h(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(isHalfCarry(n, 1));
            
            clock.m(1);
            clock.t(4);
        });
        
        //Increment register L
        Operation INC_L = new Operation(0x2C, "INC L", map, (args) => {
            int n = reg.l();
            int v = n + 1;
            reg.l(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(isHalfCarry(n, 1));
            
            clock.m(1);
            clock.t(4);
        });
        
        //Increment memory at HL
        Operation INC_HL = new Operation(0x34, "INC (HL)", map, (args) => {
            int n = mem.ReadByte(reg.hl());
            int v = n + 1;
            mem.WriteByte(reg.hl(), v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(isHalfCarry(n, 1));
            
            clock.m(3);
            clock.t(12);
        });
        
        //Decrement register A
        Operation DEC_A = new Operation(0x3D, "DEC A", map, (args) => {
            int n = reg.a();
            int v = n - 1;
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(true);
            reg.halfcarry(isHalfCarry(n, -1));
            
            clock.m(1);
            clock.t(4);
        });
        
        //Decrement register B
        Operation DEC_B = new Operation(0x05, "DEC B", map, (args) => {
            int n = reg.b();
            int v = n - 1;
            reg.b(v);
            
            reg.zero(isZero(v));
            reg.subtract(true);
            reg.halfcarry(isHalfCarry(n, -1));
            
            clock.m(1);
            clock.t(4);
        });
        
        //Decrement register C
        Operation DEC_C = new Operation(0x0D, "DEC C", map, (args) => {
            int n = reg.c();
            int v = n - 1;
            reg.c(v);
            
            reg.zero(isZero(v));
            reg.subtract(true);
            reg.halfcarry(isHalfCarry(n, -1));
            
            clock.m(1);
            clock.t(4);
        });
        
        //Decrement register D
        Operation DEC_D = new Operation(0x15, "DEC D", map, (args) => {
            int n = reg.d();
            int v = n - 1;
            reg.d(v);
            
            reg.zero(isZero(v));
            reg.subtract(true);
            reg.halfcarry(isHalfCarry(n, -1));
            
            clock.m(1);
            clock.t(4);
        });
        
        //Decrement register E
        Operation DEC_E = new Operation(0x1D, "DEC E", map, (args) => {
            int n = reg.e();
            int v = n - 1;
            reg.e(v);
            
            reg.zero(isZero(v));
            reg.subtract(true);
            reg.halfcarry(isHalfCarry(n, -1));
            
            clock.m(1);
            clock.t(4);
        });
        
        //Decrement register H
        Operation DEC_H = new Operation(0x25, "DEC H", map, (args) => {
            int n = reg.h();
            int v = n - 1;
            reg.h(v);
            
            reg.zero(isZero(v));
            reg.subtract(true);
            reg.halfcarry(isHalfCarry(n, -1));
            
            clock.m(1);
            clock.t(4);
        });
        
        //Decrement register L
        Operation DEC_L = new Operation(0x2D, "DEC L", map, (args) => {
            int n = reg.l();
            int v = n - 1;
            reg.l(v);
            
            reg.zero(isZero(v));
            reg.subtract(true);
            reg.halfcarry(isHalfCarry(n, -1));
            
            clock.m(1);
            clock.t(4);
        });
        
        //Decrement memory at HL
        Operation DEC_HL = new Operation(0x35, "DEC (HL)", map, (args) => {
            int n = mem.ReadByte(reg.hl());
            int v = n - 1;
            mem.WriteByte(reg.hl(), v);
            
            reg.zero(isZero(v));
            reg.subtract(true);
            reg.halfcarry(isHalfCarry(n, -1));
            
            clock.m(3);
            clock.t(12);
        });
        
        //PAGE 90
        
        ///
        // 16 Bit ALU
        ///
        
        //Add to register HL the register BC
        Operation ADD_HL_BC = new Operation(0x09, "ADD HL,BC", map, (args) => {
            int n = reg.bc();
            int hl = reg.hl();
            int v = hl + n;
            reg.hl(v);
            
            reg.subtract(false);
            reg.halfcarry(isHalfCarry16(hl, n));
            reg.carry(isCarry16(v));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Add to register HL the register DE
        Operation ADD_HL_DE = new Operation(0x19, "ADD HL,DE", map, (args) => {
            int n = reg.de();
            int hl = reg.hl();
            int v = hl + n;
            reg.hl(v);
            
            reg.subtract(false);
            reg.halfcarry(isHalfCarry16(hl, n));
            reg.carry(isCarry16(v));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Add to register HL the register HL
        Operation ADD_HL_HL = new Operation(0x29, "ADD HL,HL", map, (args) => {
            int n = reg.hl();
            int hl = reg.hl();
            int v = hl + n;
            reg.hl(v);
            
            reg.subtract(false);
            reg.halfcarry(isHalfCarry16(hl, n));
            reg.carry(isCarry16(v));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Add to register HL the register SP
        Operation ADD_HL_SP = new Operation(0x39, "ADD HL,SP", map, (args) => {
            int n = reg.sp();
            int hl = reg.hl();
            int v = hl + n;
            reg.hl(v);
            
            reg.subtract(false);
            reg.halfcarry(isHalfCarry16(hl, n));
            reg.carry(isCarry16(v));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Add to register SP the signed immediate value n
        Operation ADD_SP_n = new Operation(0xE8, "ADD SP,n", new ArgT[]{ ArgT.Byte }, map, (args) => {
            //Read byte from pc and convert to a signed value
            var un = args[0];
            int n = unsignedByteToSigned(un);
            
            //Add the signed value to the stack pointer value
            int sp = reg.sp();
            int v = sp + n;
            reg.sp(v);
            
            reg.zero(false);
            reg.subtract(false);
            reg.halfcarry(( sp & 0x0F ) + ( n & 0x0F ) > 0x0F); 
            reg.carry(( sp & 0xFF ) + (un & 0xFF) > 0xFF);
            
            clock.m(4);
            clock.t(16);
        });
        
        //Increment the register BC
        Operation INC_BC = new Operation(0x03, "INC BC", map, (args) => {
            int v = reg.bc() + 1;
            reg.bc(v);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Increment the register DE
        Operation INC_DE = new Operation(0x13, "INC DE", map, (args) => {
            int v = reg.de() + 1;
            reg.de(v);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Increment the register HL
        Operation INC_rHL = new Operation(0x23, "INC HL", map, (args) => {
            int v = reg.hl() + 1;
            reg.hl(v);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Increment the register SP
        Operation INC_SP = new Operation(0x33, "INC SP", map, (args) => {
            int v = reg.sp() + 1;
            reg.sp(v);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Decrement the register BC
        Operation DEC_BC = new Operation(0x0B, "DEC BC", map, (args) => {
            int v = reg.bc() - 1;
            reg.bc(v);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Decrement the register DE
        Operation DEC_DE = new Operation(0x1B, "DEC DE", map, (args) => {
            int v = reg.de() - 1;
            reg.de(v);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Decrement the register BC
        Operation DEC_rHL = new Operation(0x2B, "DEC HL", map, (args) => {
            int v = reg.hl() - 1;
            reg.hl(v);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Decrement the register SP
        Operation DEC_SP = new Operation(0x3B, "DEC SP", map, (args) => {
            int v = reg.sp() - 1;
            reg.sp(v);
            
            clock.m(2);
            clock.t(8);
        });
        
        ///
        // Miscellaneous Operationerations
        ///
        
        //Swap the upper and lower nibbles of register A
        Operation SWAP_A = new Operation(0x37, "SWAP A", cbmap, (args) => {
            int t = reg.a();
            int v = ((t&0x0F) << 4) | ((t&0xF0) >> 4);
            reg.a(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(false);
            reg.carry(false);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Swap the upper and lower nibbles of register B
        Operation SWAP_B = new Operation(0x30, "SWAP B", cbmap, (args) => {
            int t = reg.b();
            int v = ((t&0x0F) << 4) | ((t&0xF0) >> 4);
            reg.b(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(false);
            reg.carry(false);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Swap the upper and lower nibbles of register C
        Operation SWAP_C = new Operation(0x31, "SWAP C", cbmap, (args) => {
            int t = reg.c();
            int v = ((t&0x0F) << 4) | ((t&0xF0) >> 4);
            reg.c(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(false);
            reg.carry(false);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Swap the upper and lower nibbles of register D
        Operation SWAP_D = new Operation(0x32, "SWAP D", cbmap, (args) => {
            int t = reg.d();
            int v = ((t&0x0F) << 4) | ((t&0xF0) >> 4);
            reg.d(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(false);
            reg.carry(false);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Swap the upper and lower nibbles of register E
        Operation SWAP_E = new Operation(0x33, "SWAP E", cbmap, (args) => {
            int t = reg.e();
            int v = ((t&0x0F) << 4) | ((t&0xF0) >> 4);
            reg.e(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(false);
            reg.carry(false);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Swap the upper and lower nibbles of register H
        Operation SWAP_H = new Operation(0x34, "SWAP H", cbmap, (args) => {
            int t = reg.h();
            int v = ((t&0x0F) << 4) | ((t&0xF0) >> 4);
            reg.h(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(false);
            reg.carry(false);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Swap the upper and lower nibbles of register L
        Operation SWAP_L = new Operation(0x35, "SWAP L", cbmap, (args) => {
            int t = reg.l();
            int v = ((t&0x0F) << 4) | ((t&0xF0) >> 4);
            reg.l(v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(false);
            reg.carry(false);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Swap the upper and lower nibbles of memory at HL
        Operation SWAP_HL = new Operation(0x36, "SWAP (HL)", cbmap, (args) => {
            int t = mem.ReadByte(reg.hl());
            int v = ((t&0x0F) << 4) | ((t&0xF0) >> 4);
            mem.WriteByte(reg.hl(), v);
            
            reg.zero(isZero(v));
            reg.subtract(false);
            reg.halfcarry(false);
            reg.carry(false);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Adjust register A so that the correct representation of a binary coded decimal is obtained
        Operation DAA = new Operation(0x27, "DAA", map, (args) => {
            var correction = 0;
            var value = reg.a();
            var setCarry = false;

            if (reg.halfcarry() || (!reg.subtract() && (value & 0xF) > 9)) {
                correction |= 0x06;
            }

            if (reg.carry() || (!reg.subtract() && value > 0x99)) {
                correction |= 0x60;
                setCarry = true;
            }

            value += reg.subtract() ? -correction : correction;
            value &= 0xFF;

            reg.a(value);

            reg.zero(value == 0);
            reg.carry(setCarry);
            reg.halfcarry(false);

            clock.m(1);
            clock.t(4);
        });
        
        //Complement the A register
        Operation CPL = new Operation(0x2F, "CPL", map, (args) => {
            int a = reg.a() ^ 255;
            reg.a(a);
            
            reg.subtract(true);
            reg.halfcarry(true);
            
            clock.m(1);
            clock.t(4);
        });
        
        //If carry flag is set, reset it. If flag is reset, set it
        Operation CCF = new Operation(0x3F, "CCF", map, (args) => {
            reg.carry(!reg.carry());
            reg.halfcarry(false);
            reg.subtract(false);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Set the carry flag
        Operation SCF = new Operation(0x37, "SCF", map, (args) => {
            reg.carry(true);
            reg.halfcarry(false);
            reg.subtract(false);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Power down the CPU
        Operation HALT = new Operation(0x76, "HALT", map, (args) => {
            // http://z80-heaven.wikidot.com/instructions-set:halt
            EnterHaltMode();
            clock.m(1);
            clock.t(4);
        });
        
        //Disables interrupts
        Operation DI = new Operation(0xF3, "DI", map, (args) => {
            reg.ime(0);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Enabled interrupts
        Operation EI = new Operation(0xFB, "EI", map, (args) => {
            reg.ime(1);
            
            clock.m(1);
            clock.t(4);
        });
        
        ///
        // Rotates and Shifts
        ///
        //MARK
        //Rotate A left, old bit 7 to Carry flag
        Operation RLCA = new Operation(0x07, "RCLA", map, (args) => {
            int a = reg.a();
            bool carry = ((a & 0x80) != 0);
            a = (a << 1) | (carry ? 1: 0);
            reg.a(a);
            
            reg.zero(false);
            reg.subtract(false);
            reg.halfcarry(false);
            reg.carry(carry);

            clock.m(1);
            clock.t(4);
        });
        
        //Rotate A left through carry flag 
        Operation RLA = new Operation(0x17, "RLA", map, (args) => {
            int a = reg.a();
            bool carry = ((a & 0x80) != 0);
            a = (a << 1) | (reg.carry() ? 1 : 0);
            reg.a(a);
            
            reg.zero(false);
            reg.subtract(false);
            reg.halfcarry(false);
            reg.carry(carry);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Rotate A right, old bit 0 to carry flag
        Operation RRCA = new Operation(0x0F, "RRCA", map, (args) => {
            int a = reg.a();
            bool toCarry = ((a & 0b1) != 0);
            a = (a >> 1) | (toCarry ? 0x80 : 0);
            reg.a(a);
            
            reg.zero(false);
            reg.subtract(false);
            reg.halfcarry(false);
            reg.carry(toCarry);
            
            clock.m(1);
            clock.t(4);
        });
        
        //Rotate A right though the carry flag, old bit 0 to carry flag
        Operation RRA = new Operation(0x1F, "RRA", map, (args) => {
            int a = reg.a();
            bool toCarry = ((a & 0b1) != 0);
            a = (a >> 1) | (reg.carry() ? 0x80 : 0);
            reg.a(a);
            
            reg.zero(false);
            reg.subtract(false);
            reg.halfcarry(false);
            reg.carry(toCarry);
            
            clock.m(1);
            clock.t(4);
        }); 
        
        ///
        // Jumps
        ///
        
        //Jump to immediate address nn
        Operation JP_nn = new Operation(0xC3, "JP nn", new ArgT[]{ ArgT.Short }, map, (args) => {
            int nn = args[0];
            reg.pc(nn);
            
            clock.m(3);
            clock.t(12);
        });
        
        //Jump to address nn if Z flag is reset
        Operation JP_NZ_nn = new Operation(0xC2, "JP NZ,nn", new ArgT[]{ ArgT.Short }, map, (args) => {
            int nn = args[0];
            
            if(!reg.zero()){
                reg.pc(nn); //Maybe clock.m++;
                clock.m(1);
                clock.t(4);
            }
            
            clock.m(3);
            clock.t(12);
        });
        
        //Jump to address nn if Z flag is set
        Operation JP_Z_nn = new Operation(0xCA, "JP Z,nn", new ArgT[]{ ArgT.Short }, map, (args) => {
            int nn = args[0];
            
            if(reg.zero()){
                reg.pc(nn); //Maybe clock.m++;
                clock.m(1);
                clock.t(4);
            }
            
            clock.m(3);
            clock.t(12);
        });
        
        //Jump to address nn if Carry flag is reset
        Operation JP_NC_nn = new Operation(0xD2, "JP NC,nn", new ArgT[]{ ArgT.Short }, map, (args) => {
            int nn = args[0];
            
            if(!reg.carry()){
                reg.pc(nn); //Maybe clock.m++;
                clock.m(1);
                clock.t(4);
            }
            
            clock.m(3);
            clock.t(12);
        });
        
        //Jump to address nn if Carry flag is set
        Operation JP_C_nn = new Operation(0xDA, "JP C,nn", new ArgT[]{ ArgT.Short }, map, (args) => {
            int nn = args[0];
            
            if(reg.carry()){
                reg.pc(nn); //Maybe clock.m++;
                clock.m(1);
                clock.t(4);
            }
            
            clock.m(3);
            clock.t(12);
        });
        
        //Jump to address in HL
        Operation JP_HL = new Operation(0xE9, "JP (HL)", map, (args) => {
            reg.pc(reg.hl());
            
            clock.m(1);
            clock.t(4);
        });
        
        //Add signed value n to the current address and jump to it
        Operation JR_n = new Operation(0x18, "JR n", new ArgT[]{ ArgT.Byte }, map, (args) => {
            int n = unsignedByteToSigned(args[0]);
            int a = reg.pc() + n;
            reg.pc(a);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Jump to pc + signed n if Z flag is reset
        Operation JR_NZ_n = new Operation(0x20, "JR NZ,n", new ArgT[]{ ArgT.Byte }, map, (args) => {
            int i = unsignedByteToSigned(args[0]);
            
            if(!reg.zero()){
                reg.pc(reg.pc() + i);
                clock.m(1);
                clock.t(4);
            }
            
            clock.m(2);
            clock.t(8);
        });
        
        //Jump to pc + signed n if Z flag is set
        Operation JR_Z_n = new Operation(0x28, "JR Z,n", new ArgT[]{ ArgT.Byte }, map, (args) => {
            int i = unsignedByteToSigned(args[0]);
            
            if(reg.zero()){
                reg.pc(reg.pc() + i);
                clock.m(1);
                clock.t(4);
            }
            
            clock.m(2);
            clock.t(8);
        });
        
        //Jump to pc + signed n if C flag is reset
        Operation JR_NC_n = new Operation(0x30, "JR NC,n", new ArgT[]{ ArgT.Byte }, map, (args) => {
            int i = unsignedByteToSigned(args[0]);
            
            if(!reg.carry()){
                reg.pc(reg.pc() + i);
                clock.m(1);
                clock.t(4);
            }
            
            clock.m(2);
            clock.t(8);
        });
        
        //Jump to pc + signed n if C flag is set
        Operation JR_C_n = new Operation(0x38, "JR C,n", new ArgT[]{ ArgT.Byte }, map, (args) => {
            int i = unsignedByteToSigned(args[0]);
            
            if(reg.carry()){
                reg.pc(reg.pc() + i);
                clock.m(1);
                clock.t(4);
            }
            
            clock.m(2);
            clock.t(8);
        });
        
        //PAGE 114
        
        ///
        // Calls
        //
        
        //Push next instruction onto stack and jump to address nn
        Operation CALL_nn = new Operation(0xCD, "CALL nn", new ArgT[]{ ArgT.Short }, map, (args) => {
            //Get inst params
            int addr = args[0];
            
            //Push next to stack
            push(reg.pc());
            
            //Jump to immediate value
            reg.pc(addr);
            
            clock.m(5);
            clock.t(24);
        });
        
        //Call address n if the condtion is true
        Operation CALL_NZ_nn = new Operation(0xC4, "CALL NZ,nn", new ArgT[]{ ArgT.Short }, map, (args) => {
            if(!reg.zero()){
                //Get inst params
                int addr = args[0];

                //Push next to stack
                push(reg.pc());

                //Jump to immediate value
                reg.pc(addr);
                
                clock.m(2);
                clock.t(8);
            }
            
            clock.m(3);
            clock.t(12);
        });
        
        //Call address n if the condtion is true
        Operation CALL_Z_nn = new Operation(0xCC, "CALL Z,nn", new ArgT[]{ ArgT.Short }, map, (args) => {
            if(reg.zero()){
                //Get inst params
                int addr = args[0];

                //Push next to stack
                push(reg.pc());

                //Jump to immediate value
                reg.pc(addr);
                
                clock.m(2);
                clock.t(8);
            }
            
            clock.m(3);
            clock.t(12);
        });
        
        //Call address n if the condtion is true
        Operation CALL_NC_nn = new Operation(0xD4, "CALL NC,nn", new ArgT[]{ ArgT.Short }, map, (args) => {
            if(!reg.carry()){
                //Get inst params
                int addr = args[0];

                //Push next to stack
                push(reg.pc());

                //Jump to immediate value
                reg.pc(addr);
                
                clock.m(2);
                clock.t(8);
            }
            
            clock.m(3);
            clock.t(12);
        });
        
        //Call address n if the condtion is true
        Operation CALL_C_nn = new Operation(0xDC, "CALL C,nn", new ArgT[]{ ArgT.Short }, map, (args) => {
            if(reg.carry()) {
                //Get inst params
                int addr = args[0];

                //Push next to stack
                push(reg.pc());

                //Jump to immediate value
                reg.pc(addr);
                
                clock.m(2);
                clock.t(8);
            }
            
            clock.m(3);
            clock.t(12);
        });
        
        //PAGE 116

        ///
        // Restarts
        ///
        
        //Push present address onto stack and jump to 0x0000 + n
        Operation RST_00h = new Operation(0xC7, "RST 00H", map, (args) => {
            rst(0x00);
        });
        
        //Push present address onto stack and jump to 0x0000 + n
        Operation RST_08h = new Operation(0xCF, "RST 08H", map, (args) => {
            rst(0x08);
        });
        
        //Push present address onto stack and jump to 0x0000 + n
        Operation RST_10h = new Operation(0xD7, "RST 10H", map, (args) => {
            rst(0x10);
        });
        
        //Push present address onto stack and jump to 0x0000 + n
        Operation RST_18h = new Operation(0xDF, "RST 18H", map, (args) => {
            rst(0x18);
        });
        
        //Push present address onto stack and jump to 0x0000 + n
        Operation RST_20h = new Operation(0xE7, "RST 20H", map, (args) => {
            rst(0x20);
        });
        
        //Push present address onto stack and jump to 0x0000 + n
        Operation RST_28h = new Operation(0xEF, "RST 28H", map, (args) => {
            rst(0x28);
        });
        
        //Push present address onto stack and jump to 0x0000 + n
        Operation RST_30h = new Operation(0xF7, "RST 30H", map, (args) => {
            rst(0x30);
        });
        
        //Push present address onto stack and jump to 0x0000 + n
        Operation RST_38h = new Operation(0xFF, "RST 38H", map, (args) => {
            rst(0x38);
        });
        
        ///
        // Returns
        ///
        
        //POperation 2 bytes off stack and jump to that address
        Operation RET = new Operation(0xC9, "RET", map, (args) => {
            reg.pc(pop());
            
            clock.m(3);
            clock.t(12);
        });
        
        //Return if Z flag is reset
        Operation RET_NZ = new Operation(0xC0, "RET NZ", map, (args) => {
            if(!reg.zero()){
                reg.pc(pop());
                clock.m(3);
                clock.t(12);
            }else{
                clock.m(1);
                clock.t(4);
            }
        });
        
        //Return if Z flag is set
        Operation RET_Z = new Operation(0xC8, "RET Z", map, (args) => {
            if(reg.zero()){
                reg.pc(pop());
                clock.m(3);
                clock.t(12);
            }else{
                clock.m(1);
                clock.t(4);
            }
        });
        
        //Return if C flag is reset
        Operation RET_CZ = new Operation(0xD0, "RET NC", map, (args) => {
            if(!reg.carry()){
                reg.pc(pop());
                clock.m(3);
                clock.t(12);
            }else{
                clock.m(1);
                clock.t(4);
            }
        });
        
        //Return if C flag is set
        Operation RET_C = new Operation(0xD8, "RET C", map, (args) => {
            if(reg.carry()){
                reg.pc(pop());
                clock.m(3);
                clock.t(12);
            }else{
                clock.m(1);
                clock.t(4);
            }
        });
        
        //Pop 2 bytes off the stack, and jump there then enable interrupts
        Operation RETI = new Operation(0xD9, "RETI", map, (args) => {
            //rrs(); //Restore registry //TODO
            EI.Invoke(args);
            reg.pc(pop());
            
            clock.m(2);
            clock.t(8);
        });
        
        //Use CB operation
        Operation PREFIX = new Operation(0xCB, "CBOP", new ArgT[]{ ArgT.Byte }, map, (args) => {
            int i = args[0];
            var cbop = this.FetchCbPrefixedOperation(i);
            cbop.Invoke(noArgs);
        });
        
        Operation DJNZn = new Operation(0x10, "DJNZn", new ArgT[]{ ArgT.Byte }, map, (args) => { 
            int i = unsignedByteToSigned(args[0]);
            
            clock.m(2);
            clock.t(8);
            
            reg.b(reg.b() - 1);
            if(reg.b() != 0){
                reg.pcpp(i);
                clock.m(1);
                clock.t(4);
            }
        });
        
        ///
        // CB Operations
        ///
        
        //Rotate register A left
        Operation RLC_A = new Operation(0x07, "RLC A", cbmap, (args) => {
            int a = rotateLeftCarry(reg.a());
            reg.a(a);

            clock.m(2);
            clock.t(8);
        });
        
        //Rotate register B left
        Operation RLC_B = new Operation(0x00, "RLC B", cbmap, (args) => {
            int a = rotateLeftCarry(reg.b());
            reg.b(a);

            clock.m(2);
            clock.t(8);
        });
        
        //Rotate register C left
        Operation RLC_C = new Operation(0x01, "RLC C", cbmap, (args) => {
            int a = rotateLeftCarry(reg.c());
            reg.c(a);

            clock.m(2);
            clock.t(8);
        });
        
        //Rotate register D left
        Operation RLC_D = new Operation(0x02, "RLC D", cbmap, (args) => {
            int a = rotateLeftCarry(reg.d());
            reg.d(a);

            clock.m(2);
            clock.t(8);
        });
        
        //Rotate register E left
        Operation RLC_E = new Operation(0x03, "RLC E", cbmap, (args) => {
            int a = rotateLeftCarry(reg.e());
            reg.e(a);

            clock.m(2);
            clock.t(8);
        });
        
        //Rotate register H left
        Operation RLC_H = new Operation(0x04, "RLC H", cbmap, (args) => {
            int a = rotateLeftCarry(reg.h());
            reg.h(a);

            clock.m(2);
            clock.t(8);
        });
        
        //Rotate register L left
        Operation RLC_L = new Operation(0x05, "RLC L", cbmap, (args) => {
            int a = rotateLeftCarry(reg.l());
            reg.l(a);

            clock.m(2);
            clock.t(8);
        });
        
        //Rotate memory location at HL left
        Operation RLC_HL = new Operation(0x06, "RLC (HL)", cbmap, (args) => {
            int a = rotateLeftCarry(mem.ReadByte(reg.hl()));
            mem.WriteByte(reg.hl(), a);

            clock.m(4);
            clock.t(16);
        });
        
        //Rotate register A left through the carry flag
        Operation RL_A = new Operation(0x17, "RL A", cbmap, (args) => {
            int a = rotateLeft(reg.a());
            reg.a(a);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Rotate register B left through the carry flag
        Operation RL_B = new Operation(0x10, "RL B", cbmap, (args) => {
            int a = rotateLeft(reg.b());
            reg.b(a);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Rotate register C left through the carry flag
        Operation RL_C = new Operation(0x11, "RL C", cbmap, (args) => {
            int a = rotateLeft(reg.c());
            reg.c(a);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Rotate register D left through the carry flag
        Operation RL_D = new Operation(0x12, "RL D", cbmap, (args) => {
            int a = rotateLeft(reg.d());
            reg.d(a);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Rotate register E left through the carry flag
        Operation RL_E = new Operation(0x13, "RL E", cbmap, (args) => {
            int a = rotateLeft(reg.e());
            reg.e(a);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Rotate register H left through the carry flag
        Operation RL_H = new Operation(0x14, "RL H", cbmap, (args) => {
            int a = rotateLeft(reg.h());
            reg.h(a);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Rotate register L left through the carry flag
        Operation RL_L = new Operation(0x15, "RL L", cbmap, (args) => {
            int a = rotateLeft(reg.l());
            reg.l(a);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Rotate memry at HL left through the carry flag
        Operation RL_HL = new Operation(0x16, "RL (HL)", cbmap, (args) => {
            int a = rotateLeft(mem.ReadByte(reg.hl()));
            mem.WriteByte(reg.hl(), a);
            
            clock.m(4);
            clock.t(16);
        });
        
        //Rotate A right though the carry flag, old bit 0 to carry flag
        Operation RR_A = new Operation(0x1F, "RR A", cbmap, (args) => {
            int a = rotateRight(reg.a());
            reg.a(a);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Rotate B right though the carry flag, old bit 0 to carry flag
        Operation RR_B = new Operation(0x18, "RR B", cbmap, (args) => {
            int a = rotateRight(reg.b());
            reg.b(a);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Rotate C right though the carry flag, old bit 0 to carry flag
        Operation RR_C = new Operation(0x19, "RR C", cbmap, (args) => {
            int a = rotateRight(reg.c());
            reg.c(a);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Rotate D right though the carry flag, old bit 0 to carry flag
        Operation RR_D = new Operation(0x1A, "RR D", cbmap, (args) => {
            int a = rotateRight(reg.d());
            reg.d(a);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Rotate E right though the carry flag, old bit 0 to carry flag
        Operation RR_E = new Operation(0x1B, "RR E", cbmap, (args) => {
            int a = rotateRight(reg.e());
            reg.e(a);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Rotate H right though the carry flag, old bit 0 to carry flag
        Operation RR_H = new Operation(0x1C, "RR H", cbmap, (args) => {
            int a = rotateRight(reg.h());
            reg.h(a);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Rotate L right though the carry flag, old bit 0 to carry flag
        Operation RR_L = new Operation(0x1D, "RR L", cbmap, (args) => {
            int a = rotateRight(reg.l());
            reg.l(a);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Rotate memory HL right though the carry flag, old bit 0 to carry flag
        Operation RR_HL = new Operation(0x1E, "RR (HL)", cbmap, (args) => {
            int a = rotateRight(mem.ReadByte(reg.hl()));
            mem.WriteByte(reg.hl(), a);
            
            clock.m(4);
            clock.t(16);
        });
        
        //Rotate A right, old bit 0 to carry flag
        Operation RRC_A = new Operation(0x0F, "RRC A", cbmap, (args) => {
            int a = rotateRightCarry(reg.a());
            reg.a(a);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Rotate B right, old bit 0 to carry flag
        Operation RRC_B = new Operation(0x08, "RRC B", cbmap, (args) => {
            int a = rotateRightCarry(reg.b());
            reg.b(a);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Rotate C right, old bit 0 to carry flag
        Operation RRC_C = new Operation(0x09, "RRC C", cbmap, (args) => {
            int a = rotateRightCarry(reg.c());
            reg.c(a);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Rotate D right, old bit 0 to carry flag
        Operation RRC_D = new Operation(0x0A, "RRC D", cbmap, (args) => {
            int a = rotateRightCarry(reg.d());
            reg.d(a);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Rotate E right, old bit 0 to carry flag
        Operation RRC_E = new Operation(0x0B, "RRC E", cbmap, (args) => {
            int a = rotateRightCarry(reg.e());
            reg.e(a);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Rotate H right, old bit 0 to carry flag
        Operation RRC_H = new Operation(0x0C, "RRC H", cbmap, (args) => {
            int a = rotateRightCarry(reg.h());
            reg.h(a);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Rotate L right, old bit 0 to carry flag
        Operation RRC_L = new Operation(0x0D, "RRC L", cbmap, (args) => {
            int a = rotateRightCarry(reg.l());
            reg.l(a);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Rotate memory at HL right, old bit 0 to carry flag
        Operation RRC_HL = new Operation(0x0E, "RRC (HL)", cbmap, (args) => {
            int a = rotateRightCarry(mem.ReadByte(reg.hl()));
            mem.WriteByte(reg.hl(), a);
            
            clock.m(4);
            clock.t(16);
        });
        
        //PAGE 105 specifically 0x86 and 0xFE
        
        //Shift A into carry, LSB is set to 0;
        Operation SLA_A = new Operation(0x27, "SLA A", cbmap, (args) => {
            int n = this.shiftLeft(reg.a());
            reg.a(n);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Shift B into carry, LSB is set to 0;
        Operation SLA_B = new Operation(0x20, "SLA B", cbmap, (args) => {
            int n = this.shiftLeft(reg.b());
            reg.b(n);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Shift C into carry, LSB is set to 0;
        Operation SLA_C = new Operation(0x21, "SLA C", cbmap, (args) => {
            int n = this.shiftLeft(reg.c());
            reg.c(n);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Shift D into carry, LSB is set to 0;
        Operation SLA_D = new Operation(0x22, "SLA D", cbmap, (args) => {
            int n = this.shiftLeft(reg.d());
            reg.d(n);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Shift E into carry, LSB is set to 0;
        Operation SLA_E = new Operation(0x23, "SLA E", cbmap, (args) => {
            int n = this.shiftLeft(reg.e());
            reg.e(n);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Shift H into carry, LSB is set to 0;
        Operation SLA_H = new Operation(0x24, "SLA H", cbmap, (args) => {
            int n = this.shiftLeft(reg.h());
            reg.h(n);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Shift L into carry, LSB is set to 0;
        Operation SLA_L = new Operation(0x25, "SLA L", cbmap, (args) => {
            int n = this.shiftLeft(reg.l());
            reg.l(n);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Shift memory at HL into carry, LSB is set to 0;
        Operation SLA_HL = new Operation(0x26, "SLA (HL)", cbmap, (args) => {
            int n = this.shiftLeft(mem.ReadByte(reg.hl()));
            mem.WriteByte(reg.hl(), n);
            
            clock.m(4);
            clock.t(16);
        });
    
        //Shift A into carry, MSB doesnt change
        Operation SRA_A = new Operation(0x2F, "SRA A", cbmap, (args) => {
            int n = this.shiftRightExtend(reg.a());
            reg.a(n);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Shift B into carry, MSB doesnt change
        Operation SRA_B = new Operation(0x28, "SRA B", cbmap, (args) => {
            int n = this.shiftRightExtend(reg.b());
            reg.b(n);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Shift C into carry, MSB doesnt change
        Operation SRA_C = new Operation(0x29, "SRA C", cbmap, (args) => {
            int n = this.shiftRightExtend(reg.c());
            reg.c(n);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Shift D into carry, MSB doesnt change
        Operation SRA_D = new Operation(0x2A, "SRA D", cbmap, (args) => {
            int n = this.shiftRightExtend(reg.d());
            reg.d(n);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Shift E into carry, MSB doesnt change
        Operation SRA_E = new Operation(0x2B, "SRA E", cbmap, (args) => {
            int n = this.shiftRightExtend(reg.e());
            reg.e(n);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Shift H into carry, MSB doesnt change
        Operation SRA_H = new Operation(0x2C, "SRA H", cbmap, (args) => {
            int n = this.shiftRightExtend(reg.h());
            reg.h(n);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Shift L into carry, MSB doesnt change
        Operation SRA_L = new Operation(0x2D, "SRA L", cbmap, (args) => {
            int n = this.shiftRightExtend(reg.l());
            reg.l(n);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Shift memory at HL into carry, MSB doesnt change
        Operation SRA_HL = new Operation(0x2E, "SRA (HL)", cbmap, (args) => {
            int n = this.shiftRightExtend(mem.ReadByte(reg.hl()));
            mem.WriteByte(reg.hl(), n);
            
            clock.m(4);
            clock.t(16);
        });
        
        //Shift A into carry, MSB 0
        Operation SRL_A = new Operation(0x3F, "SRL A", cbmap, (args) => {
            int n = this.shiftRight0(reg.a());
            reg.a(n);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Shift B into carry, MSB 0
        Operation SRL_B = new Operation(0x38, "SRL B", cbmap, (args) => {
            int n = this.shiftRight0(reg.b());
            reg.b(n);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Shift C into carry, MSB 0
        Operation SRL_C = new Operation(0x39, "SRL C", cbmap, (args) => {
            int n = this.shiftRight0(reg.c());
            reg.c(n);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Shift D into carry, MSB 0
        Operation SRL_D = new Operation(0x3A, "SRL D", cbmap, (args) => {
            int n = this.shiftRight0(reg.d());
            reg.d(n);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Shift E into carry, MSB 0
        Operation SRL_E = new Operation(0x3B, "SRL E", cbmap, (args) => {
            int n = this.shiftRight0(reg.e());
            reg.e(n);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Shift H into carry, MSB 0
        Operation SRL_H = new Operation(0x3C, "SRL H", cbmap, (args) => {
            int n = this.shiftRight0(reg.h());
            reg.h(n);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Shift L into carry, MSB 0
        Operation SRL_L = new Operation(0x3D, "SRL L", cbmap, (args) => {
            int n = this.shiftRight0(reg.l());
            reg.l(n);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Shift memory at HL into carry, MSB 0
        Operation SRL_HL = new Operation(0x3E, "SRL (HL)", cbmap, (args) => {
            int n = this.shiftRight0(mem.ReadByte(reg.hl()));
            mem.WriteByte(reg.hl(), n);
            
            clock.m(4);
            clock.t(16);
        });
        
        ///
        // Bit manipulations
        ///
        
        //PAGE 108
        
        //Check if bit 0 of register A is set
        Operation BIT0_A = new Operation(0x47, "BIT 0,A", cbmap, (args) => {
            testBit(reg.a(), 0);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 0 of register B is set
        Operation BIT0_B = new Operation(0x40, "BIT 0,B", cbmap, (args) => {
            testBit(reg.b(), 0);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 0 of register C is set
        Operation BIT0_C = new Operation(0x41, "BIT 0,C", cbmap, (args) => {
            testBit(reg.c(), 0);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 0 of register D is set
        Operation BIT0_D = new Operation(0x42, "BIT 0,D", cbmap, (args) => {
            testBit(reg.d(), 0);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 0 of register E is set
        Operation BIT0_E = new Operation(0x43, "BIT 0,E", cbmap, (args) => {
            testBit(reg.e(), 0);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 0 of register H is set
        Operation BIT0_H = new Operation(0x44, "BIT 0,H", cbmap, (args) => {
            testBit(reg.h(), 0);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 0 of register L is set
        Operation BIT0_L = new Operation(0x45, "BIT 0,L", cbmap, (args) => {
            testBit(reg.l(), 0);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 0 of memory HL is set
        Operation BIT0_HL = new Operation(0x46, "BIT 0,(HL)", cbmap, (args) => {
            testBit(mem.ReadByte(reg.hl()), 0);
            
            clock.m(4);
            clock.t(16);
        });
        
        //Check if bit 1 of register A is set
        Operation BIT1_A = new Operation(0x4F, "BIT 1,A", cbmap, (args) => {
            testBit(reg.a(), 1);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 1 of register B is set
        Operation BIT1_B = new Operation(0x48, "BIT 1,B", cbmap, (args) => {
            testBit(reg.b(), 1);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 1 of register C is set
        Operation BIT1_C = new Operation(0x49, "BIT 1,C", cbmap, (args) => {
            testBit(reg.c(), 1);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 1 of register D is set
        Operation BIT1_D = new Operation(0x4A, "BIT 1,D", cbmap, (args) => {
            testBit(reg.d(), 1);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 1 of register E is set
        Operation BIT1_E = new Operation(0x4B, "BIT 1,E", cbmap, (args) => {
            testBit(reg.e(), 1);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 1 of register H is set
        Operation BIT1_H = new Operation(0x4C, "BIT 1,H", cbmap, (args) => {
            testBit(reg.h(), 1);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 1 of register L is set
        Operation BIT1_L = new Operation(0x4D, "BIT 1,L", cbmap, (args) => {
            testBit(reg.l(), 1);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 1 of memory HL is set
        Operation BIT1_HL = new Operation(0x4E, "BIT 1,(HL)", cbmap, (args) => {
            testBit(mem.ReadByte(reg.hl()), 1);
            
            clock.m(4);
            clock.t(16);
        });
        
        //Check if bit 2 of register A is set
        Operation BIT2_A = new Operation(0x57, "BIT 2,A", cbmap, (args) => {
            testBit(reg.a(), 2);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 2 of register B is set
        Operation BIT2_B = new Operation(0x50, "BIT 2,B", cbmap, (args) => {
            testBit(reg.b(), 2);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 2 of register C is set
        Operation BIT2_C = new Operation(0x51, "BIT 2,C", cbmap, (args) => {
            testBit(reg.c(), 2);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 2 of register D is set
        Operation BIT2_D = new Operation(0x52, "BIT 2,D", cbmap, (args) => {
            testBit(reg.d(), 2);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 2 of register E is set
        Operation BIT2_E = new Operation(0x53, "BIT 2,E", cbmap, (args) => {
            testBit(reg.e(), 2);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 2 of register H is set
        Operation BIT2_H = new Operation(0x54, "BIT 2,H", cbmap, (args) => {
            testBit(reg.h(), 2);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 2 of register L is set
        Operation BIT2_L = new Operation(0x55, "BIT 2,L", cbmap, (args) => {
            testBit(reg.l(), 2);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 2 of memory HL is set
        Operation BIT2_HL = new Operation(0x56, "BIT 2,(HL)", cbmap, (args) => {
            testBit(mem.ReadByte(reg.hl()), 2);
            
            clock.m(4);
            clock.t(16);
        });
        
        //Check if bit 3 of register A is set
        Operation BIT3_A = new Operation(0x5F, "BIT 3,A", cbmap, (args) => {
            testBit(reg.a(), 3);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 3 of register B is set
        Operation BIT3_B = new Operation(0x58, "BIT 3,B", cbmap, (args) => {
            testBit(reg.b(), 3);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 3 of register C is set
        Operation BIT3_C = new Operation(0x59, "BIT 3,C", cbmap, (args) => {
            testBit(reg.c(), 3);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 3 of register D is set
        Operation BIT3_D = new Operation(0x5A, "BIT 3,D", cbmap, (args) => {
            testBit(reg.d(), 3);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 3 of register E is set
        Operation BIT3_E = new Operation(0x5B, "BIT 3,E", cbmap, (args) => {
            testBit(reg.e(), 3);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 3 of register H is set
        Operation BIT3_H = new Operation(0x5C, "BIT 3,H", cbmap, (args) => {
            testBit(reg.h(), 3);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 3 of register L is set
        Operation BIT3_L = new Operation(0x5D, "BIT 3,L", cbmap, (args) => {
            testBit(reg.l(), 3);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 3 of memory HL is set
        Operation BIT3_HL = new Operation(0x5E, "BIT 3,(HL)", cbmap, (args) => {
            testBit(mem.ReadByte(reg.hl()), 3);
            
            clock.m(4);
            clock.t(16);
        });
        
        //Check if bit 4 of register A is set
        Operation BIT4_A = new Operation(0x67, "BIT 4,A", cbmap, (args) => {
            testBit(reg.a(), 4);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 4 of register B is set
        Operation BIT4_B = new Operation(0x60, "BIT 4,B", cbmap, (args) => {
            testBit(reg.b(), 4);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 4 of register C is set
        Operation BIT4_C = new Operation(0x61, "BIT 4,C", cbmap, (args) => {
            testBit(reg.c(), 4);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 4 of register D is set
        Operation BIT4_D = new Operation(0x62, "BIT 4,D", cbmap, (args) => {
            testBit(reg.d(), 4);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 4 of register E is set
        Operation BIT4_E = new Operation(0x63, "BIT 4,E", cbmap, (args) => {
            testBit(reg.e(), 4);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 4 of register H is set
        Operation BIT4_H = new Operation(0x64, "BIT 4,H", cbmap, (args) => {
            testBit(reg.h(), 4);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 4 of register L is set
        Operation BIT4_L = new Operation(0x65, "BIT 4,L", cbmap, (args) => {
            testBit(reg.l(), 4);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 4 of memory HL is set
        Operation BIT4_HL = new Operation(0x66, "BIT 4,(HL)", cbmap, (args) => {
            testBit(mem.ReadByte(reg.hl()), 4);

            clock.m(4);
            clock.t(16);
        });
        
        //Check if bit 5 of register A is set
        Operation BIT5_A = new Operation(0x6F, "BIT 5,A", cbmap, (args) => {
            testBit(reg.a(), 5);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 5 of register B is set
        Operation BIT5_B = new Operation(0x68, "BIT 5,B", cbmap, (args) => {
            testBit(reg.b(), 5);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 5 of register C is set
        Operation BIT5_C = new Operation(0x69, "BIT 5,C", cbmap, (args) => {
            testBit(reg.c(), 5);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 5 of register D is set
        Operation BIT5_D = new Operation(0x6A, "BIT 5,D", cbmap, (args) => {
            testBit(reg.d(), 5);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 5 of register E is set
        Operation BIT5_E = new Operation(0x6B, "BIT 5,E", cbmap, (args) => {
            testBit(reg.e(), 5);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 5 of register H is set
        Operation BIT5_H = new Operation(0x6C, "BIT 5,H", cbmap, (args) => {
            testBit(reg.h(), 5);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 5 of register L is set
        Operation BIT5_L = new Operation(0x6D, "BIT 5,L", cbmap, (args) => {
            testBit(reg.l(), 5);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 5 of memory HL is set
        Operation BIT5_HL = new Operation(0x6E, "BIT 5,(HL)", cbmap, (args) => {
            testBit(mem.ReadByte(reg.hl()), 5);
            
            clock.m(4);
            clock.t(16);
        });
        
        //Check if bit 6 of register A is set
        Operation BIT6_A = new Operation(0x77, "BIT 6,A", cbmap, (args) => {
            testBit(reg.a(), 6);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 6 of register B is set
        Operation BIT6_B = new Operation(0x70, "BIT 6,B", cbmap, (args) => {
            testBit(reg.b(), 6);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 6 of register C is set
        Operation BIT6_C = new Operation(0x71, "BIT 6,C", cbmap, (args) => {
            testBit(reg.c(), 6);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 6 of register D is set
        Operation BIT6_D = new Operation(0x72, "BIT 6,D", cbmap, (args) => {
            testBit(reg.d(), 6);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 6 of register E is set
        Operation BIT6_E = new Operation(0x73, "BIT 6,E", cbmap, (args) => {
            testBit(reg.e(), 6);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 6 of register H is set
        Operation BIT6_H = new Operation(0x74, "BIT 6,H", cbmap, (args) => {
            testBit(reg.h(), 6);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 6 of register L is set
        Operation BIT6_L = new Operation(0x75, "BIT 6,L", cbmap, (args) => {
            testBit(reg.l(), 6);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 6 of memory HL is set
        Operation BIT6_HL = new Operation(0x76, "BIT 6,(HL)", cbmap, (args) => {
            testBit(mem.ReadByte(reg.hl()), 6);

            clock.m(4);
            clock.t(16);
        });
        
        //Check if bit 7 of register A is set
        Operation BIT7_A = new Operation(0x7F, "BIT 7,A", cbmap, (args) => {
            testBit(reg.a(), 7);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 7 of register B is set
        Operation BIT7_B = new Operation(0x78, "BIT 7,B", cbmap, (args) => {
            testBit(reg.b(), 7);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 7 of register C is set
        Operation BIT7_C = new Operation(0x79, "BIT 7,C", cbmap, (args) => {
            testBit(reg.c(), 7);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 7 of register D is set
        Operation BIT7_D = new Operation(0x7A, "BIT 7,D", cbmap, (args) => {
            testBit(reg.d(), 7);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 7 of register E is set
        Operation BIT7_E = new Operation(0x7B, "BIT 7,E", cbmap, (args) => {
            testBit(reg.e(), 7);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 7 of register H is set
        Operation BIT7_H = new Operation(0x7C, "BIT 7,H", cbmap, (args) => {
            testBit(reg.h(), 7);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 7 of register L is set
        Operation BIT7_L = new Operation(0x7D, "BIT 7,L", cbmap, (args) => {
            testBit(reg.l(), 7);
            
            clock.m(2);
            clock.t(8);
        });
        
        //Check if bit 7 of memory HL is set
        Operation BIT7_HL = new Operation(0x7E, "BIT 7,(HL)", cbmap, (args) => {
            testBit(mem.ReadByte(reg.hl()), 7);
            
            clock.m(4);
            clock.t(16);
        });
        
        //Set bit b in register r
        Operation SET0_A = new Operation(0xC7, "SET 0,A", cbmap, (args) => {
            reg.a(setBit(reg.a(), 0));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET0_B = new Operation(0xC0, "SET 0,B", cbmap, (args) => {
            reg.b(setBit(reg.b(), 0));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET0_C = new Operation(0xC1, "SET 0,C", cbmap, (args) => {
            reg.c(setBit(reg.c(), 0));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET0_D = new Operation(0xC2, "SET 0,D", cbmap, (args) => {
            reg.d(setBit(reg.d(), 0));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET0_E = new Operation(0xC3, "SET 0,E", cbmap, (args) => {
            reg.e(setBit(reg.e(), 0));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET0_H = new Operation(0xC4, "SET 0,H", cbmap, (args) => {
            reg.h(setBit(reg.h(), 0));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET0_L = new Operation(0xC5, "SET 0,L", cbmap, (args) => {
            reg.l(setBit(reg.l(), 0));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET0_HL = new Operation(0xC6, "SET 0,(HL)", cbmap, (args) => {
            mem.WriteByte( reg.hl(), setBit(mem.ReadByte(reg.hl()), 0));
            
            clock.m(4);
            clock.t(16);
        });
        
        //Set bit b in register r
        Operation SET1_A = new Operation(0xCF, "SET 1,A", cbmap, (args) => {
            reg.a(setBit(reg.a(), 1));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET1_B = new Operation(0xC8, "SET 1,B", cbmap, (args) => {
            reg.b(setBit(reg.b(), 1));
            
            clock.m(2);
            clock.t(8);
        });
    
        //Set bit b in register r
        Operation SET1_C = new Operation(0xC9, "SET 1,C", cbmap, (args) => {
            reg.c(setBit(reg.c(), 1));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET1_D = new Operation(0xCA, "SET 1,D", cbmap, (args) => {
            reg.d(setBit(reg.d(), 1));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET1_E = new Operation(0xCB, "SET 1,E", cbmap, (args) => {
            reg.e(setBit(reg.e(), 1));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET1_H = new Operation(0xCC, "SET 1,H", cbmap, (args) => {
            reg.h(setBit(reg.h(), 1));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET1_L = new Operation(0xCD, "SET 1,L", cbmap, (args) => {
            reg.l(setBit(reg.l(), 1));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET1_HL = new Operation(0xCE, "SET 1,(HL)", cbmap, (args) => {
            mem.WriteByte( reg.hl(), setBit(mem.ReadByte(reg.hl()), 1));
            
            clock.m(4);
            clock.t(16);
        });
        
        //Set bit b in register r
        Operation SET2_A = new Operation(0xD7, "SET 2,A", cbmap, (args) => {
            reg.a(setBit(reg.a(), 2));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET2_B = new Operation(0xD0, "SET 2,B", cbmap, (args) => {
            reg.b(setBit(reg.b(), 2));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET2_C = new Operation(0xD1, "SET 2,C", cbmap, (args) => {
            reg.c(setBit(reg.c(), 2));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET2_D = new Operation(0xD2, "SET 2,D", cbmap, (args) => {
            reg.d(setBit(reg.d(), 2));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET2_E = new Operation(0xD3, "SET 2,E", cbmap, (args) => {
            reg.e(setBit(reg.e(), 2));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET2_H = new Operation(0xD4, "SET 2,H", cbmap, (args) => {
            reg.h(setBit(reg.h(), 2));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET2_L = new Operation(0xD5, "SET 2,L", cbmap, (args) => {
            reg.l(setBit(reg.l(), 2));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET2_HL = new Operation(0xD6, "SET 2,(HL)", cbmap, (args) => {
            mem.WriteByte( reg.hl(), setBit(mem.ReadByte(reg.hl()), 2));
            
            clock.m(4);
            clock.t(16);
        });
        
        //Set bit b in register r
        Operation SET3_A = new Operation(0xDF, "SET 3,A", cbmap, (args) => {
            reg.a( setBit(reg.a(), 3));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET3_B = new Operation(0xD8, "SET 3,B", cbmap, (args) => {
            reg.b(setBit(reg.b(), 3));
            
            clock.m(2);
            clock.t(8);
        });
    
        //Set bit b in register r
        Operation SET3_C = new Operation(0xD9, "SET 3,C", cbmap, (args) => {
            reg.c(setBit(reg.c(), 3));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET3_D = new Operation(0xDA, "SET 3,D", cbmap, (args) => {
            reg.d( setBit(reg.d(), 3));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET3_E = new Operation(0xDB, "SET 3,E", cbmap, (args) => {
            reg.e(setBit(reg.e(), 3));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET3_H = new Operation(0xDC, "SET 3,H", cbmap, (args) => {
            reg.h(setBit(reg.h(), 3));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET3_L = new Operation(0xDD, "SET 3,L", cbmap, (args) => {
            reg.l(setBit(reg.l(), 3));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET3_HL = new Operation(0xDE, "SET 3,(HL)", cbmap, (args) => {
            mem.WriteByte( reg.hl(), setBit(mem.ReadByte(reg.hl()), 3));
            
            clock.m(4);
            clock.t(16);
        });
        
        //Set bit b in register r
        Operation SET4_A = new Operation(0xE7, "SET 4,A", cbmap, (args) => {
            reg.a(setBit(reg.a(), 4));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET4_B = new Operation(0xE0, "SET 4,B", cbmap, (args) => {
            reg.b(setBit(reg.b(), 4));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET4_C = new Operation(0xE1, "SET 4,C", cbmap, (args) => {
            reg.c(setBit(reg.c(), 4));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET4_D = new Operation(0xE2, "SET 4,D", cbmap, (args) => {
            reg.d(setBit(reg.d(), 4));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET4_E = new Operation(0xE3, "SET 4,E", cbmap, (args) => {
            reg.e(setBit(reg.e(), 4));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET4_H = new Operation(0xE4, "SET 4,H", cbmap, (args) => {
            reg.h(setBit(reg.h(), 4));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET4_L = new Operation(0xE5, "SET 4,L", cbmap, (args) => {
            reg.l(setBit(reg.l(), 4));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET4_HL = new Operation(0xE6, "SET 4,(HL)", cbmap, (args) => {
            mem.WriteByte( reg.hl(), setBit(mem.ReadByte(reg.hl()), 4));
            
            clock.m(4);
            clock.t(16);
        });
        
        //Set bit b in register r
        Operation SET5_A = new Operation(0xEF, "SET 5,A", cbmap, (args) => {
            reg.a(setBit(reg.a(), 5));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET5_B = new Operation(0xE8, "SET 5,B", cbmap, (args) => {
            reg.b( setBit(reg.b(), 5));
            
            clock.m(2);
            clock.t(8);
        });
    
        //Set bit b in register r
        Operation SET5_C = new Operation(0xE9, "SET 5,C", cbmap, (args) => {
            reg.c(setBit(reg.c(), 5));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET5_D = new Operation(0xEA, "SET 5,D", cbmap, (args) => {
            reg.d(setBit(reg.d(), 5));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET5_E = new Operation(0xEB, "SET 5,E", cbmap, (args) => {
            reg.e(setBit(reg.e(), 5));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET5_H = new Operation(0xEC, "SET 5,H", cbmap, (args) => {
            reg.h( setBit(reg.h(), 5));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET5_L = new Operation(0xED, "SET 5,L", cbmap, (args) => {
            reg.l(setBit(reg.l(), 5));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET5_HL = new Operation(0xEE, "SET 5,(HL)", cbmap, (args) => {
            mem.WriteByte( reg.hl(), setBit(mem.ReadByte(reg.hl()), 5));
            
            clock.m(4);
            clock.t(16);
        });
        
        //Set bit b in register r
        Operation SET6_A = new Operation(0xF7, "SET 6,A", cbmap, (args) => {
            reg.a(setBit(reg.a(), 6));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET6_B = new Operation(0xF0, "SET 6,B", cbmap, (args) => {
            reg.b(setBit(reg.b(), 6));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET6_C = new Operation(0xF1, "SET 6,C", cbmap, (args) => {
            reg.c(setBit(reg.c(), 6));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET6_D = new Operation(0xF2, "SET 6,D", cbmap, (args) => {
            reg.d(setBit(reg.d(), 6));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET6_E = new Operation(0xF3, "SET 6,E", cbmap, (args) => {
            reg.e(setBit(reg.e(), 6));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET6_H = new Operation(0xF4, "SET 6,H", cbmap, (args) => {
            reg.h(setBit(reg.h(), 6));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET6_L = new Operation(0xF5, "SET 6,L", cbmap, (args) => {
            reg.l(setBit(reg.l(), 6));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET6_HL = new Operation(0xF6, "SET 6,(HL)", cbmap, (args) => {
            mem.WriteByte( reg.hl(), setBit(mem.ReadByte(reg.hl()), 6));
            
            clock.m(4);
            clock.t(16);
        });
        
        //Set bit b in register r
        Operation SET7_A = new Operation(0xFF, "SET 7,A", cbmap, (args) => {
            reg.a(setBit(reg.a(), 7));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET7_B = new Operation(0xF8, "SET 7,B", cbmap, (args) => {
            reg.b(setBit(reg.b(), 7));
            
            clock.m(2);
            clock.t(8);
        });
    
        //Set bit b in register r
        Operation SET7_C = new Operation(0xF9, "SET 7,C", cbmap, (args) => {
            reg.c(setBit(reg.c(), 7));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET7_D = new Operation(0xFA, "SET 7,D", cbmap, (args) => {
            reg.d(setBit(reg.d(), 7));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET7_E = new Operation(0xFB, "SET 7,E", cbmap, (args) => {
            reg.e(setBit(reg.e(), 7));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET7_H = new Operation(0xFC, "SET 7,H", cbmap, (args) => {
            reg.h(setBit(reg.h(), 7));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET7_L = new Operation(0xFD, "SET 7,L", cbmap, (args) => {
            reg.l( setBit(reg.l(), 7));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Set bit b in register r
        Operation SET7_HL = new Operation(0xFE, "SET 7,(HL)", cbmap, (args) => {
            mem.WriteByte( reg.hl(), setBit(mem.ReadByte(reg.hl()), 7));
            
            clock.m(4);
            clock.t(16);
        });
        
        
        //Reset the bit b in register r
        Operation RES0_A = new Operation(0x87, "RES 0,A", cbmap, (args) => {
            //Reset value
            reg.a(resetBit(reg.a(), 0));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES0_B = new Operation(0x80, "RES 0,B", cbmap, (args) => {
            //Reset value
            reg.b(resetBit(reg.b(), 0));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES0_C = new Operation(0x81, "RES 0,C", cbmap, (args) => {
            //Reset value
            reg.c(resetBit(reg.c(), 0));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES0_D = new Operation(0x82, "RES 0,D", cbmap, (args) => {
            //Reset value
            reg.d(resetBit(reg.d(), 0));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES0_E = new Operation(0x83, "RES 0,E", cbmap, (args) => {
            //Reset value
            reg.e(resetBit(reg.e(), 0));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES0_H = new Operation(0x84, "RES 0,H", cbmap, (args) => {
            //Reset value
            reg.h(resetBit(reg.h(), 0));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES0_L = new Operation(0x85, "RES 0,L", cbmap, (args) => {
            //Reset value
            reg.l(resetBit(reg.l(), 0));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES0_HL = new Operation(0x86, "RES 0,(HL)", cbmap, (args) => {
            //Reset value
            mem.WriteByte( reg.hl(), resetBit(mem.ReadByte(reg.hl()), 0) );
            
            clock.m(4);
            clock.t(16);
        });
        
        //Reset the bit b in register r
        Operation RES1_A = new Operation(0x8F, "RES 1,A", cbmap, (args) => {
            //Reset value
            reg.a( resetBit(reg.a(), 1));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES1_B = new Operation(0x88, "RES 1,B", cbmap, (args) => {
            //Reset value
            reg.b( resetBit(reg.b(), 1));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES1_C = new Operation(0x89, "RES 1,C", cbmap, (args) => {
            //Reset value
            reg.c(resetBit(reg.c(), 1));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES1_D = new Operation(0x8A, "RES 1,D", cbmap, (args) => {
            //Reset value
            reg.d(resetBit(reg.d(), 1));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES1_E = new Operation(0x8B, "RES 1,E", cbmap, (args) => {
            //Reset value
            reg.e( resetBit(reg.e(), 1));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES1_H = new Operation(0x8C, "RES 1,H", cbmap, (args) => {
            //Reset value
            reg.h(resetBit(reg.h(), 1));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES1_L = new Operation(0x8D, "RES 1,L", cbmap, (args) => {
            //Reset value
            reg.l(resetBit(reg.l(), 1));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES1_HL = new Operation(0x8E, "RES 1,(HL)", cbmap, (args) => {
            //Reset value
            mem.WriteByte( reg.hl(), resetBit(mem.ReadByte(reg.hl()), 1));
            
            clock.m(4);
            clock.t(16);
        });
        
        //Reset the bit b in register r
        Operation RES2_A = new Operation(0x97, "RES 2,A", cbmap, (args) => {
            //Reset value
            reg.a(resetBit(reg.a(), 2));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES2_B = new Operation(0x90, "RES 2,B", cbmap, (args) => {
            //Reset value
            reg.b(resetBit(reg.b(), 2));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES2_C = new Operation(0x91, "RES 2,C", cbmap, (args) => {
            //Reset value
            reg.c( resetBit(reg.c(), 2));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES2_D = new Operation(0x92, "RES 2,D", cbmap, (args) => {
            //Reset value
            reg.d(resetBit(reg.d(), 2));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES2_E = new Operation(0x93, "RES 2,E", cbmap, (args) => {
            //Reset value
            reg.e(resetBit(reg.e(), 2));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES2_H = new Operation(0x94, "RES 2,H", cbmap, (args) => {
            //Reset value
            reg.h(resetBit(reg.h(), 2));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES2_L = new Operation(0x95, "RES 2,L", cbmap, (args) => {
            //Reset value
            reg.l(resetBit(reg.l(), 2));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES2_HL = new Operation(0x96, "RES 2,(HL)", cbmap, (args) => {
            //Reset value
            mem.WriteByte( reg.hl(), resetBit(mem.ReadByte(reg.hl()), 2));
            
            clock.m(4);
            clock.t(16);
        });
        
        //Reset the bit b in register r
        Operation RES3_A = new Operation(0x9F, "RES 3,A", cbmap, (args) => {
            //Reset value
            reg.a(resetBit(reg.a(), 3));
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES3_B = new Operation(0x98, "RES 3,B", cbmap, (args) => {
            //Reset value
            reg.b( resetBit(reg.b(), 3) );
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES3_C = new Operation(0x99, "RES 3,C", cbmap, (args) => {
            //Reset value
            reg.c( resetBit(reg.c(), 3) );
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES3_D = new Operation(0x9A, "RES 3,D", cbmap, (args) => {
            //Reset value
            reg.d( resetBit(reg.d(), 3) );
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES3_E = new Operation(0x9B, "RES 3,E", cbmap, (args) => {
            //Reset value
            reg.e( resetBit(reg.e(), 3) );
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES3_H = new Operation(0x9C, "RES 3,H", cbmap, (args) => {
            //Reset value
            reg.h( resetBit(reg.h(), 3) );
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES3_L = new Operation(0x9D, "RES 3,L", cbmap, (args) => {
            //Reset value
            reg.l( resetBit(reg.l(), 3) );
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES3_HL = new Operation(0x9E, "RES 3,(HL)", cbmap, (args) => {
            //Reset value
            mem.WriteByte( reg.hl(), resetBit(mem.ReadByte(reg.hl()), 3));
            
            clock.m(4);
            clock.t(16);
        });
        
        //Reset the bit b in register r
        Operation RES4_A = new Operation(0xA7, "RES 4,A", cbmap, (args) => {
            //Reset value
            reg.a( resetBit(reg.a(), 4) );
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES4_B = new Operation(0xA0, "RES 4,B", cbmap, (args) => {
            //Reset value
            reg.b( resetBit(reg.b(), 4) );
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES4_C = new Operation(0xA1, "RES 4,C", cbmap, (args) => {
            //Reset value
            reg.c( resetBit(reg.c(), 4) );
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES4_D = new Operation(0xA2, "RES 4,D", cbmap, (args) => {
            //Reset value
            reg.d( resetBit(reg.d(), 4) );
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES4_E = new Operation(0xA3, "RES 4,E", cbmap, (args) => {
            //Reset value
            reg.e( resetBit(reg.e(), 4) );
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES4_H = new Operation(0xA4, "RES 4,H", cbmap, (args) => {
            //Reset value
            reg.h( resetBit(reg.h(), 4) );
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES4_L = new Operation(0xA5, "RES 4,L", cbmap, (args) => {
            //Reset value
            reg.l( resetBit(reg.l(), 4) );
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES4_HL = new Operation(0xA6, "RES 4,(HL)", cbmap, (args) => {
            //Reset value
            mem.WriteByte( reg.hl(), resetBit(mem.ReadByte(reg.hl()), 4) );
            
            clock.m(4);
            clock.t(16);
        });
        
        //Reset the bit b in register r
        Operation RES5_A = new Operation(0xAF, "RES 5,A", cbmap, (args) => {
            //Reset value
            reg.a( resetBit(reg.a(), 5) );
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES5_B = new Operation(0xA8, "RES 5,B", cbmap, (args) => {
            //Reset value
            reg.b( resetBit(reg.b(), 5) );
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES5_C = new Operation(0xA9, "RES 5,C", cbmap, (args) => {
            //Reset value
            reg.c( resetBit(reg.c(), 5) );
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES5_D = new Operation(0xAA, "RES 5,D", cbmap, (args) => {
            //Reset value
            reg.d( resetBit(reg.d(), 5) );
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES5_E = new Operation(0xAB, "RES 5,E", cbmap, (args) => {
            //Reset value
            reg.e( resetBit(reg.e(), 5) );
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES5_H = new Operation(0xAC, "RES 5,H", cbmap, (args) => {
            //Reset value
            reg.h( resetBit(reg.h(), 5) );
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES5_L = new Operation(0xAD, "RES 5,L", cbmap, (args) => {
            //Reset value
            reg.l( resetBit(reg.l(), 5) );
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES5_HL = new Operation(0xAE, "RES 5,(HL)", cbmap, (args) => {
            //Reset value
            mem.WriteByte( reg.hl(), resetBit(mem.ReadByte(reg.hl()), 5) );
            
            clock.m(4);
            clock.t(16);
        });
        
        //Reset the bit b in register r
        Operation RES6_A = new Operation(0xB7, "RES 6,A", cbmap, (args) => {
            //Reset value
            reg.a( resetBit(reg.a(), 6) );
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES6_B = new Operation(0xB0, "RES 6,B", cbmap, (args) => {
            //Reset value
            reg.b( resetBit(reg.b(), 6) );
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES6_C = new Operation(0xB1, "RES 6,C", cbmap, (args) => {
            //Reset value
            reg.c( resetBit(reg.c(), 6) );
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES6_D = new Operation(0xB2, "RES 6,D", cbmap, (args) => {
            //Reset value
            reg.d( resetBit(reg.d(), 6) );
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES6_E = new Operation(0xB3, "RES 6,E", cbmap, (args) => {
            //Reset value
            reg.e( resetBit(reg.e(), 6) );
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES6_H = new Operation(0xB4, "RES 6,H", cbmap, (args) => {
            //Reset value
            reg.h( resetBit(reg.h(), 6) );
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES6_L = new Operation(0xB5, "RES 6,L", cbmap, (args) => {
            //Reset value
            reg.l( resetBit(reg.l(), 6) );
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES6_HL = new Operation(0xB6, "RES 6,(HL)", cbmap, (args) => {
            //Reset value
            mem.WriteByte( reg.hl(), resetBit(mem.ReadByte(reg.hl()), 6) );
            
            clock.m(4);
            clock.t(16);
        });
        
        //Reset the bit b in register r
        Operation RES7_A = new Operation(0xBF, "RES 7,A", cbmap, (args) => {
            //Reset value
            reg.a( resetBit(reg.a(), 7) );
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES7_B = new Operation(0xB8, "RES 7,B", cbmap, (args) => {
            //Reset value
            reg.b( resetBit(reg.b(), 7) );
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES7_C = new Operation(0xB9, "RES 7,C", cbmap, (args) => {
            //Reset value
            reg.c( resetBit(reg.c(), 7) );
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES7_D = new Operation(0xBA, "RES 7,D", cbmap, (args) => {
            //Reset value
            reg.d( resetBit(reg.d(), 7) );
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES7_E = new Operation(0xBB, "RES 7,E", cbmap, (args) => {
            //Reset value
            reg.e( resetBit(reg.e(), 7) );
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES7_H = new Operation(0xBC, "RES 7,H", cbmap, (args) => {
            //Reset value
            reg.h( resetBit(reg.h(), 7) );
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES7_L = new Operation(0xBD, "RES 7,L", cbmap, (args) => {
            //Reset value
            reg.l( resetBit(reg.l(), 7) );
            
            clock.m(2);
            clock.t(8);
        });
        
        //Reset the bit b in register r
        Operation RES7_HL = new Operation(0xBE, "RES 7,(HL)", cbmap, (args) => {
            //Reset value
            mem.WriteByte( reg.hl(), resetBit(mem.ReadByte(reg.hl()), 7) );
            
            clock.m(4);
            clock.t(16);
        });
    }

    ///
    // System Specific Interrupt handlers
    ///
    /*
    public Operation RST_40h = new Operation(0xFF1, "RST 40H", null, (args) => {
        rst(0x40);
    });
    
    public Operation RST_48h = new Operation(0xFF2, "RST 48H", null, (args) => {
        rst(0x48);
    });
    
    public Operation RST_50h = new Operation(0xFF3, "RST 50H", null, (args) => {
        rst(0x50);
    });
    
    public Operation RST_58h = new Operation(0xFF4, "RST 58H", null, (args) => {
        rst(0x58);
    });
    
    public Operation RST_60h = new Operation(0xFF5, "RST 60H", null, (args) => {
        rst(0x60);
    });*/
}