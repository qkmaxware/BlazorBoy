using Microsoft.JSInterop;

namespace BlazorBoy.Player;

public class AlertService : JSInterop {

    public AlertService(IJSRuntime javascript) : base(javascript) { }

    public async Task Alert(string message) {
        await JavaScript.InvokeVoidAsync("alert", message);
    }
}