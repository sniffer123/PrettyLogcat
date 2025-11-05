@echo off
echo Starting PrettyLogcat in debug mode...
cd /d "%~dp0"

echo Building project...
dotnet build --configuration Debug
if %ERRORLEVEL% NEQ 0 (
    echo Build failed!
    pause
    exit /b 1
)

echo Starting application with verbose output...
dotnet run --project PrettyLogcat --configuration Debug --verbosity normal

echo Application exited with code: %ERRORLEVEL%
pause