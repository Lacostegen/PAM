namespace PragmaticAnalyzer.WorkingServer.Core
{
    public class Result<T>
    {
        public bool IsSuccess { get; } // возвращает true если код от сервера 200, а иначе false
        public T Value { get; } // тело ответа
        public string ErrorMessage { get; } // сообщение об ошибке

        private Result(bool isSuccess, T value, string errorMessage)
        {
            IsSuccess = isSuccess;
            Value = value;
            ErrorMessage = errorMessage;
        }

        public static Result<T> Success(T value) => new(true, value, null); // возвращает экземляр класса для 200 кода от сервера
        public static Result<T> Failure(string errorMessage) => new(false, default, errorMessage); // возвращает экземляр класса для отличного от 200 кода сервера
    } // обертка для ответ от сервера
}