using System;
using SQLite;

namespace SqlTester
{
    public class Product
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        
        [MaxLength(100)]
        public string Name { get; set; }
        
        public string Description { get; set; }
        
        [Indexed]
        public decimal Price { get; set; }
        
        [Indexed]
        public int CategoryId { get; set; }
        
        public int StockQuantity { get; set; }
        
        public DateTime CreatedDate { get; set; }
        
        public bool IsActive { get; set; }
    }
}