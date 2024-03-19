namespace Raft_Library.Shop.shared;

public class Product
{
    public string Name { get; set; }
    public decimal Price { get; set; } = 1.00m; // All products have a fixed cost of $1.00
    public int Stock { get; set; } = 0; // All products start with 0 stock
}
