using Bogus;
using FluentAssertions;
using System.Xml;
using TRX_Merger.Utilities;

namespace TRX_Merger.Tests.Utilities
{
    /// <summary>
    /// Tests for TRXSerializationUtils DeserializeTRX method
    /// </summary>
    public class TRXSerializationUtilsDeserializeTRX : IDisposable
    {
        private readonly string _targetPath;

        public TRXSerializationUtilsDeserializeTRX()
        {
            var faker = new Faker();
            _targetPath = faker.System.FileName("xml");
        }

        [Fact]
        public void ReturnsTestRun()
        {
            var expectedTestRun = TestRunGenerator.GenerateTestRun();

            TRXSerializationUtils.SerializeAndSaveTestRun(expectedTestRun, _targetPath);

            var actualTestRun = TRXSerializationUtils.DeserializeTRX(_targetPath);

            actualTestRun.Should().BeEquivalentTo(expectedTestRun, opt =>
                opt.For(tr => tr.Results).Exclude(r => r.RelativeResultsDirectory)
                .For(tr => tr.Results).Exclude(r => r.Output.StdOut));
        }

        [Fact]
        public void ReturnsTestRunWithNullRunUser()
        {
            var expectedTestRun = TestRunGenerator.GenerateTestRun();

            TRXSerializationUtils.SerializeAndSaveTestRun(expectedTestRun, _targetPath);

            expectedTestRun.RunUser = string.Empty;

            var xmlDocument = new XmlDocument();
            xmlDocument.Load(_targetPath);
            var namespaceManager = new XmlNamespaceManager(xmlDocument.NameTable);
            var namespaceUri = xmlDocument.DocumentElement?.NamespaceURI;
            var ns = "ns";
            if (namespaceUri != null)
            {
                namespaceManager.AddNamespace("ns", namespaceUri);
            }
            var testRunNode = xmlDocument.SelectSingleNode($"/{ns}:TestRun", namespaceManager);
            var runUserAttribute = testRunNode?.Attributes?["runUser"];
            testRunNode?.Attributes?.Remove(runUserAttribute);
            xmlDocument.Save(_targetPath);

            var actualTestRun = TRXSerializationUtils.DeserializeTRX(_targetPath);

            actualTestRun.Should().BeEquivalentTo(expectedTestRun, opt =>
                opt.For(tr => tr.Results).Exclude(r => r.RelativeResultsDirectory)
                .For(tr => tr.Results).Exclude(r => r.Output.StdOut));
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
