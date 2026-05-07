using PragmaticAnalyzer.Abstractions;
using PragmaticAnalyzer.Core;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace PragmaticAnalyzer.Databases
{
    public class VulnerabilitieNvd : ViewModelBase, IHasId, IHasDescription
    {
        public Guid GuidId { get; set; } = Guid.NewGuid(); // уникальный идентификатор

        [Description("Идентификатор")]
        public string Identifier { get => Get<string>(); set => Set(value); }

        [Description("Описание уязвимости")]
        public string Description { get => Get<string>(); set => Set(value); }

        [Description("Дата публикации")]
        public string Published { get => Get<string>(); set => Set(value); }

        [Description("Дата последнего изменения")]
        public string LastModified { get => Get<string>(); set => Set(value); }

        [Description("Статус по выявленной уязвимости")]
        public string VulnStatus { get => Get<string>(); set => Set(value); }

        [Description("CVSS")]
        public string VectorString { get => Get<string>(); set => Set(value); }

        [Description("Полезные ссылки")]
        public ObservableCollection<string> References = [];

        [JsonIgnore]
        public string ReferencesToString => string.Join(",\n", References);

        public VulnerabilitieNvd Clone()
        {
            VulnerabilitieNvd vulnerabilitieNvd = new()
            {
                GuidId = GuidId,
                Identifier = Identifier,
                Description = Description,
                Published = Published,
                LastModified = LastModified,
                VulnStatus = VulnStatus,
                References = References,
                VectorString = VectorString
            };

            return vulnerabilitieNvd;
        }
    } // представление уязвимости NVD

    public class NvdApiResponse
    {
        public int TotalResults { get; set; }
        public List<VulnerabilityWrapper> Vulnerabilities { get; set; } = new();
    }

    public class VulnerabilityWrapper
    {
        public CveData Cve { get; set; } = new();
    }

    public class CveData
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Published { get; set; }
        public DateTime LastModified { get; set; }
        public string VulnStatus { get; set; } = string.Empty;
        public List<DescriptionItem> Descriptions { get; set; } = new();
        public MetricsContainer? Metrics { get; set; }
        public List<ReferenceItem> References { get; set; } = new();
    }

    public class DescriptionItem
    {
        public string Lang { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class ReferenceItem
    {
        public string Url { get; set; } = string.Empty;
    }

    public class MetricsContainer
    {
        public List<CvssMetric>? CvssMetricV31 { get; set; }
        public List<CvssMetric>? CvssMetricV30 { get; set; }
        public List<CvssMetricV2>? CvssMetricV2 { get; set; }
    }

    public class CvssMetric
    {
        public CvssData? CvssData { get; set; }
    }

    public class CvssMetricV2
    {
        public CvssDataV2? CvssData { get; set; }
    }

    public class CvssData
    {
        public string Version { get; set; } = string.Empty;
        public string VectorString { get; set; } = string.Empty;
    }

    public class CvssDataV2
    {
        public string Version { get; set; } = string.Empty;
        public string VectorString { get; set; } = string.Empty;
    }
}