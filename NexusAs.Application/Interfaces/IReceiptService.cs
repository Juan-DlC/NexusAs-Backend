namespace NexusAs.Application.Interfaces
{
    public interface IReceiptService
    {
        Task<byte[]> GenerateSaleReceiptAsync(int saleId);
        Task<byte[]> GeneratePartnerSaleReceiptAsync(int saleId);
    }
}
