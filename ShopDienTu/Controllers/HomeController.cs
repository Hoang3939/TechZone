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

        public async Task<IActionResult> Index(string searchTerm, int? categoryId = null, int? subcategoryId = null, string sortOrder = null, int? page = 1, int? pageSize = 10, decimal? minPrice = null, decimal? maxPrice = null)
        {
            var now = DateTime.Now;

            // L?y t?t c? các khuy?n mãi ?ang ho?t ??ng (nh? ban ??u, ?? hi?n th? chung n?u c?n)
            var activePromotions = await _context.Promotions
                .Where(p => p.IsActive && p.ProductID != null && p.StartDate <= now && p.EndDate >= now)
                .ToListAsync();
            ViewBag.ActivePromotions = activePromotions; // Gi? l?i ViewBag này

            // L?y chi?t kh?u rank c?a ng??i dùng hi?n t?i
            decimal rankDiscountPercentage = 0m;
            if (User.Identity.IsAuthenticated)
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdClaim, out int userId))
                {
                    var user = await _context.Users
                        .Include(u => u.Rank)
                        .FirstOrDefaultAsync(u => u.UserID == userId);

                    if (user != null && user.Rank != null)
                    {
                        rankDiscountPercentage = user.Rank.DiscountPercentage;
                    }
                }
            }
            ViewBag.RankDiscountPercentage = rankDiscountPercentage; 

            // L?y t?t c? danh m?c và danh m?c con cho sidebar
            var categories = await _context.Categories
                .Include(c => c.SubCategories)
                .ToListAsync();
            ViewBag.Categories = categories; // Gi? l?i ViewBag này

            // B?t ??u truy v?n s?n ph?m
            // S? d?ng m?t ki?u ?n danh ?? ch?a Product và EffectivePrice ?ã tính toán
            var productsQueryWithCalculatedPrice = _context.Products
                .Include(p => p.SubCategory)
                    .ThenInclude(s => s.Category)
                .Include(p => p.ProductImages)
                .AsSplitQuery()
                .Where(p => p.IsActive)
                .Select(p => new
                {
                    Product = p,
                    BestPromotion = _context.Promotions
                        .Where(promo => promo.ProductID == p.ProductID &&
                                        promo.IsActive &&
                                        promo.StartDate <= now &&
                                        promo.EndDate >= now)
                        .OrderByDescending(promo => promo.DiscountPercentage) // Ch? s?p x?p theo DiscountPercentage
                        .FirstOrDefault()
                })
                .AsQueryable(); // Gi? l?i là IQueryable ?? ti?p t?c l?c trên database

            // Tính toán EffectivePrice ch? d?a vào DiscountPercentage (vì Promotion model không có FixedDiscountAmount)
            var finalProductsQuery = productsQueryWithCalculatedPrice.Select(x => new
            {
                Product = x.Product,
                EffectivePrice = (x.Product.Price - (x.BestPromotion != null ? x.Product.Price * (x.BestPromotion.DiscountPercentage / 100M)  : 0m)) * (1 - rankDiscountPercentage / 100m)
            }).AsQueryable();

            // L?c theo t? khóa tìm ki?m
            if (!string.IsNullOrEmpty(searchTerm))
            {
                finalProductsQuery = finalProductsQuery.Where(x =>
                    x.Product.ProductName.ToLower().Contains(searchTerm.ToLower()) ||
                    (x.Product.Description != null && x.Product.Description.ToLower().Contains(searchTerm.ToLower())));
                ViewBag.SearchTerm = searchTerm; 
            }
            else
            {
                finalProductsQuery = finalProductsQuery.OrderByDescending(x => x.Product.CreatedAt);
            }

            
            if (categoryId.HasValue)
            {
                finalProductsQuery = finalProductsQuery.Where(x => x.Product.SubCategory.CategoryID == categoryId.Value);
                ViewBag.SelectedCategoryId = categoryId.Value; 
            }

            if (subcategoryId.HasValue)
            {
                finalProductsQuery = finalProductsQuery.Where(x => x.Product.SubCategoryID == subcategoryId.Value);
                ViewBag.SelectedSubcategoryId = subcategoryId.Value;
            }

            if (minPrice.HasValue)
            {
                finalProductsQuery = finalProductsQuery.Where(x => x.EffectivePrice >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                finalProductsQuery = finalProductsQuery.Where(x => x.EffectivePrice <= maxPrice.Value);
            }

            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;

            // Áp d?ng s?p x?p
            switch (sortOrder)
            {
                case "price_asc":
                    finalProductsQuery = finalProductsQuery.OrderBy(x => x.EffectivePrice);
                    break;
                case "price_desc":
                    finalProductsQuery = finalProductsQuery.OrderByDescending(x => x.EffectivePrice);
                    break;
                case "newest":
                    finalProductsQuery = finalProductsQuery.OrderByDescending(x => x.Product.CreatedAt);
                    break;
                default:
                    finalProductsQuery = finalProductsQuery.OrderByDescending(x => x.Product.CreatedAt);
                    break;
            }

            ViewBag.CurrentSort = sortOrder;

            int currentPage = page ?? 1;
            int itemsPerPage = pageSize ?? 10;

            int totalItems = await finalProductsQuery.CountAsync(); 
            ViewBag.TotalItems = totalItems; 

            int totalPages = (int)Math.Ceiling((double)totalItems / itemsPerPage);
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = currentPage;
            ViewBag.PageSize = itemsPerPage;

            // Áp d?ng Skip và Take ?? l?y s?n ph?m cho trang hi?n t?i
            // Cu?i cùng, ch?n l?i ch? Product ?? truy?n v? View
            var products = await finalProductsQuery
                                .Skip((currentPage - 1) * itemsPerPage)
                                .Take(itemsPerPage)
                                .Select(x => x.Product) // Ch? l?y ??i t??ng Product ?? truy?n v? View
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
