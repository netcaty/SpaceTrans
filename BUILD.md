# Development Guide

## Build Commands

### Quick Commands
```bash
# Standard clean (bin, obj folders)
dotnet clean

# Build and package release (one command)
dotnet msbuild -c Release -t:ReleaseCli
dotnet msbuild -c Release -t:ReleaseTray

# Standard publish with auto-packaging
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Output Structure
```
dist/
├── win-x64/
│   └── SpaceTrans.exe          # Main executable
├── package/                    # Temporary packaging folder
│   ├── SpaceTrans.exe
│   ├── README.md
│   └── config-sample.json

releases/
└── SpaceTrans-v1.0.0-win-x64.zip    # Final release package
```

## Development Workflow

1. **Standard Clean**
   ```bash
   dotnet clean
   ```

2. **Debug Build**
   ```bash
   dotnet build
   ```

3. **Test Run**
   ```bash
   dotnet run
   ```

4. **Release Build**
   ```bash
   dotnet msbuild -t:ReleaseCli
   dotnet msbuild -t:ReleaseTray
   dotnet msbuild -t:ReleaseTray-net8
   ```

## MSBuild Targets

- `CustomClean`: Auto-triggered with dotnet clean to remove dist/releases
- `Release`: Build and package in one step  
- `Package`: Create ZIP after publish (auto-triggered)

## Version Management

Update version in `SpaceTrans.csproj`:
```xml
<Version>1.0.0</Version>
```

This version is automatically used in:
- Assembly metadata
- Package filenames
- Release notes