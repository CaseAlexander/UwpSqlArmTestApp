using SqlTester;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UwpSqlTest
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a <see cref="Frame">.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private StringBuilder _outputBuilder;
        private Dictionary<string, double> _testResults;

        public MainPage()
        {
            InitializeComponent();
            _outputBuilder = new StringBuilder();
            _testResults = new Dictionary<string, double>();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await Task.Delay(500);
            await RunPerformanceTests();
        }

        private async Task RunPerformanceTests()
        {
            try
            {
                var dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "performance_test.db");
                
                if (File.Exists(dbPath))
                {
                    File.Delete(dbPath);
                }

                AppendOutput("Starting SQLite Performance Tests...");
                AppendOutput($"Database: {dbPath}");
                AppendOutput("");

                var tester = new SqlitePerformanceTest(dbPath, "test-encryption-key-123");

                var results = await tester.RunTests(progress =>
                {
                    UpdateStatus(progress);
                });

                ParseAndDisplayResults(results);

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    StatusText.Text = "Tests completed!";
                    ProgressBar.IsIndeterminate = false;
                });
            }
            catch (Exception ex)
            {
                AppendOutput("");
                AppendOutput("ERROR OCCURRED:");
                AppendOutput(ex.Message);
                AppendOutput(ex.StackTrace);
                
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    StatusText.Text = "Test failed!";
                    ProgressBar.IsIndeterminate = false;
                });
            }
        }

        private void ParseAndDisplayResults(string results)
        {
            AppendOutput(results);
            AppendOutput("");
            AppendOutput("╔══════════════════════════════════════════════════════════════════════════════╗");
            AppendOutput("║                         PERFORMANCE TEST SUMMARY                             ║");
            AppendOutput("╠══════════════════════════════════════════════════════════════════════════════╣");
            AppendOutput("║ Test Name                                              │ Avg (ms) │  Ratio  ║");
            AppendOutput("╠════════════════════════════════════════════════════════╪══════════╪═════════╣");

            var lines = results.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var testData = new List<(string name, double avg, double min, double max)>();

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("---"))
                {
                    var testName = lines[i].Replace("---", "").Trim();
                    if (i + 1 < lines.Length && lines[i + 1].Contains("Avg:"))
                    {
                        var statsLine = lines[i + 1];
                        var avgStr = ExtractValue(statsLine, "Avg:", "ms");
                        if (double.TryParse(avgStr, out double avg))
                        {
                            var minStr = ExtractValue(statsLine, "Min:", "ms");
                            var maxStr = ExtractValue(statsLine, "Max:", "ms");
                            double.TryParse(minStr, out double min);
                            double.TryParse(maxStr, out double max);
                            testData.Add((testName, avg, min, max));
                        }
                    }
                }
            }

            var groupedTests = new[]
            {
                ("INSERT OPERATIONS", new[] { "Individual Inserts (100 items) - No Transaction", "Individual Inserts (100 items) - With Transaction", "Bulk Insert (1000 items) - No Transaction", "Bulk Insert (1000 items) - With Transaction" }),
                ("SELECT OPERATIONS", new[] { "Simple Query (WHERE + TAKE 100)", "Complex Query (Multiple WHERE + ORDER BY + TAKE 500)", "Aggregation Query (AVG)", "Subquery with JOIN-like behavior (1000 rows)" }),
                ("UPDATE OPERATIONS", new[] { "Individual Updates (100 items) - No Transaction", "Individual Updates (100 items) - With Transaction", "Bulk Update (1000 items) - No Transaction", "Bulk Update (1000 items) - With Transaction" }),
                ("DELETE OPERATIONS", new[] { "Individual Deletes (100 items) - No Transaction", "Individual Deletes (100 items) - With Transaction", "Bulk Delete (1000 items) - No Transaction", "Bulk Delete (1000 items) - With Transaction" })
            };

            foreach (var (groupName, testNames) in groupedTests)
            {
                var groupTests = testData.Where(t => testNames.Any(name => t.name.Contains(name))).ToList();
                if (groupTests.Any())
                {
                    AppendOutput($"║ {groupName,-54} │          │         ║");
                    AppendOutput("╟────────────────────────────────────────────────────────┼──────────┼─────────╢");

                    var baselineAvg = groupTests.First().avg;
                    foreach (var test in groupTests)
                    {
                        var ratio = test.avg / baselineAvg;
                        var shortName = ShortenTestName(test.name);
                        AppendOutput($"║ {shortName,-54} │ {test.avg,8:F2} │ {ratio,7:F2}x ║");
                    }

                    if (groupName != "DELETE OPERATIONS")
                    {
                        AppendOutput("╟────────────────────────────────────────────────────────┼──────────┼─────────╢");
                    }
                }
            }

            AppendOutput("╚════════════════════════════════════════════════════════╧══════════╧═════════╝");
            AppendOutput("");
        }

        private string ShortenTestName(string fullName)
        {
            return fullName
                .Replace("Individual Inserts (100 items) - No Transaction", "  Insert 100 (No Txn)")
                .Replace("Individual Inserts (100 items) - With Transaction", "  Insert 100 (With Txn)")
                .Replace("Bulk Insert (1000 items) - No Transaction", "  Insert 1000 Bulk (No Txn)")
                .Replace("Bulk Insert (1000 items) - With Transaction", "  Insert 1000 Bulk (With Txn)")
                .Replace("Simple Query (WHERE + TAKE 100)", "  Simple Query (100 rows)")
                .Replace("Complex Query (Multiple WHERE + ORDER BY + TAKE 500)", "  Complex Query (500 rows)")
                .Replace("Aggregation Query (AVG)", "  Aggregation (AVG)")
                .Replace("Subquery with JOIN-like behavior (1000 rows)", "  Subquery with JOIN (1000 rows)")
                .Replace("Individual Updates (100 items) - No Transaction", "  Update 100 (No Txn)")
                .Replace("Individual Updates (100 items) - With Transaction", "  Update 100 (With Txn)")
                .Replace("Bulk Update (1000 items) - No Transaction", "  Update 1000 Bulk (No Txn)")
                .Replace("Bulk Update (1000 items) - With Transaction", "  Update 1000 Bulk (With Txn)")
                .Replace("Individual Deletes (100 items) - No Transaction", "  Delete 100 (No Txn)")
                .Replace("Individual Deletes (100 items) - With Transaction", "  Delete 100 (With Txn)")
                .Replace("Bulk Delete (1000 items) - No Transaction", "  Delete 1000 Bulk (No Txn)")
                .Replace("Bulk Delete (1000 items) - With Transaction", "  Delete 1000 Bulk (With Txn)");
        }

        private string ExtractValue(string line, string startMarker, string endMarker)
        {
            var startIndex = line.IndexOf(startMarker);
            if (startIndex == -1) return "0";
            
            startIndex += startMarker.Length;
            var endIndex = line.IndexOf(endMarker, startIndex);
            if (endIndex == -1) return "0";
            
            return line.Substring(startIndex, endIndex - startIndex).Trim();
        }

        private async void UpdateStatus(string status)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                StatusText.Text = status;
            });
        }

        private async void AppendOutput(string text)
        {
            _outputBuilder.AppendLine(text);
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                OutputText.Text = _outputBuilder.ToString();
            });
        }
    }
}
