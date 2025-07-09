using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace SpaceTrans
{
    public class ConfigManager
    {
        private const string ConfigFileName = "config.json";
        private TranslationConfig config;
        private string configFilePath;

        public ConfigManager()
        {
            configFilePath = FindConfigFile();
            LoadConfig();
        }

        public TranslationConfig GetConfig()
        {
            return config;
        }

        public void SaveConfig(TranslationConfig newConfig)
        {
            config = newConfig;
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            
            // 如果没有找到现有配置文件，保存到exe路径
            if (string.IsNullOrEmpty(configFilePath))
            {
                var exePath = GetExecutablePath();
                configFilePath = Path.Combine(exePath, ConfigFileName);
            }
            
            File.WriteAllText(configFilePath, json);
        }

        private string FindConfigFile()
        {
            var searchPaths = GetConfigSearchPaths();

            foreach (var path in searchPaths)
            {
                var configPath = Path.Combine(path, ConfigFileName);
                if (File.Exists(configPath))
                {
                    return configPath;
                }
            }
            
            return string.Empty; // 没有找到现有配置文件
        }

        private List<string> GetConfigSearchPaths()
        {
            var paths = new List<string>();
            
            // 1. 当前工作目录
            paths.Add(Environment.CurrentDirectory);
            
            // 2. exe所在目录 - 多种方式获取
            var exePath = GetExecutablePath();

            if (!string.IsNullOrEmpty(exePath) && !paths.Contains(exePath))
            {
                paths.Add(exePath);
            }
            
            // 3. exe路径逐层往上直到根路径
            if (!string.IsNullOrEmpty(exePath))
            {
                var currentDir = new DirectoryInfo(exePath);
                while (currentDir?.Parent != null)
                {
                    currentDir = currentDir.Parent;
                    if (!paths.Contains(currentDir.FullName))
                    {
                        paths.Add(currentDir.FullName);
                    }
                }
            }
            
            return paths;
        }

        private string GetExecutablePath()
        {
            // 方法1: Assembly.GetExecutingAssembly().Location
            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (!string.IsNullOrEmpty(assemblyLocation))
            {
                return assemblyLocation;
            }
            
            // 方法2: Environment.ProcessPath (适用于单文件发布)
            var processPath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(processPath))
            {
                var processDir = Path.GetDirectoryName(processPath);
                if (!string.IsNullOrEmpty(processDir))
                {
                    return processDir;
                }
            }
            
            // 方法3: AppDomain.CurrentDomain.BaseDirectory
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrEmpty(baseDirectory))
            {
                return baseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            }
            
            // 方法4: AppContext.BaseDirectory
            var appContextBase = AppContext.BaseDirectory;
            if (!string.IsNullOrEmpty(appContextBase))
            {
                return appContextBase.TrimEnd(Path.DirectorySeparatorChar);
            }
            
            // 最后fallback到当前目录
            return Environment.CurrentDirectory;
        }

        private void LoadConfig()
        {
            if (!string.IsNullOrEmpty(configFilePath) && File.Exists(configFilePath))
            {
                try
                {
                    var json = File.ReadAllText(configFilePath);
                    config = JsonSerializer.Deserialize<TranslationConfig>(json) ?? GetDefaultConfig();
                    Logger.Instance?.Info($"Loaded config from: {configFilePath}");
                }
                catch (Exception ex)
                {
                    Logger.Instance?.Error($"Failed to load config from {configFilePath}: {ex.Message}");
                    config = GetDefaultConfig();
                }
            }
            else
            {
                Logger.Instance?.Info("No existing config file found, creating default config");
                config = GetDefaultConfig();
                SaveConfig(config);
            }
        }

        private static TranslationConfig GetDefaultConfig()
        {
            return new TranslationConfig
            {
                CurrentEngine = "Youdao",
                TargetLanguage = "en",
                YoudaoConfig = new YoudaoConfig
                {
                    AppKey = "",
                    AppSecret = ""
                },
                GeminiConfig = new GeminiConfig
                {
                    ApiKey = ""
                }
            };
        }
    }

    public class TranslationConfig
    {
        public string CurrentEngine { get; set; } = "Youdao";
        public string TargetLanguage { get; set; } = "en";
        public YoudaoConfig YoudaoConfig { get; set; } = new();
        public GeminiConfig GeminiConfig { get; set; } = new();
    }

    public class YoudaoConfig
    {
        public string AppKey { get; set; } = "";
        public string AppSecret { get; set; } = "";
    }

    public class GeminiConfig
    {
        public string ApiKey { get; set; } = "";
    }
}