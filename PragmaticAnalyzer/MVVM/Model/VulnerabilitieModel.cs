using Aspose.Cells;
using PragmaticAnalyzer.Configs;
using PragmaticAnalyzer.Databases;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Xml.Serialization;

namespace PragmaticAnalyzer.MVVM.Model
{
    public class VulnerabilityModel
    {
        private readonly HttpClient _httpClient; // _httpClient для обращения к веб-ресурсам
        private readonly VulConfig _vulConfig; // конфигурационный файл настройки парсинга уязвимостей
        public event Action<string>? NotifyRequested; // Action для оповещения vm

        public VulnerabilityModel(VulConfig vulConfig)
        {
            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback =
               (httpRequestMessage, cert, cetChain, policyErrors) => { return true; }
            };
            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(60)
            };
            _vulConfig = vulConfig;
        }

        public async Task<ObservableCollection<VulnerabilitieFstec>> GetByLink(CancellationToken ct)
        {
            ObservableCollection<VulnerabilitieFstec> vulnerabilities = [];
            using HttpRequestMessage request = new(HttpMethod.Get, _vulConfig.UrlFstec);
            NotifyRequested?.Invoke("Начало работы с БД ФСТЭК");
            NotifyRequested?.Invoke($"Обращение к {_vulConfig.UrlFstec}");
            using HttpResponseMessage response = await _httpClient.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
            {
                using Stream stream = await response.Content.ReadAsStreamAsync(ct);
                using Workbook workbook = new(stream, new(LoadFormat.Xlsx));
                using Worksheet worksheet = workbook.Worksheets[0];
                var numberRows = worksheet.Cells.MaxDataRow;

                ct.ThrowIfCancellationRequested();
                NotifyRequested?.Invoke($"Обработка полученной информации от {_vulConfig.UrlFstec}");
                // for (int rowIterator = 3; rowIterator < numberRows; rowIterator++)
                for (int rowIterator = 3; rowIterator < 200; rowIterator++)
                {
                    VulnerabilitieFstec vulnerability = new()
                    {
                        Identifier = worksheet.Cells[rowIterator, 0].Value?.ToString() ?? "Информация отсутствует",
                        Name = worksheet.Cells[rowIterator, 1].Value?.ToString() ?? "Информация отсутствует",
                        Description = worksheet.Cells[rowIterator, 2].Value?.ToString() ?? "Информация отсутствует",
                        Vendor = worksheet.Cells[rowIterator, 3].Value?.ToString() ?? "Информация отсутствует",
                        NameSoftware = worksheet.Cells[rowIterator, 4].Value?.ToString() ?? "Информация отсутствует",
                        Version = worksheet.Cells[rowIterator, 5].Value?.ToString() ?? "Информация отсутствует",
                        Type = worksheet.Cells[rowIterator, 6].Value?.ToString() ?? "Информация отсутствует",
                        NameOperatingSystem = worksheet.Cells[rowIterator, 7].Value?.ToString() ?? "Информация отсутствует",
                        Class = worksheet.Cells[rowIterator, 8].Value?.ToString() ?? "Информация отсутствует",
                        Date = worksheet.Cells[rowIterator, 9].Value?.ToString() ?? "Информация отсутствует",
                        CvssTwo = worksheet.Cells[rowIterator, 10].Value?.ToString() ?? "Информация отсутствует",
                        CvssThree = worksheet.Cells[rowIterator, 11].Value?.ToString() ?? "Информация отсутствует",
                        DangerLevel = worksheet.Cells[rowIterator, 12].Value?.ToString() ?? "Информация отсутствует",
                        Measure = worksheet.Cells[rowIterator, 13].Value?.ToString() ?? "Информация отсутствует",
                        Exploit = worksheet.Cells[rowIterator, 14].Value?.ToString() ?? "Информация отсутствует",
                        Information = worksheet.Cells[rowIterator, 15].Value?.ToString() ?? "Информация отсутствует",
                        Links = worksheet.Cells[rowIterator, 16].Value?.ToString() ?? "Информация отсутствует",
                        OtherIdentifier = worksheet.Cells[rowIterator, 17].Value?.ToString() ?? "Информация отсутствует",
                        OtherInformation = worksheet.Cells[rowIterator, 18].Value?.ToString() ?? "Информация отсутствует",
                        Incident = worksheet.Cells[rowIterator, 19].Value?.ToString() ?? "Информация отсутствует",
                        OperatingMethod = worksheet.Cells[rowIterator, 20].Value?.ToString() ?? "Информация отсутствует",
                        EliminationMethod = worksheet.Cells[rowIterator, 21].Value?.ToString() ?? "Информация отсутствует",
                        DescriptionCwe = worksheet.Cells[rowIterator, 22].Value?.ToString() ?? "Информация отсутствует",
                        Cwe = worksheet.Cells[rowIterator, 23].Value?.ToString() ?? "Информация отсутствует"
                    };
                    vulnerabilities.Add(vulnerability);
                    NotifyRequested?.Invoke($"Добавлена запись {vulnerability.Identifier}");
                }
            }
            return vulnerabilities;
        } // возвращает базу данных уязвимостей ФСТЭК

        public async Task<ObservableCollection<VulnerabilitieNvd>?> GetByApiRequest(CancellationToken ct)
        {
            if (!string.IsNullOrWhiteSpace(_vulConfig.ApiKeyNvd))
            {
                _httpClient.DefaultRequestHeaders.Remove("apiKey");
                _httpClient.DefaultRequestHeaders.Add("apiKey", _vulConfig.ApiKeyNvd);
            }

            ObservableCollection<VulnerabilitieNvd> vulnerabilities = [];

            while (true)
            {
                var url = $"{_vulConfig.UrlNvd}?resultsPerPage={_vulConfig.ResultsPerPageNvd}&startIndex={_vulConfig.StartIndexNvd}";

                try
                {
                    NotifyRequested?.Invoke($"Обращение к {url}");
                    var response = await _httpClient.GetAsync(url, ct);
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync(ct);

                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var apiResponse = JsonSerializer.Deserialize<NvdApiResponse>(json, options);

                    if (apiResponse?.Vulnerabilities == null || apiResponse.Vulnerabilities.Count == 0)
                    {
                        break;
                    }

                    foreach (var wrapper in apiResponse.Vulnerabilities)
                    {
                        var cve = wrapper.Cve;

                        var enDescription = cve.Descriptions
                            .FirstOrDefault(d => d.Lang.Equals("en", StringComparison.OrdinalIgnoreCase))?.Value
                            ?? cve.Descriptions.FirstOrDefault()?.Value
                            ?? string.Empty;

                        string vector = string.Empty;
                        if (!string.IsNullOrEmpty(cve.Metrics?.CvssMetricV31?.FirstOrDefault()?.CvssData?.VectorString))
                            vector = cve.Metrics.CvssMetricV31[0].CvssData.VectorString;
                        else if (!string.IsNullOrEmpty(cve.Metrics?.CvssMetricV30?.FirstOrDefault()?.CvssData?.VectorString))
                            vector = cve.Metrics.CvssMetricV30[0].CvssData.VectorString;
                        else if (!string.IsNullOrEmpty(cve.Metrics?.CvssMetricV2?.FirstOrDefault()?.CvssData?.VectorString))
                            vector = cve.Metrics.CvssMetricV2[0].CvssData.VectorString;

                        var references = new ObservableCollection<string>(
                            cve.References
                                .Where(r => !string.IsNullOrWhiteSpace(r.Url))
                                .Select(r => r.Url)
                        );

                        var vul = new VulnerabilitieNvd
                        {
                            Identifier = cve.Id ?? "Информация отсутствует",
                            Published = cve.Published.ToString("d") ?? "Информация отсутствует",
                            LastModified = cve.LastModified.ToString("d") ?? "Информация отсутствует",
                            VulnStatus = cve.VulnStatus ?? "Информация отсутствует",
                            Description = enDescription ?? "Информация отсутствует",
                            VectorString = vector ?? "Информация отсутствует",
                            References = references
                        };

                        vulnerabilities.Add(vul);
                        NotifyRequested?.Invoke($"Добавлена запись {cve.Id}");
                    }

                    _vulConfig.StartIndexNvd += _vulConfig.ResultsPerPageNvd;

                    var delayMs = string.IsNullOrWhiteSpace(_vulConfig.ApiKeyNvd) ? 7000 : 600;
                    await Task.Delay(delayMs, ct);
                }
                catch (Exception ex)
                {
                    NotifyRequested?.Invoke($"Ошибка при startIndex={_vulConfig.StartIndexNvd}: {ex.Message}");
                    break;
                }
            }
            return vulnerabilities;
        } // возвращает базу данных уязвимостей NVD

        public async Task<ObservableCollection<VulnerabilitieJvn>?> GetByPageParsing(CancellationToken ct)
        {
            ObservableCollection<VulnerabilitieJvn> vulnerabilities = [];
            var countYears = DateTime.Now.Year - _vulConfig.StartIndexJvn;
            for (int i = 0; i <= countYears; i++)
            {
                var items = await LoadYearAsync(_vulConfig.StartIndexJvn);
                var a = items.Count;
                if (items is null) continue;
                foreach (var item in items)
                {
                    vulnerabilities.Add(new VulnerabilitieJvn()
                    {
                        Identifier = item.Identifier ?? "Информация отсутствует",
                        Description = item.Description ?? "Информация отсутствует",
                        Link = item.Link ?? "Информация отсутствует",
                        DateChange = item.Modified ?? "Информация отсутствует",
                        DateAdded = item.Date ?? "Информация отсутствует",
                        Name = item.Title ?? "Информация отсутствует",
                        Cvss = item.Cvss?.Vector ?? "Информация отсутствует"
                    });
                    var vul = vulnerabilities.Last();
                    foreach (var reference in item.References)
                    {
                        vul.References.Add(reference?.Url ?? "Информация отсутствует");
                    }
                    foreach (var cpe in item.Cpes)
                    {
                        vul.Vendor.Add(cpe?.Vendor ?? "Информация отсутствует");
                        vul.NameSoftware.Add(cpe?.Product ?? "Информация отсутствует");
                        vul.VersionSoftware.Add(cpe?.Version ?? "Информация отсутствует");
                    }
                    NotifyRequested?.Invoke($"Добавлена запись {item.Identifier}");
                }

                _vulConfig.StartIndexJvn++;
                await Task.Delay(1000, ct);
            }

            return vulnerabilities;

            async Task<List<Item>?> LoadYearAsync(int year)
            {
                string url = $"{_vulConfig.UrlJvn}{year}.rdf";
                NotifyRequested?.Invoke($"Обращение к {url}");
                try
                {
                    var xml = await _httpClient.GetStringAsync(url, ct);

                    xml = xml.Replace("  \"", "\"");

                    var serializer = new XmlSerializer(typeof(JvnRdf));
                    using var reader = new StringReader(xml);
                    var rdf = (JvnRdf)serializer.Deserialize(reader);

                    return rdf.Items;
                }
                catch (Exception ex)
                {
                    NotifyRequested?.Invoke($"Ошибка при обработке {url}.\nПодробности:\n{ex.Message}");
                    return null;
                }
            }
        } // возвращает базу данных уязвимостей JVN
    }
}