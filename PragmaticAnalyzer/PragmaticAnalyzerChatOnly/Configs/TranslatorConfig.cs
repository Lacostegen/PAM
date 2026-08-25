using System;
using System.IO;

namespace PragmaticAnalyzer.Configs
{
    public class TranslatorConfig
    {
        public string ChatMode { get; set; } = "Обычный";

        public string ResponseMode { get; set; } = "Подробно";

        public string RagMode { get; set; } = "Авто";

        public string PerformanceProfile { get; set; } = "Баланс";

        public bool UseCompactSystemPrompt { get; set; } = true;

        public int HistoryMessagesCount { get; set; } = 6;

        public string SystemPrompt { get; set; } =
            "Ты русскоязычный ассистент и всегда отвечаешь на русском.\n" +
            "Отвечай структурированно, развернуто и по делу.\n" +
            "Ты всегда отвечаешь от лица ассистента, а не от лица пользователя.\n" +
            "Если вопрос связан с информационной безопасностью, уязвимостями, угрозами, эксплойтами, моделями или работой программы, помогай как технический специалист.\n" +
            "Не выдумывай факты, версии, пути к файлам, имена файлов, названия вредоносных программ или результаты анализа, если их нет в вопросе.\n" +
            "Не показывай скрытые рассуждения, служебные теги и технические токены.\n" +
            "Если есть контекст базы знаний, используй только релевантные сведения и не пересказывай случайные определения.";

        public string LlamaServerPath { get; set; } = Path.Combine(
            Environment.CurrentDirectory,
            "NativeLlama",
            "llama-server.exe");

        public string ModelPath { get; set; } = Path.Combine(
            Environment.CurrentDirectory,
            "Translator",
            "Qwen3.5-9B-Uncensored-HauhauCS-Aggressive-Q4_K_M.gguf");

        public string ArenaJudgeModelPath { get; set; } = string.Empty;

        public List<ArenaModelConfig> ArenaModels { get; set; } = [];

        public string Port { get; set; } = "11435";

        public int ContextSize { get; set; } = 4096;

        public int MaxTokens { get; set; } = 1100;

        public double Temperature { get; set; } = 0.3;

        public double TopP { get; set; } = 0.9;

        public double RepeatPenalty { get; set; } = 1.08;

        public int GpuLayerCount { get; set; } = 0;

        public int ThreadCount { get; set; } = 0;

        public int BatchSize { get; set; } = 512;

        public int MicroBatchSize { get; set; } = 512;

        public int ReadinessProbeMaxTokens { get; set; } = 3;

        public void Update(TranslatorConfig config)
        {
            ChatMode = config.ChatMode;
            ResponseMode = config.ResponseMode;
            RagMode = config.RagMode;
            PerformanceProfile = config.PerformanceProfile;
            UseCompactSystemPrompt = config.UseCompactSystemPrompt;
            HistoryMessagesCount = config.HistoryMessagesCount;
            SystemPrompt = config.SystemPrompt;
            LlamaServerPath = config.LlamaServerPath;
            ModelPath = config.ModelPath;
            ArenaJudgeModelPath = config.ArenaJudgeModelPath;
            ArenaModels = config.ArenaModels ?? [];
            Port = config.Port;
            ContextSize = config.ContextSize;

            MaxTokens = config.MaxTokens;
            Temperature = config.Temperature;
            TopP = config.TopP;
            RepeatPenalty = config.RepeatPenalty;
            GpuLayerCount = config.GpuLayerCount;
            ThreadCount = config.ThreadCount;
            BatchSize = config.BatchSize;
            MicroBatchSize = config.MicroBatchSize;
            ReadinessProbeMaxTokens = config.ReadinessProbeMaxTokens;
        }

        public bool NormalizePortablePaths()
        {
            var changed = false;

            var normalizedServerPath = ResolveFilePath(
                LlamaServerPath,
                Path.Combine("NativeLlama", "llama-server.exe"),
                "llama-server.exe");

            if (!string.Equals(LlamaServerPath, normalizedServerPath, StringComparison.OrdinalIgnoreCase))
            {
                LlamaServerPath = normalizedServerPath;
                changed = true;
            }

            var normalizedModelPath = ResolveGgufModelPath(ModelPath);
            if (!string.Equals(ModelPath, normalizedModelPath, StringComparison.OrdinalIgnoreCase))
            {
                ModelPath = normalizedModelPath;
                changed = true;
            }

            if (!string.IsNullOrWhiteSpace(ArenaJudgeModelPath))
            {
                var normalizedJudgeModelPath = ResolveGgufModelPath(ArenaJudgeModelPath);
                if (!string.Equals(ArenaJudgeModelPath, normalizedJudgeModelPath, StringComparison.OrdinalIgnoreCase))
                {
                    ArenaJudgeModelPath = normalizedJudgeModelPath;
                    changed = true;
                }
            }

            foreach (var model in ArenaModels ?? [])
            {
                var normalizedPath = ResolveGgufModelPath(model.Path);

                if (!string.Equals(model.Path, normalizedPath, StringComparison.OrdinalIgnoreCase))
                {
                    model.Path = normalizedPath;
                    changed = true;
                }

                if (string.IsNullOrWhiteSpace(model.Name) && !string.IsNullOrWhiteSpace(model.Path))
                {
                    model.Name = Path.GetFileNameWithoutExtension(model.Path);
                    changed = true;
                }
            }

            return changed;
        }

        private static string ResolveGgufModelPath(string configuredPath)
        {
            var fileName = string.IsNullOrWhiteSpace(configuredPath)
                ? string.Empty
                : Path.GetFileName(configuredPath);

            var candidates = new List<string>();

            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                candidates.Add(configuredPath);
            }

            if (!string.IsNullOrWhiteSpace(fileName))
            {
                candidates.Add(Path.Combine(Environment.CurrentDirectory, "Translator", fileName));
                candidates.Add(Path.Combine(Environment.CurrentDirectory, fileName));
            }

            candidates.AddRange(Directory.Exists(Path.Combine(Environment.CurrentDirectory, "Translator"))
                ? Directory.EnumerateFiles(
                    Path.Combine(Environment.CurrentDirectory, "Translator"),
                    "*.gguf",
                    SearchOption.TopDirectoryOnly)
                : []);

            return candidates
                .Where(path =>
                    !string.IsNullOrWhiteSpace(path) &&
                    File.Exists(path) &&
                    string.Equals(Path.GetExtension(path), ".gguf", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault() ?? configuredPath;
        }

        private static string ResolveFilePath(string configuredPath, params string[] relativeCandidates)
        {
            var candidates = new List<string>();

            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                candidates.Add(configuredPath);
            }

            foreach (var relativeCandidate in relativeCandidates)
            {
                candidates.Add(Path.Combine(Environment.CurrentDirectory, relativeCandidate));
            }

            return candidates
                .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault() ?? configuredPath;
        }

    }

    public class ArenaModelConfig
    {
        public string Name { get; set; } = string.Empty;

        public string Path { get; set; } = string.Empty;

        public bool IsEnabled { get; set; } = true;
    }
}
