using Bogus;
using FluentAssertions;
using System.Reflection;
using System.Text.Json;
using System.Xml.Serialization;
using TRX_Merger.TrxModel;
using TRX_Merger.Utilities;

namespace TRX_Merger.Tests.Utilities
{
    /// <summary>
    /// Tests for TRXSerializationUtils SerializeAndSaveTestRun method
    /// </summary>
    public class TRXSerializationUtilsSerializeAndSaveTestRun : IDisposable
    {
        private readonly string _targetPath;

        public TRXSerializationUtilsSerializeAndSaveTestRun()
        {
            var faker = new Faker();
            _targetPath = faker.System.FileName("xml");
        }

        [Fact]
        public void SavesTestRuntoTargetPath()
        {
            var expectedTestRun = TestRunGenerator.GenerateTestRun();

            var expectedResult = Path.Combine(Directory.GetCurrentDirectory(), _targetPath);

            var actualResult = TRXSerializationUtils.SerializeAndSaveTestRun(expectedTestRun, _targetPath);

            Assert.Equal(expectedResult, actualResult);
            Assert.True(File.Exists(expectedResult));

            // Set RelativeResultsDirectory to null since it is not serialized
            foreach (var testResult in expectedTestRun.Results)
            {
                testResult.RelativeResultsDirectory = null;
            }

            var xmlAttributeOverrides = new XmlAttributeOverrides();
            var rootType = typeof(TestRun);
            var rootNamespace = rootType.Namespace;
            var assembly = Assembly.GetAssembly(rootType);
            var types = assembly?.GetTypes().Where(type => type.Namespace == rootNamespace) ?? [];
            var excludedMembers = new[] 
            { 
                typeof(RunInfo).GetProperty(nameof(RunInfo.Text)),
                typeof(UnitTestResultOutput).GetProperty(nameof(UnitTestResultOutput.StdOut)),
                typeof(UnitTestResultOutput).GetProperty(nameof(UnitTestResultOutput.StdErr)),
                typeof(ErrorInfo).GetProperty(nameof(ErrorInfo.Message)),
                typeof(ErrorInfo).GetProperty(nameof(ErrorInfo.StackTrace)),
                typeof(Counters).GetProperty(nameof(Counters.Еxecuted))
            };

            foreach (var type in types)
            {
                var attributeProperties = type.GetProperties().Where(p => !p.PropertyType.IsGenericType && p.PropertyType.Namespace != rootNamespace && !excludedMembers.Contains(p));
                foreach (var property in attributeProperties)
                {
                    var xmlAttributes = new XmlAttributes();
                    var xmlAttributeAttribute = new XmlAttributeAttribute(JsonNamingPolicy.CamelCase.ConvertName(property.Name));
                    xmlAttributes.XmlAttribute = xmlAttributeAttribute;
                    xmlAttributeOverrides.Add(property.DeclaringType!, property.Name, xmlAttributes);
                }
            }

            var customXmlAttributes = new XmlAttributes();
            var customXmlAttributeAttribute = new XmlAttributeAttribute("executed");
            customXmlAttributes.XmlAttribute = customXmlAttributeAttribute;
            xmlAttributeOverrides.Add(typeof(Counters), nameof(Counters.Еxecuted), customXmlAttributes);

            var defaultNamespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
            var serializer = new XmlSerializer(rootType, xmlAttributeOverrides, [], null, defaultNamespace);
            using var reader = new StreamReader(expectedResult);
            var actualTestRun = serializer.Deserialize(reader) as TestRun;
            actualTestRun.Should().BeEquivalentTo(expectedTestRun);
        }

        public void Dispose()
        {
            if (File.Exists(_targetPath))
            {
                File.Delete(_targetPath);
            }
        }
    }
}
 