using PragmaticAnalyzer.Abstractions;
using PragmaticAnalyzer.Enums;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace PragmaticAnalyzer.WorkingServer.Train
{
    public class RequestTrain : IRequest
    {
        public string Url { get; private set; } // url-адрес сервера
        public StringContent? Content { get; private set; } // контент, отправляемый серверу

        public RequestTrain(string ip, string port, Algorithm nameAlgorithm)
        {
            var payload = new
            {
                NameAlgorithm = nameAlgorithm.ToString() // наименование используемого алгоритма
            };
            string json = JsonSerializer.Serialize(payload);
            Url = $"http://{ip}:{port}/Train";
            Content = new(json, Encoding.UTF8, "application/json");
        } // запрос для обучения новой языковой модели на основе используемых баз данных
    }
}