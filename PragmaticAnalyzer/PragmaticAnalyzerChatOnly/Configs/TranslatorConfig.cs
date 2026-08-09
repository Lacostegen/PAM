using System;
using System.IO;

namespace PragmaticAnalyzer.Configs
{
    public class TranslatorConfig
    {
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
            ResponseMode = config.ResponseMode;
            RagMode = config.RagMode;
            PerformanceProfile = config.PerformanceProfile;
            UseCompactSystemPrompt = config.UseCompactSystemPrompt;
            HistoryMessagesCount = config.HistoryMessagesCount;
            SystemPrompt = config.SystemPrompt;
            LlamaServerPath = config.LlamaServerPath;
            ModelPath = config.ModelPath;
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
    }
}
