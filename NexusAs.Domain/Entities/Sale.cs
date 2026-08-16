using NexusAs.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusAs.Domain.Entities
{
    public class Sale : BaseEntity
    {
        public string SaleNumber { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal? DiscountPercent { get; set; }
        public decimal Total { get; set; }
        public int PaymentMethodId { get; set; }
        public PaymentMethodEntity? PaymentMethodEntity { get; set; }
        public string? Notes { get; set; }
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        
        // BUG 1 FIX: Usuario que procesó la venta (Admin que hizo la venta)
        public int? ProcessedByUserId { get; set; }
        public User? ProcessedByUser { get; set; }
        
        // BUG 4 FIX: Para prevenir ventas duplicadas por doble click
        public string? RequestId { get; set; }
        
        public SaleStatus Status { get; set; } = SaleStatus.Active;
        public ICollection<SaleDetail> SaleDetails { get; set; } = new List<SaleDetail>();
        public Credit? Credit { get; set; }
        public ICollection<Return> Returns { get; set; } = new List<Return>();
    }
}
