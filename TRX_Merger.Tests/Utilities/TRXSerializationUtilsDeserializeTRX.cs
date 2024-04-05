using Bogus;
using FluentAssertions;
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

            // Set RelativeResultsDirectory to null since it is not serialized
            foreach (var testResult in expectedTestRun.Results)
            {
                testResult.RelativeResultsDirectory = null;
            }

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
