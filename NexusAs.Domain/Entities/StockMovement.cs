using NexusAs.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusAs.Domain.Entities
{
    public class StockMovement : BaseEntity
    {
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public DateTime Date { get; set; }
        public MovementType Type { get; set; }
        public int Quantity { get; set; }
        public int StockBefore { get; set; }
        public int StockAfter { get; set; }
        public string? Reason { get; set; }
        public int? SaleId { get; set; }
        public Sale? Sale { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
    }
}
