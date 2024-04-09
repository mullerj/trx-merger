using Bogus;
using System.Globalization;
using TRX_Merger.TrxModel;

namespace TRX_Merger.Tests
{
    public static class TestRunGenerator
    {
        private static string ToTitleCase(string str) => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str);

        public static TestRun GenerateTestRun()
        {
            var testMethodFaker = new Faker<TestMethod>()
                .RuleFor(tm => tm.CodeBase, f => f.System.FilePath())
                .RuleFor(tm => tm.AdapterTypeName, f => f.System.FileName())
                .RuleFor(tm => tm.ClassName, f => string.Join('.', f.Make(f.Random.Int(3, 5), f.Hacker.Noun).Select(cn => ToTitleCase(cn))))
                .RuleFor(tm => tm.Name, f => ToTitleCase(f.Hacker.Verb()));
            var executionFaker = new Faker<Execution>()
                .RuleFor(e => e.Id, Guid.NewGuid().ToString());
            var unitTestFaker = new Faker<UnitTest>()
                .RuleFor(ut => ut.Id, Guid.NewGuid().ToString())
                .RuleFor(ut => ut.TestMethod, () => testMethodFaker.Generate(1).First())
                .RuleFor(ut => ut.Name, (f, ut) => $"{ut.TestMethod.ClassName}.{ut.TestMethod.Name}")
                .RuleFor(ut => ut.Storage, (f, ut) => ut.TestMethod.CodeBase)
                .RuleFor(ut => ut.Execution, () => executionFaker.Generate(1).First());

            var testListFaker = new Faker<TestList>()
                .RuleFor(tl => tl.Id, () => Guid.NewGuid().ToString())
                .RuleFor(tl => tl.Name, f => f.Lorem.Text());
            var testLists = testListFaker.GenerateBetween(2, 10);

            var testEntryFaker = new Faker<TestEntry>()
                .RuleFor(te => te.TestListId, f => f.PickRandom(testLists).Id);

            var errorInfoFaker = new Faker<ErrorInfo>()
                .RuleFor(ei => ei.Message, f => f.System.Exception().Message)
                .RuleFor(ei => ei.StackTrace, f => f.System.Exception().StackTrace);

            var unitTestResultOutputFaker = new Faker<UnitTestResultOutput>()
                .RuleFor(uo => uo.ErrorInfo, () => errorInfoFaker.Generate(1).First())
                .RuleFor(uo => uo.StdErr, (f, uo) => uo.ErrorInfo.StackTrace)
                .RuleFor(uo => uo.StdOut, (f, uo) => uo.ErrorInfo.Message);

            const string passed = "Passed";
            const string failed = "Failed";
            var unitTestResultOutcomes = new[] { passed, failed };

            var unitTestResultFaker = new Faker<UnitTestResult>()
                .RuleFor(ur => ur.ComputerName, f => f.Hacker.Noun())
                .RuleFor(ur => ur.StartTime, f => f.Date.Recent().ToString())
                .RuleFor(ur => ur.EndTime, f => f.Date.Soon().ToString())
                .RuleFor(ur => ur.Duration, (f, ur) => (DateTime.Parse(ur.EndTime) - DateTime.Parse(ur.StartTime)).ToString())
                .RuleFor(ur => ur.TestType, () => Guid.NewGuid().ToString())
                .RuleFor(ur => ur.Outcome, f => f.PickRandom(unitTestResultOutcomes))
                .RuleFor(ur => ur.Output, () => unitTestResultOutputFaker.Generate(1).First());

            var timesFaker = new Faker<Times>()
                .RuleFor(tf => tf.Start, f => f.Date.Recent().ToString())
                .RuleFor(tf => tf.Finish, f => f.Date.Soon().ToString())
                .RuleFor(tf => tf.Queuing, (f, tf) => f.Date.Recent(refDate: DateTime.Parse(tf.Start)).ToString())
                .RuleFor(tf => tf.Creation, (f, tf) => tf.Finish);

            var runInfoFaker = new Faker<RunInfo>()
                .RuleFor(rf => rf.ComputerName, f => f.Hacker.Noun())
                .RuleFor(rf => rf.Timestamp, f => f.Date.Recent().ToString())
                .RuleFor(rf => rf.Text, f => f.Lorem.Paragraph());

            var unitTests = unitTestFaker.GenerateBetween(10, 50);

            var testRunFaker = new Faker<TestRun>()
                .RuleFor(tf => tf.Id, () => Guid.NewGuid().ToString())
                .RuleFor(tf => tf.Name, f => f.Lorem.Sentence())
                .RuleFor(tf => tf.RunUser, f => f.Internet.UserName())
                .RuleFor(tf => tf.Times, timesFaker.Generate(1).First())
                .RuleFor(tf => tf.TestLists, () => testLists)
                .RuleFor(tf => tf.TestDefinitions, () => unitTests)
                .RuleFor(tf => tf.TestEntries, (f, tf) => tf.TestDefinitions.Select(td =>
                {
                    var te = testEntryFaker.Generate(1).First();
                    te.TestId = td.Id;
                    te.ExecutionId = td.Execution.Id;
                    return te;
                }).ToList())
                .RuleFor(tf => tf.Results, (f, tf) => tf.TestDefinitions.Select(td =>
                {
                    var ur = unitTestResultFaker.Generate(1).First();
                    ur.ExecutionId = td.Execution.Id;
                    ur.TestId = td.Id;
                    ur.TestName = td.Name;
                    ur.RelativeResultsDirectory = td.Execution.Id;
                    ur.TestListId = tf.TestEntries.First(te => te.TestId == td.Id).TestListId;
                    return ur;
                }).ToList())
                .RuleFor(tf => tf.ResultSummary, (f, tf) => {
                    var count = tf.Results.Count;
                    var passedCount = tf.Results.Count(r => r.Outcome == passed);
                    var failedCount = tf.Results.Count(r => r.Outcome == failed);
                    var outcome = failedCount > 0 ? failed : passed;

                    var runInfos = runInfoFaker.Generate(1);
                    runInfos.First().Outcome = outcome;

                    var rs = new ResultSummary
                    {
                        Outcome = outcome,
                        Counters = new Counters
                        {
                            Total = count,
                            Еxecuted = count,
                            Passed = passedCount,
                            Failed = failedCount,
                            Timeout = 0,
                            Aborted = 0,
                            Inconclusive = 0,
                            PassedButRunAborted = 0,
                            NotRunnable = 0,
                            Disconnected = 0,
                            Warning = 0,
                            NotExecuted = 0,
                            Completed = count,
                            InProgress = 0,
                            Pending = 0
                        },
                        RunInfos = runInfos
                    };
                    return rs;
                });

            return testRunFaker.Generate(1).First();
        }

        public static List<TestRun> GenerateTestRuns(int count)
        {
            var testRuns = new List<TestRun>();
            for (int i = 0; i < count; i++)
            {
                var testRun = GenerateTestRun();
                testRuns.Add(testRun);
            }
            return testRuns;
        }

        public static List<TestRun> GenerateTestRuns()
        {
            var faker = new Faker();
            var count = faker.Random.Int(3, 10);
            return GenerateTestRuns(count);
        }
    }
}
