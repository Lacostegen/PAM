using PragmaticAnalyzer.Core;

namespace PragmaticAnalyzer.Configs
{
    /// <summary>
    /// Объект, хранящий настройки для парсинга угроз
    /// </summary>
    public class ThreatConfig : ViewModelBase
    {
        public string ParsingFstecUrl {  get => Get<string>(); set => Set(value); } // url-адресс для парсинга угроз с сайта ФСТЭК

        public ThreatConfig()
        {
            ParsingFstecUrl = "https://bdu.fstec.ru/files/documents/thrlist.xlsx";
        }

        public void Update(ThreatConfig newThreatConfig)
        {
            if (newThreatConfig is null) return;

            ParsingFstecUrl = newThreatConfig.ParsingFstecUrl;
        } // клонирование текущего объекта
    }
}