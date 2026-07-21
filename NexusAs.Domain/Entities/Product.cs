using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusAs.Domain.Entities
{
    public class Product : BaseEntity
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Brand { get; set; }
        public decimal Cost { get; set; }
        public decimal SalePrice { get; set; }
        public int Stock { get; set; }
        public int MinStock { get; set; }
        public string? ImagePath { get; set; }
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public int? SupplierId { get; set; }
        public Supplier? Supplier { get; set; }
        public ICollection<SaleDetail> SaleDetails { get; set; } = new List<SaleDetail>();
        public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
        public bool IsPartnership { get; set; } = false;
    }
}
