namespace NexusAs.Application.DTOs.Returns
{
    public class CreateReturnDto
    {
        public int SaleId { get; set; }
        public string? Notes { get; set; }
        public List<CreateReturnDetailDto> Details { get; set; } = new();
    }

    public class CreateReturnDetailDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
