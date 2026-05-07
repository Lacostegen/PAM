using PragmaticAnalyzer.Abstractions;
using PragmaticAnalyzer.Enums;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace PragmaticAnalyzer.WorkingServer.Communication
{
    public class RequestCommunication : IRequest
    {
        public string Url { get; private set; }
        public StringContent? Content { get; private set; }

        public RequestCommunication(
            string userMessage,
            string port,
            int contextSize,
            int maxTokens,
            double temperature,
            double topP,
            double repeatPenalty,
            string systemPrompt)
            : this(
                new[]
                {
                    new CommunicationPromptMessage(MessageSender.User, userMessage)
                },
                port,
                contextSize,
                maxTokens,
                temperature,
                topP,
                repeatPenalty,
                systemPrompt)
        {
        }

        public RequestCommunication(
            IEnumerable<CommunicationPromptMessage> messages,
            string port,
            int contextSize,
            int maxTokens,
            double temperature,
            double topP,
            double repeatPenalty,
            string systemPrompt)
        {
            var prompt = BuildPrompt(messages, systemPrompt);

            var payload = new
            {
                prompt,

                // Для совместимости с разными API-режимами KoboldCpp.
                max_tokens = maxTokens,
                max_length = maxTokens,

                // Размер контекста, который мы задали в GUI.
                max_context_length = contextSize,

                temperature,
                top_p = topP,
                rep_pen = repeatPenalty,

                stop_sequence = new[]
                {
                    "<|im_end|>",
                    "<|im_start|>user",
                    "<|im_start|>system"
                },

                n = 1
            };

            string json = JsonSerializer.Serialize(payload);

            Url = $"http://127.0.0.1:{port}/api/v1/generate";
            Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        private static string BuildPrompt(
            IEnumerable<CommunicationPromptMessage> messages,
            string systemPrompt)
        {
            var builder = new StringBuilder();

            builder.AppendLine("<|im_start|>system");

            if (string.IsNullOrWhiteSpace(systemPrompt))
            {
                builder.AppendLine("Ты русскоязычный ассистент внутри программы PragmaticAnalyzer.");
                builder.AppendLine("Отвечай понятно, структурированно и по делу.");
                builder.AppendLine("Ты всегда отвечаешь от лица ассистента, а не от лица пользователя.");
                builder.AppendLine("Не копируй формулировки пользователя как свой ответ.");
                builder.AppendLine("Если пользователь спрашивает о себе, отвечай во втором лице: 'тебя', 'ты', 'у тебя'.");
                builder.AppendLine("Не выводи служебные теги <think>, </think> и не показывай внутренние рассуждения.");
                builder.AppendLine("Не начинай ответ с анализа вопроса пользователя.");
            }
            else
            {
                builder.AppendLine(NormalizeMessageText(systemPrompt));
            }

            builder.AppendLine("<|im_end|>");

            foreach (var message in messages)
            {
                var role = message.Sender == MessageSender.User
                    ? "user"
                    : "assistant";

                var text = NormalizeMessageText(message.Text);

                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                builder.AppendLine($"<|im_start|>{role}");
                builder.AppendLine(text);
                builder.AppendLine("<|im_end|>");
            }

            builder.AppendLine("<|im_start|>assistant");

            return builder.ToString();
        }

        private static string NormalizeMessageText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            return text
                .Replace("<|im_start|>", string.Empty)
                .Replace("<|im_end|>", string.Empty)
                .Trim();
        }
    }

    public class CommunicationPromptMessage
    {
        public MessageSender Sender { get; }
        public string Text { get; }

        public CommunicationPromptMessage(MessageSender sender, string text)
        {
            Sender = sender;
            Text = text;
        }
    }
}