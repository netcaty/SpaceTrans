using System.Threading.Tasks;

namespace SpaceTrans.Engines
{
    public interface ITranslationEngine
    {
        string Name { get; }
        string Description { get; }
        Task<string> TranslateAsync(string text, string fromLanguage, string toLanguage);
        Task<bool> IsAvailableAsync();
    }
}