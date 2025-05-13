using ShopDienTu.Data;
using ShopDienTu.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ShopDienTu.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index(int? categoryId = null, int? subcategoryId = null, string sortOrder = null)
        {
            var now = DateTime.Now;
            var activePromotions = await _context.Promotions
                .Where(p => p.IsActive && p.StartDate <= now && p.EndDate >= now)
                .ToListAsync(); // L?y t?t c? active promotions (có th? t?i ?u h?n)
            ViewBag.ActivePromotions = activePromotions;

            // Get all categories with subcategories for the sidebar
            var categories = await _context.Categories
                .Include(c => c.SubCategories)
                .ToListAsync();
            ViewBag.Categories = categories;

            // Get featured products
            var productsQuery = _context.Products
                .Include(p => p.SubCategory)
                .ThenInclude(s => s.Category)
                .Include(p => p.ProductImages)
                .Where(p => p.IsActive);

            // Filter by category if specified
            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.SubCategory.CategoryID == categoryId.Value);
                ViewBag.SelectedCategoryId = categoryId.Value;
            }

            // Filter by subcategory if specified
            if (subcategoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.SubCategoryID == subcategoryId.Value);
                ViewBag.SelectedSubcategoryId = subcategoryId.Value;
            }

            // Apply sorting
            switch (sortOrder)
            {
                case "price_asc":
                    productsQuery = productsQuery.OrderBy(p => p.Price);
                    break;
                case "price_desc":
                    productsQuery = productsQuery.OrderByDescending(p => p.Price);
                    break;
                case "newest":
                    productsQuery = productsQuery.OrderByDescending(p => p.CreatedAt);
                    break;
                default:
                    productsQuery = productsQuery.OrderByDescending(p => p.CreatedAt);
                    break;
            }

            ViewBag.CurrentSort = sortOrder;
            var products = await productsQuery.ToListAsync();

            return View(products);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class ErrorViewModel
    {
        public string RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
