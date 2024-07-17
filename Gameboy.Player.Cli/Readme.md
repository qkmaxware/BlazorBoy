# CLI Player
CLI front-end for BlazorBoy. 

## Installation
1. Clone or Download repo 
2. Using dotnet 8 or newer run the following commands
   1. dotnet pack Gameboy.Player.Cli.csproj
   2. dotnet tool install --global --add-source ./nupkg Blazorboy.Cli

## Uninstall
1. Using dotnet cli run the following commands
   1. dotnet tool uninstall --global Blazorboy.Cli