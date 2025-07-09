# 体积分析和优化建议

## 当前CLI版本64MB的原因分析

1. **自包含运行时** (40-50MB)
   - .NET 8运行时库
   - JIT编译器
   - 垃圾收集器
   - 基础类库

2. **HTTP客户端依赖** (5-10MB)
   - System.Net.Http
   - SSL/TLS支持
   - HTTP/2支持

3. **JSON序列化** (2-5MB)
   - System.Text.Json
   - 反射元数据

## 进一步减少体积的选项

### 选项1: Framework依赖部署 (推荐)
- 设置 `SelfContained=false`
- 体积: ~5-15MB
- 缺点: 需要安装.NET 8运行时

### 选项2: 使用AOT编译
- 设置 `PublishAot=true`
- 体积: ~15-25MB  
- 缺点: 编译复杂，兼容性问题

### 选项3: 最小化HTTP依赖
- 使用原生Win32 API代替HttpClient
- 手动实现简单HTTP请求
- 可减少10-15MB

### 选项4: 移除JSON依赖
- 手动解析JSON响应
- 使用轻量级JSON库
- 可减少3-5MB

## 建议
对于CLI工具，Framework依赖部署是最佳选择：
- 体积最小
- 性能最好
- 维护简单
- .NET 8运行时现在很常见