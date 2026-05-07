using PragmaticAnalyzer.Core;

namespace PragmaticAnalyzer.Configs
{
    public class VulConfig : ViewModelBase
    {
        public string UrlFstec { get => Get<string>(); set => Set(value); } // url-адресс для парсинга с сайта ФСТЭК
        public string UrlNvd { get => Get<string>(); set => Set(value); } // url-адресс для парсинга с сайта NVD
        public string UrlJvn { get => Get<string>(); set => Set(value); } // url-адресс для парсинга с сайта JVN
        public string ApiKeyNvd { get => Get<string>(); set => Set(value); } // API-ключ для API NVD
        public int StartIndexNvd { get => Get<int>(); set => Set(value); } // последняя добавленная запись из NVD
        public int StartIndexJvn { get => Get<int>(); set => Set(value); } // последняя добавленная запись из JVN
        public int ResultsPerPageNvd { get => Get<int>(); set => Set(value); } // количество получаемых уязвимостей с одного API-обращения к NVD

        public VulConfig()
        {
            UrlFstec = "https://bdu.fstec.ru/files/documents/vullist.xlsx";
            UrlNvd = "https://services.nvd.nist.gov/rest/json/cves/2.0";
            UrlJvn = "https://jvndb.jvn.jp/en/rss/years/jvndb_";
            ApiKeyNvd = "";
            StartIndexNvd = 0;
            StartIndexJvn = 2002;
            ResultsPerPageNvd = 2000;
        }

        public void Update(VulConfig newVulConfig)
        {
            if (newVulConfig is null) return;

            UrlFstec = newVulConfig.UrlFstec;
            UrlNvd = newVulConfig.UrlNvd;
            UrlJvn = newVulConfig.UrlJvn;
            ApiKeyNvd = newVulConfig.ApiKeyNvd;
            StartIndexNvd = newVulConfig.StartIndexNvd;
        } // клонирование текущего объекта
    } // объект, хранящий настройки для парсинга уязвимостей
}