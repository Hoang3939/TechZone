using ShopDienTu.Data;
using ShopDienTu.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

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

        public async Task<IActionResult> Index(string searchTerm, int? categoryId = null, int? subcategoryId = null, string sortOrder = null, int? page = 1, int? pageSize = 10)
        {
            var now = DateTime.Now;
            var activePromotions = await _context.Promotions
                .Where(p => p.IsActive && p.ProductID != null && p.StartDate <= now && p.EndDate >= now)
                .ToListAsync(); // L?y t?t c? active promotions (có th? t?i ?u h?n)
            ViewBag.ActivePromotions = activePromotions;

            decimal rankDiscountPercentage = 0m;
            if (User.Identity.IsAuthenticated)
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var user = await _context.Users
                    .Include(u => u.Rank) // ??m b?o include Rank ?? l?y DiscountPercentage
                    .FirstOrDefaultAsync(u => u.UserID == userId);

                if (user != null && user.Rank != null)
                {
                    rankDiscountPercentage = user.Rank.DiscountPercentage;
                }
            }
            ViewBag.RankDiscountPercentage = rankDiscountPercentage; // Truy?n vào ViewBag

            // Get all categories with subcategories for the sidebar
            var categories = await _context.Categories
                .Include(c => c.SubCategories)
                .ToListAsync();
            ViewBag.Categories = categories;

            // Get featured products
            IQueryable<Product> productsQuery = _context.Products
                .Include(p => p.SubCategory)
                .ThenInclude(s => s.Category)
                .Include(p => p.ProductImages)
                .AsSplitQuery() // V?n nên dùng AsSplitQuery n?u có nhi?u Include collection
                .Where(p => p.IsActive);

            // L?c theo t? khóa tìm ki?m
            if (!string.IsNullOrEmpty(searchTerm))
            {
                productsQuery = productsQuery.Where(p =>
                    p.ProductName.ToLower().Contains(searchTerm.ToLower()) ||
                    (p.Description != null && p.Description.ToLower().Contains(searchTerm.ToLower())));
                ViewBag.SearchTerm = searchTerm;
            }
            else
            {
                productsQuery = productsQuery.OrderByDescending(p => p.CreatedAt);
            }

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

            int currentPage = page ?? 1; // Trang hi?n t?i, m?c ??nh là 1
            int itemsPerPage = pageSize ?? 10; // S? s?n ph?m trên m?i trang, m?c ??nh là 9

            int totalItems = await productsQuery.CountAsync(); // T?ng s? s?n ph?m (tr??c khi phân trang)
            ViewBag.TotalItems = totalItems;

            int totalPages = (int)Math.Ceiling((double)totalItems / itemsPerPage);
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = currentPage;
            ViewBag.PageSize = itemsPerPage;

            // Áp d?ng Skip và Take ?? l?y s?n ph?m cho trang hi?n t?i
            var products = await productsQuery
                                .Skip((currentPage - 1) * itemsPerPage)
                                .Take(itemsPerPage)
                                .ToListAsync();

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
