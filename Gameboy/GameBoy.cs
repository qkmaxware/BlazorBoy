using System.Diagnostics;
using Qkmaxware.Emulators.Gameboy.Hardware;

namespace Qkmaxware.Emulators.Gameboy;

public class Gameboy {

    public Cpu CPU {get; init;}
    public Gpu GPU {get; init;}
    private MemoryMap mmu {get; init;}
    public Input Input {get; init;}
    private Hardware.Timer timer {get; init;}
    private CartridgeAdapter cart {get; init;}
    public SerialConnection Serial {get; init;}

    public Gameboy() {
        mmu = new MemoryMap();

        Hardrive onboard = new Hardrive();
        GPU = new Gpu();
        Input = new Input();
        timer = new Hardware.Timer();
        cart = new CartridgeAdapter();
        SerialConnection sysio = new SerialConnection(null, null);
        this.Serial = sysio;

        mmu.SetMappedComponent(MemoryMap.INTERNAL_RAM, onboard);
        mmu.SetMappedComponent(MemoryMap.ZRAM, onboard);
        
        mmu.SetMappedComponent(MemoryMap.JOYSTICK, Input);
        mmu.SetMappedComponent(MemoryMap.TIMER, timer);
        
        mmu.SetMappedComponent(MemoryMap.OAM, GPU);
        mmu.SetMappedComponent(MemoryMap.VRAM, GPU);
        mmu.SetMappedComponent(MemoryMap.GPU, GPU);
        
        mmu.SetMappedComponent(MemoryMap.ROM_BANK_0, cart);
        mmu.SetMappedComponent(MemoryMap.ROM_BANK_1, cart);
        mmu.SetMappedComponent(MemoryMap.EXTERNAL_RAM, cart);
        
        mmu.SetMappedComponent(MemoryMap.SERIALIO, sysio);
        
        CPU = new Cpu(mmu);
    }

    public void Reset(){
        mmu.Reset();
        GPU.Reset();
        Input.Reset();
        timer.Reset();
        cart.Reset();
        CPU.Reset();
    }

    public Cartridge? GetCartridge() {
        return this.cart.LoadedCart();
    }

    public void LoadCartridge(Cartridge cart){
        Reset();
        this.cart.LoadCart(cart);
    }

    public bool IsCartridgeLoaded() => this.cart.HasCart();

    public void Dispatch(){
        //Step the cpu
        #if DEBUG
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        #endif
        int deltaTime = CPU.Step();
        #if DEBUG
        stopwatch.Stop();
        PerformanceAnalyzer.Record(CPU, stopwatch.Elapsed);
        #endif

        //Step the gpu
        #if DEBUG
        stopwatch = new Stopwatch();
        stopwatch.Start();
        #endif
        GPU.Step(deltaTime);
        #if DEBUG
        stopwatch.Stop();
        PerformanceAnalyzer.Record(GPU, stopwatch.Elapsed);
        #endif
        
        //Step the timer
        timer.Increment(deltaTime);
    }

    public void DispatchUntilBufferFlush() {
        bool hasVBlanked = false;
        while (!hasVBlanked) {
            Dispatch();
            if (GPU.HasBufferJustFlushed) {
                hasVBlanked = true;
                break;
            }
        }
    }
}