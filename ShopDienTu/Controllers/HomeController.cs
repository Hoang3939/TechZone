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

        public async Task<IActionResult> Index(string searchTerm, int? categoryId = null, int? subcategoryId = null, string sortOrder = null, int? page = 1, int? pageSize = 10, decimal? minPrice = null, decimal? maxPrice = null, int? suggestedPage = 1)
        {
            var now = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string currentSearchHistory = Request.Cookies["SearchHistory"] ?? "";
                List<string> searchTerms = currentSearchHistory.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList();

                searchTerms.RemoveAll(s => s.Equals(searchTerm.Trim(), StringComparison.OrdinalIgnoreCase));
                searchTerms.Insert(0, searchTerm.Trim());

                if (searchTerms.Count > 5) searchTerms = searchTerms.Take(5).ToList();

                var cookieOptions = new CookieOptions { Expires = DateTime.Now.AddDays(30), HttpOnly = true, IsEssential = true };
                Response.Cookies.Append("SearchHistory", string.Join("|", searchTerms), cookieOptions);
            }
            // L?y t?t c? c�c khuy?n m�i ?ang ho?t ??ng (nh? ban ??u, ?? hi?n th? chung n?u c?n)
            var activePromotions = await _context.Promotions
                .Where(p => p.IsActive && p.ProductID != null && p.StartDate <= now && p.EndDate >= now)
                .ToListAsync();
            ViewBag.ActivePromotions = activePromotions; // Gi? l?i ViewBag n�y

            // L?y chi?t kh?u rank c?a ng??i d�ng hi?n t?i
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

            // L?y t?t c? danh m?c v� danh m?c con cho sidebar
            var categories = await _context.Categories
                .Include(c => c.SubCategories)
                .ToListAsync();
            ViewBag.Categories = categories; // Gi? l?i ViewBag n�y

            // B?t ??u truy v?n s?n ph?m
            // S? d?ng m?t ki?u ?n danh ?? ch?a Product v� EffectivePrice ?� t�nh to�n
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
                .AsQueryable(); // Gi? l?i l� IQueryable ?? ti?p t?c l?c tr�n database

            // T�nh to�n EffectivePrice ch? d?a v�o DiscountPercentage (v� Promotion model kh�ng c� FixedDiscountAmount)
            var finalProductsQuery = productsQueryWithCalculatedPrice.Select(x => new
            {
                Product = x.Product,
                EffectivePrice = (x.Product.Price - (x.BestPromotion != null ? x.Product.Price * (x.BestPromotion.DiscountPercentage / 100M)  : 0m)) * (1 - rankDiscountPercentage / 100m)
            }).AsQueryable();

            // L?c theo t? kh�a t�m ki?m
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

            // �p d?ng s?p x?p
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

            // �p d?ng Skip v� Take ?? l?y s?n ph?m cho trang hi?n t?i
            // Cu?i c�ng, ch?n l?i ch? Product ?? truy?n v? View
            var products = await finalProductsQuery
                                .Skip((currentPage - 1) * itemsPerPage)
                                .Take(itemsPerPage)
                                .Select(x => x.Product) // Ch? l?y ??i t??ng Product ?? truy?n v? View
                                .ToListAsync();

            // Logic l?c s?n ph?m g?i �
            // ================= LOGIC G?I � S?N PH?M H?P NH?T (?� CH?NH S?A) =================
            const int maxTotalSuggested = 10;
            const int suggestedItemsPerPage = 5;
            int currentSuggestedPage = suggestedPage ?? 1;

            // L?y d? li?u t? cookie
            string searchHistoryCookie = Request.Cookies["SearchHistory"] ?? "";
            string viewedProductsCookie = Request.Cookies["ViewedProducts"] ?? "";
            var recentSearchTerms = searchHistoryCookie.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList();
            var viewedProductIds = viewedProductsCookie.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                       .Select(idStr => int.TryParse(idStr, out int id) ? id : (int?)null)
                                                       .Where(id => id.HasValue).Select(id => id.Value).ToList();

            // L?y danh m?c con t? c�c s?n ph?m ?� xem
            var subCategoryIdsFromHistory = new List<int?>();
            if (viewedProductIds.Any())
            {
                subCategoryIdsFromHistory = await _context.Products
                    .Where(p => viewedProductIds.Contains(p.ProductID))
                    .Select(p => p.SubCategoryID)
                    .Distinct()
                    .ToListAsync();
            }

            // X�y d?ng truy v?n h?p nh?t
            IQueryable<Product> combinedSuggestedQuery;
            if (recentSearchTerms.Any() || subCategoryIdsFromHistory.Any())
            {
                combinedSuggestedQuery = _context.Products
                    .Where(p => p.IsActive  && (recentSearchTerms.Any(term => p.ProductName.Contains(term)) || subCategoryIdsFromHistory.Contains(p.SubCategoryID)));
            }
            else
            {
                // Fallback: G?i � s?n ph?m m?i nh?t n?u kh�ng c� l?ch s?
                combinedSuggestedQuery = _context.Products
                    .Where(p => p.IsActive);
            }

            // L?y t?i ?a 10 s?n ph?m t? DB
            var allSuggestedProducts = await combinedSuggestedQuery
                .Include(p => p.ProductImages)
                .OrderByDescending(p => p.ProductID) // S?p x?p ?? c� k?t qu? nh?t qu�n
                .Take(maxTotalSuggested)
                .ToListAsync();

            // D�ng DistinctBy ?? lo?i b? s?n ph?m tr�ng l?p (n?u c�)
            allSuggestedProducts = allSuggestedProducts.DistinctBy(p => p.ProductID).ToList();

            // T�nh to�n ph�n trang
            int totalSuggestedPages = (int)Math.Ceiling((double)allSuggestedProducts.Count / suggestedItemsPerPage);

            // L?y 5 s?n ph?m cho trang g?i � hi?n t?i
            var pagedSuggestedProducts = allSuggestedProducts
                .Skip((currentSuggestedPage - 1) * suggestedItemsPerPage)
                .Take(suggestedItemsPerPage)
                .ToList();

            // G?i d? li?u g?i � ??n View
            ViewBag.SuggestedProducts = pagedSuggestedProducts;
            ViewBag.CurrentSuggestedPage = currentSuggestedPage;
            ViewBag.TotalSuggestedPages = totalSuggestedPages;

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
