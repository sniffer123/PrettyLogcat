@echo off
echo Testing auto-connect functionality...

REM 检查设备状态
echo Current device status:
adb devices

REM 生成一些测试日志来验证连接
echo Generating test logs...
adb shell log -t "AutoConnectTest" "Test message 1 - Auto connect verification"
adb shell log -t "AutoConnectTest" "Test message 2 - Column visibility test"
adb shell log -t "AutoConnectTest" "Test message 3 - Multi-line start"
adb shell "echo '    Multi-line continuation' | log -t 'AutoConnectTest'"

echo Test completed. Check PrettyLogcat:
echo 1. Should auto-connect on startup
echo 2. Right-click column headers to test column visibility
echo 3. Should show multi-line logs properly merged