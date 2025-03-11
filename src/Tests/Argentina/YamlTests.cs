using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Clarius.OpenLaw.Argentina;

public class YamlTests(ITestOutputHelper output)
{
    [Fact]
    public void YamlPascalCase()
    {
        var json =
            """
            {
              "id": "123456789-0abc-104-0000-4202xvorpced",
              "type": "legislacion",
              "alias": "X20240000401",
              "ref": "DEC X 000401 2024 12 27",
              "name": "DECRETO 401/2024",
              "number": 401,
              "title": "Convocatoria a Elecciones Primarias",
              "summary": "",
              "kind": {
                "code": "DEC",
                "text": "Decreto"
              },
              "status": "Vigente, de alcance general",
              "date": "2024-12-27",
              "modified": "2024-12-30",
              "timestamp": 1735561866,
              "terms": [
                "Derecho constitucional",
                "convocatoria a elecciones",
                "convocatoria electoral (convocatoria a elecciones)",
                "derecho electoral",
                "derechos políticos",
                "elecciones",
                "elecciones primarias",
                "proceso electoral"
              ],
              "pub": {
                "org": "Boletín Oficial",
                "date": "2024-12-27"
              }
            }
            """;

        var doc = new TestDocument { Id = "123456789-0abc-104-0000-4202xvorpced", JQ = json };
        var yaml = doc.ToFrontMatter();

        Assert.Equal(
            """
            Fecha: 2024-12-27
            Título: DECRETO 401/2024
            Publicación:
              Organismo: Boletín Oficial
              Fecha: 2024-12-27
            Código SAIJ: X20240000401
            Id: 123456789-0abc-104-0000-4202xvorpced
            Timestamp: 1735561866
            Web: https://www.saij.gob.ar/123456789-0abc-104-0000-4202xvorpced
            Datos: https://www.saij.gob.ar/view-document?guid=123456789-0abc-104-0000-4202xvorpced
            """, yaml);
    }

    [Fact]
    public void OmitsEmptyStringsKeepsDefaultEnum()
    {
        var yaml = new TestDocument { Id = "asdf" }.ToYaml();
        Assert.Equal(
            """
            Id: asdf
            ContentType: Legislacion
            """.ReplaceLineEndings(), yaml.Trim().ReplaceLineEndings());

    }

    class TestDocument : IWebDocument
    {
        public string Id { get; set; } = "";

        public string Json { get; set; } = "";

        public ContentType ContentType { get; set; } = ContentType.Legislacion;

        public string JQ { get; set; } = "";

        public Dictionary<string, object?> Data => new();
    }
}
