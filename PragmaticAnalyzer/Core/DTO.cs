using PragmaticAnalyzer.Enums;

namespace PragmaticAnalyzer.Core
{
    public class DTO<T>
    {
        public required DataType DtoType { get; set; } // тип хранимого файла
        public required T Value { get; set; } // значение хранимого файла
        public required DateTime DateCreation { get; set; } // дата упаковки хранимого файла
    } // представления объекта для хранения файлов программы
}