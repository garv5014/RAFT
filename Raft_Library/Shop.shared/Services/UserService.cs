using System.Text.Json;
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

    public async Task<bool> CreateOrderAsync(
        string orderId,
        Dictionary<string, int> items,
        string username
    )
    {
        const int maxRetries = 5;
        int currentRetry = 0;
        bool success = false;

        var orderInfo = new OrderInfo { Purchaser = username, Products = items };
        var orderInfoJson = JsonSerializer.Serialize(orderInfo);

        // Create order info entry
        await gateway.CompareAndSwap(
            new CompareAndSwapRequest
            {
                Key = $"order-info {orderId}",
                OldValue = null, // Assuming new order, so no old value
                NewValue = orderInfoJson
            }
        );

        // Set initial order status to pending
        var status = "pending";
        await gateway.CompareAndSwap(
            new CompareAndSwapRequest
            {
                Key = $"order-status {orderId}",
                OldValue = null,
                NewValue = status
            }
        );

        // Add to pending orders
        while (!success && currentRetry < maxRetries)
        {
            var pendingOrdersResponse = await gateway.StrongGet("pending-orders");
            var pendingOrders =
                pendingOrdersResponse != null && !string.IsNullOrEmpty(pendingOrdersResponse.Value)
                    ? JsonSerializer.Deserialize<List<string>>(pendingOrdersResponse.Value)
                    : new List<string>();

            if (!pendingOrders.Contains(orderId))
            {
                pendingOrders.Add(orderId);
                var serializedPendingOrders = JsonSerializer.Serialize(pendingOrders);
                var response = await gateway.CompareAndSwap(
                    new CompareAndSwapRequest
                    {
                        Key = "pending-orders",
                        OldValue = pendingOrdersResponse?.Value,
                        NewValue = serializedPendingOrders
                    }
                );

                success = response.IsSuccessStatusCode;
            }
            else
            {
                success = true; // Order already in pending, consider success
            }
            currentRetry++;
            if (!success)
            {
                await Task.Delay(100); // Delay before retrying
            }
        }

        return success;
    }

    public async Task<IEnumerable<string>> GetPendingOrdersAsync()
    {
        var versionedValue = await gateway.StrongGet("pending-orders");
        if (versionedValue == null || string.IsNullOrEmpty(versionedValue.Value))
        {
            return Enumerable.Empty<string>();
        }

        try
        {
            var pendingOrders = JsonSerializer.Deserialize<List<string>>(versionedValue.Value);
            return pendingOrders ?? Enumerable.Empty<string>();
        }
        catch (JsonException)
        {
            return Enumerable.Empty<string>();
        }
    }

    public async Task<OrderStatus> GetOrderStatusAsync(string orderId)
    {
        var versionedValue = await gateway.StrongGet($"order-status {orderId}");
        if (versionedValue == null || string.IsNullOrEmpty(versionedValue.Value))
        {
            return null;
        }
        var status = versionedValue.Value;
        return new OrderStatus { OrderId = orderId, Status = status };
    }

    public async Task<OrderInfo> GetOrderInfoAsync(string orderId)
    {
        var versionedValue = await gateway.StrongGet($"order-info {orderId}");
        if (versionedValue == null || string.IsNullOrEmpty(versionedValue.Value))
        {
            return null;
        }

        try
        {
            var orderInfo = JsonSerializer.Deserialize<OrderInfo>(versionedValue.Value);
            return orderInfo;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task<IEnumerable<OrderInfo>> GetAllPendingOrdersAsync()
    {
        var pendingOrderIds = await GetPendingOrdersAsync();
        var pendingOrdersInfoTasks = pendingOrderIds.Select(id => GetOrderInfoAsync(id));
        var pendingOrdersInfo = await Task.WhenAll(pendingOrdersInfoTasks);

        return pendingOrdersInfo.Where(info => info != null);
    }
}
