using PragmaticAnalyzer.Abstractions;
using PragmaticAnalyzer.Core;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Security.Policy;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace PragmaticAnalyzer.Databases
{
    public class VulnerabilitieJvn : ViewModelBase, IHasId, IHasDescription
    {
        public Guid GuidId { get; set; } = Guid.NewGuid(); // уникальный идентификатор

        [Description("Идентификатор")]
        public string Identifier { get => Get<string>(); set => Set(value); }

        [Description("Описание уязвимости")]
        public string Description { get => Get<string>(); set => Set(value); }

        [Description("Ссылка на уязвимость")]
        public string Link { get => Get<string>(); set => Set(value); }

        [Description("Дата изменения")]
        public string DateChange { get => Get<string>(); set => Set(value); }

        [Description("Дата добавления")]
        public string DateAdded { get => Get<string>(); set => Set(value); }

        [Description("Полезные ссылки")]
        public ObservableCollection<string> References { get; set; } = [];

        [Description("Название")]
        public string Name { get => Get<string>(); set => Set(value); }

        [Description("CVSS")]
        public string Cvss { get => Get<string>(); set => Set(value); }

        [Description("Вендор ПО")]
        public ObservableCollection<string> Vendor { get; set; } = [];

        [Description("Нзвание ПО")]
        public ObservableCollection<string> NameSoftware { get; set; } = [];

        [Description("Версия ПО")]
        public ObservableCollection<string> VersionSoftware { get; set; } = [];

        [JsonIgnore]
        public string ReferencesToString => string.Join(",\n", References);

        [JsonIgnore]
        public string VendorToString => string.Join(",\n", Vendor);

        [JsonIgnore]
        public string NameSoftwareToString => string.Join(",\n", NameSoftware);

        [JsonIgnore]
        public string VersionSoftwareToString => string.Join(",\n", VersionSoftware);

        public VulnerabilitieJvn Clone()
        {
            VulnerabilitieJvn vulnerabilitieJvn = new()
            {
                GuidId = GuidId,
                Identifier = Identifier,
                Description = Description,
                Link = Link,
                DateChange = DateChange,
                DateAdded = DateAdded,
                References = References,
                Name = Name,
                Cvss = Cvss,
                Vendor = Vendor,
                NameSoftware = NameSoftware,
                VersionSoftware = VersionSoftware
            };

            return vulnerabilitieJvn;
        }
    } // представление уязвимости JVN

    [XmlRoot("RDF", Namespace = "http://www.w3.org/1999/02/22-rdf-syntax-ns#")]
    public class JvnRdf
    {
        [XmlElement("channel", Namespace = "http://purl.org/rss/1.0/")]
        public Channel Channel { get; set; }

        [XmlElement("item", Namespace = "http://purl.org/rss/1.0/")]
        public List<Item> Items { get; set; } = [];
    }

    public class Channel
    {
        [XmlElement("title", Namespace = "http://purl.org/rss/1.0/")]
        public string Title { get; set; }

        [XmlElement("link", Namespace = "http://purl.org/rss/1.0/")]
        public string Link { get; set; }

        [XmlElement("description", Namespace = "http://purl.org/rss/1.0/")]
        public string Description { get; set; }

        [XmlElement("date", Namespace = "http://purl.org/dc/elements/1.1/")]
        public string Date { get; set; }

        [XmlElement("modified", Namespace = "http://purl.org/dc/terms/")]
        public string Modified { get; set; }
    }

    public class Item
    {
        [XmlAttribute("about", Namespace = "http://www.w3.org/1999/02/22-rdf-syntax-ns#")]
        public string About { get; set; }

        [XmlElement("title", Namespace = "http://purl.org/rss/1.0/")]
        public string Title { get; set; }

        [XmlElement("link", Namespace = "http://purl.org/rss/1.0/")]
        public string Link { get; set; }

        [XmlElement("description", Namespace = "http://purl.org/rss/1.0/")]
        public string DescriptionRaw { get; set; }

        [XmlIgnore]
        public string Description
        {
            get
            {
                if (string.IsNullOrEmpty(DescriptionRaw))
                    return string.Empty;
                var clean = Regex.Replace(DescriptionRaw, @"<[^>]*>", "");
                clean = Regex.Replace(clean, @"\s+", " ");
                return clean.Trim();
            }
        }

        [XmlElement("identifier", Namespace = "http://jvn.jp/rss/mod_sec/")]
        public string Identifier { get; set; }

        [XmlElement("references", Namespace = "http://jvn.jp/rss/mod_sec/")]
        public List<Reference> References { get; set; } = new List<Reference>();

        [XmlElement("cpe", Namespace = "http://jvn.jp/rss/mod_sec/")]
        public List<Cpe> Cpes { get; set; } = new List<Cpe>();

        [XmlElement("cvss", Namespace = "http://jvn.jp/rss/mod_sec/")]
        public Cvss Cvss { get; set; }

        [XmlElement("date", Namespace = "http://purl.org/dc/elements/1.1/")]
        public string Date { get; set; }

        [XmlElement("issued", Namespace = "http://purl.org/dc/terms/")]
        public string Issued { get; set; }

        [XmlElement("modified", Namespace = "http://purl.org/dc/terms/")]
        public string Modified { get; set; }
    }

    public class Reference
    {
        [XmlAttribute("source")]
        public string Source { get; set; }

        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlText]
        public string Url { get; set; }
    }

    public class Cpe
    {
        [XmlAttribute("version")]
        public string Version { get; set; }

        [XmlAttribute("vendor")]
        public string Vendor { get; set; }

        [XmlAttribute("product")]
        public string Product { get; set; }

        [XmlText]
        public string CpeUri { get; set; }
    }

    public class Cvss
    {
        [XmlAttribute("version")]
        public string Version { get; set; }

        [XmlAttribute("score")]
        public string Score { get; set; }

        [XmlAttribute("type")]
        public string Type { get; set; }

        [XmlAttribute("severity")]
        public string Severity { get; set; }

        [XmlAttribute("vector")]
        public string Vector { get; set; }
    }
}