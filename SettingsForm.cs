using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using SpaceTrans.Engines;

namespace SpaceTrans
{
    public partial class SettingsForm : Form
    {
        private ConfigManager configManager;
        private TranslationConfig config;

        private ComboBox engineComboBox;
        private ComboBox targetLanguageComboBox;
        private TextBox youdaoAppKeyTextBox;
        private TextBox youdaoAppSecretTextBox;
        private TextBox geminiApiKeyTextBox;
        private TextBox geminiApiUrlTextBox;
        private TextBox geminiModelTextBox;
        private Button youdaoSecretToggleButton;
        private Button geminiKeyToggleButton;
        private CheckBox autoStartCheckBox;

        public SettingsForm(ConfigManager configManager)
        {
            this.configManager = configManager;
            this.config = configManager.GetConfig();
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void InitializeComponent()
        {
            this.Text = "SpaceTrans 设置";
            this.Size = new System.Drawing.Size(480, 490);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.AutoScaleMode = AutoScaleMode.Font;
            this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));

            // Create controls
            var engineLabel = new Label
            {
                Text = "翻译引擎：",
                Location = new System.Drawing.Point(12, 15),
                Size = new System.Drawing.Size(120, 23)
            };

            engineComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(140, 12),
                Size = new System.Drawing.Size(216, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            engineComboBox.Items.AddRange(new[] { "Youdao", "Gemini" });

            var targetLanguageLabel = new Label
            {
                Text = "目标语言：",
                Location = new System.Drawing.Point(12, 50),
                Size = new System.Drawing.Size(120, 23)
            };

            targetLanguageComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(140, 47),
                Size = new System.Drawing.Size(216, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            targetLanguageComboBox.Items.AddRange(new[] { "en", "zh", "ja", "ko", "fr", "de", "es", "ru" });

            // Youdao Configuration
            var youdaoGroupBox = new GroupBox
            {
                Text = "有道配置",
                Location = new System.Drawing.Point(12, 85),
                Size = new System.Drawing.Size(436, 100)
            };

            var youdaoAppKeyLabel = new Label
            {
                Text = "App Key：",
                Location = new System.Drawing.Point(10, 25),
                Size = new System.Drawing.Size(80, 23)
            };

            youdaoAppKeyTextBox = new TextBox
            {
                Location = new System.Drawing.Point(100, 22),
                Size = new System.Drawing.Size(316, 23)
            };

            var youdaoAppSecretLabel = new Label
            {
                Text = "App Secret：",
                Location = new System.Drawing.Point(10, 55),
                Size = new System.Drawing.Size(80, 23)
            };

            youdaoAppSecretTextBox = new TextBox
            {
                Location = new System.Drawing.Point(100, 52),
                Size = new System.Drawing.Size(286, 23),
                UseSystemPasswordChar = true
            };

            youdaoSecretToggleButton = new Button
            {
                Text = "显示",
                Location = new System.Drawing.Point(391, 52),
                Size = new System.Drawing.Size(40, 23),
                TabStop = false
            };
            youdaoSecretToggleButton.Click += (s, e) => TogglePasswordVisibility(youdaoAppSecretTextBox, youdaoSecretToggleButton);

            youdaoGroupBox.Controls.AddRange(new Control[] {
                youdaoAppKeyLabel, youdaoAppKeyTextBox,
                youdaoAppSecretLabel, youdaoAppSecretTextBox, youdaoSecretToggleButton
            });

            // Gemini Configuration
            var geminiGroupBox = new GroupBox
            {
                Text = "Gemini 配置",
                Location = new System.Drawing.Point(12, 195),
                Size = new System.Drawing.Size(436, 120)
            };

            var geminiApiKeyLabel = new Label
            {
                Text = "API Key：",
                Location = new System.Drawing.Point(10, 25),
                Size = new System.Drawing.Size(80, 23)
            };

            geminiApiKeyTextBox = new TextBox
            {
                Location = new System.Drawing.Point(100, 22),
                Size = new System.Drawing.Size(286, 23),
                UseSystemPasswordChar = true
            };

            geminiKeyToggleButton = new Button
            {
                Text = "显示",
                Location = new System.Drawing.Point(391, 22),
                Size = new System.Drawing.Size(40, 23),
                TabStop = false
            };
            geminiKeyToggleButton.Click += (s, e) => TogglePasswordVisibility(geminiApiKeyTextBox, geminiKeyToggleButton);

            var geminiApiUrlLabel = new Label
            {
                Text = "API URL：",
                Location = new System.Drawing.Point(10, 55),
                Size = new System.Drawing.Size(80, 23)
            };

            geminiApiUrlTextBox = new TextBox
            {
                Location = new System.Drawing.Point(100, 52),
                Size = new System.Drawing.Size(316, 23)
            };

            var geminiModelLabel = new Label
            {
                Text = "模型：",
                Location = new System.Drawing.Point(10, 88),
                Size = new System.Drawing.Size(80, 23)
            };

            geminiModelTextBox = new TextBox
            {
                Location = new System.Drawing.Point(100, 85),
                Size = new System.Drawing.Size(316, 23)
            };

            geminiGroupBox.Controls.AddRange(new Control[] {
                geminiApiKeyLabel, geminiApiKeyTextBox, geminiKeyToggleButton,
                geminiApiUrlLabel, geminiApiUrlTextBox,
                geminiModelLabel, geminiModelTextBox
            });

            // Buttons
            var okButton = new Button
            {
                Text = "确定",
                Location = new System.Drawing.Point(285, 410),
                Size = new System.Drawing.Size(75, 30),
                DialogResult = DialogResult.OK
            };
            okButton.Click += OkButton_Click;

            var cancelButton = new Button
            {
                Text = "取消",
                Location = new System.Drawing.Point(365, 410),
                Size = new System.Drawing.Size(75, 30),
                DialogResult = DialogResult.Cancel
            };

            var testButton = new Button
            {
                Text = "测试连接",
                Location = new System.Drawing.Point(12, 410),
                Size = new System.Drawing.Size(125, 30)
            };
            testButton.Click += TestButton_Click;

            var openConfigButton = new Button
            {
                Text = "打开配置文件",
                Location = new System.Drawing.Point(142, 410),
                Size = new System.Drawing.Size(130, 30)
            };
            openConfigButton.Click += OpenConfigButton_Click;

            // Auto Start Checkbox
            autoStartCheckBox = new CheckBox
            {
                Text = "开机自启（随 Windows 启动）",
                Location = new System.Drawing.Point(12, 345),
                Size = new System.Drawing.Size(250, 24)
            };
            autoStartCheckBox.Checked = AutoStartHelper.IsAutoStartEnabled();
            autoStartCheckBox.CheckedChanged += (s, e) =>
            {
                try
                {
                    AutoStartHelper.SetAutoStart(autoStartCheckBox.Checked);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("设置开机自启失败：" + ex.Message);
                }
            };

            // Add controls to form
            this.Controls.AddRange(new Control[] {
                engineLabel, engineComboBox,
                targetLanguageLabel, targetLanguageComboBox,
                youdaoGroupBox, geminiGroupBox,
                okButton, cancelButton, testButton, openConfigButton,
                autoStartCheckBox
            });

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        private void LoadCurrentSettings()
        {
            engineComboBox.SelectedItem = config.CurrentEngine;
            targetLanguageComboBox.SelectedItem = config.TargetLanguage;
            youdaoAppKeyTextBox.Text = config.YoudaoConfig.AppKey;
            youdaoAppSecretTextBox.Text = string.IsNullOrEmpty(config.YoudaoConfig.AppSecret) ? "" : "••••••••••••••••";
            geminiApiKeyTextBox.Text = string.IsNullOrEmpty(config.GeminiConfig.ApiKey) ? "" : "••••••••••••••••";
            geminiApiUrlTextBox.Text = config.GeminiConfig.ApiUrl;
            geminiModelTextBox.Text = config.GeminiConfig.Model;
        }

        private void TogglePasswordVisibility(TextBox textBox, Button toggleButton)
        {
            if (textBox.UseSystemPasswordChar)
            {
                textBox.UseSystemPasswordChar = false;
                toggleButton.Text = "隐藏";
                
                // Load actual values when showing
                if (textBox == youdaoAppSecretTextBox)
                {
                    textBox.Text = config.YoudaoConfig.AppSecret;
                }
                else if (textBox == geminiApiKeyTextBox)
                {
                    textBox.Text = config.GeminiConfig.ApiKey;
                }
            }
            else
            {
                textBox.UseSystemPasswordChar = true;
                toggleButton.Text = "显示";
                
                // Show placeholder when hiding
                if (textBox == youdaoAppSecretTextBox)
                {
                    textBox.Text = string.IsNullOrEmpty(config.YoudaoConfig.AppSecret) ? "" : "••••••••••••••••";
                }
                else if (textBox == geminiApiKeyTextBox)
                {
                    textBox.Text = string.IsNullOrEmpty(config.GeminiConfig.ApiKey) ? "" : "••••••••••••••••";
                }
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(engineComboBox.SelectedItem?.ToString()))
                {
                    MessageBox.Show("请选择翻译引擎。", "验证错误", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(targetLanguageComboBox.SelectedItem?.ToString()))
                {
                    MessageBox.Show("请选择目标语言。", "验证错误", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var selectedEngine = engineComboBox.SelectedItem.ToString();
                
                // Validate engine-specific configuration
                if (selectedEngine == "Youdao" && 
                    (string.IsNullOrWhiteSpace(youdaoAppKeyTextBox.Text) || 
                     (string.IsNullOrWhiteSpace(youdaoAppSecretTextBox.Text) && string.IsNullOrWhiteSpace(config.YoudaoConfig.AppSecret))))
                {
                    MessageBox.Show("请提供有道 App Key 和 App Secret。", "验证错误", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (selectedEngine == "Gemini" && 
                    (string.IsNullOrWhiteSpace(geminiApiKeyTextBox.Text) && string.IsNullOrWhiteSpace(config.GeminiConfig.ApiKey)))
                {
                    MessageBox.Show("请提供 Gemini API Key。", "验证错误", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Update configuration
                config.CurrentEngine = selectedEngine;
                config.TargetLanguage = targetLanguageComboBox.SelectedItem.ToString();
                config.YoudaoConfig.AppKey = youdaoAppKeyTextBox.Text.Trim();
                
                // Only update secrets if they're not placeholder text
                if (!youdaoAppSecretTextBox.Text.StartsWith("••••"))
                {
                    config.YoudaoConfig.AppSecret = youdaoAppSecretTextBox.Text.Trim();
                }
                
                if (!geminiApiKeyTextBox.Text.StartsWith("••••"))
                {
                    config.GeminiConfig.ApiKey = geminiApiKeyTextBox.Text.Trim();
                }

                config.GeminiConfig.ApiUrl = geminiApiUrlTextBox.Text.Trim();
                config.GeminiConfig.Model = geminiModelTextBox.Text.Trim();

                // Save configuration
                configManager.SaveConfig(config);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存设置失败：{ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void TestButton_Click(object sender, EventArgs e)
        {
            try
            {
                var testButton = sender as Button;
                testButton.Enabled = false;
                testButton.Text = "测试中...";

                var selectedEngine = engineComboBox.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(selectedEngine))
                {
                    MessageBox.Show("请先选择引擎。", "测试错误", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Create a temporary engine manager for testing
                var tempEngineManager = new TranslationEngineManager();
                var httpClient = new System.Net.Http.HttpClient();

                try
                {
                    if (selectedEngine == "Youdao")
                    {
                        var appKey = youdaoAppKeyTextBox.Text.Trim();
                        var appSecret = youdaoAppSecretTextBox.Text.StartsWith("••••") ? config.YoudaoConfig.AppSecret : youdaoAppSecretTextBox.Text.Trim();
                        
                        if (string.IsNullOrWhiteSpace(appKey) || string.IsNullOrWhiteSpace(appSecret))
                        {
                            MessageBox.Show("请提供有道凭据。", "测试错误", 
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        var youdaoEngine = new YoudaoTranslationEngine(appKey, appSecret, httpClient);
                        tempEngineManager.RegisterEngine(youdaoEngine);
                    }
                    else if (selectedEngine == "Gemini")
                    {
                        var apiKey = geminiApiKeyTextBox.Text.StartsWith("••••") ? config.GeminiConfig.ApiKey : geminiApiKeyTextBox.Text.Trim();
                        
                        if (string.IsNullOrWhiteSpace(apiKey))
                        {
                            MessageBox.Show("请提供 Gemini API Key。", "测试错误", 
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        var apiUrl = geminiApiUrlTextBox.Text.Trim();
                        var model = geminiModelTextBox.Text.Trim();
                        var geminiEngine = new GeminiTranslationEngine(apiKey, apiUrl, model, httpClient);
                        tempEngineManager.RegisterEngine(geminiEngine);
                    }

                    tempEngineManager.SetCurrentEngine(selectedEngine);
                    
                    // Test translation
                    var testResult = await tempEngineManager.TranslateAsync("Hello", "en", "zh");
                    
                    MessageBox.Show($"测试成功！\n翻译结果：{testResult}", 
                        "测试结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                finally
                {
                    httpClient?.Dispose();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"测试失败：{ex.Message}", "测试错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                var testButton = sender as Button;
                testButton.Enabled = true;
                testButton.Text = "测试连接";
            }
        }

        private void OpenConfigButton_Click(object sender, EventArgs e)
        {
            try
            {
                var configFilePath = GetConfigFilePath();
                
                if (string.IsNullOrEmpty(configFilePath))
                {
                    MessageBox.Show("未找到配置文件路径。", "错误", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!File.Exists(configFilePath))
                {
                    MessageBox.Show($"配置文件不存在：{configFilePath}", "错误", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Open with default editor
                var startInfo = new ProcessStartInfo
                {
                    FileName = configFilePath,
                    UseShellExecute = true
                };
                
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开配置文件失败：{ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetConfigFilePath()
        {
            // Use reflection to access private field
            var configManagerType = typeof(ConfigManager);
            var configFilePathField = configManagerType.GetField("configFilePath", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (configFilePathField != null)
            {
                return configFilePathField.GetValue(configManager) as string;
            }
            
            return null;
        }
    }
}