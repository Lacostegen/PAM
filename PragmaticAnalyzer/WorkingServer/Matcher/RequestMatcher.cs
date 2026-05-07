using PragmaticAnalyzer.Abstractions;
using PragmaticAnalyzer.Enums;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace PragmaticAnalyzer.WorkingServer.Matcher
{
    public class RequestMatcher : IRequest
    {
        public string Url { get; private set; } // url-адрес сервера
        public StringContent? Content { get; private set; } // контент, отправляемый серверу

        public RequestMatcher(string ip, string port, string textDescription, Algorithm algorithm, bool filteringCvss, string? usedModel, List<string> usedSources)
        {
            var payload = new
            {
                TextDescription = textDescription, // текст для сопоставления
                ModelName = algorithm.ToString(), // наименование используемого алгоритма
                FilteringCvss = filteringCvss, // необходима ли фильтрация по CVSS
                UsedModel = usedModel, // абсолютный путь к используемой модели
                UsedSources = usedSources // список используемых источников для сопоставления
            };
            string json = JsonSerializer.Serialize(payload);
            Url = $"http://{ip}:{port}/Matcher";
            Content = new(json, Encoding.UTF8, "application/json");
        }
    } // запрос для сопоставления источников 
}