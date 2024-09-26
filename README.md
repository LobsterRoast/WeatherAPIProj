# Weather API Project
## For Grade 12 Computer Science
This is a weather API interface written in C# and XAML. It uses the Avalonia platform for compatibility across operating systems, and uses ipinfo.io as well as Open-Meteo as the API's it depends on.

*Note for teacher: Much of the project is just a template. See **MainWindow.axaml.cs** and **MainWindow.axaml** for the stuff I coded personally.*

## Compatibility
Works on Windows and Linux. Coded specifically on/for Fedora Linux.

## Dependences
- .NET 8.0
Working on this

## Installation
Simply clone the repository and run either build_win.bat or build_linux.sh depending on your operating system. The exe/dll will be created in bin/Release/publish.
Note: This targets  .NET 8.0 by default. You may need to edit WeatherAPIProj.csproj to target a different version of .NET depending on what works for you and your environment.
