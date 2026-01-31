using BerkYazilim.Data;
using BerkYazilim.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace BerkYazilim.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. DASHBOARD & İSTATİSTİKLER
        // ==========================================
        [HttpGet("dashboard-stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var totalRevenue = await _context.Orders.SumAsync(o => o.TotalAmount);
            var activeDealers = await _context.Users.CountAsync(u => u.Role == "Dealer");
            var pendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending" || o.Status == "Beklemede");
            var totalProducts = await _context.Products.CountAsync();

            var openTickets = await _context.SupportTickets.CountAsync(t => t.Status == "Open");
            var pendingServices = await _context.ServiceRequests.CountAsync(s => s.Status == "waiting");

            var recentOrders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .Select(o => new
                {
                    id = o.OrderNumber,
                    dealer = o.User.FullName,
                    date = o.OrderDate,
                    total = o.TotalAmount,
                    status = o.Status
                })
                .ToListAsync();

            return Ok(new { totalRevenue, activeDealers, pendingOrders, totalProducts, openTickets, pendingServices, recentOrders });
        }

        // ==========================================
        // 2. BAYİ YÖNETİMİ
        // ==========================================
        [HttpGet("dealers")]
        public async Task<ActionResult<IEnumerable<object>>> GetDealers()
        {
            var dealers = await _context.Users
                .Where(u => u.Role == "Dealer")
                .Select(u => new
                {
                    u.Id,
                    u.DealerCode,
                    u.FullName,
                    u.Email,
                    u.Phone,
                    u.Address,
                    u.CreditLimit,
                    OrderCount = _context.Orders.Count(o => o.User.Id == u.Id),
                    TotalSpend = _context.Orders.Where(o => o.User.Id == u.Id).Sum(o => o.TotalAmount)
                })
                .ToListAsync();

            return Ok(dealers);
        }

        [HttpPost("dealers")]
        public async Task<IActionResult> CreateDealer([FromBody] User dealer)
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            if (settings == null) settings = new SystemSetting();

            if (!settings.AllowNewDealers)
                return BadRequest("Sistem ayarlarında 'Yeni Bayi Kaydı' kapalıdır. Şu an bayi eklenemez.");

            if (await _context.Users.AnyAsync(u => u.DealerCode == dealer.DealerCode))
                return BadRequest("Bu bayi kodu zaten kullanılıyor!");

            dealer.Role = "Dealer";
            if (string.IsNullOrEmpty(dealer.Password)) dealer.Password = "123456";

            if (settings.EnforceStrongPassword)
            {
                if (dealer.Password.Length < 8 || !dealer.Password.Any(char.IsDigit))
                    return BadRequest("Güvenlik Politikası: Şifre en az 8 karakter olmalı ve en az bir rakam içermelidir.");
            }

            if (dealer.CreditLimit == 0) dealer.CreditLimit = settings.DefaultCreditLimit;

            _context.Users.Add(dealer);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Bayi başarıyla oluşturuldu.", limit = dealer.CreditLimit });
        }

        [HttpDelete("dealers/{id}")]
        public async Task<IActionResult> DeleteDealer(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("Bayi bulunamadı.");

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Bayi silindi." });
        }

        // ==========================================
        // 3. SİPARİŞ YÖNETİMİ
        // ==========================================
        [HttpGet("orders")]
        public async Task<ActionResult<IEnumerable<object>>> GetAllOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new
                {
                    o.Id,
                    o.OrderNumber,
                    DealerName = o.User.FullName,
                    o.OrderDate,
                    o.TotalAmount,
                    o.Status,
                    ItemCount = o.Items.Count
                })
                .ToListAsync();

            return Ok(orders);
        }

        //buraya başka şeyler eklenecek........

        [HttpPut("orders/{orderNumber}/status")]
        public async Task<IActionResult> UpdateOrderStatus(string orderNumber, [FromBody] UpdateStatusRequest request)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
            if (order == null) return NotFound("Sipariş bulunamadı.");

            order.Status = request.Status;
            if (!string.IsNullOrEmpty(request.TrackingNumber))
            {
                order.TrackingNumber = request.TrackingNumber;
            }
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = null, // Şimdilik null, çünkü admin ID'sini bu methodda bilmiyoruz (Token decode etmedik)
                UserName = "Admin/Sistem",
                Action = "Sipariş Güncelleme",
                Details = $"Sipariş {order.OrderNumber} durumu '{request.Status}' olarak değiştirildi.",
                IpAddress = Request.Headers.ContainsKey("X-Forwarded-For")
    ? Request.Headers["X-Forwarded-For"].ToString()
    : (HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Bilinmiyor"),
                Timestamp = DateTime.Now
            });
            await _context.SaveChangesAsync();
            return Ok(new { message = "Sipariş durumu güncellendi." });
        }

        // ==========================================
        // 4. DESTEK (TICKET) YÖNETİMİ
        // ==========================================
        [HttpGet("tickets")]
        public async Task<IActionResult> GetAllTickets()
        {
            var tickets = await _context.SupportTickets
                .Include(t => t.User)
                .Include(t => t.Messages)
                .OrderByDescending(t => t.LastUpdate)
                .Select(t => new
                {
                    t.Id,
                    t.TicketNumber,
                    Dealer = t.User.FullName,
                    t.Subject,
                    t.Status,
                    t.Priority,
                    LastUpdate = t.LastUpdate.ToString("dd MMM HH:mm", new CultureInfo("tr-TR")),
                    MessageCount = t.Messages.Count
                })
                .ToListAsync();

            return Ok(tickets);
        }

        [HttpGet("ticket/{ticketNumber}")]
        public async Task<IActionResult> GetTicketDetail(string ticketNumber)
        {
            var ticket = await _context.SupportTickets
                .Include(t => t.User)
                .Include(t => t.Messages)
                .FirstOrDefaultAsync(t => t.TicketNumber == ticketNumber);

            if (ticket == null) return NotFound();

            return Ok(new
            {
                ticket.TicketNumber,
                ticket.Subject,
                Dealer = ticket.User.FullName,
                ticket.Status,
                Messages = ticket.Messages.OrderBy(m => m.SentDate).Select(m => new
                {
                    m.Message,
                    Time = m.SentDate.ToString("HH:mm"),
                    m.IsAgent
                })
            });
        }

        [HttpPost("ticket/reply")]
        public async Task<IActionResult> ReplyTicket([FromBody] ReplyTicketDto request)
        {
            var ticket = await _context.SupportTickets.FirstOrDefaultAsync(t => t.TicketNumber == request.TicketNumber);
            if (ticket == null) return NotFound();

            var message = new SupportMessage
            {
                SupportTicketId = ticket.Id,
                Message = request.Message,
                IsAgent = true,
                SentDate = DateTime.Now
            };

            ticket.LastUpdate = DateTime.Now;
            ticket.Status = "Answered";

            _context.SupportMessages.Add(message);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        // ==========================================
        // 5. TEKNİK SERVİS YÖNETİMİ
        // ==========================================
        [HttpGet("services")]
        public async Task<IActionResult> GetAllServices()
        {
            var services = await _context.ServiceRequests
                .Include(s => s.User)
                .OrderByDescending(s => s.CreatedDate)
                .Select(s => new
                {
                    s.Id,
                    Code = s.ServiceCode,
                    Dealer = s.User.FullName,
                    Product = s.ProductName,
                    s.Category,
                    s.IssueDescription,
                    s.Status,
                    Date = s.CreatedDate.ToString("dd.MM.yyyy")
                })
                .ToListAsync();

            return Ok(services);
        }

        [HttpPost("service/update")]
        public async Task<IActionResult> UpdateServiceStatus([FromBody] UpdateServiceDto request)
        {
            var service = await _context.ServiceRequests.FirstOrDefaultAsync(s => s.ServiceCode == request.ServiceCode);
            if (service == null) return NotFound();

            service.Status = request.NewStatus;
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        // ==========================================
        // 6. ÜRÜN YÖNETİMİ
        // ==========================================
        [HttpGet("products")]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _context.Products
                .OrderByDescending(p => p.Id)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Category,
                    p.Price,
                    p.Stock,
                    p.Brand
                })
                .ToListAsync();
            return Ok(products);
        }

        [HttpPost("product")]
        public async Task<IActionResult> AddProduct([FromBody] Product product)
        {
            // DÜZELTME: existingProduct hatası silindi.
            // product.Image -> product.ImageUrl olarak düzeltildi.
            if (string.IsNullOrEmpty(product.ImageUrl)) product.ImageUrl = "https://via.placeholder.com/150";

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Ürün eklendi." });
        }

        [HttpDelete("product/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Ürün silindi." });
        }

        [HttpGet("product/{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound("Ürün bulunamadı.");
            return Ok(product);
        }

        [HttpPut("product/{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] Product product)
        {
            if (id != product.Id) return BadRequest("ID uyuşmazlığı.");

            var existingProduct = await _context.Products.FindAsync(id);
            if (existingProduct == null) return NotFound("Ürün bulunamadı.");

            existingProduct.Title = product.Title;
            existingProduct.Category = product.Category;
            existingProduct.Brand = product.Brand;
            existingProduct.Price = product.Price;
            existingProduct.Stock = product.Stock;

            // DÜZELTME: .Image yerine .ImageUrl kullandık
            existingProduct.ImageUrl = product.ImageUrl;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Ürün güncellendi." });
        }

        // AdminController.cs içine ekleyin:

        [HttpGet("logs")]
        public async Task<IActionResult> GetAuditLogs()
        {
            var logs = await _context.AuditLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(50) // Son 50 işlem
                .ToListAsync();

            return Ok(logs);
        }
    }
}