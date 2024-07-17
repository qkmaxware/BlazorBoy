<p align="center">
  <img width="120" height="120" src="Gameboy.Player.Blazor/wwwroot/title.svg">
</p>

# BlazorBoy
A simple (and buggy) Nintendo Game Boy emulator written in C# using Blazor as a front-end for rendering the game. 

Total conversion from my original pure Java version [here](https://github.com/qkmaxware/GBemu). Hopefully it's a little better organized, optimized, and accurate compared to the original. 

## Front-Ends 
Front-ends are applications that implement the rendering/io for BlazorBoy. Front-ends support different environments such as for desktop, mobile, or the web. Additionally, each front-end will have differing levels of performance and may or may not support all console features. These restictions are often dictated by the restrictions of the environment the front-end is designed to run on (eg. the CLI rendering being slow given how slow writing large chunks of text to the console can be). 

The following table shows each front-end inluded in this repository and the hardware features that they support. 

| Front-End | Download | Speed | Display | Input | Sound | Serial |
|-----------|----------|-------|---------|-------|-------|--------|
| CLI       | [nuget](https://github.com/qkmaxware/BlazorBoy/pkgs/nuget/Blazorboy.Cli) | SLOW 		| Mono/Symbolic ASCII	| Keyboard<sup>(1)</sup> 	| -- | -- |
| Blazor    | [link](https://qkmaxware.github.io/BlazorBoy/) 					| MEDIUM 	| Mono    				| Touch/Keyboard 			| -- | -- |
| Godot     | [exe](https://github.com/qkmaxware/BlazorBoy/releases/latest) 	| FAST 		| Colour 				| Touch/Keyboard/Gamepad 	| -- | -- | 

- <small>(1) Can only press 1 button at a time.</small>

## Folder Structure
### Gameboy
The actual code for the hardware emulation of the console. Doesn't include any hardware implementations. Console features like the screen, input, sounds, and more are specific to the run-time environment and as such are left without some implementation details that front-ends need to map to features from the rum-time environment. IE this won't just work out of the box as nothing will be drawn, heard, or interacted with.

### Gameboy.Database
A small database of metadata for some popular Game Boy games. 

### Gameboy.Player.Blazor
A Blazor Web Assembly app front-end for BlazorBoy (the namesake front-end). Glue code is included to map emulated hardware features to HTML events and Canvas elements. 

### Gameboy.Player.Cli
A CLI application that serves as a front-end for BlazorBoy. Simple example of how to create your own front-end for BlazorBoy. Glue code is included to map emulated hardware features to CLI key events and character printing. 

### Gameboy.Player.Godot
A Godot 4 front-end for BlazorBoy. Glue code is inlcuded to map emulated hardware features from the emulated console to Godot Nodes/Features.

### Gameboy.Test
Automated unit tests for the emulated hardware.

### Qkmaxware.Vm.LR35902
Emulation of the CPU for the Sharp LR35902 processor. Kept separately from the rest of the project in case it gets used somewhere else in the future.