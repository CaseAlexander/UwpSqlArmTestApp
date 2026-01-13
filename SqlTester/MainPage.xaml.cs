using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using SQLite;
using SqlTester.Models;

namespace SqlTester
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private string _dbPath;
        private StringBuilder _results;

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await RunPerformanceTest();
        }

        private async Task RunPerformanceTest()
        {
            _results = new StringBuilder();
            
            try
            {
                // Set up database path
                _dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "test.db");
                
                // Delete existing database
                if (File.Exists(_dbPath))
                {
                    File.Delete(_dbPath);
                }

                UpdateStatus("Running SQL Performance Test...");
                AppendResult("=".PadRight(60, '='));
                AppendResult("SQL PERFORMANCE TEST");
                AppendResult("Database: sqlite-net-base 1.7.335");
                AppendResult("SQLCipher: SQLitePCLRaw.bundle_e_sqlcipher 2.0.4");
                AppendResult("=".PadRight(60, '='));
                AppendResult("");

                // Create connection
                var db = new SQLiteConnection(_dbPath);
                db.CreateTable<TestRecord>();
                
                AppendResult($"Database created at: {_dbPath}");
                AppendResult("");

                // Test 1: INSERT Performance
                await TestInsert(db);

                // Test 2: SELECT Performance
                await TestSelect(db);

                // Test 3: UPDATE Performance
                await TestUpdate(db);

                // Test 4: DELETE Performance
                await TestDelete(db);

                db.Close();

                AppendResult("");
                AppendResult("=".PadRight(60, '='));
                AppendResult("TEST COMPLETED SUCCESSFULLY");
                AppendResult("=".PadRight(60, '='));

                UpdateStatus("Test Complete!");
                ProgressBar.IsIndeterminate = false;
                ProgressBar.Visibility = Visibility.Collapsed;
                ResultsScrollViewer.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                AppendResult("");
                AppendResult($"ERROR: {ex.Message}");
                AppendResult($"Stack Trace: {ex.StackTrace}");
                UpdateStatus("Test Failed!");
                ProgressBar.IsIndeterminate = false;
                ResultsScrollViewer.Visibility = Visibility.Visible;
            }
        }

        private async Task TestInsert(SQLiteConnection db)
        {
            UpdateStatus("Testing INSERT operations...");
            AppendResult("Test 1: INSERT 10,000 records");
            
            var stopwatch = Stopwatch.StartNew();
            
            db.BeginTransaction();
            for (int i = 1; i <= 10000; i++)
            {
                var record = new TestRecord
                {
                    Name = $"Record_{i}",
                    Value = i,
                    Score = i * 1.5,
                    Description = $"This is test record number {i}"
                };
                db.Insert(record);
            }
            db.Commit();
            
            stopwatch.Stop();
            
            AppendResult($"  Time: {stopwatch.ElapsedMilliseconds} ms");
            AppendResult($"  Rate: {10000.0 / stopwatch.Elapsed.TotalSeconds:F2} records/sec");
            AppendResult("");

            await Task.Delay(100); // Allow UI to update
        }

        private async Task TestSelect(SQLiteConnection db)
        {
            UpdateStatus("Testing SELECT operations...");
            AppendResult("Test 2: SELECT all 10,000 records");
            
            var stopwatch = Stopwatch.StartNew();
            var records = db.Table<TestRecord>().ToList();
            stopwatch.Stop();
            
            AppendResult($"  Records retrieved: {records.Count}");
            AppendResult($"  Time: {stopwatch.ElapsedMilliseconds} ms");
            AppendResult($"  Rate: {records.Count / stopwatch.Elapsed.TotalSeconds:F2} records/sec");
            AppendResult("");

            await Task.Delay(100);
        }

        private async Task TestUpdate(SQLiteConnection db)
        {
            UpdateStatus("Testing UPDATE operations...");
            AppendResult("Test 3: UPDATE 10,000 records");
            
            var stopwatch = Stopwatch.StartNew();
            
            db.BeginTransaction();
            var records = db.Table<TestRecord>().ToList();
            foreach (var record in records)
            {
                record.Value *= 2;
                record.Score *= 2;
                db.Update(record);
            }
            db.Commit();
            
            stopwatch.Stop();
            
            AppendResult($"  Time: {stopwatch.ElapsedMilliseconds} ms");
            AppendResult($"  Rate: {10000.0 / stopwatch.Elapsed.TotalSeconds:F2} records/sec");
            AppendResult("");

            await Task.Delay(100);
        }

        private async Task TestDelete(SQLiteConnection db)
        {
            UpdateStatus("Testing DELETE operations...");
            AppendResult("Test 4: DELETE 10,000 records");
            
            var stopwatch = Stopwatch.StartNew();
            
            db.BeginTransaction();
            db.DeleteAll<TestRecord>();
            db.Commit();
            
            stopwatch.Stop();
            
            var remainingCount = db.Table<TestRecord>().Count();
            
            AppendResult($"  Records remaining: {remainingCount}");
            AppendResult($"  Time: {stopwatch.ElapsedMilliseconds} ms");
            AppendResult($"  Rate: {10000.0 / stopwatch.Elapsed.TotalSeconds:F2} records/sec");
            AppendResult("");

            await Task.Delay(100);
        }

        private void UpdateStatus(string status)
        {
            StatusText.Text = status;
        }

        private void AppendResult(string line)
        {
            _results.AppendLine(line);
            ResultsText.Text = _results.ToString();
        }
    }
}
