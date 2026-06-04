namespace MultiVendor.Ecommerce.Application.Events;

public interface IEventHandler<T>
{
    Task HandleAsync(T @event);
}
