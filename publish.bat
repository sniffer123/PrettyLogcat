@echo off
title PrettyLogcat Publisher
echo ========================================
echo    PrettyLogcat - Publishing Application
echo ========================================
echo.

cd /d "%~dp0"

echo Cleaning previous builds...
dotnet clean --configuration Release
if exist "publish" rmdir /s /q "publish"

echo.
echo Building and publishing application...
dotnet publish PrettyLogcat -c Release -o publish --self-contained false --runtime win-x64

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo    Publication Successful!
    echo ========================================
    echo.
    echo Published files are in: %~dp0publish\
    echo.
    echo To run the published application:
    echo   cd publish
    echo   PrettyLogcat.exe
    echo.
    echo To create a portable version:
    echo   Copy the 'publish' folder to any location
    echo   Make sure .NET 7.0 Runtime is installed on target machine
    echo.
) else (
    echo.
    echo ========================================
    echo    Publication Failed!
    echo ========================================
    echo Please check the error messages above.
)

pause