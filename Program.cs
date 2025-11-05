using System.Text;
using HtmlAgilityPack;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

string yamlFile = @"C:\Projeler\CVForge\examples\example.yaml";

string yamlContent = File.ReadAllText(yamlFile);

CVForgeValue cv = CVForgeValue.FromYaml(yamlContent);
cv = cv.FilterTags(new List<string> { "cs" });
Console.WriteLine(cv.ToYaml());
