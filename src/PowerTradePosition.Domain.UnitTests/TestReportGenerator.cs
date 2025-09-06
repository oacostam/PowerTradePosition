using Xunit.Abstractions;

namespace PowerTradePosition.Domain.UnitTests;

/// <summary>
///     Provides test reporting utilities
/// </summary>
public static class TestReportGenerator
{
    /// <summary>
    ///     Displays information about available coverage reports
    /// </summary>
    public static void DisplayCoverageReportInfo(ITestOutputHelper output)
    {
        output.WriteLine("Coverage Report Information:");
        output.WriteLine("- Coverage reports are generated in the TestReports directory");
        output.WriteLine("- Open index.html in your browser to view detailed coverage");
        output.WriteLine("- Coverage includes line, branch, and method coverage metrics");
        output.WriteLine("- Reports are generated automatically after test execution");
    }
}