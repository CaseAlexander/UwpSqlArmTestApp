using SQLite;

namespace SqlTester
{
    public class Category
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        
        [MaxLength(50)]
        public string Name { get; set; }
    }
}