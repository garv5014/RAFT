using Raft_Library.Gateway.shared;
using Raft_Library.Models;

namespace Raft_Library.Shop.shared.Services;

public class ShopInventoryService : IInventoryService
{
    private readonly IGatewayClient gatewayService;

    public ShopInventoryService(IGatewayClient gatewayService)
    {
        this.gatewayService = gatewayService;
    }

    public async Task<bool> AddItemToStockAsync(string itemId, int quantity)
    {
        var itemStock = await gatewayService.StrongGet(itemId); // or EventualGet for eventual consistency

        bool updated;
        if (itemStock == null)
        {
            updated = await TryUpdateStockAsync(itemId, quantity, null);
        }
        else
        {
            int currentStock = int.Parse(itemStock.Value);
            int newStock = currentStock + quantity;
            updated = await TryUpdateStockAsync(
                itemId,
                newStock,
                itemStock == null ? null : itemStock.Value
            );
        }
        return updated;
    }

    public async Task<int> GetItemStockAsync(string itemId)
    {
        var itemStock = await gatewayService.StrongGet(itemId); // or EventualGet for eventual consistency
        return itemStock != null ? int.Parse(itemStock?.Value) : 0;
    }

    public async Task<bool> RemoveItemFromStockAsync(string itemId, int quantity)
    {
        var itemStock = await gatewayService.StrongGet(itemId); // or EventualGet for eventual consistency
        if (itemStock == null)
            return false;

        int currentStock = int.Parse(itemStock.Value);
        int newStock = Math.Max(0, currentStock - quantity); // Ensure stock doesn't go below zero
        var updated = await TryUpdateStockAsync(itemId, newStock, itemStock.Value);
        return updated;
    }

    private async Task<bool> TryUpdateStockAsync(string itemId, int newStock, string? oldValue)
    {
        var compareAndSwapRequest = new CompareAndSwapRequest
        {
            Key = itemId,
            OldValue = oldValue,
            NewValue = newStock.ToString()
        };

        Func<Task<bool>> operation = async () =>
        {
            var response = await gatewayService.CompareAndSwap(compareAndSwapRequest);
            return response.IsSuccessStatusCode;
        };

        return await ExecuteWithRetryAsync(
            operation,
            TimeSpan.FromMilliseconds(500),
            maxRetries: 3
        );
    }

    private async Task<bool> ExecuteWithRetryAsync(
        Func<Task<bool>> operation,
        TimeSpan delayBetweenRetries,
        int maxRetries = 3
    )
    {
        int attempt = 0;
        while (attempt < maxRetries)
        {
            if (await operation())
                return true; // Success
            await Task.Delay(delayBetweenRetries);
            attempt++;
        }

        return false; // Exceeded max retries
    }
}
