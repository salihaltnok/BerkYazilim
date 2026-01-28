using BerkYazilim.Data;
using BerkYazilim.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BerkYazilim.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            // Veritabanındaki tüm ürünleri listeler
            return await _context.Products.ToListAsync();
        }

        // GET: api/products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return product;
        }
        // YENİ ÜRÜN EKLEME
        // POST: api/products
        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct([FromBody] Product product)
        {
            // Basit validasyonlar
            if (product == null) return BadRequest("Ürün verisi boş olamaz.");

            // Eğer resim URL boşsa varsayılan resim ata
            if (string.IsNullOrEmpty(product.ImageUrl))
                product.ImageUrl = "https://via.placeholder.com/500?text=Urun+Resmi";

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Ürün başarıyla eklendi.", id = product.Id });
        }

        // ÜRÜN SİLME
        // DELETE: api/products/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound("Ürün bulunamadı.");
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Ürün silindi." });
        }
        
    }
}