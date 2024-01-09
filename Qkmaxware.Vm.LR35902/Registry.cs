namespace Qkmaxware.Vm.LR35902;

public class Registry : IResetable {

    private static readonly int BITMASK_8BIT = 0xFF;
    private static readonly int BITMASK_16BIT = 0xFFFF;

    //Internal registry values --------------------------------------------
    private int registerA, registerB, registerC, registerD, registerE;
    private int flags;
    private int registerHigh, registerLow;
    private int registerStackPointer, registerProgramCounter;
    private int registerInterrupt = 1;
    
    //Manipulators --------------------------------------------------------
    public void Reset(){
        registerA = 0; registerB = 0; registerC = 0; registerD = 0; registerE = 0;
        flags = 0;
        registerHigh = 0; registerLow = 0;
        registerStackPointer = 0; registerProgramCounter = 0;
        registerInterrupt = 1;
    }
    
    //Accessors -----------------------------------------------------------
    //8BIT ----------------------------------------------------------------
    public int ime(){
        return registerInterrupt;
    }
    
    public void ime(int i){
        registerInterrupt = i & BITMASK_8BIT;
    }
    
    public int a() {
        return registerA;
    }

    public void a(int i) {
        registerA = i & BITMASK_8BIT;
    }

    public int b() {
        return registerB;
    }

    public void b(int i) {
        registerB = i & BITMASK_8BIT;
    }
    
    public int c() {
        return registerC;
    }
    
    public void c(int i) {
        registerC = i & BITMASK_8BIT;
    }

    public int d() {
        return registerD;
    }

    public void d(int i) {
        registerD = i & BITMASK_8BIT;
    }
    
    public int e() {
        return registerE;
    }

    public void e(int i) {
        registerE = i & BITMASK_8BIT;
    }
    
    public int f() {
        return flags;
    }

    public void f(int i) {
        //Last 4 digits are always 0 even if one is written to
        flags = (i & BITMASK_8BIT) & 0b11110000;
    }
    
    /*
    FLAG REGISTER BITS
    7     6	5	4	3	2	1	0
    Z	  N	H	C	0	0	0	0
    */
    public void clearFlags(){
        flags = 0;
    }
    
    public bool zero(){
        return (flags & 0b10000000) == 0b10000000;
    }
    
    public void zero(bool b){
        if(b)
            flags |= 0b10000000;
        else 
            flags &= 0b01111111;
    }

    public bool subtract(){
        return (flags & 0b01000000) == 0b01000000;
    }
    
    public void subtract(bool b){
        if(b)
            flags |= 0b01000000;
        else 
            flags &= 0b10111111;
    }
    
    public bool halfcarry(){
        return (flags & 0b00100000) == 0b00100000;
    }
    
    public void halfcarry(bool b){
        if(b)
            flags |= 0b00100000;
        else 
            flags &= 0b11011111;
    }
    
    public bool carry(){
        return (flags & 0b00010000) == 0b00010000;
    }
    
    public void carry(bool b){
        if(b)
            flags |= 0b00010000;
        else 
            flags &= 0b11101111;
    }
    
    public int h() {
        return registerHigh;
    }

    public void h(int i) {
        registerHigh = i & BITMASK_8BIT;
    }
    
    public int l() {
        return registerLow;
    }
    
    public void l(int i) {
        registerLow = i & BITMASK_8BIT; 
    }
    
    //16BIT ---------------------------------------------------------------
    //TODO Confirm high low extraction methods
    public int sp(){
        return registerStackPointer;
    }
    public void sp(int i){
        registerStackPointer = i & BITMASK_16BIT;
    }
    
    //Increment the sp
    public int sppp(int i){
        int k = sp();
        sp(k + i); // Differs from original implementation. Not sure if its suppose to return the old or incremented value
        return k;
    }
    
    public int pc() {
        return registerProgramCounter;
    }
    
    //Increment the pc
    public int pcpp(int i){
        int k = pc();
        pc(k + i); // Differs from original implementation. Not sure if its suppose to return the old or incremented value
        return k;
    }
   
    public void pc(int i){
        registerProgramCounter = i & BITMASK_16BIT;
    }

    public int af(){
        //High a, Low f
        int la = registerA << 8;
        return la | flags;
    }
    
    public void af(int i){
        //TODO the proper way to do this?
        f(i);
        a(i >> 8);
    }
    
    public int bc(){
        //High b, Low c
        int lb = registerB << 8;
        return lb | registerC;
    }
    
    public void bc(int i){
        //TODO the proper way to do this?
        c(i);
        b(i >> 8);
    }
    
    public int de(){
        //High d, Low e
        int ld = registerD << 8;
        return ld | registerE;
    }
    
    public void de(int i){
        //TODO the proper way to do this?
        e(i);
        d(i >> 8);
    }
   
    public int hl(){
        //High h, Low l
        int lh = registerHigh << 8;
        return lh | registerLow;
    }

    public void hl(int i){
        //TODO the proper way to do this?
        l(i);
        h(i >> 8);
    }

    public override string ToString() {
        return $"PC: 0x{pc():X4}, SP: 0x{sp():X4}, AF: 0x{a():X2}{f():X2}, BC: 0x{b():X2}{c():X2}, DE: 0x{d():X2}{e():X2}, HL: 0x{h():X2}{l():X2}";
    }
}