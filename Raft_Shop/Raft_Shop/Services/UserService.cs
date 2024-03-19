using Raft_Library.Shop.shared;

namespace Raft_Shop.Services;

public class UserService : IUserService
{
    public Task<OrderInfo> GetOrderInfoAsync(string orderId)
    {
        throw new NotImplementedException();
    }

    public Task<OrderStatus> GetOrderStatusAsync(string orderId)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<OrderInfo>> GetPendingOrdersAsync(string userId)
    {
        throw new NotImplementedException();
    }

    public Task<decimal> GetUserBalanceAsync(string userId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateUserBalanceAsync(string userId, decimal amountChange)
    {
        throw new NotImplementedException();
    }
}
