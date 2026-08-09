using System.IO;

namespace PragmaticAnalyzer.Configs
{
    public static class GlobalConfig
    {
        public static readonly string DatabasePath = Path.Combine(Environment.CurrentDirectory, "Database");
        public static readonly string ConfigPath = Path.Combine(Environment.CurrentDirectory, "Config");

        public static readonly string RagConfigPath = Path.Combine(Environment.CurrentDirectory, "Config", "ragConfig.json");
        public static readonly string TranslatorConfigPath = Path.Combine(Environment.CurrentDirectory, "Config", "translatorConfig.json");
    }
}
