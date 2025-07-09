using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpaceTrans.Engines
{
    public class TranslationEngineManager
    {
        private readonly Dictionary<string, ITranslationEngine> engines = new();
        private string currentEngineKey = string.Empty;

        public void RegisterEngine(ITranslationEngine engine)
        {
            engines[engine.Name] = engine;
            if (string.IsNullOrEmpty(currentEngineKey))
            {
                currentEngineKey = engine.Name;
            }
        }

        public void SetCurrentEngine(string engineName)
        {
            if (engines.ContainsKey(engineName))
            {
                currentEngineKey = engineName;
            }
            else
            {
                throw new ArgumentException($"Engine '{engineName}' not found");
            }
        }

        public ITranslationEngine GetCurrentEngine()
        {
            if (string.IsNullOrEmpty(currentEngineKey) || !engines.ContainsKey(currentEngineKey))
            {
                throw new InvalidOperationException("No translation engine available");
            }
            return engines[currentEngineKey];
        }

        public IEnumerable<ITranslationEngine> GetAvailableEngines()
        {
            return engines.Values.ToList();
        }

        public async Task<string> TranslateAsync(string text, string fromLanguage, string toLanguage)
        {
            var engine = GetCurrentEngine();
            return await engine.TranslateAsync(text, fromLanguage, toLanguage);
        }
    }
}