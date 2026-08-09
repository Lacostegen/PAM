using System.IO;

namespace PragmaticAnalyzer.Configs
{
    /// <summary>
    /// Глобальные абсолютные пути файлов программы
    /// </summary>
    public static class GlobalConfig
    {
        //
        // Сводка:
        //     Пути к каталогам.
        public static readonly string DatabasePath = Path.Combine(Environment.CurrentDirectory, "Database");
        public static readonly string ExploitTextPath = Path.Combine(Environment.CurrentDirectory, "ExploitText");
        public static readonly string ModelsPath = Path.Combine(Environment.CurrentDirectory, "Models");
        public static readonly string ConfigPath = Path.Combine(Environment.CurrentDirectory, "Config");
        public static readonly string ResourcePath = Path.Combine(Environment.CurrentDirectory, "Resource");

        //
        public static readonly string RagConfigPath = Path.Combine(Environment.CurrentDirectory, "Config", "ragConfig.json");
        //
        // Сводка:
        //     Пути к сторонним исполняемым файлам.
        public static readonly string MatcherPath = Path.Combine(Environment.CurrentDirectory, "matcher.exe");
        //
        // Сводка:
        //     Пути к конфигурационным файлам.
        public static readonly string LastUpdateConfig = Path.Combine(ConfigPath, "lastUpdateConfig.json");
        public static readonly string ExploitConfigPath = Path.Combine(ConfigPath, "exploitConfig.json");
        public static readonly string ThreatConfigPath = Path.Combine(ConfigPath, "threadConfig.json");
        public static readonly string VulConfigPath = Path.Combine(ConfigPath, "vulConfig.json");
        public static readonly string WordTwoVecConfigPath = Path.Combine(ConfigPath, "word2vecConfig.json");
        public static readonly string FastTextConfigPath = Path.Combine(ConfigPath, "fastTextConfig.json");
        public static readonly string VulJvnHashSetPath = Path.Combine(ConfigPath, "vulJvnHashSetPath.json");
        public static readonly string VulNvdHashSetPath = Path.Combine(ConfigPath, "vulNvdHashSetPath.json");
        //
        // Сводка:
        //     Пути к базам данных.
        public static readonly string VulnerabilitieFstecPath = Path.Combine(DatabasePath, "vulnerabilitieFstecDb.json");
        public static readonly string VulnerabilitieNvdPath = Path.Combine(DatabasePath, "vulnerabilitieNvdDb.json");
        public static readonly string VulnerabilitieJvnPath = Path.Combine(DatabasePath, "vulnerabilitieJvnDb.json");
        public static readonly string ThreatPath = Path.Combine(DatabasePath, "threatDb.json");
        public static readonly string TacticPath = Path.Combine(DatabasePath, "tacticDb.json");
        public static readonly string ProtectionMeasurePath = Path.Combine(DatabasePath, "protectionMeasureDb.json");
        public static readonly string OutcomesPath = Path.Combine(DatabasePath, "outcomesDb.json");
        public static readonly string ExploitPath = Path.Combine(DatabasePath, "exploitDb.json");
        public static readonly string ViolatorPath = Path.Combine(DatabasePath, "violatorDb.json");
        public static readonly string SpecialistPath = Path.Combine(DatabasePath, "specialistDb.json");
        public static readonly string RefStatPath = Path.Combine(DatabasePath, "refStatusDb.json");
        public static readonly string CurStatPath = Path.Combine(DatabasePath, "curStatusDb.json");
        public static readonly string OntologyPath = Path.Combine(DatabasePath, "ontologyDb.json");
        public static readonly string VulnerabilitieJvnTranslated = Path.Combine(DatabasePath, "vulnerabilitieJvnTranslatedDb.json");
        public static readonly string VulnerabilitieNvdTranslated = Path.Combine(DatabasePath, "vulnerabilitieNvdTranslatedDb.json");
        public static readonly string SchemeDatabasePath = Path.Combine(DatabasePath, "schemeDb.json");
        //

        public static readonly string TranslatorConfigPath = Path.Combine(Environment.CurrentDirectory, "Config", "translatorConfig.json");
        // Сводка:
        //     Пути к информационным ресурсам.
        public static readonly string LinkOne = "https://fasttext.cc/";
        public static readonly string LinkTwo = Path.Combine(ResourcePath, "Word2Vec", "vectorization.pdf");
        public static readonly string LinkThree = Path.Combine(ResourcePath, "Word2Vec", "preprocessing.pdf");
        public static readonly string LinkFour = Path.Combine(ResourcePath, "Word2Vec", "word2vec.pdf");
        public static readonly string LinkFive = Path.Combine(ResourcePath, "Word2Vec", "word2vec.pdf");
        public static readonly string LinkSix = Path.Combine(ResourcePath, "Word2Vec", "softmax.pdf");
        public static readonly string LinkSeven = Path.Combine(ResourcePath, "Word2Vec", "backpropagation.pdf");
        public static readonly string LinkEight = Path.Combine(ResourcePath, "Word2Vec", "cosine-similarity.pdf");
        public static readonly string LinkNine = Path.Combine(ResourcePath, "Word2Vec", "euclidean-distance.pdf");
        public static readonly string LinkTen = Path.Combine(ResourcePath, "Word2Vec", "word2vec.pdf");
        public static readonly string LinkEleven = Path.Combine(ResourcePath, "TfIdf", "habr.pdf");
        //
        //Сводка:
        //  Порты серверов
        public static readonly string MatcherPort = "5000";
    }
}
