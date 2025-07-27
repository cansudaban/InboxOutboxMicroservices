namespace StockService.Models
{
    public class StockItem
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int AvailableQuantity { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}

