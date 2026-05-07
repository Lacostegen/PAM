namespace PragmaticAnalyzer.WorkingServer.Retrain
{
    public class ResponseRetrain
    {
        public string ModelPath { get; set; } // абсолютный путь по которому сохранена переобученная модель

        public ResponseRetrain() { }
    } // ответ для запроса переобучения языковой модели
}