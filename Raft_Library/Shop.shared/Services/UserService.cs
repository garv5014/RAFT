using Raft_Library.Gateway.shared;
using Raft_Library.Models;

namespace Raft_Library.Shop.shared.Services;

public class UserService : IUserService
{
    private readonly IGatewayClient gateway;

    public UserService(IGatewayClient gateway)
    {
        this.gateway = gateway;
    }

    public async Task<bool> DepositBalanceAsync(string userId, decimal amountChange)
    {
        var userBalanceValue = await gateway.StrongGet($"user-balance-{userId}");
        decimal currentBalance =
            userBalanceValue != null ? decimal.Parse(userBalanceValue.Value) : 0;
        decimal newBalance = currentBalance + amountChange;

        // Directly set the new balance using CompareAndSwap to handle concurrent updates
        var result = await gateway.CompareAndSwap(
            new CompareAndSwapRequest
            {
                Key = $"user-balance-{userId}",
                OldValue = userBalanceValue?.Value,
                NewValue = newBalance.ToString()
            }
        );

        return result.IsSuccessStatusCode;
    }

    public async Task<decimal> GetUserBalanceAsync(string userId)
    {
        var userBalanceValue = await gateway.StrongGet($"user-balance-{userId}");
        return userBalanceValue != null ? decimal.Parse(userBalanceValue.Value) : 0;
    }

    public async Task<bool> WithdrawBalanceAsync(string userId, decimal amountChange)
    {
        var userBalanceValue = await gateway.StrongGet($"user-balance-{userId}");
        if (userBalanceValue == null || decimal.Parse(userBalanceValue.Value) <= 0)
        {
            return false;
        }

        decimal currentBalance = decimal.Parse(userBalanceValue.Value);
        if (currentBalance - amountChange < 0)
        {
            return false;
        }

        decimal newBalance = currentBalance - amountChange;

        var result = await gateway.CompareAndSwap(
            new CompareAndSwapRequest
            {
                Key = $"user-balance-{userId}",
                OldValue = userBalanceValue.Value,
                NewValue = newBalance.ToString()
            }
        );

        return result.IsSuccessStatusCode;
    }

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
}
