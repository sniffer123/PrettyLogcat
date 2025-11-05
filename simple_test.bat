@echo off
echo Simple startup test...
cd /d "%~dp0"

echo Building...
dotnet build --configuration Debug

echo Running with console output...
dotnet run --project PrettyLogcat --configuration Debug --no-build

pause