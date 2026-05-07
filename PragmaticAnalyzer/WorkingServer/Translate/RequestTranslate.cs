using PragmaticAnalyzer.Abstractions;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace PragmaticAnalyzer.WorkingServer.Translate
{
    public class RequestTranslate : IRequest
    {
        public string Url { get; private set; } // url-адрес сервера
        public StringContent? Content { get; private set; } // контент, отправляемый серверу

        public RequestTranslate(string ip, string port, string textToTranslate)
        {
            var payload = new
            {     
                prompt = $"{{[INPUT]}}Переведи на русский: {textToTranslate}{{[OUTPUT]}}", // промпт для перевода текста
                max_tokens = 100, // максимальное количество токенов в диалоговом окне
                temperature = 0.1, //
                stop_sequence = new[] { "\n" }, //
                rep_pen = 1.05, //
                top_p = 0.9, //
                n = 1 //
            };
            string json = JsonSerializer.Serialize(payload);
            Url = $"http://{ip}:{port}/api/v1/generate";
            Content = new(json, Encoding.UTF8, "application/json");
        }
    } // запрос для перевода текста
}