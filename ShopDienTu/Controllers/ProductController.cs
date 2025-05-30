using ShopDienTu.Data;
using ShopDienTu.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace ShopDienTu.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Product/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.SubCategory)
                    .ThenInclude(s => s.Category)
                .Include(p => p.Promotions)
                .Include(p => p.ProductImages)
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(m => m.ProductID == id);

            if (product == null)
            {
                return NotFound();
            }

            decimal rankDiscountPercentage = 0m;
            if (User.Identity.IsAuthenticated)
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var user = await _context.Users
                    .Include(u => u.Rank) // Đảm bảo include Rank để lấy DiscountPercentage
                    .FirstOrDefaultAsync(u => u.UserID == userId);

                if (user != null && user.Rank != null)
                {
                    rankDiscountPercentage = user.Rank.DiscountPercentage;
                }
            }
            ViewBag.RankDiscountPercentage = rankDiscountPercentage;

            // Get related products from the same subcategory
            var relatedProducts = await _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.Promotions)
                .Where(p => p.SubCategoryID == product.SubCategoryID && p.ProductID != product.ProductID && p.IsActive)
                .Take(4)
                .ToListAsync();

            ViewBag.RelatedProducts = relatedProducts;

            return View(product);
        }

        // GET: Product/Search
        public async Task<IActionResult> Search(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                return RedirectToAction("Index", "Home");
            }

            var products = await _context.Products
                .Include(p => p.SubCategory)
                .ThenInclude(s => s.Category)
                .Include(p => p.ProductImages)
                .Where(p => p.IsActive && (p.ProductName.Contains(searchTerm) || p.Description.Contains(searchTerm)))
                .ToListAsync();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.ResultCount = products.Count;

            // Get all categories with subcategories for the sidebar
            var categories = await _context.Categories
                .Include(c => c.SubCategories)
                .ToListAsync();
            ViewBag.Categories = categories;

            return View(products);
        }
    }
}
