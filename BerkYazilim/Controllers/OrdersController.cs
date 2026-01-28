using BerkYazilim.Data;
using BerkYazilim.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BerkYazilim.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/orders
        // Siparişleri Listeleme Metodu
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.Items)            // Siparişin içindeki ürün kalemlerini getir
                .ThenInclude(oi => oi.Product)    // Kalemlerin ürün detaylarını getir
                .OrderByDescending(o => o.OrderDate) // En yeniden eskiye sırala
                .Select(o => new
                {
                    id = o.OrderNumber,           // Frontend'deki 'id' alanına eşledik
                    date = o.OrderDate,
                    status = o.Status.ToLower(),  // 'shipped', 'pending' vb. küçük harf olsun
                    total = o.TotalAmount,
                    trackingNo = o.TrackingNumber,
                    address = o.Address,
                    items = o.Items.Select(i => new {
                        name = i.Product.Title,
                        qty = i.Quantity,
                        price = i.UnitPrice,
                        img = i.Product.ImageUrl
                    }).ToList()
                })
                .ToListAsync();

            return Ok(orders);
        }

        // POST: api/orders
        // Yeni Sipariş Oluşturma Metodu
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            if (request.Items == null || !request.Items.Any())
            {
                return BadRequest("Sepetiniz boş.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.DealerCode == request.DealerCode);

            if (user == null)
            {
                // Eğer kullanıcı bulunamazsa hata mesajı dön
                return Unauthorized(new { error = "Kullanıcı bulunamadı veya oturum süresi dolmuş." });
            }
            // 2. Yeni bir sipariş nesnesi oluştur
            var order = new Order
            {
                OrderNumber = "SIP-" + DateTime.Now.Year + "-" + new Random().Next(1000, 9999),
                OrderDate = DateTime.Now,
                Status = "Pending", // Beklemede
                Address = "Merkez Mah. Teknoloji Cad. No:1", // Şimdilik sabit
                User = user,
                TotalAmount = 0 // Aşağıda hesaplanacak
            };

            decimal totalAmount = 0;

            // 3. Sepetteki ürünleri dön, stok kontrolü yap ve ekle
            foreach (var itemDto in request.Items)
            {
                var product = await _context.Products.FindAsync(itemDto.ProductId);

                if (product == null) continue; // Ürün silinmişse atla

                // Stok Yetersizse Hata Dön
                if (product.Stock < itemDto.Quantity)
                {
                    return BadRequest($"'{product.Title}' ürünü için yeterli stok yok. Kalan: {product.Stock}");
                }

                // Stoktan Düş
                product.Stock -= itemDto.Quantity;

                // Sipariş Kalemini Oluştur
                var orderItem = new OrderItem
                {
                    Product = product,
                    Quantity = itemDto.Quantity,
                    UnitPrice = product.Price // Fiyatı veritabanından alıyoruz (Güvenlik)
                };

                order.Items.Add(orderItem);
                totalAmount += orderItem.Quantity * orderItem.UnitPrice;
            }

            order.TotalAmount = totalAmount;

            // 4. Veritabanına Kaydet
            _context.Orders.Add(order);
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                UserName = user.FullName,
                Action = "Yeni Sipariş",
                Details = $"Sipariş No: {order.OrderNumber}, Tutar: {order.TotalAmount:C2}",
                IpAddress = Request.Headers.ContainsKey("X-Forwarded-For")
    ? Request.Headers["X-Forwarded-For"].ToString()
    : (HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Bilinmiyor"),
                Timestamp = DateTime.Now
            });
            await _context.SaveChangesAsync();

            return Ok(new { message = "Sipariş başarıyla oluşturuldu!", orderId = order.OrderNumber });
        }
        [HttpGet("detail/{orderNumber}")]
        public async Task<ActionResult<object>> GetOrderDetails(string orderNumber)
        {
            // Veritabanında bu numaraya sahip siparişi bul
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

            if (order == null)
            {
                // Sipariş bulunamazsa 404 dön
                return NotFound(new { message = "Sipariş bulunamadı." });
            }

            // Frontend'in beklediği formatta cevap hazırla
            var result = new
            {
                orderNumber = order.OrderNumber,
                date = order.OrderDate,
                status = order.Status, // Status bilgisini olduğu gibi gönderiyoruz
                totalAmount = order.TotalAmount,
                address = order.Address,
                dealer = order.User?.FullName ?? "Bilinmiyor", // User null gelirse hata vermesin
                items = order.Items.Select(i => new
                {
                    productName = i.Product?.Title ?? "Silinmiş Ürün",
                    quantity = i.Quantity,
                    unitPrice = i.UnitPrice,
                    total = i.Quantity * i.UnitPrice,
                    image = i.Product?.ImageUrl // Resim yoksa null gider, frontend halleder
                })
            };

            return Ok(result);
        }
        // GET: api/orders/invoice/SIP-2025-1234
        [HttpGet("invoice/{orderNumber}")]
        public async Task<IActionResult> DownloadInvoice(string orderNumber)
        {
            // 1. Siparişi Veritabanından Çek
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

            if (order == null) return NotFound("Sipariş bulunamadı.");

            // 2. PDF Dokümanını Oluştur (QuestPDF)
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                    // --- HEADER (BAŞLIK) ---
                    page.Header().Row(row =>
                    {
                        // Sol Taraf: Logo ve Firma Adı
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("BERK YAZILIM").SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);
                            col.Item().Text("Teknoloji Mah. Yazılım Cad. No:1");
                            col.Item().Text("İstanbul, Türkiye");
                            col.Item().Text("Vergi No: 1234567890");
                        });

                        // Sağ Taraf: Fatura Bilgileri
                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text("FATURA").FontSize(24).SemiBold().FontColor(Colors.Grey.Lighten1);
                            col.Item().Text($"Sipariş No: {order.OrderNumber}");
                            col.Item().Text($"Tarih: {order.OrderDate:dd.MM.yyyy}");
                        });
                    });

                    // --- CONTENT (İÇERİK) ---
                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        // Müşteri Bilgileri
                        col.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Sayın:").SemiBold();
                                c.Item().Text(order.User?.FullName ?? "Misafir Müşteri");
                                c.Item().Text(order.Address);
                            });
                        });

                        col.Item().PaddingVertical(10); // Boşluk

                        // Ürün Tablosu
                        col.Item().Table(table =>
                        {
                            // Kolon Genişlikleri
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(25); // Sıra No
                                columns.RelativeColumn(3);  // Ürün Adı
                                columns.RelativeColumn();   // Adet
                                columns.RelativeColumn();   // Birim Fiyat
                                columns.RelativeColumn();   // Toplam
                            });

                            // Tablo Başlığı
                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("#");
                                header.Cell().Element(CellStyle).Text("Ürün Adı");
                                header.Cell().Element(CellStyle).AlignRight().Text("Adet");
                                header.Cell().Element(CellStyle).AlignRight().Text("Birim Fiyat");
                                header.Cell().Element(CellStyle).AlignRight().Text("Toplam");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Medium);
                                }
                            });

                            // Tablo Satırları
                            foreach (var item in order.Items)
                            {
                                table.Cell().Element(CellStyle).Text((order.Items.IndexOf(item) + 1).ToString());
                                table.Cell().Element(CellStyle).Text(item.Product.Title);
                                table.Cell().Element(CellStyle).AlignRight().Text(item.Quantity.ToString());
                                table.Cell().Element(CellStyle).AlignRight().Text($"{item.UnitPrice:N2} ₺");
                                table.Cell().Element(CellStyle).AlignRight().Text($"{(item.Quantity * item.UnitPrice):N2} ₺");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(5);
                                }
                            }
                        });

                        // Toplamlar
                        col.Item().PaddingTop(10).AlignRight().Column(c =>
                        {
                            var subTotal = order.TotalAmount / 1.20m;
                            var tax = order.TotalAmount - subTotal;

                            c.Item().Text($"Ara Toplam: {subTotal:N2} ₺");
                            c.Item().Text($"KDV (%20): {tax:N2} ₺");
                            c.Item().Text($"GENEL TOPLAM: {order.TotalAmount:N2} ₺").SemiBold().FontSize(14).FontColor(Colors.Blue.Medium);
                        });
                    });

                    // --- FOOTER (ALT BİLGİ) ---
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Sayfa ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });

            // 3. Dosyayı Oluştur ve Döndür
            var pdfBytes = document.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"Fatura_{order.OrderNumber}.pdf");
        }
    }
}