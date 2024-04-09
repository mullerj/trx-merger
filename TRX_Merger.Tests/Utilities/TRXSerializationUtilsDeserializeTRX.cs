using Bogus;
using FluentAssertions;
using System.Text.RegularExpressions;
using System.Xml.Linq;
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

            var xDocument = XDocument.Load(_targetPath);
            xDocument.Root?.Attribute("runUser")?.Remove();
            xDocument.Save(_targetPath);

            var actualTestRun = TRXSerializationUtils.DeserializeTRX(_targetPath);

            actualTestRun.Should().BeEquivalentTo(expectedTestRun, opt =>
                opt.For(tr => tr.Results).Exclude(r => r.RelativeResultsDirectory)
                .For(tr => tr.Results).Exclude(r => r.Output.StdOut));
        }

        [Fact]
        public void ReturnsTestRunWithoutNamespace()
        {
            var expectedTestRun = TestRunGenerator.GenerateTestRun();

            TRXSerializationUtils.SerializeAndSaveTestRun(expectedTestRun, _targetPath);

            var xmlContent = File.ReadAllText(_targetPath);
            xmlContent = Regex.Replace(xmlContent, "xmlns=\".*\"", string.Empty);
            File.WriteAllText(_targetPath, xmlContent);

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
