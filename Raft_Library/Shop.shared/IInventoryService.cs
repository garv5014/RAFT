namespace Raft_Library.Shop.shared;

public interface IInventoryService
{
    Task<bool> AddItemToStockAsync(string itemId, int quantity);
    Task<bool> RemoveItemFromStockAsync(string itemId, int quantity);
    Task<int> GetItemStockAsync(string itemId);
}
