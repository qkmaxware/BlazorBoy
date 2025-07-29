using Qkmaxware.Vm.LR35902;

namespace Qkmaxware.Emulators.Gameboy.Hardware;

public abstract class Channel {
    // Raw byte registers
    private int _NRX1;
    private int _NRX2;
    private int _NRX3;
    private int _NRX0;
    private int _NRX4;

    private bool _enabled;
    public bool IsEnabled {
        get => _enabled && IsDacEnabled;
        set => _enabled = value;
    }

    public int NRX0 {
        get => this._NRX0;
        set {
            var prior = this._NRX0;
            var update = value & 0b1111_1111;
            this._NRX0 = update;
            OnChangeNRX0(prior, update);
        }
    }
    protected virtual void OnChangeNRX0(int old, int update) {}
    public int NRX1 {
        get => this._NRX1;
        set {
            var prior = this._NRX1;
            var update = value & 0b1111_1111;
            this._NRX1 = update;
            OnChangeNRX1(prior, update);
        }
    }
    protected virtual void OnChangeNRX1(int old, int update) {}
    public int NRX2 {
        get => this._NRX2;
        set {
            var prior = this._NRX2;
            var update = value & 0b1111_1111;
            this._NRX2 = update;
            OnChangeNRX2(prior, update);
        }
    }
    protected virtual void OnChangeNRX2(int old, int update) {}
    public int NRX3 {
        get => this._NRX3;
        set {
            var prior = this._NRX3;
            var update = value & 0b1111_1111;
            this._NRX3 = update;
            OnChangeNRX3(prior, update);
        }
    }
    protected virtual void OnChangeNRX3(int old, int update) {}
    public int NRX4 {
        get => this._NRX4;
        set {
            var prior = this._NRX4;
            var update = value & 0b1111_1111;
            this._NRX4 = update;
            OnChangeNRX4(prior, update);
        }
    }
    protected virtual void OnChangeNRX4(int old, int update) {
        var byteV = update;

        // If triggered
        if ((byteV & 0b1000_0000) != 0) {
            // Channel is enabled
            this._enabled = true;
            
            // If the length is 0, reload with max length
            if (this.LengthLoad == 0) {
                this.LengthLoad = this.LengthMax;
            }
            this.LengthTimer = this.LengthMax - this.LengthLoad;
            
            // Frequency timer is loaded with period
            
            // Volume envelope timer is loaded with period
            
            // Channel volume is loaded with NRx2

            // Channel specific things
            this.OnTrigger();

            // If the DAC is off then channel is immediately disabled
            if (!this.IsDacEnabled) {
                this._enabled = false;
            }
        }
    }

    // Register Decoders
    public virtual bool IsDacEnabled => (NRX2 & 0b1111_1000) != 0; 
    public virtual bool LengthEnable => (NRX4 & 0b0100_0000) != 0;
    public virtual int LengthLoad {
        get {
            return NRX1 & 0b0011_1111;
        }
        set {
            NRX1 = (NRX1 & 0b1100_0000) | (value & 0b0011_1111);
        }
    }
    public int LengthTimer {get; set;}
    public virtual int LengthMax => 0b0011_1111;

    // Common functions
    public virtual void Reset() {
        this.NRX0 = 0;
        this.NRX1 = 0;
        this.NRX2 = 0;
        this.NRX3 = 0;
        this.NRX4 = 0;
        this.LengthTimer = LengthMax;
    }

    public virtual void OnTrigger() { }

    public virtual void ClockLength(int fsStep) {
        var len = LengthTimer;
        if (LengthEnable && len > 0) {
            var nextLen = len - 1;
            if (nextLen == 0) {
                _enabled = false;
            }
            LengthTimer = nextLen;
        }
    }

    public virtual void ClockSweep(int fsStep) { } 

    public virtual void ClockVolumeEnvelope(int fsStep) { }

    public abstract void Tick(ref ClockDelta dt);
    public abstract void MixInSamples(float playbackFreq, float leftVolume, float rightVolume, Span<Sample> samples);
}