using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SpaceTrans.Engines
{
    public class GeminiTranslationEngine : ITranslationEngine
    {
        private readonly string apiKey;
        private readonly HttpClient httpClient;

        public string Name => "Gemini";
        public string Description => "Google Gemini AI Translation";

        public GeminiTranslationEngine(string apiKey, HttpClient httpClient)
        {
            this.apiKey = apiKey;
            this.httpClient = httpClient;
        }

        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                var testResult = await TranslateAsync("test", "auto", "en");
                return !string.IsNullOrEmpty(testResult);
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> TranslateAsync(string text, string fromLanguage, string toLanguage)
        {
            try
            {
                var targetLang = ConvertLanguageCode(toLanguage);
                var sourceLang = fromLanguage == "auto" ? "auto-detect" : ConvertLanguageCode(fromLanguage);
                
                var prompt = $"Translate the following text from {sourceLang} to {targetLang}. Return only the translation, no explanations or additional text:\\n\\n{text}";
                
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync($"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-lite:generateContent?key={apiKey}", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                Logger.Instance.Info($"Gemini response: {responseContent}");

                var jsonDoc = JsonDocument.Parse(responseContent);
                if (jsonDoc.RootElement.TryGetProperty("candidates", out var candidates) && 
                    candidates.GetArrayLength() > 0)
                {
                    var candidate = candidates[0];
                    if (candidate.TryGetProperty("content", out var contentProp) &&
                        contentProp.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0)
                    {
                        var part = parts[0];
                        if (part.TryGetProperty("text", out var textProp))
                        {
                            return textProp.GetString()?.Trim() ?? text;
                        }
                    }
                }

                return text;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Gemini translation error: {ex.Message}");
                return text;
            }
        }

        private static string ConvertLanguageCode(string languageCode)
        {
            return languageCode switch
            {
                "zh" or "zh-CN" => "Chinese",
                "en" => "English",
                "ja" => "Japanese",
                "ko" => "Korean",
                "fr" => "French",
                "de" => "German",
                "es" => "Spanish",
                "it" => "Italian",
                "pt" => "Portuguese",
                "ru" => "Russian",
                _ => languageCode
            };
        }
    }
}