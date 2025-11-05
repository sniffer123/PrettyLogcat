@echo off
echo Building PrettyLogcat...
cd /d "%~dp0"
dotnet build
if %ERRORLEVEL% EQU 0 (
    echo Build successful!
) else (
    echo Build failed!
)
pause