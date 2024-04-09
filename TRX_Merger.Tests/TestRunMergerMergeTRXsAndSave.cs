using Bogus;
using FluentAssertions;
using TRX_Merger.TrxModel;
using TRX_Merger.Utilities;

namespace TRX_Merger.Tests
{
    /// <summary>
    /// Tests for TestRunMerger MergeTRXsAndSave method
    /// </summary>
    public class TestRunMergerMergeTRXsAndSave : IDisposable
    {
        private readonly string _targetDirectory;
        private readonly string _outputFile;

        public TestRunMergerMergeTRXsAndSave()
        {
            var directoryName = Guid.NewGuid().ToString();
            _targetDirectory = Path.Combine(Directory.GetCurrentDirectory(), directoryName);
            if (!Directory.Exists(_targetDirectory))
            {
                Directory.CreateDirectory(_targetDirectory);
            }
            var faker = new Faker();
            _outputFile = faker.System.FileName("xml");
        }

        [Fact]
        public void SavesAndReturnsCombinedTrx()
        {
            var testRuns = TestRunGenerator.GenerateTestRuns();
            var trxFiles = new List<string>();
            var faker = new Faker();
            foreach (var testRun in testRuns) 
            {
                var fileName = faker.System.FileName("xml");
                var targetPath = Path.Combine(_targetDirectory, fileName);
                TRXSerializationUtils.SerializeAndSaveTestRun(testRun, targetPath);
                trxFiles.Add(targetPath);
            }
            var allResults = testRuns.SelectMany(tr => tr.Results).ToList();
            var allTestDefinitions = testRuns.SelectMany(tr => tr.TestDefinitions).ToList();
            var allTestEntries = testRuns.SelectMany(tr => tr.TestEntries).ToList();
            var allTestLists = testRuns.SelectMany(tr => tr.TestLists).ToList();
            var allRunInfos = testRuns.SelectMany(tr => tr.ResultSummary.RunInfos).ToList();
            var firstTestRun = testRuns.First();
            var startDate = testRuns.MinBy(tr => DateTime.Parse(tr.Times.Start))?.Times.Start;
            var endDate = testRuns.MaxBy(tr => DateTime.Parse(tr.Times.Finish))?.Times.Finish;
            const string passed = "Passed";
            const string failed = "Failed";
            var expectedTestRun = new TestRun
            {
                Id = Guid.NewGuid().ToString(),
                Name = firstTestRun.Name,
                RunUser = firstTestRun.RunUser,
                Times = new Times
                {
                    Start = startDate,
                    Queuing = startDate,
                    Creation = startDate,
                    Finish = endDate
                },
                Results = allResults,
                TestDefinitions = allTestDefinitions,
                TestEntries = allTestEntries,
                TestLists = allTestLists,
                ResultSummary = new ResultSummary
                {
                    RunInfos = allRunInfos,
                    Counters = new Counters
                    {
                        Aborted = testRuns.Sum(tr => tr.ResultSummary.Counters.Aborted),
                        Completed = testRuns.Sum(tr => tr.ResultSummary.Counters.Completed),
                        Disconnected = testRuns.Sum(tr => tr.ResultSummary.Counters.Disconnected),
                        Еxecuted = testRuns.Sum(tr => tr.ResultSummary.Counters.Еxecuted),
                        Failed = testRuns.Sum(tr => tr.ResultSummary.Counters.Failed),
                        Inconclusive = testRuns.Sum(tr => tr.ResultSummary.Counters.Inconclusive),
                        InProgress = testRuns.Sum(tr => tr.ResultSummary.Counters.InProgress),
                        NotExecuted = testRuns.Sum(tr => tr.ResultSummary.Counters.NotExecuted),
                        NotRunnable = testRuns.Sum(tr => tr.ResultSummary.Counters.NotRunnable),
                        Passed = testRuns.Sum(tr => tr.ResultSummary.Counters.Passed),
                        PassedButRunAborted = testRuns.Sum(tr => tr.ResultSummary.Counters.PassedButRunAborted),
                        Pending = testRuns.Sum(tr => tr.ResultSummary.Counters.Pending),
                        Timeout = testRuns.Sum(tr => tr.ResultSummary.Counters.Timeout),
                        Total = testRuns.Sum(tr => tr.ResultSummary.Counters.Total),
                        Warning = testRuns.Sum(tr => tr.ResultSummary.Counters.Warning)
                    },
                    Outcome = testRuns.All(tr => tr.ResultSummary.Outcome == passed) ? passed : failed
                }
            };

            var actualTestRun = TestRunMerger.MergeTRXsAndSave(trxFiles, _outputFile);

            expectedTestRun.Id = actualTestRun.Id;

            actualTestRun.Should().BeEquivalentTo(expectedTestRun, opt =>
                opt.For(tr => tr.Results).Exclude(r => r.RelativeResultsDirectory)
                .For(tr => tr.Results).Exclude(r => r.Output.StdOut));

            Assert.True(File.Exists(_outputFile));

            var actualSavedTestRun = TRXSerializationUtils.DeserializeTRX(_outputFile);
            actualSavedTestRun.Should().BeEquivalentTo(expectedTestRun, opt =>
                opt.For(tr => tr.Results).Exclude(r => r.RelativeResultsDirectory)
                .For(tr => tr.Results).Exclude(r => r.Output.StdOut));
        }

        public void Dispose()
        {
            if (Directory.Exists(_targetDirectory))
            {
                Directory.Delete(_targetDirectory, true);
            }
            if (File.Exists(_outputFile))
            {
                File.Delete(_outputFile);
            }
        }
    }
}
