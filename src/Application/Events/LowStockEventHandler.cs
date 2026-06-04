using MultiVendor.Ecommerce.Application.Interfaces;

namespace MultiVendor.Ecommerce.Application.Events;

public class LowStockEventHandler : IEventHandler<LowStockEvent>
{
    private readonly IEmailService _emailService;

    public LowStockEventHandler(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task HandleAsync(LowStockEvent @event)
    {
        await _emailService.SendLowStockAlertAsync(@event.MerchantId, @event.SKU);
    }
}
