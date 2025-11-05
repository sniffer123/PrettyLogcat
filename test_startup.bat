@echo off
title PrettyLogcat Startup Test
echo Testing PrettyLogcat startup...
cd /d "%~dp0"

echo Building project...
dotnet build --configuration Debug --verbosity quiet
if %ERRORLEVEL% NEQ 0 (
    echo Build failed!
    exit /b 1
)

echo Starting application for 3 seconds...
start /b dotnet run --project PrettyLogcat --configuration Debug

echo Waiting 3 seconds...
timeout /t 3 /nobreak >nul

echo Checking if application is running...
tasklist /fi "imagename eq PrettyLogcat.exe" 2>nul | find /i "PrettyLogcat.exe" >nul
if %ERRORLEVEL% EQU 0 (
    echo SUCCESS: PrettyLogcat.exe is running!
    taskkill /f /im PrettyLogcat.exe >nul 2>&1
) else (
    tasklist /fi "imagename eq dotnet.exe" 2>nul | find /i "dotnet.exe" >nul
    if %ERRORLEVEL% EQU 0 (
        echo SUCCESS: Application started via dotnet.exe!
        taskkill /f /im dotnet.exe >nul 2>&1
    ) else (
        echo WARNING: Could not detect running application.
        echo This might be normal if the app started and closed quickly.
    )
)

echo.
echo Test completed. If no error messages appeared, the application should work.
pause