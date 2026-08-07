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

                HttpResponseMessage response;
                try
                {
                    response = await httpClient.PostAsync($"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-lite:generateContent?key={apiKey}", content);
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
                {
                    // 网络层错误（连接失败、SSL 错误、超时等）附加友好提示后抛出，绝不静默返回原文
                    throw new HttpRequestException($"无法连接 Gemini 服务器（网络错误）：{ex.Message}", ex);
                }
                var responseContent = await response.Content.ReadAsStringAsync();

                Logger.Instance.Debug($"Gemini response: {responseContent}");

                // 非 2xx 响应（如 API key 无效 403、限流 429、服务端错误 500 等）一律视为翻译失败
                if (!response.IsSuccessStatusCode)
                {
                    throw BuildGeminiHttpException((int)response.StatusCode, responseContent);
                }

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
                            var translated = textProp.GetString()?.Trim();
                            if (!string.IsNullOrWhiteSpace(translated))
                            {
                                return translated;
                            }
                        }
                    }
                }

                // 响应缺少翻译结果时同样视为失败，绝不把原文当作翻译结果返回
                throw new InvalidOperationException("Gemini returned an empty translation or an unexpected response format.");
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Gemini translation error: {ex.Message}", ex);
                throw;
            }
        }

        // 解析 Gemini 错误响应体（{ "error": { "code", "message", "status" } }），构造带状态码和服务器错误信息的异常
        private static Exception BuildGeminiHttpException(int statusCode, string responseContent)
        {
            var message = string.Empty;
            var status = string.Empty;
            try
            {
                using var jsonDoc = JsonDocument.Parse(responseContent);
                if (jsonDoc.RootElement.TryGetProperty("error", out var error))
                {
                    if (error.TryGetProperty("message", out var msgProp))
                    {
                        message = msgProp.GetString() ?? string.Empty;
                    }
                    if (error.TryGetProperty("status", out var statusProp))
                    {
                        status = statusProp.GetString() ?? string.Empty;
                    }
                }
            }
            catch
            {
                // 响应体不是合法 JSON 时退回原始内容片段
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                message = string.IsNullOrEmpty(responseContent)
                    ? "no response body"
                    : (responseContent.Length > 200 ? responseContent[..200] + "..." : responseContent);
            }

            var statusSuffix = string.IsNullOrWhiteSpace(status) ? string.Empty : $" ({status})";
            return new HttpRequestException($"Gemini API 错误 (HTTP {statusCode}){statusSuffix}: {message}");
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