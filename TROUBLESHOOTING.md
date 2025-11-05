# PrettyLogcat 故障排除指南

## 常见问题及解决方案

### 1. 构建问题

#### 问题：.NET SDK 版本不兼容
```
error NETSDK1045: 当前 .NET SDK 不支持将 .NET 7.0 设置为目标
```

**解决方案：**
- 安装 .NET 7.0 SDK 或更高版本
- 下载地址：https://dotnet.microsoft.com/download
- 验证安装：`dotnet --version`

#### 问题：NuGet 包还原失败
```
error NU1101: 无法找到包
```

**解决方案：**
```bash
dotnet restore
dotnet clean
dotnet build
```

### 2. 运行时问题

#### 问题：应用程序无法启动
**可能原因：**
1. .NET Runtime 未安装
2. 依赖项缺失
3. 权限问题

**解决方案：**
1. 安装 .NET 7.0 Runtime
2. 以管理员身份运行
3. 检查防病毒软件是否阻止

#### 问题：Material Design 主题加载失败
```
XamlParseException: 无法找到资源
```

**解决方案：**
- 确保 MaterialDesignThemes 包正确安装
- 重新构建项目：`dotnet clean && dotnet build`

### 3. ADB 相关问题

#### 问题：ADB 未找到
```
ADB not found. Please install Android SDK Platform Tools.
```

**解决方案：**
1. 安装 Android SDK Platform Tools
2. 将 ADB 路径添加到系统 PATH
3. 常见 ADB 路径：
   - `C:\Android\Sdk\platform-tools\`
   - `C:\Program Files (x86)\Android\android-sdk\platform-tools\`

#### 问题：设备未检测到
**解决方案：**
1. 启用 USB 调试模式
2. 接受 USB 调试授权
3. 检查 USB 驱动程序
4. 尝试不同的 USB 端口/线缆
5. 运行 `adb devices` 验证连接

### 4. 设备连接问题

#### 问题：设备显示为 "unauthorized"
**解决方案：**
1. 在设备上接受 USB 调试授权
2. 重新连接设备
3. 重启 ADB 服务：
   ```bash
   adb kill-server
   adb start-server
   ```

#### 问题：设备显示为 "offline"
**解决方案：**
1. 重新连接 USB 线缆
2. 重启设备
3. 重启 ADB 服务
4. 检查 USB 驱动程序

### 5. 日志显示问题

#### 问题：日志不显示或显示不完整
**可能原因：**
1. 过滤器设置过于严格
2. 设备日志缓冲区为空
3. 权限问题

**解决方案：**
1. 重置所有过滤器
2. 清空并重新开始日志收集
3. 检查设备是否有日志输出

#### 问题：应用程序卡顿或无响应
**解决方案：**
1. 减少显示的日志级别
2. 使用更具体的过滤器
3. 定期清空日志显示
4. 重启应用程序

### 6. 性能问题

#### 问题：内存使用过高
**解决方案：**
1. 定期清空日志缓存
2. 使用过滤器减少显示的日志数量
3. 关闭不需要的日志级别
4. 重启应用程序

#### 问题：UI 响应缓慢
**解决方案：**
1. 减少同时显示的日志数量
2. 使用更严格的过滤条件
3. 关闭自动滚动功能
4. 检查系统资源使用情况

### 7. 文件操作问题

#### 问题：无法保存日志文件
**可能原因：**
1. 权限不足
2. 磁盘空间不足
3. 文件路径无效

**解决方案：**
1. 选择有写入权限的目录
2. 检查磁盘空间
3. 使用简单的文件名和路径

#### 问题：无法加载日志文件
**解决方案：**
1. 检查文件格式是否正确
2. 确保文件未被其他程序占用
3. 尝试使用不同的文件

## 调试技巧

### 1. 启用详细日志
在应用程序启动时，会在控制台显示详细的调试信息。

### 2. 检查 ADB 连接
```bash
# 检查 ADB 版本
adb version

# 列出连接的设备
adb devices

# 测试 logcat 连接
adb logcat -d | head -10
```

### 3. 手动测试 logcat
```bash
# 清空日志缓冲区
adb logcat -c

# 显示实时日志
adb logcat

# 显示特定级别的日志
adb logcat *:E
```

## 联系支持

如果以上解决方案都无法解决您的问题，请：

1. 收集错误信息和日志
2. 记录重现步骤
3. 提供系统环境信息：
   - 操作系统版本
   - .NET 版本
   - ADB 版本
   - 设备型号和 Android 版本

## 系统要求

### 最低要求
- Windows 10 或更高版本
- .NET 7.0 Runtime
- 2GB RAM
- 100MB 可用磁盘空间

### 推荐配置
- Windows 11
- .NET 7.0 SDK
- 4GB RAM 或更多
- SSD 存储
- 多核处理器

### Android 设备要求
- Android 4.1 (API 16) 或更高版本
- 启用开发者选项
- 启用 USB 调试
- 安装适当的 USB 驱动程序