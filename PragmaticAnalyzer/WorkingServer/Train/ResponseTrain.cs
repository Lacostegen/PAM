namespace PragmaticAnalyzer.WorkingServer.Train
{
    public class ResponseTrain
    {
        public string ModelPath { get; set; } // абсолютный путь по которому сохранена обученная модель

        public ResponseTrain() { }
    } // ответ для запроса обучения языковой модели
}