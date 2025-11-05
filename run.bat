@echo off
title PrettyLogcat Launcher
echo ========================================
echo    PrettyLogcat - Android Logcat Viewer
echo ========================================
echo.

echo Checking .NET Runtime...
dotnet --version >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: .NET Runtime not found!
    echo Please install .NET 7.0 Runtime or SDK
    echo Download from: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo .NET Runtime found.
echo.

echo Building project...
cd /d "%~dp0"
dotnet build --configuration Release --verbosity quiet
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Build failed!
    echo Running detailed build to show errors...
    dotnet build --configuration Release
    pause
    exit /b 1
)

echo Build successful.
echo.

echo Starting PrettyLogcat...
echo.
echo NOTE: Make sure you have:
echo - Android SDK Platform Tools (ADB) installed
echo - Android device connected with USB Debugging enabled
echo.
echo If the application doesn't start, check for error messages.
echo.
echo Starting application...

dotnet run --project PrettyLogcat --configuration Release

echo.
echo Application closed.
pause