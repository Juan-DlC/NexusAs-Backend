using NexusAs.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusAs.Domain.Entities
{
    public class Credit : BaseEntity
    {
        public int SaleId { get; set; }
        public Sale? Sale { get; set; }
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public CreditStatus Status { get; set; }
        public DateTime? DueDate { get; set; }
        public string? Notes { get; set; }
    }
}
