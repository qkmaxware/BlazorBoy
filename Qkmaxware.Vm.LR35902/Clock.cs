namespace Qkmaxware.Vm.LR35902;

/// <summary>
/// CPU clock 
/// </summary>
public class Clock : IResetable {
    
    private long machine; private long cycles; 
    
    private int inst_cycle;
    private int inst_machine;
    
    public void Reset(){
        machine = 0L;
        cycles = 0L;
        
        inst_cycle = 0;
        inst_machine = 0;
    }
    
    public long m(){
        return machine;
    }
    
    public void m(int i){
        inst_machine += i;
    }
    
    public int delM(){
        return this.inst_machine;
    }
    
    public int delT(){
        return this.inst_cycle;
    }
    
    public long t(){
        return cycles;
    }
    
    public void t(int i){
       inst_cycle += i;
    }
    
    public void Accept(){
        cycles = inst_cycle;
        inst_cycle = 0;
        
        machine = inst_machine;
        inst_machine = 0;
    }

    public void Reject(){
        inst_machine = 0;
        inst_cycle = 0;
    }
}
