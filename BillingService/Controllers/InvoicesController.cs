using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BillingService.Data;
using BillingService.Models;

namespace BillingService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoicesController : ControllerBase
    {
        private readonly BillingDbContext _dbContext;
        private readonly ILogger<InvoicesController> _logger;

        public InvoicesController(BillingDbContext dbContext, ILogger<InvoicesController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllInvoices()
        {
            var invoices = await _dbContext.Invoices
                .Include(i => i.Items)
                .ToListAsync();
            return Ok(invoices);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetInvoice(Guid id)
        {
            var invoice = await _dbContext.Invoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
            {
                return NotFound();
            }

            return Ok(invoice);
        }

        [HttpGet("order/{orderId:guid}")]
        public async Task<IActionResult> GetInvoiceByOrderId(Guid orderId)
        {
            var invoice = await _dbContext.Invoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.OrderId == orderId);

            if (invoice == null)
            {
                return NotFound();
            }

            return Ok(invoice);
        }

        [HttpPatch("{id:guid}/pay")]
        public async Task<IActionResult> PayInvoice(Guid id)
        {
            var invoice = await _dbContext.Invoices.FindAsync(id);

            if (invoice == null)
            {
                return NotFound();
            }

            if (invoice.IsPaid)
            {
                return BadRequest("Invoice already paid");
            }

            invoice.IsPaid = true;
            invoice.PaidAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            return Ok(invoice);
        }
    }
}
