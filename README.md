# SpaceTrans .NET CLI

> [English Document README.md](README-en.md)

一个基于 .NET 的控制台与系统托盘翻译工具，支持双空格快捷键即时翻译。

## 🚀 使用演示

![翻译功能演示](docs/screen2.gif)

## 功能特性

- **全局快捷键检测**（双击空格）
- **智能内容识别自动翻译**
- **多翻译引擎支持**（有道 API、Google Gemini）
- **系统托盘集成** 带状态指示
- **跨平台支持**（主要面向 Windows）
- **图形界面配置设置**

## 系统要求

- .NET 8.0 或更高版本
- Windows（用于全局快捷键功能）

## 使用方法

### 托盘模式（默认）
```bash
SpaceTrans-Tray.exe
```
- 运行在系统托盘中
- 双击托盘图标打开设置
- 右键点击可访问菜单选项

### 控制台模式
```bash
SpaceTrans-CLI.exe
```
- 在命令行中运行并输出日志
- 适用于调试和查看实时日志信息

### 快捷翻译流程
1. 在任意输入框中打字
2. 按下 **两次空格键** 即可翻译输入的内容
3. 翻译结果将自动替换原文字

## 配置说明

### 初始设置
1. 双击托盘图标打开设置界面
2. 配置你偏好的翻译引擎：
   - **有道翻译**：需要 App Key 和 App Secret
   - **Gemini**：需要 Google API Key
3. 设置目标语言（en, zh, ja, ko, fr, de, es, ru）
4. 测试连接以验证配置是否正确

### 配置文件说明
所有设置保存在 [config.json](file:///mnt/c/Users/netcat/Desktop/YoudaoTranslator/config.json) 中，示例如下：
```json
{
  "CurrentEngine": "Gemini",
  "TargetLanguage": "en",
  "YoudaoConfig": {
    "AppKey": "your-app-key",
    "AppSecret": "your-app-secret"
  },
  "GeminiConfig": {
    "ApiKey": "your-api-key"
  }
}
```

## 构建方式

### 开发环境构建
```bash
dotnet build
dotnet run
```

### 发布版本构建
```bash
dotnet build -c Release
```

### 独立运行包构建
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
或
# CLI 版本
dotnet msbuild -t:ReleaseCli
# 托盘版本
dotnet msbuild -t:ReleaseTray
# 托盘版本（依赖 .NET 8）
dotnet msbuild -t:ReleaseTray-net8
```

## 自定义图标

请将自定义图标 [icon.ico](file:///mnt/c/Users/netcat/Desktop/YoudaoTranslator/Resources/icon.ico) 放入应用程序目录，或嵌入到资源 [Resources/icon.ico](file:///mnt/c/Users/netcat/Desktop/YoudaoTranslator/Resources/icon.ico) 中。

## 系统托盘功能

- **状态可视化**：翻译过程中图标会变化
- **静默运行**：托盘模式无弹窗通知
- **右键菜单**：快速访问所有功能
- **图形界面设置**：便于管理配置
- **自动日志记录**：所有事件都会被记录
- **日志文件访问**：右键菜单可直接打开日志文件

## 日志记录

SpaceTrans 自动记录所有操作日志，便于调试和监控：

### 日志路径
- **托盘模式**：`%LOCALAPPDATA%\SpaceTrans\app.log`
- **控制台模式**：`%LOCALAPPDATA%\SpaceTrans\console.log`

### 日志等级
- **托盘模式**：记录 Info 级别及以上
- **控制台模式**：记录 Debug 级别及以上（更详细）

### 日志功能
- **自动滚动**：当日志超过 10MB 时自动归档
- **带时间戳**：每条日志都包含精确时间
- **线程安全**：并发写入安全
- **翻译追踪**：记录详细的翻译请求与响应

### 示例日志
```
[2024-12-08 14:30:25.123] [Info] [T1] 翻译成功 [Gemini]: 'Hello world...' -> '你好世界...'
```

## 快捷键说明

- **双击空格键**：翻译当前选中文本
- **托盘菜单**：切换快捷键开关、打开日志、进入设置
- **双击托盘图标**：直接打开设置界面

## 支持的翻译引擎

### 有道 API
- 提供高质量翻译服务
- 支持多种语言对
- 需要从有道申请 API 凭证

### Google Gemini
- 基于 AI 的翻译能力
- 自然语言理解更强
- 需要 Google AI 平台 API 密钥

## 常见问题排查

### 快捷键无效
- 检查托盘菜单中是否启用了快捷键
- 查看 Windows 是否允许全局钩子权限
- 如有必要，请以管理员身份运行
- 查看日志是否有快捷键安装失败信息

### 翻译失败
- 核对设置中的 API 凭据
- 使用内置测试功能检查连接
- 检查网络连接是否正常
- 查看日志获取详细错误信息

### 配置异常
- 删除 [config.json](file:///mnt/c/Users/netcat/Desktop/YoudaoTranslator/config.json) 文件恢复默认设置
- 确保 API Key 输入正确
- 检查目标语言是否支持
- 查看日志确认配置加载是否出错

### 日志相关问题
- 日志文件存储路径为 `%LOCALAPPDATA%\SpaceTrans\`
- 超过 10MB 后自动归档
- 控制台模式提供更详细的调试信息
- 如果日志未生成，请检查文件权限

## 版本信息
- **当前版本**: 1.0.0
- **开发框架**: .NET 8.0
- **支持平台**: Windows 7.0+
