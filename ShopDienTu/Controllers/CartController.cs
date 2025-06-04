using ShopDienTu.Data;
using ShopDienTu.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using ShopDienTu.Services;

namespace ElectronicsShop.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IShoppingCartService _shoppingCartService;

        public CartController(ApplicationDbContext context, IShoppingCartService shoppingCartService)
        {
            _context = context;
            _shoppingCartService = shoppingCartService;
        }

        // GET: Cart
        public async Task<IActionResult> Index()
        {
            var cart = await _shoppingCartService.GetCartAsync(User, HttpContext.Session);
            return View(cart);
        }

        // POST: Cart/AddToCart
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            try
            {
                // Ủy quyền việc thêm sản phẩm cho Service
                await _shoppingCartService.AddItemAsync(User, HttpContext.Session, productId, quantity);

                // Lấy thông tin sản phẩm để hiển thị TempData (không cần logic giá/giỏ hàng ở đây)
                var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductID == productId);
                TempData["SuccessMessage"] = $"Đã thêm {quantity} sản phẩm '{product?.ProductName}' vào giỏ hàng.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message; // Service sẽ ném Exception với thông báo lỗi
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Cart/RemoveFromCart
        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            // Ủy quyền việc xóa sản phẩm cho Service
            await _shoppingCartService.RemoveItemAsync(User, HttpContext.Session, productId);
            TempData["SuccessMessage"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Cart/UpdateQuantity
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
        {
            try
            {
                // Ủy quyền việc cập nhật số lượng cho Service
                await _shoppingCartService.UpdateQuantityAsync(User, HttpContext.Session, productId, quantity);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message; // Service sẽ ném Exception với thông báo lỗi
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Cart/ClearCart
        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            // Ủy quyền việc xóa toàn bộ giỏ hàng cho Service
            await _shoppingCartService.ClearCartAsync(User, HttpContext.Session);
            TempData["SuccessMessage"] = "Giỏ hàng đã được làm trống.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Cart/Checkout
        public async Task<IActionResult> Checkout()
        {
            var cart = await _shoppingCartService.GetCartAsync(User, HttpContext.Session);

            if (cart.Items.Count == 0)
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction(nameof(Index));
            }

            // Get payment methods for checkout
            var paymentMethods = await _context.PaymentMethods
                .Where(p => p.IsActive)
                .ToListAsync();

            ViewBag.PaymentMethods = paymentMethods;

            List<UserAddress> userAddresses = new List<UserAddress>();
            if (User.Identity.IsAuthenticated)
            {
                // Lấy ID người dùng hiện tại
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdString, out int userId)) // Sử dụng TryParse để an toàn hơn
                {
                    // Lấy danh sách địa chỉ của người dùng
                    userAddresses = await _context.UserAddresses
                                                    .Where(ua => ua.UserID == userId)
                                                    .OrderByDescending(ua => ua.IsDefault) // Ưu tiên địa chỉ mặc định
                                                    .ToListAsync();
                }
            }
            ViewBag.UserAddresses = userAddresses;

            return View(cart);
        }
    }
}
