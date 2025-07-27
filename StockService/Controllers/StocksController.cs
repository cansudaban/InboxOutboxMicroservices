using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockService.Data;
using StockService.Models;

namespace StockService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StocksController : ControllerBase
    {
        private readonly StockDbContext _dbContext;
        private readonly ILogger<StocksController> _logger;

        public StocksController(StockDbContext dbContext, ILogger<StocksController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllStocks()
        {
            var stocks = await _dbContext.StockItems.ToListAsync();
            return Ok(stocks);
        }

        [HttpGet("{productId:guid}")]
        public async Task<IActionResult> GetStockByProductId(Guid productId)
        {
            var stock = await _dbContext.StockItems
                .FirstOrDefaultAsync(s => s.ProductId == productId);

            if (stock == null)
            {
                return NotFound();
            }

            return Ok(stock);
        }

        // Bu endpoint elle stok oluşturmak/güncellemek için test amaçlı eklenmiştir
        [HttpPost]
        public async Task<IActionResult> CreateOrUpdateStock([FromBody] CreateStockRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingStock = await _dbContext.StockItems
                .FirstOrDefaultAsync(s => s.ProductId == request.ProductId);

            if (existingStock != null)
            {
                // Update existing stock
                existingStock.AvailableQuantity = request.AvailableQuantity;
                existingStock.LastUpdated = DateTime.UtcNow;
            }
            else
            {
                // Create new stock
                var stock = new StockItem
                {
                    ProductId = request.ProductId,
                    ProductName = request.ProductName,
                    AvailableQuantity = request.AvailableQuantity,
                    LastUpdated = DateTime.UtcNow
                };
                _dbContext.StockItems.Add(stock);
            }

            await _dbContext.SaveChangesAsync();
            return Ok();
        }
    }

    public class CreateStockRequest
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int AvailableQuantity { get; set; }
    }
}
