namespace MultiVendor.Ecommerce.Application.Interfaces;

public interface IEmailService
{
    Task<bool> SendLowStockAlertAsync(Guid merchantId, string sku);
}
