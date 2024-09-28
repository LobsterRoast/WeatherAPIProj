# Weather API Project
## For Grade 12 Computer Science
This is a weather API interface written in C# and XAML. It uses the Avalonia platform for compatibility across operating systems, and uses ipinfo.io as well as Open-Meteo as the API's it depends on. It also uses [ScottPlot](https://scottplot.net/) in order to display data in a visual format.

*Note for teacher: Much of the project is just a template. See **MainWindow.axaml.cs** and **MainWindow.axaml** for the stuff I coded personally.*

## Compatibility
Works on Windows and Linux. Developed specifically on/for Fedora Linux.

## Dependencies
- .NET 8.0
-   Earlier .NET versions work too, but WeatherAPIProj.csproj needs to be updated to target earlier .NET versions.

## Installation
### Linux
```bash
./build_linux.sh
cd ./build
dotnet WeatherAPIProj.dll
```
### Windows
```shell
# This can all be done in File Explorer too
./build_win.bat
cd ./build
./WeatherAPIProj.exe
```


**Note:** This targets  .NET 8.0 by default. You may need to edit WeatherAPIProj.csproj to target a different version of .NET depending on what works for you and your environment.

## Resources used:
- [Avalonia](https://avaloniaui.net/)
- [ScottPlot](https://scottplot.net/)
- [ipinfo.io](https://ipinfo.io/)
- [Open-Meteo](https://open-meteo.com/)
- [fluentui-emoji](https://github.com/microsoft/fluentui-emoji)
