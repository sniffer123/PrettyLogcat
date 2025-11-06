@echo off
echo Testing multi-line log functionality...

REM 检查是否有设备连接
adb devices

REM 清空logcat缓冲区
adb logcat -c

REM 生成一些测试日志，包括多行日志
echo Generating single line log...
adb shell log -t "TestApp" "Single line log message"

echo Generating multi-line log...
adb shell "log -t 'TestApp' 'Multi-line log start'; echo '    continuation line 1' >> /dev/kmsg; echo '    continuation line 2' >> /dev/kmsg"

echo Generating another single line log...
adb shell log -t "TestApp" "Another single line log"

echo Test logs generated. Check PrettyLogcat application for multi-line merging.