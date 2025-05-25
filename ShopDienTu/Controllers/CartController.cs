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

            var now = DateTime.Now;
            var promo = await _context.Promotions
                .Where(p => p.ProductID == productId && p.IsActive && p.StartDate <= now && p.EndDate >= now)
                .FirstOrDefaultAsync();

            var unitPrice = promo != null ? product.Price * (1 - promo.DiscountPercentage / 100m) : product.Price;

            product.Price = unitPrice;

            var cart = GetCartFromSession();
            cart.AddItem(product, quantity);
            SaveCartToSession(cart);

            return RedirectToAction(nameof(Index));
        }

        // POST: Cart/RemoveFromCart
        [HttpPost]
        public IActionResult RemoveFromCart(int productId)
        {
            var cart = GetCartFromSession();
            cart.RemoveItem(productId);
            SaveCartToSession(cart);

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

            var paymentMethods = await _context.PaymentMethods
                .Where(p => p.IsActive)
                .ToListAsync();

            ViewBag.PaymentMethods = paymentMethods;

            if (User.Identity.IsAuthenticated)
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var user = await _context.Users
                    .Include(u => u.Rank)
                    .FirstOrDefaultAsync(u => u.UserID == userId);
                var addresses = await _context.UserAddresses
                    .Where(a => a.UserID == userId)
                    .OrderByDescending(a => a.AddedAt)
                    .ToListAsync();

                ViewBag.SavedAddresses = addresses;
                ViewBag.RankName = user?.Rank?.RankName ?? "Đồng";
                ViewBag.RankDiscount = user?.Rank?.DiscountPercentage ?? 0;
            }

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
