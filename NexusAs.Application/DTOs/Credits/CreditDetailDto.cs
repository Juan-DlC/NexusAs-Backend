namespace NexusAs.Application.DTOs.Credits
{
    public class CreditDetailDto
    {
        public int Id { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public int? CustomerId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? Notes { get; set; }
        public int NumberOfInstallments { get; set; }
        public int SaleId { get; set; }

        public List<CreditDetailProductDto> Products { get; set; } = new();
        public List<CreditPaymentHistoryDto> Payments { get; set; } = new();
        public List<CreditInstallmentDto> Installments { get; set; } = new();
    }

    public class CreditDetailProductDto
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class CreditPaymentHistoryDto
    {
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string? Notes { get; set; }
        public string UserName { get; set; } = string.Empty;
    }

    public class CreditInstallmentDto
    {
        public int Number { get; set; }
        public decimal Amount { get; set; }
        public bool IsPaid { get; set; }
    }
}