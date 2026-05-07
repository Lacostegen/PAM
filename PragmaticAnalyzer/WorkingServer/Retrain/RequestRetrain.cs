using PragmaticAnalyzer.Abstractions;
using PragmaticAnalyzer.Enums;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace PragmaticAnalyzer.WorkingServer.Retrain
{
    public class RequestRetrain : IRequest
    {
        public string Url { get; private set; } // url-адрес сервера
        public StringContent? Content { get; private set; } // // контент, отправляемый серверу

        public RequestRetrain(string ip, string port, string modelPath, Algorithm nameAlgorithm)
        {
            var payload = new
            {
                ModelPath = modelPath, // абсолютный путь к используемой модели
                NameAlgorithm = nameAlgorithm.ToString() // наименование используемого алгоритма
            };
            string json = JsonSerializer.Serialize(payload);
            Url = $"http://{ip}:{port}/Retrain";
            Content = new(json, Encoding.UTF8, "application/json");
        }
    } // запрос к серверу на переобучение языковой модели
}