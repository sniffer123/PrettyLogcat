@echo off
echo Testing PrettyLogcat fixes...
echo.
echo Starting application...
cd /d "g:\UGit\zkhuang\PrettyLogcat"
dotnet run --project PrettyLogcat
echo.
echo Application closed. Check if there were any errors above.
pause