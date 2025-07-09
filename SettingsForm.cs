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
        private Button youdaoSecretToggleButton;
        private Button geminiKeyToggleButton;

        public SettingsForm(ConfigManager configManager)
        {
            this.configManager = configManager;
            this.config = configManager.GetConfig();
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void InitializeComponent()
        {
            this.Text = "SpaceTrans Settings";
            this.Size = new System.Drawing.Size(450, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Create controls
            var engineLabel = new Label
            {
                Text = "Translation Engine:",
                Location = new System.Drawing.Point(12, 15),
                Size = new System.Drawing.Size(120, 23)
            };

            engineComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(140, 12),
                Size = new System.Drawing.Size(200, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            engineComboBox.Items.AddRange(new[] { "Youdao", "Gemini" });

            var targetLanguageLabel = new Label
            {
                Text = "Target Language:",
                Location = new System.Drawing.Point(12, 50),
                Size = new System.Drawing.Size(120, 23)
            };

            targetLanguageComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(140, 47),
                Size = new System.Drawing.Size(200, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            targetLanguageComboBox.Items.AddRange(new[] { "en", "zh", "ja", "ko", "fr", "de", "es", "ru" });

            // Youdao Configuration
            var youdaoGroupBox = new GroupBox
            {
                Text = "Youdao Configuration",
                Location = new System.Drawing.Point(12, 85),
                Size = new System.Drawing.Size(400, 100)
            };

            var youdaoAppKeyLabel = new Label
            {
                Text = "App Key:",
                Location = new System.Drawing.Point(10, 25),
                Size = new System.Drawing.Size(80, 23)
            };

            youdaoAppKeyTextBox = new TextBox
            {
                Location = new System.Drawing.Point(100, 22),
                Size = new System.Drawing.Size(280, 23)
            };

            var youdaoAppSecretLabel = new Label
            {
                Text = "App Secret:",
                Location = new System.Drawing.Point(10, 55),
                Size = new System.Drawing.Size(80, 23)
            };

            youdaoAppSecretTextBox = new TextBox
            {
                Location = new System.Drawing.Point(100, 52),
                Size = new System.Drawing.Size(250, 23),
                UseSystemPasswordChar = true
            };

            youdaoSecretToggleButton = new Button
            {
                Text = "👁",
                Location = new System.Drawing.Point(355, 52),
                Size = new System.Drawing.Size(25, 23),
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
                Text = "Gemini Configuration",
                Location = new System.Drawing.Point(12, 195),
                Size = new System.Drawing.Size(400, 60)
            };

            var geminiApiKeyLabel = new Label
            {
                Text = "API Key:",
                Location = new System.Drawing.Point(10, 25),
                Size = new System.Drawing.Size(80, 23)
            };

            geminiApiKeyTextBox = new TextBox
            {
                Location = new System.Drawing.Point(100, 22),
                Size = new System.Drawing.Size(250, 23),
                UseSystemPasswordChar = true
            };

            geminiKeyToggleButton = new Button
            {
                Text = "👁",
                Location = new System.Drawing.Point(355, 22),
                Size = new System.Drawing.Size(25, 23),
                TabStop = false
            };
            geminiKeyToggleButton.Click += (s, e) => TogglePasswordVisibility(geminiApiKeyTextBox, geminiKeyToggleButton);

            geminiGroupBox.Controls.AddRange(new Control[] {
                geminiApiKeyLabel, geminiApiKeyTextBox, geminiKeyToggleButton
            });

            // Buttons
            var okButton = new Button
            {
                Text = "OK",
                Location = new System.Drawing.Point(250, 320),
                Size = new System.Drawing.Size(75, 30),
                DialogResult = DialogResult.OK
            };
            okButton.Click += OkButton_Click;

            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new System.Drawing.Point(335, 320),
                Size = new System.Drawing.Size(75, 30),
                DialogResult = DialogResult.Cancel
            };

            var testButton = new Button
            {
                Text = "Test Connection",
                Location = new System.Drawing.Point(12, 320),
                Size = new System.Drawing.Size(120, 30)
            };
            testButton.Click += TestButton_Click;

            var openConfigButton = new Button
            {
                Text = "Open Config File",
                Location = new System.Drawing.Point(140, 320),
                Size = new System.Drawing.Size(100, 30)
            };
            openConfigButton.Click += OpenConfigButton_Click;

            // Add controls to form
            this.Controls.AddRange(new Control[] {
                engineLabel, engineComboBox,
                targetLanguageLabel, targetLanguageComboBox,
                youdaoGroupBox, geminiGroupBox,
                okButton, cancelButton, testButton, openConfigButton
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
        }

        private void TogglePasswordVisibility(TextBox textBox, Button toggleButton)
        {
            if (textBox.UseSystemPasswordChar)
            {
                textBox.UseSystemPasswordChar = false;
                toggleButton.Text = "🙈";
                
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
                toggleButton.Text = "👁";
                
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
                    MessageBox.Show("Please select a translation engine.", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(targetLanguageComboBox.SelectedItem?.ToString()))
                {
                    MessageBox.Show("Please select a target language.", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var selectedEngine = engineComboBox.SelectedItem.ToString();
                
                // Validate engine-specific configuration
                if (selectedEngine == "Youdao" && 
                    (string.IsNullOrWhiteSpace(youdaoAppKeyTextBox.Text) || 
                     (string.IsNullOrWhiteSpace(youdaoAppSecretTextBox.Text) && string.IsNullOrWhiteSpace(config.YoudaoConfig.AppSecret))))
                {
                    MessageBox.Show("Please provide Youdao App Key and App Secret.", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (selectedEngine == "Gemini" && 
                    (string.IsNullOrWhiteSpace(geminiApiKeyTextBox.Text) && string.IsNullOrWhiteSpace(config.GeminiConfig.ApiKey)))
                {
                    MessageBox.Show("Please provide Gemini API Key.", "Validation Error", 
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

                // Save configuration
                configManager.SaveConfig(config);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void TestButton_Click(object sender, EventArgs e)
        {
            try
            {
                var testButton = sender as Button;
                testButton.Enabled = false;
                testButton.Text = "Testing...";

                var selectedEngine = engineComboBox.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(selectedEngine))
                {
                    MessageBox.Show("Please select an engine first.", "Test Error", 
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
                            MessageBox.Show("Please provide Youdao credentials first.", "Test Error", 
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
                            MessageBox.Show("Please provide Gemini API key first.", "Test Error", 
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        var geminiEngine = new GeminiTranslationEngine(apiKey, httpClient);
                        tempEngineManager.RegisterEngine(geminiEngine);
                    }

                    tempEngineManager.SetCurrentEngine(selectedEngine);
                    
                    // Test translation
                    var testResult = await tempEngineManager.TranslateAsync("Hello", "en", "zh");
                    
                    MessageBox.Show($"Test successful!\nTranslation result: {testResult}", 
                        "Test Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                finally
                {
                    httpClient?.Dispose();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Test failed: {ex.Message}", "Test Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                var testButton = sender as Button;
                testButton.Enabled = true;
                testButton.Text = "Test Connection";
            }
        }

        private void OpenConfigButton_Click(object sender, EventArgs e)
        {
            try
            {
                var configFilePath = GetConfigFilePath();
                
                if (string.IsNullOrEmpty(configFilePath))
                {
                    MessageBox.Show("Config file path not found.", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!File.Exists(configFilePath))
                {
                    MessageBox.Show($"Config file does not exist at: {configFilePath}", "Error", 
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
                MessageBox.Show($"Failed to open config file: {ex.Message}", "Error", 
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