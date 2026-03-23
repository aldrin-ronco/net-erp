using Models.Global;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Inventory
{
    public class StockGraphQLModel
    {
        public int Id { get; set; }
        public CompanyGraphQLModel Company { get; set; } = new();
        public ItemGraphQLModel Item { get; set; } = new();
        public StorageGraphQLModel Storage { get; set; } = new();
        public decimal Quantity { get; set; }
        public decimal Cost { get; set; }
        public decimal AverageCost { get; set; }
        public DateTime InsertedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
