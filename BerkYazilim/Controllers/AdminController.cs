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
        [HttpGet("dashboard-stats")] // Frontend'de bu endpoint kullanılıyor
        public async Task<IActionResult> GetDashboardStats()
        {
            var totalRevenue = await _context.Orders.SumAsync(o => o.TotalAmount);
            var activeDealers = await _context.Users.CountAsync(u => u.Role == "Dealer");
            var pendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending" || o.Status == "Beklemede");
            var totalProducts = await _context.Products.CountAsync();

            // Ekstra İstatistikler (Yeni eklenen modüller için)
            var openTickets = await _context.SupportTickets.CountAsync(t => t.Status == "Open");
            var pendingServices = await _context.ServiceRequests.CountAsync(s => s.Status == "waiting");

            var recentOrders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .Select(o => new {
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
                    OrderCount = _context.Orders.Count(o => o.User.Id == u.Id),
                    TotalSpend = _context.Orders.Where(o => o.User.Id == u.Id).Sum(o => o.TotalAmount)
                })
                .ToListAsync();

            return Ok(dealers);
        }

        [HttpPost("dealers")]
        public async Task<IActionResult> CreateDealer([FromBody] User dealer)
        {
            if (await _context.Users.AnyAsync(u => u.DealerCode == dealer.DealerCode))
                return BadRequest("Bu bayi kodu zaten kullanılıyor!");

            dealer.Role = "Dealer";
            if (string.IsNullOrEmpty(dealer.Password)) dealer.Password = "123456";

            _context.Users.Add(dealer);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Bayi başarıyla oluşturuldu." });
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

            await _context.SaveChangesAsync();
            return Ok(new { message = "Sipariş durumu güncellendi." });
        }

        // ==========================================
        // 4. DESTEK (TICKET) YÖNETİMİ (YENİ)
        // ==========================================

        // Tüm Talepleri Getir
        [HttpGet("tickets")]
        public async Task<IActionResult> GetAllTickets()
        {
            var tickets = await _context.SupportTickets
                .Include(t => t.User)
                .Include(t => t.Messages)
                .OrderByDescending(t => t.LastUpdate)
                .Select(t => new {
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

        // Talep Detayı
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
                Messages = ticket.Messages.OrderBy(m => m.SentDate).Select(m => new {
                    m.Message,
                    Time = m.SentDate.ToString("HH:mm"),
                    m.IsAgent // True=Admin, False=Bayi
                })
            });
        }

        // Admin Cevap Yazıyor
        [HttpPost("ticket/reply")]
        public async Task<IActionResult> ReplyTicket([FromBody] ReplyTicketDto request)
        {
            var ticket = await _context.SupportTickets.FirstOrDefaultAsync(t => t.TicketNumber == request.TicketNumber);
            if (ticket == null) return NotFound();

            var message = new SupportMessage
            {
                SupportTicketId = ticket.Id,
                Message = request.Message,
                IsAgent = true, // Admin olduğu için True
                SentDate = DateTime.Now
            };

            ticket.LastUpdate = DateTime.Now;
            ticket.Status = "Answered";

            _context.SupportMessages.Add(message);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        // ==========================================
        // 5. TEKNİK SERVİS YÖNETİMİ (YENİ)
        // ==========================================

        // Tüm Servis Taleplerini Getir
        [HttpGet("services")]
        public async Task<IActionResult> GetAllServices()
        {
            var services = await _context.ServiceRequests
                .Include(s => s.User)
                .OrderByDescending(s => s.CreatedDate)
                .Select(s => new {
                    s.Id,
                    Code = s.ServiceCode,
                    Dealer = s.User.FullName,
                    Product = s.ProductName, // Modeldeki isim
                    s.Category,
                    s.IssueDescription,
                    s.Status,
                    Date = s.CreatedDate.ToString("dd.MM.yyyy")
                })
                .ToListAsync();

            return Ok(services);
        }

        // Servis Durumunu Güncelle
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
        // 6. ÜRÜN YÖNETİMİ (YENİ EKLENDİ)
        // ==========================================

        [HttpGet("products")]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _context.Products
                .OrderByDescending(p => p.Id)
                .Select(p => new {
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
            // Basit validasyon ve varsayılanlar
            if (string.IsNullOrEmpty(product.Image))
                product.Image = "https://via.placeholder.com/150";

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
    }


}