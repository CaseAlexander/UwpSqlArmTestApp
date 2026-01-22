using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace SqlTester
{
    public class SqlitePerformanceTest
    {
        private SQLiteAsyncConnection _connection;
        private StringBuilder _results;
        private Random _random;
        private const int TestIterations = 50;
        private const int TotalRows = 100000;

        public SqlitePerformanceTest(string databasePath, string encryptionKey = null)
        {
            SQLiteConnectionString options;
            
            if (!string.IsNullOrEmpty(encryptionKey))
            {
                options = new SQLiteConnectionString(
                    databasePath,
                    SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex,
                    storeDateTimeAsTicks: true,
                    key: encryptionKey);
            }
            else
            {
                options = new SQLiteConnectionString(
                    databasePath,
                    SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex,
                    storeDateTimeAsTicks: true);
            }
            
            _connection = new SQLiteAsyncConnection(options);
            _results = new StringBuilder();
            _random = new Random();
        }

        public async Task<string> RunTests(Action<string> progressCallback)
        {
            _results.Clear();
            AppendResult("=== SQLite Performance Test Started ===");
            AppendResult($"Test Iterations: {TestIterations}");
            AppendResult($"Target Rows: {TotalRows}");
            AppendResult($"Date: {DateTime.Now}");
            AppendResult("");
            
            progressCallback?.Invoke("Initializing database...");
            await InitializeDatabase();
            
            progressCallback?.Invoke("Seeding initial data...");
            await SeedData();
            
            // Individual Insert Tests
            progressCallback?.Invoke("Testing individual inserts without transaction...");
            await TestIndividualInsertsWithoutTransaction();
            
            progressCallback?.Invoke("Testing individual inserts with transaction...");
            await TestIndividualInsertsWithTransaction();
            
            // Bulk Insert Tests
            progressCallback?.Invoke("Testing bulk inserts without transaction...");
            await TestBulkInsertsWithoutTransaction();
            
            progressCallback?.Invoke("Testing bulk inserts with transaction...");
            await TestBulkInsertsWithTransaction();
            
            // Read Tests
            progressCallback?.Invoke("Testing simple queries...");
            await TestSimpleQueries();
            
            progressCallback?.Invoke("Testing complex queries...");
            await TestComplexQueries();
            
            // Update Tests
            progressCallback?.Invoke("Testing individual updates without transaction...");
            await TestIndividualUpdatesWithoutTransaction();
            
            progressCallback?.Invoke("Testing individual updates with transaction...");
            await TestIndividualUpdatesWithTransaction();
            
            progressCallback?.Invoke("Testing bulk updates without transaction...");
            await TestBulkUpdatesWithoutTransaction();
            
            progressCallback?.Invoke("Testing bulk updates with transaction...");
            await TestBulkUpdatesWithTransaction();
            
            // Delete Tests
            progressCallback?.Invoke("Testing individual deletes without transaction...");
            await TestIndividualDeletesWithoutTransaction();
            
            progressCallback?.Invoke("Testing individual deletes with transaction...");
            await TestIndividualDeletesWithTransaction();
            
            progressCallback?.Invoke("Testing bulk deletes without transaction...");
            await TestBulkDeletesWithoutTransaction();
            
            progressCallback?.Invoke("Testing bulk deletes with transaction...");
            await TestBulkDeletesWithTransaction();
            
            AppendResult("");
            AppendResult("=== All Tests Completed ===");
            progressCallback?.Invoke("All tests completed!");
            
            return _results.ToString();
        }

        private async Task InitializeDatabase()
        {
            var journalMode = await _connection.ExecuteScalarAsync<string>("PRAGMA journal_mode=WAL");
            
            await _connection.DropTableAsync<Product>();
            await _connection.DropTableAsync<Category>();
            await _connection.CreateTableAsync<Category>();
            await _connection.CreateTableAsync<Product>();
            
            AppendResult("Database initialized successfully");
            AppendResult("Encryption: Enabled");
            AppendResult($"Journal Mode: {journalMode}");
        }

        private async Task SeedData()
        {
            var sw = Stopwatch.StartNew();
            
            // Insert categories
            var categories = new List<Category>();
            for (int i = 1; i <= 10; i++)
            {
                categories.Add(new Category { Name = $"Category {i}" });
            }
            await _connection.InsertAllAsync(categories);
            
            // Insert products in batches
            const int batchSize = 1000;
            for (int batch = 0; batch < TotalRows / batchSize; batch++)
            {
                var products = new List<Product>();
                for (int i = 0; i < batchSize; i++)
                {
                    products.Add(CreateRandomProduct());
                }
                await _connection.InsertAllAsync(products);
            }
            
            sw.Stop();
            AppendResult($"Seeded {TotalRows} products in {sw.ElapsedMilliseconds}ms");
            AppendResult("");
        }

        // Individual Insert Tests
        private async Task TestIndividualInsertsWithoutTransaction()
        {
            var times = new List<long>();
            
            for (int run = 0; run < TestIterations; run++)
            {
                var products = Enumerable.Range(0, 100).Select(_ => CreateRandomProduct()).ToList();
                
                var sw = Stopwatch.StartNew();
                foreach (var product in products)
                {
                    await _connection.InsertAsync(product);
                }
                sw.Stop();
                times.Add(sw.ElapsedMilliseconds);
                
                // Cleanup
                await _connection.ExecuteAsync($"DELETE FROM Product WHERE Id > {TotalRows}");
            }
            
            AppendTestResult("Individual Inserts (100 items) - No Transaction", times);
        }

        private async Task TestIndividualInsertsWithTransaction()
        {
            var times = new List<long>();
            
            for (int run = 0; run < TestIterations; run++)
            {
                var products = Enumerable.Range(0, 100).Select(_ => CreateRandomProduct()).ToList();
                
                var sw = Stopwatch.StartNew();
                await _connection.RunInTransactionAsync(conn =>
                {
                    foreach (var product in products)
                    {
                        conn.Insert(product);
                    }
                });
                sw.Stop();
                times.Add(sw.ElapsedMilliseconds);
                
                // Cleanup
                await _connection.ExecuteAsync($"DELETE FROM Product WHERE Id > {TotalRows}");
            }
            
            AppendTestResult("Individual Inserts (100 items) - With Transaction", times);
        }

        // Bulk Insert Tests
        private async Task TestBulkInsertsWithoutTransaction()
        {
            var times = new List<long>();
            
            for (int run = 0; run < TestIterations; run++)
            {
                var products = Enumerable.Range(0, 1000).Select(_ => CreateRandomProduct()).ToList();
                
                var sw = Stopwatch.StartNew();
                await _connection.InsertAllAsync(products);
                sw.Stop();
                times.Add(sw.ElapsedMilliseconds);
                
                // Cleanup
                await _connection.ExecuteAsync($"DELETE FROM Product WHERE Id > {TotalRows}");
            }
            
            AppendTestResult("Bulk Insert (1000 items) - No Transaction", times);
        }

        private async Task TestBulkInsertsWithTransaction()
        {
            var times = new List<long>();
            
            for (int run = 0; run < TestIterations; run++)
            {
                var products = Enumerable.Range(0, 1000).Select(_ => CreateRandomProduct()).ToList();
                
                var sw = Stopwatch.StartNew();
                await _connection.RunInTransactionAsync(conn =>
                {
                    conn.InsertAll(products);
                });
                sw.Stop();
                times.Add(sw.ElapsedMilliseconds);
                
                // Cleanup
                await _connection.ExecuteAsync($"DELETE FROM Product WHERE Id > {TotalRows}");
            }
            
            AppendTestResult("Bulk Insert (1000 items) - With Transaction", times);
        }

        // Query Tests
        private async Task TestSimpleQueries()
        {
            var times = new List<long>();
            
            for (int run = 0; run < TestIterations; run++)
            {
                var sw = Stopwatch.StartNew();
                var products = await _connection.Table<Product>()
                    .Where(p => p.IsActive)
                    .Take(100)
                    .ToListAsync();
                sw.Stop();
                times.Add(sw.ElapsedMilliseconds);
            }
            
            AppendTestResult("Simple Query (WHERE + TAKE 100)", times);
        }

        private async Task TestComplexQueries()
        {
            var times = new List<long>();
            
            for (int run = 0; run < TestIterations; run++)
            {
                var sw = Stopwatch.StartNew();
                var products = await _connection.Table<Product>()
                    .Where(p => p.Price > 50 && p.Price < 150)
                    .Where(p => p.CategoryId >= 3 && p.CategoryId <= 7)
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.Price)
                    .Take(500)
                    .ToListAsync();
                sw.Stop();
                times.Add(sw.ElapsedMilliseconds);
            }
            
            AppendTestResult("Complex Query (Multiple WHERE + ORDER BY + TAKE 500)", times);
            
            // Aggregation query
            times.Clear();
            for (int run = 0; run < TestIterations; run++)
            {
                var sw = Stopwatch.StartNew();
                var result = await _connection.ExecuteScalarAsync<decimal>(
                    "SELECT AVG(Price) FROM Product WHERE CategoryId = ?", 5);
                sw.Stop();
                times.Add(sw.ElapsedMilliseconds);
            }
            
            AppendTestResult("Aggregation Query (AVG)", times);
            
            // Join-like query
            times.Clear();
            for (int run = 0; run < TestIterations; run++)
            {
                var sw = Stopwatch.StartNew();
                var result = await _connection.QueryAsync<Product>(
                    @"SELECT p.* FROM Product p 
                      WHERE p.CategoryId IN (SELECT Id FROM Category WHERE Id <= 5)
                      AND p.Price > 75
                      LIMIT 1000");
                sw.Stop();
                times.Add(sw.ElapsedMilliseconds);
            }
            
            AppendTestResult("Subquery with JOIN-like behavior (1000 rows)", times);
        }

        // Individual Update Tests
        private async Task TestIndividualUpdatesWithoutTransaction()
        {
            var times = new List<long>();
            
            for (int run = 0; run < TestIterations; run++)
            {
                var products = await _connection.Table<Product>().Take(100).ToListAsync();
                
                var sw = Stopwatch.StartNew();
                foreach (var product in products)
                {
                    product.Price = _random.Next(10, 500);
                    await _connection.UpdateAsync(product);
                }
                sw.Stop();
                times.Add(sw.ElapsedMilliseconds);
            }
            
            AppendTestResult("Individual Updates (100 items) - No Transaction", times);
        }

        private async Task TestIndividualUpdatesWithTransaction()
        {
            var times = new List<long>();
            
            for (int run = 0; run < TestIterations; run++)
            {
                var products = await _connection.Table<Product>().Take(100).ToListAsync();
                
                var sw = Stopwatch.StartNew();
                await _connection.RunInTransactionAsync(conn =>
                {
                    foreach (var product in products)
                    {
                        product.Price = _random.Next(10, 500);
                        conn.Update(product);
                    }
                });
                sw.Stop();
                times.Add(sw.ElapsedMilliseconds);
            }
            
            AppendTestResult("Individual Updates (100 items) - With Transaction", times);
        }

        // Bulk Update Tests
        private async Task TestBulkUpdatesWithoutTransaction()
        {
            var times = new List<long>();
            
            for (int run = 0; run < TestIterations; run++)
            {
                var products = await _connection.Table<Product>().Take(1000).ToListAsync();
                foreach (var p in products) p.Price = _random.Next(10, 500);
                
                var sw = Stopwatch.StartNew();
                await _connection.UpdateAllAsync(products);
                sw.Stop();
                times.Add(sw.ElapsedMilliseconds);
            }
            
            AppendTestResult("Bulk Update (1000 items) - No Transaction", times);
        }

        private async Task TestBulkUpdatesWithTransaction()
        {
            var times = new List<long>();
            
            for (int run = 0; run < TestIterations; run++)
            {
                var products = await _connection.Table<Product>().Take(1000).ToListAsync();
                foreach (var p in products) p.Price = _random.Next(10, 500);
                
                var sw = Stopwatch.StartNew();
                await _connection.RunInTransactionAsync(conn =>
                {
                    conn.UpdateAll(products);
                });
                sw.Stop();
                times.Add(sw.ElapsedMilliseconds);
            }
            
            AppendTestResult("Bulk Update (1000 items) - With Transaction", times);
        }

        // Individual Delete Tests
        private async Task TestIndividualDeletesWithoutTransaction()
        {
            var times = new List<long>();
            
            for (int run = 0; run < TestIterations; run++)
            {
                // Create test records
                var products = Enumerable.Range(0, 100).Select(_ => CreateRandomProduct()).ToList();
                await _connection.InsertAllAsync(products);
                
                var sw = Stopwatch.StartNew();
                foreach (var product in products)
                {
                    await _connection.DeleteAsync(product);
                }
                sw.Stop();
                times.Add(sw.ElapsedMilliseconds);
            }
            
            AppendTestResult("Individual Deletes (100 items) - No Transaction", times);
        }

        private async Task TestIndividualDeletesWithTransaction()
        {
            var times = new List<long>();
            
            for (int run = 0; run < TestIterations; run++)
            {
                // Create test records
                var products = Enumerable.Range(0, 100).Select(_ => CreateRandomProduct()).ToList();
                await _connection.InsertAllAsync(products);
                
                var sw = Stopwatch.StartNew();
                await _connection.RunInTransactionAsync(conn =>
                {
                    foreach (var product in products)
                    {
                        conn.Delete(product);
                    }
                });
                sw.Stop();
                times.Add(sw.ElapsedMilliseconds);
            }
            
            AppendTestResult("Individual Deletes (100 items) - With Transaction", times);
        }

        // Bulk Delete Tests
        private async Task TestBulkDeletesWithoutTransaction()
        {
            var times = new List<long>();
            
            for (int run = 0; run < TestIterations; run++)
            {
                // Create test records
                var products = Enumerable.Range(0, 1000).Select(_ => CreateRandomProduct()).ToList();
                await _connection.InsertAllAsync(products);
                var ids = products.Select(p => p.Id).ToList();
                
                var sw = Stopwatch.StartNew();
                await _connection.ExecuteAsync(
                    $"DELETE FROM Product WHERE Id IN ({string.Join(",", ids)})");
                sw.Stop();
                times.Add(sw.ElapsedMilliseconds);
            }
            
            AppendTestResult("Bulk Delete (1000 items) - No Transaction", times);
        }

        private async Task TestBulkDeletesWithTransaction()
        {
            var times = new List<long>();
            
            for (int run = 0; run < TestIterations; run++)
            {
                // Create test records
                var products = Enumerable.Range(0, 1000).Select(_ => CreateRandomProduct()).ToList();
                await _connection.InsertAllAsync(products);
                var ids = products.Select(p => p.Id).ToList();
                
                var sw = Stopwatch.StartNew();
                await _connection.RunInTransactionAsync(conn =>
                {
                    conn.Execute($"DELETE FROM Product WHERE Id IN ({string.Join(",", ids)})");
                });
                sw.Stop();
                times.Add(sw.ElapsedMilliseconds);
            }
            
            AppendTestResult("Bulk Delete (1000 items) - With Transaction", times);
        }

        // Helper Methods
        private Product CreateRandomProduct()
        {
            return new Product
            {
                Name = $"Product {Guid.NewGuid().ToString().Substring(0, 8)}",
                Description = $"Description {_random.Next(1000, 9999)}",
                Price = _random.Next(10, 500),
                CategoryId = _random.Next(1, 11),
                StockQuantity = _random.Next(0, 1000),
                CreatedDate = DateTime.Now.AddDays(-_random.Next(0, 365)),
                IsActive = _random.Next(0, 2) == 1
            };
        }

        private void AppendTestResult(string testName, List<long> times)
        {
            var avg = times.Average();
            var min = times.Min();
            var max = times.Max();
            
            AppendResult($"--- {testName} ---");
            AppendResult($"  Avg: {avg:F2}ms | Min: {min}ms | Max: {max}ms");
            AppendResult("");
        }

        private void AppendResult(string message)
        {
            _results.AppendLine(message);
        }
    }
}