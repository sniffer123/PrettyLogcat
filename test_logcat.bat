@echo off
echo Testing logcat functionality...

REM 检查是否有设备连接
adb devices

REM 清空logcat缓冲区
adb logcat -c

REM 生成一些测试日志
adb shell log -t "TestApp" "Test log message 1 - Info level"
adb shell log -t "TestApp" -p i "Test log message 2 - Info level"
adb shell log -t "TestApp" -p d "Test log message 3 - Debug level"
adb shell log -t "TestApp" -p w "Test log message 4 - Warning level"
adb shell log -t "TestApp" -p e "Test log message 5 - Error level"

echo Test logs generated. Check PrettyLogcat application.