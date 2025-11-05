@echo off
title PrettyLogcat Test
echo ========================================
echo    PrettyLogcat - Quick Test
echo ========================================
echo.

cd /d "%~dp0"

echo Testing build...
dotnet build --configuration Debug --verbosity quiet
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)

echo Build successful.
echo.
echo Testing application startup (will close automatically in 5 seconds)...
echo.

timeout /t 2 /nobreak >nul

rem Start the application and kill it after 5 seconds for testing
start /b dotnet run --project PrettyLogcat --configuration Debug
timeout /t 5 /nobreak >nul
taskkill /f /im PrettyLogcat.exe >nul 2>&1
taskkill /f /im dotnet.exe >nul 2>&1

echo.
echo Test completed. If no errors appeared above, the application should work correctly.
echo.
echo To run the full application, use: run.bat
echo To publish the application, use: publish.bat
echo.
pause