using Microsoft.JSInterop;

namespace BlazorBoy.Player;

public abstract class JSInterop {

    protected IJSRuntime JavaScript; 

    public JSInterop(IJSRuntime javascript) {
        this.JavaScript = javascript;
    }
}