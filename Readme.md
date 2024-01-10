<p align="center">
  <img width="120" height="120" src="Gameboy.Player.Blazor/wwwroot/title.svg">
</p>

# BlazorBoy
A simple (and buggy) Nintendo Game Boy emulator written in C# using Blazor as a front-end for rendering the game. 

Total conversion from my original pure Java version [here](https://github.com/qkmaxware/GBemu).

## Folder Structure
### Qkmaxware.Vm.LR35902
Emulation of the CPU for the Sharp LR35902 processor. Kept separately from the rest of the project in case it gets used somewhere else in the future.

### Gameboy
The actual code for the hardware emulation of the console. Doesn't include any rendering/drawing to the screen.

### Gameboy.Test
Automated unit tests for the emulated hardware.

### Gameboy.Player.Blazor
The main renderer for the emulated hardware. A Blazor app which runs the emulated console and displays the graphics as well as handles user input. Runs entirely in the browser thanks to WebAssembly.

### Gameboy.Player.Cli
An example renderer that prints the images to the terminal's console. Doesn't support any input.

### Gameboy.Player.Godot
A simple Godot 4 project running the emulated hardware. 