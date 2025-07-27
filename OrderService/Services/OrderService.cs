using Contracts.Events;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;
using System.Text.Json;

namespace OrderService.Services
{
    public class OrderService
    {
        private readonly OrderDbContext _dbContext;
        private readonly ILogger<OrderService> _logger;

        public OrderService(OrderDbContext dbContext, ILogger<OrderService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
            // Begin transaction to ensure both order and outbox message are saved atomically
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // Set order properties
                order.Id = Guid.NewGuid();
                order.CreatedAt = DateTime.UtcNow;
                order.TotalAmount = order.Items.Sum(i => i.UnitPrice * i.Quantity);

                // Save order to database
                _dbContext.Orders.Add(order);

                // Create OrderCreated event and save to outbox
                var orderCreatedEvent = new OrderCreatedEventDto
                {
                    OrderId = order.Id,
                    CustomerName = order.CustomerName,
                    Items = order.Items.Select(i => new OrderItemDto
                    {
                        ProductId = i.ProductId,
                        ProductName = i.ProductName,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice
                    }).ToList(),
                    CreatedAt = order.CreatedAt,
                    TotalAmount = order.TotalAmount
                };

                // Create outbox message
                var outboxMessage = OutboxMessage.Create("OrderCreated", orderCreatedEvent);
                _dbContext.OutboxMessages.Add(outboxMessage);

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Order created: {OrderId}", order.Id);
                return order;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating order");
                throw;
            }
        }

        public async Task<IEnumerable<Order>> GetOrdersAsync()
        {
            return await _dbContext.Orders
                .Include(o => o.Items)
                .ToListAsync();
        }

        public async Task<Order?> GetOrderAsync(Guid id)
        {
            return await _dbContext.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);
        }
    }
}
