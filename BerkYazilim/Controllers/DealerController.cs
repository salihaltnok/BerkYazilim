using BerkYazilim.Data;
using BerkYazilim.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace BerkYazilim.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DealerController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DealerController(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. DASHBOARD
        // ==========================================
        [HttpGet("dashboard/{dealerCode}")]
        public async Task<IActionResult> GetDealerDashboard(string dealerCode)
        {
            if (string.IsNullOrEmpty(dealerCode)) return BadRequest("Bayi kodu boş olamaz.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.DealerCode == dealerCode);
            if (user == null) return NotFound("Bayi bulunamadı.");

            var totalOrders = await _context.Orders.CountAsync(o => o.User.Id == user.Id);
            var pendingOrders = await _context.Orders.CountAsync(o => o.User.Id == user.Id && (o.Status == "Pending" || o.Status == "Beklemede"));
            var totalSpent = await _context.Orders.Where(o => o.User.Id == user.Id).SumAsync(o => o.TotalAmount);

            var recentOrders = await _context.Orders
                .Where(o => o.User.Id == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .Select(o => new
                {
                    OrderNumber = o.OrderNumber,
                    Date = o.OrderDate,
                    Amount = o.TotalAmount,
                    Status = o.Status,
                    ItemCount = o.Items.Count
                })
                .ToListAsync();

            return Ok(new { userFullName = user.FullName, dealerCode = user.DealerCode, totalOrders, pendingOrders, totalSpent, recentOrders });
        }

        // ==========================================
        // 2. FİNANS & CARİ HESAP
        // ==========================================
        [HttpGet("finance/{dealerCode}")]
        public async Task<IActionResult> GetFinanceStats(string dealerCode)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.DealerCode == dealerCode);
            if (user == null) return NotFound("Bayi bulunamadı.");

            var totalDebt = await _context.Orders.Where(o => o.User.Id == user.Id).SumAsync(o => o.TotalAmount);
            var totalPaid = await _context.Payments.Where(p => p.User.Id == user.Id && p.Status == "Approved").SumAsync(p => p.Amount);

            var currentBalance = totalDebt - totalPaid;
            decimal creditLimit = 100000;
            decimal availableLimit = creditLimit - currentBalance;

            var orders = await _context.Orders
                .Where(o => o.User.Id == user.Id)
                .Select(o => new { Date = o.OrderDate, Type = "Sipariş", Description = $"Sipariş No: {o.OrderNumber}", Amount = -o.TotalAmount, Status = o.Status })
                .ToListAsync();

            var payments = await _context.Payments
                .Where(p => p.User.Id == user.Id)
                .Select(p => new { Date = p.Date, Type = "Ödeme", Description = $"{p.Method} ile ödeme", Amount = p.Amount, Status = p.Status == "Approved" ? "Onaylandı" : "Bekliyor" })
                .ToListAsync();

            var transactions = orders.Concat(payments).OrderByDescending(t => t.Date).Take(10).ToList();

            return Ok(new { currentBalance, totalDebt, totalPaid, creditLimit, availableLimit, transactions });
        }

        [HttpPost("payment")]
        public async Task<IActionResult> MakePayment([FromBody] PaymentRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.DealerCode == request.DealerCode);
            if (user == null) return NotFound("Bayi bulunamadı.");

            var payment = new Payment
            {
                UserId = user.Id,
                Amount = request.Amount,
                Method = request.Method,
                Date = DateTime.Now,
                Status = "Approved",
                Description = "Online Ödeme"
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Ödeme başarıyla alındı." });
        }

        // ==========================================
        // 3. RAPORLAR
        // ==========================================
        [HttpGet("reports/{dealerCode}")]
        public async Task<IActionResult> GetReports(string dealerCode)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.DealerCode == dealerCode);
            if (user == null) return NotFound("Bayi bulunamadı.");

            // Genel Özet
            var totalOrders = await _context.Orders.Where(o => o.User.Id == user.Id).SumAsync(o => o.TotalAmount);
            var totalPaid = await _context.Payments.Where(p => p.User.Id == user.Id && p.Status == "Approved").SumAsync(p => p.Amount);
            var currentBalance = totalOrders - totalPaid;
            var orderCount = await _context.Orders.CountAsync(o => o.User.Id == user.Id);

            // Satış Grafiği (Son 6 Ay)
            var last6Months = Enumerable.Range(0, 6).Select(i => DateTime.Now.AddMonths(-i)).Reverse().ToList();
            var salesData = new List<decimal>();
            var monthLabels = new List<string>();

            foreach (var date in last6Months)
            {
                var monthTotal = await _context.Orders
                    .Where(o => o.User.Id == user.Id && o.OrderDate.Month == date.Month && o.OrderDate.Year == date.Year)
                    .SumAsync(o => o.TotalAmount);
                salesData.Add(monthTotal);
                monthLabels.Add(date.ToString("MMM", new CultureInfo("tr-TR")));
            }

            // Durum Grafiği
            var statusCounts = new List<int>
            {
                await _context.Orders.CountAsync(o => o.User.Id == user.Id && (o.Status == "Delivered" || o.Status == "Teslim Edildi" || o.Status == "Tamamlandı")),
                await _context.Orders.CountAsync(o => o.User.Id == user.Id && (o.Status == "Shipped" || o.Status == "Kargoda")),
                await _context.Orders.CountAsync(o => o.User.Id == user.Id && (o.Status == "Pending" || o.Status == "Beklemede" || o.Status == "Hazırlanıyor")),
                await _context.Orders.CountAsync(o => o.User.Id == user.Id && (o.Status == "Cancelled" || o.Status == "İptal"))
            };

            // En Çok Satan Ürünler (OrderItems üzerinden hesaplama)
            var topProducts = await _context.OrderItems
                .Include(i => i.Product)
                .Where(i => i.Order.User.Id == user.Id)
                .GroupBy(i => i.Product.Title)
                .Select(g => new
                {
                    Name = g.Key,
                    Category = g.First().Product.Category,
                    Count = g.Sum(x => x.Quantity),
                    Total = g.Sum(x => x.Quantity * x.Product.Price) // Düzeltilen Kısım: Adet * Fiyat
                })
                .OrderByDescending(x => x.Total)
                .Take(5)
                .ToListAsync();

            // Kategori Analizi
            var categoryStats = await _context.Products
                .GroupBy(p => p.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToListAsync();
            var catLabels = categoryStats.Select(c => c.Category).ToList();
            var catData = categoryStats.Select(c => c.Count).ToList();

            // Ödeme Analizi
            var paymentMethods = await _context.Payments
                .Where(p => p.User.Id == user.Id && p.Status == "Approved")
                .GroupBy(p => p.Method)
                .Select(g => new { Method = g.Key, Total = g.Sum(x => x.Amount) })
                .ToListAsync();

            // Son Hareketler (Tablo için)
            var recentOrders = await _context.Orders
                .Where(o => o.User.Id == user.Id).OrderByDescending(o => o.OrderDate).Take(10)
                .Select(o => new { Date = o.OrderDate, Description = $"Sipariş: {o.OrderNumber}", Type = "Sipariş", Amount = -o.TotalAmount, Status = o.Status })
                .ToListAsync();

            var recentPayments = await _context.Payments
                .Where(p => p.User.Id == user.Id).OrderByDescending(p => p.Date).Take(10)
                .Select(p => new { Date = p.Date, Description = $"Ödeme: {p.Method}", Type = "Ödeme", Amount = p.Amount, Status = "Onaylandı" })
                .ToListAsync();

            var transactions = recentOrders.Concat(recentPayments).OrderByDescending(t => t.Date).Take(20).ToList();

            return Ok(new
            {
                balance = currentBalance,
                totalShopping = totalOrders,
                totalPayment = totalPaid,
                countOrder = orderCount,
                salesChart = new { labels = monthLabels, data = salesData },
                statusChart = new { data = statusCounts },
                categoryChart = new { labels = catLabels, data = catData },
                topProducts = topProducts,
                paymentMethods = paymentMethods,
                transactions = transactions
            });
        }

        // ==========================================
        // 4. KARGO TAKİP
        // ==========================================
        [HttpGet("shipments/{dealerCode}")]
        public async Task<IActionResult> GetShipments(string dealerCode)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.DealerCode == dealerCode);
            if (user == null) return NotFound("Bayi bulunamadı.");

            // Kargo takip numarası olan VEYA durumu kargoda/teslim edildi olanlar
            var orders = await _context.Orders
                .Where(o => o.User.Id == user.Id && (!string.IsNullOrEmpty(o.TrackingNumber) || o.Status == "Shipped" || o.Status == "Delivered" || o.Status == "Kargoda"))
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .ToListAsync();

            var result = orders.Select(o => new
            {
                orderNumber = o.OrderNumber,
                trackingNumber = string.IsNullOrEmpty(o.TrackingNumber) ? "-" : o.TrackingNumber,
                date = o.OrderDate,
                itemCount = _context.OrderItems.Where(i => i.OrderId == o.Id).Sum(i => i.Quantity),
                statusKey = GetStatusKey(o.Status),
                statusText = GetStatusText(o.Status),
                logo = "https://cdn-icons-png.flaticon.com/512/757/757572.png"
            });

            return Ok(result);
        }

        [HttpGet("tracking/{trackingNumber}")]
        public async Task<IActionResult> GetTrackingDetail(string trackingNumber)
        {
            var order = await _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.TrackingNumber == trackingNumber || o.OrderNumber == trackingNumber);

            if (order == null) return NotFound("Kargo bulunamadı.");

            // Simülasyon Verisi
            var history = new List<object>();
            history.Add(new { date = order.OrderDate, location = "Sistem", status = "Sipariş Alındı", description = "Sipariş onaylandı.", icon = "fa-file-invoice", done = true });

            bool isShipped = order.Status == "Shipped" || order.Status == "Kargoda" || order.Status == "Delivered" || order.Status == "Teslim Edildi";
            history.Add(new { date = order.OrderDate.AddHours(4), location = "Depo", status = "Hazırlanıyor", description = "Paket hazırlanıyor.", icon = "fa-box-open", done = isShipped });

            if (isShipped)
            {
                history.Add(new { date = order.OrderDate.AddDays(1), location = "MNG Kargo", status = "Kargoya Verildi", description = "Kargo firmasına teslim edildi.", icon = "fa-truck", done = true });
            }

            bool isDelivered = order.Status == "Delivered" || order.Status == "Teslim Edildi";
            if (isDelivered)
            {
                history.Add(new { date = order.OrderDate.AddDays(3), location = "Teslimat Adresi", status = "Teslim Edildi", description = "Alıcıya teslim edildi.", icon = "fa-check-circle", done = true });
            }

            return Ok(new
            {
                trackingNumber = order.TrackingNumber ?? "Henüz Yok",
                carrier = "MNG Kargo",
                carrierLogo = "https://upload.wikimedia.org/wikipedia/commons/8/87/MNG_Kargo_logo.png",
                estimatedDelivery = order.OrderDate.AddDays(3),
                currentStatus = GetStatusText(order.Status),
                statusKey = GetStatusKey(order.Status),
                history = history.OrderByDescending(x => ((dynamic)x).date).ToList(),
                progress = isDelivered ? 100 : (isShipped ? 65 : 25)
            });
        }

        // ==========================================
        // 5. TEKNİK SERVİS (SİMÜLASYON)
        // ==========================================
        [HttpGet("services/{dealerCode}")]
        public async Task<IActionResult> GetServices(string dealerCode)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.DealerCode == dealerCode);
            if (user == null) return NotFound("Bayi bulunamadı.");

            // Veritabanından bu bayiye ait servis kayıtlarını çek
            var requests = await _context.ServiceRequests
                .Where(s => s.UserId == user.Id)
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();

            // Frontend'in beklediği formata dönüştür
            var result = requests.Select(s => new
            {
                Id = s.ServiceCode,
                Date = s.CreatedDate.ToString("dd MMMM yyyy", new CultureInfo("tr-TR")),
                Product = s.ProductName,
                Category = s.Category,
                Serial = s.SerialNumber,
                Issue = s.IssueDescription,
                Status = s.Status,
                StatusText = GetServiceStatusText(s.Status),
                Icon = GetCategoryIcon(s.Category), // İkonu kategoriye göre seç
                Steps = GenerateSteps(s.Status)     // Timeline'ı duruma göre oluştur
            });

            return Ok(result);
        }

        [HttpPost("service")]
        public async Task<IActionResult> CreateService([FromBody] ServiceRequestDTO request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.DealerCode == request.DealerCode);
            if (user == null) return NotFound("Bayi bulunamadı.");

            // Yeni Kayıt Oluştur
            var newService = new ServiceRequest
            {
                UserId = user.Id,
                ServiceCode = $"SRV-{DateTime.Now.Year}-{new Random().Next(1000, 9999)}",
                Category = request.Category,
                ProductName = request.Product,
                SerialNumber = request.Serial,
                IssueDescription = request.Description,
                Status = "waiting", // İlk kayıt "Bekliyor" olarak açılır
                CreatedDate = DateTime.Now
            };

            _context.ServiceRequests.Add(newService);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"{newService.ServiceCode} numaralı servis talebiniz başarıyla oluşturuldu." });
        }
        // 6. GARANTİ SORGULAMA (GÜNCELLENDİ: SİPARİŞ NO İLE DE ÇALIŞIR)
        [HttpGet("warranty/{query}")]
        public async Task<IActionResult> CheckWarranty(string query)
        {
            if (string.IsNullOrEmpty(query)) return BadRequest("Sorgu boş olamaz.");

            // --- TEST İÇİN OTOMATİK VERİ OLUŞTURMA ---
            if (!await _context.Warranties.AnyAsync())
            {
                _context.Warranties.Add(new WarrantyInfo
                {
                    SerialNumber = "SN-12345",
                    ProductName = "NVIDIA GeForce RTX 4090 Gaming X Trio",
                    PurchaseDate = DateTime.Now.AddMonths(-5),
                    WarrantyPeriodMonths = 36
                });
                await _context.SaveChangesAsync();
            }

            WarrantyInfo warranty = null;

            // 1. Durum: Sipariş Numarası ile arama (Örn: SIP-2025-001)
            if (query.StartsWith("SIP", StringComparison.OrdinalIgnoreCase))
            {
                // Siparişi bul
                var order = await _context.Orders
                    .Include(o => o.Items).ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(o => o.OrderNumber == query);

                if (order != null && order.Items.Any())
                {
                    // Siparişteki ilk ürünü garanti var gibi gösterelim (Simülasyon)
                    var item = order.Items.First();
                    warranty = new WarrantyInfo
                    {
                        SerialNumber = "SN-" + order.OrderNumber.Replace("SIP-", "") + "-01", // Otomatik seri no üret
                        ProductName = item.Product.Title,
                        PurchaseDate = order.OrderDate,
                        WarrantyPeriodMonths = 24 // Varsayılan 2 yıl
                    };
                }
            }
            // 2. Durum: Seri Numarası ile arama
            else
            {
                warranty = await _context.Warranties.FirstOrDefaultAsync(w => w.SerialNumber == query);
            }

            if (warranty == null)
            {
                return NotFound("Kayıt bulunamadı.");
            }

            // Sonuçları döndür
            return Ok(new
            {
                IsValid = warranty.IsActive,
                Product = warranty.ProductName,
                PurchaseDate = warranty.PurchaseDate.ToString("dd MMMM yyyy", new CultureInfo("tr-TR")),
                ExpiryDate = warranty.ExpiryDate.ToString("dd MMMM yyyy", new CultureInfo("tr-TR")),
                Remaining = warranty.RemainingTime,
                Period = $"{warranty.WarrantyPeriodMonths} Ay",
                StatusText = warranty.IsActive ? "Garanti Devam Ediyor" : "Garanti Süresi Doldu"
            });
        }
        // ==========================================
        // 7. DESTEK MERKEZİ (SUPPORT)
        // ==========================================

        // Talepleri Listele
        [HttpGet("tickets/{dealerCode}")]
        public async Task<IActionResult> GetTickets(string dealerCode)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.DealerCode == dealerCode);
            if (user == null) return NotFound("Bayi bulunamadı.");

            var tickets = await _context.SupportTickets
                .Where(t => t.UserId == user.Id)
                .OrderByDescending(t => t.LastUpdate)
                .Select(t => new {
                    t.TicketNumber,
                    t.Subject,
                    t.Status,
                    t.Priority,
                    Date = t.LastUpdate.ToString("dd MMM HH:mm", new CultureInfo("tr-TR")),
                    LastMessage = t.Messages.OrderByDescending(m => m.SentDate).FirstOrDefault().Message ?? "Dosya eki"
                })
                .ToListAsync();

            return Ok(tickets);
        }

        // Talep Detayı ve Mesajları Getir
        [HttpGet("ticket-detail/{ticketNumber}")]
        public async Task<IActionResult> GetTicketDetail(string ticketNumber)
        {
            var ticket = await _context.SupportTickets
                .Include(t => t.Messages)
                .FirstOrDefaultAsync(t => t.TicketNumber == ticketNumber);

            if (ticket == null) return NotFound();

            return Ok(new
            {
                ticket.TicketNumber,
                ticket.Subject,
                ticket.Status,
                Messages = ticket.Messages.OrderBy(m => m.SentDate).Select(m => new {
                    m.Message,
                    Time = m.SentDate.ToString("HH:mm"),
                    Date = m.SentDate.ToString("dd.MM.yyyy"),
                    m.IsAgent // True ise "Destek Ekibi", False ise "Siz"
                })
            });
        }

        // Yeni Talep Oluştur
        [HttpPost("ticket/create")]
        public async Task<IActionResult> CreateTicket([FromBody] CreateTicketDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.DealerCode == request.DealerCode);
            if (user == null) return NotFound();

            var ticket = new SupportTicket
            {
                UserId = user.Id,
                Subject = request.Subject,
                Priority = request.Priority,
                TicketNumber = "TKT-" + new Random().Next(10000, 99999),
                Status = "Open",
                Messages = new List<SupportMessage>
                {
                    new SupportMessage { Message = request.Message, IsAgent = false } // İlk mesaj kullanıcıdan
                }
            };

            // Simülasyon: Otomatik Hoşgeldin Mesajı
            ticket.Messages.Add(new SupportMessage
            {
                Message = $"Merhaba {user.FullName}, talebiniz bize ulaştı. En kısa sürede inceleyip dönüş yapacağız.",
                IsAgent = true,
                SentDate = DateTime.Now.AddSeconds(2)
            });

            _context.SupportTickets.Add(ticket);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Talep oluşturuldu." });
        }

        // Mevcut Talebe Mesaj Gönder
        [HttpPost("ticket/reply")]
        public async Task<IActionResult> ReplyTicket([FromBody] ReplyTicketDto request)
        {
            var ticket = await _context.SupportTickets.FirstOrDefaultAsync(t => t.TicketNumber == request.TicketNumber);
            if (ticket == null) return NotFound();

            var message = new SupportMessage
            {
                SupportTicketId = ticket.Id,
                Message = request.Message,
                IsAgent = false,
                SentDate = DateTime.Now
            };

            ticket.LastUpdate = DateTime.Now;
            ticket.Status = "Open"; // Kullanıcı yazınca tekrar açık olur

            _context.SupportMessages.Add(message);
            await _context.SaveChangesAsync();

            return Ok(new { time = message.SentDate.ToString("HH:mm") });
        }

        // S.S.S Getir (Seed Data ile)
        [HttpGet("faqs")]
        public async Task<IActionResult> GetFAQs()
        {
            if (!await _context.FAQs.AnyAsync())
            {
                _context.FAQs.AddRange(
                    new FAQ { Question = "Kargo ücreti ne kadar?", Answer = "5000 TL üzeri siparişlerde kargo ücretsizdir.", Category = "Kargo" },
                    new FAQ { Question = "İade süreci nasıl işler?", Answer = "Teslimattan sonraki 14 gün içinde iade talebi oluşturabilirsiniz.", Category = "İade" },
                    new FAQ { Question = "Ödeme seçenekleri neler?", Answer = "Kredi kartı, Havale/EFT ve Çek ile ödeme yapabilirsiniz.", Category = "Ödeme" }
                );
                await _context.SaveChangesAsync();
            }

            return Ok(await _context.FAQs.ToListAsync());
        }
    
        // ==========================================
        // YARDIMCI METODLAR
        // ==========================================
        private string GetStatusKey(string status)
        {
            if (string.IsNullOrEmpty(status)) return "processing";
            status = status.ToLower();
            if (status.Contains("deliver") || status.Contains("teslim") || status.Contains("tamam")) return "delivered";
            if (status.Contains("ship") || status.Contains("kargo")) return "transit";
            return "processing";
        }

        private string GetStatusText(string status)
        {
            if (string.IsNullOrEmpty(status)) return "İşleniyor";
            status = status.ToLower();
            if (status.Contains("deliver") || status.Contains("teslim") || status.Contains("tamam")) return "Teslim Edildi";
            if (status.Contains("ship") || status.Contains("kargo")) return "Yolda";
            return "Hazırlanıyor";
        }
        private string GetServiceStatusText(string status)
        {
            return status switch
            {
                "waiting" => "Bekliyor",
                "progress" => "İşlemde",
                "testing" => "Test Ediliyor",
                "completed" => "Tamamlandı",
                "shipped" => "Kargolandı",
                _ => "Bilinmiyor"
            };
        }

        private string GetCategoryIcon(string category)
        {
            return category?.ToLower() switch
            {
                "ekran kartı" => "fa-microchip", // GPU
                "işlemci" => "fa-server",        // CPU
                "anakart" => "fa-chess-board",   // Mobo
                "ram" => "fa-memory",
                "ssd" => "fa-hdd",
                "güç kaynağı" => "fa-plug",
                "diğer" => "fa-tools",
                _ => "fa-box"
            };
        }

        // Duruma göre Timeline adımlarını dinamik oluşturur
        private object GenerateSteps(string status)
        {
            // Adımlar: Talep -> Kargo -> İnceleme -> Onarım -> Test -> Sonuç
            var steps = new List<dynamic>();

            bool isWaiting = status == "waiting";
            bool isProgress = status == "progress";
            bool isTesting = status == "testing";
            bool isCompleted = status == "completed";

            // 1. Talep Alındı (Her zaman true)
            steps.Add(new { s = "Talep Alındı", done = true });

            // 2. Kargo (Basitlik olsun diye, talep açıldıysa kargo da yolda sayalım veya kullanıcı göndermiş olsun)
            steps.Add(new { s = "Kargo Teslim", done = !isWaiting }); // Beklemedeyse henüz kargo ulaşmamış sayalım

            // 3. İnceleme / İşlem
            steps.Add(new { s = "İnceleme", done = isTesting || isCompleted, current = isProgress });

            // 4. Test
            steps.Add(new { s = "Test", done = isCompleted, current = isTesting });

            // 5. Sonuç
            steps.Add(new { s = "Tamamlandı", done = isCompleted, current = false });

            return steps;
        }
    }

}