using MultiVendor.Ecommerce.Application.Interfaces;
using Serilog;

namespace MultiVendor.Ecommerce.Infrastructure.Services;

public class DummyEmailService : IEmailService
{
    public Task<bool> SendLowStockAlertAsync(Guid merchantId, string sku)
    {
        Log.Warning(
            "This is an accelerated implementation for the system: " +
            "Dummy email sent to merchant regarding low stock for SKU: {SKU} (MerchantId: {MerchantId})",
            sku, merchantId);

        return Task.FromResult(true);
    }
}
