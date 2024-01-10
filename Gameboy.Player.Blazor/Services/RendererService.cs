using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Qkmaxware.Emulators.Gameboy.Hardware;

namespace BlazorBoy.Player;

public class RendererService : JSInterop {

    public RendererService(IJSRuntime javascript) : base(javascript) { }

    public async Task PushPixels(ElementReference? canvas, Bitmap bmp) { 
        await JavaScript.InvokeVoidAsync("GBRenderer.PushPixels", canvas, bmp.GetBytes());
    }
}