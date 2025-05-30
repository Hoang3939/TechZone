using ShopDienTu.Data;
using ShopDienTu.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace ElectronicsShop.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string CartSessionKey = "Cart";

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Cart
        public IActionResult Index()
        {
            var cart = GetCartFromSession();
            return View(cart);
        }

        // POST: Cart/AddToCart
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductID == productId);

            if (product == null)
            {
                return NotFound();
            }

            if (quantity <= 0 || quantity > product.StockQuantity)
            {
                TempData["ErrorMessage"] = $"Số lượng yêu cầu không hợp lệ. Chỉ còn {product.StockQuantity} sản phẩm trong kho.";
                return RedirectToAction(nameof(Index));
            }

            var now = DateTime.Now;

            decimal productSpecificDiscountPercentage = 0m;
            var promo = await _context.Promotions
                .Where(p => p.ProductID == productId && p.IsActive && p.StartDate <= now && p.EndDate >= now)
                .FirstOrDefaultAsync();

            if (promo != null)
            {
                productSpecificDiscountPercentage = promo.DiscountPercentage;
            }

            decimal rankDiscountPercentage = 0m;
            if (User.Identity.IsAuthenticated)
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var user = await _context.Users
                    .Include(u => u.Rank) // Đảm bảo load Rank
                    .FirstOrDefaultAsync(u => u.UserID == userId);

                if (user != null && user.Rank != null)
                {
                    rankDiscountPercentage = user.Rank.DiscountPercentage;
                }
            }

            decimal effectiveDiscountPercentage = Math.Max(productSpecificDiscountPercentage, rankDiscountPercentage);

            decimal finalUnitPrice = product.Price * (1 - effectiveDiscountPercentage / 100m);

            product.Price = finalUnitPrice;

            var cart = GetCartFromSession();
            cart.AddItem(product, quantity);
            SaveCartToSession(cart);

            TempData["SuccessMessage"] = $"Đã thêm {quantity} sản phẩm '{product.ProductName}' vào giỏ hàng.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Cart/RemoveFromCart
        [HttpPost]
        public IActionResult RemoveFromCart(int productId)
        {
            var cart = GetCartFromSession();
            cart.RemoveItem(productId);
            SaveCartToSession(cart);

            TempData["SuccessMessage"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Cart/UpdateQuantity
        [HttpPost]
        public IActionResult UpdateQuantity(int productId, int quantity)
        {
            if (quantity <= 0)
            {
                return RedirectToAction(nameof(RemoveFromCart), new { productId });
            }

            var cart = GetCartFromSession();
            cart.UpdateQuantity(productId, quantity);
            SaveCartToSession(cart);

            return RedirectToAction(nameof(Index));
        }

        // POST: Cart/ClearCart
        [HttpPost]
        public IActionResult ClearCart()
        {
            var cart = GetCartFromSession();
            cart.Clear();
            SaveCartToSession(cart);

            return RedirectToAction(nameof(Index));
        }

        // GET: Cart/Checkout
        public async Task<IActionResult> Checkout()
        {
            var cart = GetCartFromSession();

            if (cart.Items.Count == 0)
            {
                return RedirectToAction(nameof(Index));
            }

            // Get payment methods for checkout
            var paymentMethods = await _context.PaymentMethods
                .Where(p => p.IsActive)
                .ToListAsync();

            ViewBag.PaymentMethods = paymentMethods;

            return View(cart);
        }

        private ShoppingCart GetCartFromSession()
        {
            var cartJson = HttpContext.Session.GetString(CartSessionKey);
            if (string.IsNullOrEmpty(cartJson))
            {
                return new ShoppingCart();
            }
            return JsonConvert.DeserializeObject<ShoppingCart>(cartJson);
        }

        private void SaveCartToSession(ShoppingCart cart)
        {
            var cartJson = JsonConvert.SerializeObject(cart);
            HttpContext.Session.SetString(CartSessionKey, cartJson);
        }
    }
}
