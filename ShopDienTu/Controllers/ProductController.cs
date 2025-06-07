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
                .AsSplitQuery()
                .FirstOrDefaultAsync(m => m.ProductID == id);

            if (product == null)
            {
                return NotFound();
            }

            string viewedProductsCookie = Request.Cookies["ViewedProducts"] ?? "";
            List<int> viewedProductIds = viewedProductsCookie.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .ToList();

            viewedProductIds.Remove(id.Value);

            viewedProductIds.Insert(0, id.Value);

            if (viewedProductIds.Count > 15)
            {
                viewedProductIds = viewedProductIds.Take(15).ToList();
            }

            var cookieOptions = new CookieOptions
            {
                Expires = DateTime.Now.AddDays(30),
                HttpOnly = true,
                IsEssential = true
            };

            Response.Cookies.Append("ViewedProducts", string.Join(",", viewedProductIds), cookieOptions);


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

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userIdw))
            {
                var isFavorite = await _context.WishLists.AnyAsync(w => w.UserID == userIdw && w.ProductID == product.ProductID);
                ViewBag.IsFavorite = isFavorite;
            }


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
    }
}
