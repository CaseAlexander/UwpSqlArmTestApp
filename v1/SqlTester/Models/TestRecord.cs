using SQLite;

namespace SqlTester.Models
{
    [Table("TestRecords")]
    public class TestRecord
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [MaxLength(100)]
        public string Name { get; set; }

        public int Value { get; set; }

        public double Score { get; set; }

        [MaxLength(200)]
        public string Description { get; set; }
    }
}