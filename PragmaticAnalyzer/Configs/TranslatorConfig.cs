using System;
using System.IO;

namespace PragmaticAnalyzer.Configs
{
    public class TranslatorConfig
    {

        public int HistoryMessagesCount { get; set; } = 12;

        public string SystemPrompt { get; set; } =
            "Ты русскоязычный ассистент и всегда отвечаешь на русском.\n" +
            "Отвечай понятно, структурированно и по делу.\n" +
            "Ты всегда отвечаешь от лица ассистента, а не от лица пользователя.\n" +
            "Не копируй формулировки пользователя как свой ответ.\n" +
            "Если пользователь спрашивает о себе, отвечай во втором лице: 'тебя', 'ты', 'у тебя'.\n" +
            "Если вопрос связан с информационной безопасностью, уязвимостями, угрозами, эксплойтами, моделями или работой программы, помогай как технический специалист.\n" +
            "Не выдумывай факты, версии, пути к файлам или результаты анализа, если их нет в вопросе.\n" +
            "Не выводи служебные теги <think>, </think>, не показывай внутренние рассуждения и сразу пиши только финальный ответ.\n" +
            "Не начинай ответ с анализа вопроса пользователя.";

        public string KoboldCppPath { get; set; } = Path.Combine(
            Environment.CurrentDirectory,
            "Translator",
            "koboldcpp.exe");

        public string ModelPath { get; set; } = Path.Combine(
            Environment.CurrentDirectory,
            "Translator",
            "Qwen3.5-9B-Uncensored-HauhauCS-Aggressive-Q4_K_M.gguf");

        public string Port { get; set; } = "5001";

        public int ContextSize { get; set; } = 2048;

        // Настройки генерации ответа
        public int MaxTokens { get; set; } = 768;

        public double Temperature { get; set; } = 0.6;

        public double TopP { get; set; } = 0.9;

        public double RepeatPenalty { get; set; } = 1.08;

        public void Update(TranslatorConfig config)
        {
            
            HistoryMessagesCount = config.HistoryMessagesCount;
            SystemPrompt = config.SystemPrompt;
            KoboldCppPath = config.KoboldCppPath;
            ModelPath = config.ModelPath;
            Port = config.Port;
            ContextSize = config.ContextSize;

            MaxTokens = config.MaxTokens;
            Temperature = config.Temperature;
            TopP = config.TopP;
            RepeatPenalty = config.RepeatPenalty;
        }
    }
}