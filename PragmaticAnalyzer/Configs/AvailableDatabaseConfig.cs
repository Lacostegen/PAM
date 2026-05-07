using PragmaticAnalyzer.Enums;
using System.Text.Json.Serialization;

namespace PragmaticAnalyzer.Configs
{
    /// <summary>
    ///  Объект, хранящий информацию о базе данных, которая используется приложением
    /// </summary>
    public class AvailableDatabaseConfig(string displayedName, string fullName, long sizeBytes, DateTime lastModified, DataType detectedType)
    {
        [JsonIgnore]
        public bool IsChecked { get; set; } = true; // выбрана ли база данных в настройках при поиске
        public string DisplayedName { get; private set; } = displayedName; // отображаемое название базы данных
        public string FullName { get; private set; } = fullName; // абсолютный путь к базе данных
        public long SizeBytes { get; private set; } = sizeBytes; // размер базы данных
        public DateTime LastModified { get; private set; } = lastModified; // последнее изменение базы данных
        public DataType DetectedType { get; private set; } = detectedType; // тип базы данных из перечисления DataType
    }
}