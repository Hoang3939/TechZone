using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using ShopDienTu.Data;
using ShopDienTu;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using ShopDienTu.Models;

namespace ShopDienTu.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderController> _logger;
        private const string CartSessionKey = "Cart";
        private const string GuestInfoSessionKey = "GuestOrderInfo";

        public OrderController(ApplicationDbContext context, ILogger<OrderController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Order/TrackOrder
        public IActionResult TrackOrder()
        {
            return View();
        }

        // POST: Order/TrackOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TrackOrder(OrderTracking model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Tìm đơn hàng dựa trên OrderNumber
                    var order = await _context.Orders
                        .Include(o => o.User)
                        .FirstOrDefaultAsync(o => o.OrderNumber == model.OrderNumber);

                    if (order != null)
                    {
                        // Kiểm tra email
                        if (order.User != null && order.User.Email == model.Email)
                        {
                            return RedirectToAction("OrderDetails", new { id = order.OrderID });
                        }
                        else
                        {
                            // Kiểm tra thông tin khách vãng lai từ session
                            var guestInfoJson = HttpContext.Session.GetString($"{GuestInfoSessionKey}_{order.OrderID}");
                            if (!string.IsNullOrEmpty(guestInfoJson))
                            {
                                var guestInfo = JsonConvert.DeserializeObject<GuestOrderInfo>(guestInfoJson);
                                if (guestInfo.Email == model.Email)
                                {
                                    return RedirectToAction("OrderDetails", new { id = order.OrderID });
                                }
                            }
                        }
                    }

                    ModelState.AddModelError("", "Không tìm thấy đơn hàng với thông tin đã nhập. Vui lòng kiểm tra lại.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi tra cứu đơn hàng: {Message}", ex.Message);
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi tra cứu đơn hàng. Vui lòng thử lại sau.");
                }
            }

            return View(model);
        }

        // GET: Order/OrderDetails/5
        public async Task<IActionResult> OrderDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var order = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.PaymentMethod)
                    .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                    .ThenInclude(p => p.ProductImages)
                    .Include(o => o.OrderStatuses)
                    .FirstOrDefaultAsync(m => m.OrderID == id);

                if (order == null)
                {
                    return NotFound();
                }

                // Kiểm tra quyền truy cập
                if (User.Identity.IsAuthenticated)
                {
                    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                    if (order.UserID != userId && !User.IsInRole("Admin"))
                    {
                        return Forbid();
                    }
                }
                else
                {
                    // Nếu không đăng nhập, chỉ cho phép xem đơn hàng vừa đặt hoặc đã tra cứu
                    if (!TempData.ContainsKey("TrackOrderId") || (int)TempData["TrackOrderId"] != id)
                    {
                        // Kiểm tra thông tin khách vãng lai từ session
                        var guestInfoJson = HttpContext.Session.GetString($"{GuestInfoSessionKey}_{id}");
                        if (string.IsNullOrEmpty(guestInfoJson))
                        {
                            return RedirectToAction("TrackOrder");
                        }
                    }
                }

                // Nếu đơn hàng không có UserID, lấy thông tin khách vãng lai từ session
                if (order.UserID == null)
                {
                    var guestInfoJson = HttpContext.Session.GetString($"{GuestInfoSessionKey}_{order.OrderID}");
                    if (!string.IsNullOrEmpty(guestInfoJson))
                    {
                        var guestInfo = JsonConvert.DeserializeObject<GuestOrderInfo>(guestInfoJson);
                        ViewBag.GuestInfo = guestInfo;
                    }
                }

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xem chi tiết đơn hàng {OrderId}: {Message}", id, ex.Message);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải thông tin đơn hàng. Vui lòng thử lại sau.";
                return RedirectToAction("TrackOrder");
            }
        }

        // GET: Order/History
        public async Task<IActionResult> History()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                var orders = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.PaymentMethod)
                    .Where(o => o.UserID == userId)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();

                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải lịch sử đơn hàng: {Message}", ex.Message);
                ViewBag.ErrorMessage = "Đã xảy ra lỗi khi tải lịch sử đơn hàng. Vui lòng thử lại sau.";
                return View(new List<Order>());
            }
        }

        // POST: Order/PlaceOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(string fullName, string email, string phone, string address,
            string province, string district, string ward, int paymentMethodId, string? notes, string? promoCode)
        {
            var cart = GetCartFromSession();
            if (cart.Items.Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }

            var now = DateTime.Now;

            var activePromos = await _context.Promotions
                .Where(p => p.IsActive && p.ProductID != null && p.StartDate <= now && p.EndDate >= now)
                .ToListAsync();

            decimal subtotal = 0m;
            decimal rankDiscountPercentage = 0m;

            if (User.Identity.IsAuthenticated)
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                rankDiscountPercentage = await _context.Users
                    .Where(u => u.UserID == userId)
                    .Select(u => u.Rank.DiscountPercentage)
                    .FirstOrDefaultAsync();
            }

            foreach (var item in cart.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductID);

                if (item.Quantity > product.StockQuantity)
                {
                    ModelState.AddModelError("", $"Sản phẩm '{product.ProductName}' chỉ còn lại {product.StockQuantity} sản phẩm trong kho.");
                    // Need to return to checkout with error
                    ViewBag.PaymentMethods = await _context.PaymentMethods.ToListAsync(); // Ensure these are reloaded
                    return View("~/Views/Cart/Checkout.cshtml", cart);
                }
                var promo = activePromos.FirstOrDefault(p => p.ProductID == item.ProductID);
                var basePrice = product.Price;

                decimal priceAfterProductPromo = promo != null
                    ? basePrice * (1 - promo.DiscountPercentage / 100m)
                    : basePrice;

                decimal finalUnitPrice = priceAfterProductPromo * (1 - rankDiscountPercentage / 100m);

                subtotal += finalUnitPrice * item.Quantity;

                item.Price = finalUnitPrice;
            }

            decimal globalVoucherDiscount = 0m;
            ViewBag.PromoCode = promoCode;

            if(!string.IsNullOrWhiteSpace(promoCode))
            {
                var codeNormalized = promoCode.Trim().ToUpper();
                var globalPromo = await _context.Promotions
                    .Where(p => p.IsActive
                                && p.ProductID == null
                                && p.StartDate <= now
                                && p.EndDate >= now)
                    .FirstOrDefaultAsync(p => p.PromoCode.ToUpper() == codeNormalized);

                if (globalPromo == null)
                {
                    ModelState.AddModelError("promoCode", "Mã voucher không hợp lệ hoặc đã hết hạn.");
                }
                else if (subtotal < 20_000_000m)
                {
                    ModelState.AddModelError("promoCode", "Đơn hàng phải từ 20.000.000 VNĐ trở lên để sử dụng voucher.");
                }
                else
                {
                    globalVoucherDiscount = 1_000_000m;
                }
            }

            if (!ModelState.IsValid)
            {
                // Phải load lại danh sách phương thức thanh toán cho view
                ViewBag.PaymentMethods = await _context.PaymentMethods.ToListAsync();
                return View("~/Views/Cart/Checkout.cshtml", cart);
            }

            // Tạo đơn hàng mới
            var order = new Order
            {
                OrderNumber = GenerateOrderNumber(),
                ShippingAddress = $"{address}, {ward}, {district}, {province}",
                PaymentMethodID = paymentMethodId,
                Notes = notes,
                CreatedAt = now,
                OrderStatus = "Chờ xác nhận",
                UserID = User.Identity.IsAuthenticated
                            ? int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier))
                            : (int?)null,
                TotalAmount = subtotal - globalVoucherDiscount,
                Discount = globalVoucherDiscount > 0 ? globalVoucherDiscount : (decimal?)null
            };

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                if (order.UserID == null)
                {
                    var guest = new GuestOrderInfo { FullName = fullName, Email = email, Phone = phone };
                    HttpContext.Session.SetString($"{GuestInfoSessionKey}_{order.OrderID}", JsonConvert.SerializeObject(guest));
                }

                foreach (var item in cart.Items)
                {
                    var product = await _context.Products.FindAsync(item.ProductID);

                    product.StockQuantity -= item.Quantity;
                    _context.Products.Update(product);

                    _context.OrderDetails.Add(new OrderDetail
                    {
                        OrderID = order.OrderID,
                        ProductID = product.ProductID,
                        Quantity = item.Quantity,
                        UnitPrice = item.Price
                    });
                }

                _context.OrderStatuses.Add(new OrderStatus
                {
                    OrderID = order.OrderID,
                    Status = "Đang xử lý",
                    Description = "Đơn hàng đã được tạo, đang chờ xác nhân!",
                    CreatedAt = now
                });

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                if (order.UserID != null)
                {
                    var user = await _context.Users
                        .Include(u => u.Rank)
                        .FirstOrDefaultAsync(u => u.UserID == order.UserID);

                    if (user != null)
                    {
                        int earnedPoints = (int)(order.TotalAmount / 1000m);
                        user.Points = (user.Points ?? 0) + earnedPoints;

                        var newRank = await _context.Ranks
                            .Where(r => r.MinimumPoints <=  user.Points)
                            .OrderByDescending(r => r.MinimumPoints)
                            .FirstOrDefaultAsync();

                        if (newRank != null && user.RankID != newRank.RankID)
                        {
                            user.RankID = newRank.RankID;
                            _logger.LogInformation("Xếp hạng của người dùng {UserId} được cập nhật từ {OldRankId} thành {NewRankId} với {Points} điểm.", user.UserID, user.RankID, newRank.RankID, user.Points);
                        }
                        await _context.SaveChangesAsync(); // Lưu thay đổi rank và điểm
                    }
                }
            }
            catch (DbUpdateException dbEx)
            {
                await tx.RollbackAsync();
                _logger.LogError(dbEx, "Lỗi Database khi đặt hàng.");
                TempData["ErrorMessage"] = "Lỗi khi lưu đơn hàng. Vui lòng thử lại!";
                return RedirectToAction("Checkout", "Cart");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi đặt hàng!");
                ModelState.AddModelError("", ex.Message);
                return RedirectToAction("Checkout", "Cart");
            }

            cart.Clear();
            SaveCartToSession(cart);
            TempData["TrackOrderId"] = order.OrderID;
            return RedirectToAction("OrderConfirmation", new { id = order.OrderID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> ValidateVoucher([FromForm] string promoCode, [FromForm] decimal subtotal)
        {
            var now = DateTime.Now;

            // Lấy khuyến mãi toàn hệ thống (ProductID == null) đang active
            var promo = await _context.Promotions
                .Where(p => p.IsActive
                            && p.ProductID == null
                            && p.StartDate <= now
                            && p.EndDate >= now)
                .FirstOrDefaultAsync(p => p.PromoCode.ToUpper() == promoCode.Trim().ToUpper());

            if (promo == null)
            {
                return Json(new { success = false, error = "Mã voucher không hợp lệ hoặc đã hết hạn." });
            }
            if (subtotal < 20_000_000m)
            {
                return Json(new { success = false, error = "Đơn hàng phải từ 20.000.000 VNĐ trở lên để sử dụng voucher." });
            }

            // ở đây cứng là 1.000.000 nhưng bạn có thể dùng promo.DiscountAmount nếu lưu trong DB
            return Json(new { success = true, code = promo.PromoCode, discount = 1000000 });
        }

        // GET: Order/OrderConfirmation/5
        [HttpGet]
        public async Task<IActionResult> OrderConfirmation(int? id)
        {
            //if (id == null)
            //{
            //    return NotFound();
            //}

            //try
            //{
            //    var order = await _context.Orders
            //        .Include(o => o.User)
            //        .Include(o => o.PaymentMethod)
            //        .FirstOrDefaultAsync(m => m.OrderID == id);

            //    if (order == null)
            //    {
            //        return NotFound();
            //    }

            //    // Nếu đơn hàng không có UserID, lấy thông tin khách vãng lai từ session
            //    if (order.UserID == null)
            //    {
            //        var guestInfoJson = HttpContext.Session.GetString($"{GuestInfoSessionKey}_{order.OrderID}");
            //        if (!string.IsNullOrEmpty(guestInfoJson))
            //        {
            //            var guestInfo = JsonConvert.DeserializeObject<GuestOrderInfo>(guestInfoJson);
            //            ViewBag.GuestInfo = guestInfo;
            //        }
            //    }

            //    // Lưu ID đơn hàng vào TempData để cho phép xem chi tiết đơn hàng mà không cần đăng nhập
            //    TempData["TrackOrderId"] = order.OrderID;

            //    return View(order);
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "Lỗi khi xem xác nhận đơn hàng {OrderId}: {Message}", id, ex.Message);
            //    TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải thông tin xác nhận đơn hàng. Vui lòng thử lại sau.";
            //    return RedirectToAction("Index", "Home");
            //}
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.PaymentMethod)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                        .ThenInclude(p => p.ProductImages)
                .Include(o => o.OrderStatuses)
                .FirstOrDefaultAsync(o => o.OrderID == id);

            if (order == null) return NotFound();
            return View(order);
                
        }

        // GET: Order/CancelOrder/5
        public async Task<IActionResult> CancelOrder(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var order = await _context.Orders
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(m => m.OrderID == id);

                if (order == null)
                {
                    return NotFound();
                }

                // Kiểm tra quyền truy cập
                if (User.Identity.IsAuthenticated)
                {
                    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                    if (order.UserID != userId && !User.IsInRole("Admin"))
                    {
                        return Forbid();
                    }
                }
                else
                {
                    // Nếu không đăng nhập, chỉ cho phép hủy đơn hàng vừa đặt
                    if (!TempData.ContainsKey("TrackOrderId") || (int)TempData["TrackOrderId"] != id)
                    {
                        return RedirectToAction("TrackOrder");
                    }
                }

                // Chỉ cho phép hủy đơn hàng ở trạng thái "Chờ xác nhận"
                if (order.OrderStatus != "Chờ xác nhận")
                {
                    TempData["ErrorMessage"] = "Không thể hủy đơn hàng ở trạng thái hiện tại.";
                    return RedirectToAction("OrderDetails", new { id = order.OrderID });
                }

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi truy cập trang hủy đơn hàng {OrderId}: {Message}", id, ex.Message);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi truy cập trang hủy đơn hàng. Vui lòng thử lại sau.";
                return RedirectToAction("History");
            }
        }

        // POST: Order/CancelOrder/5
        [HttpPost, ActionName("CancelOrder")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrderConfirmed(int id, string cancelReason)
        {
            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null)
                {
                    return NotFound();
                }

                // Kiểm tra quyền truy cập
                if (User.Identity.IsAuthenticated)
                {
                    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                    if (order.UserID != userId && !User.IsInRole("Admin"))
                    {
                        return Forbid();
                    }
                }
                else
                {
                    // Nếu không đăng nhập, chỉ cho phép hủy đơn hàng vừa đặt
                    if (!TempData.ContainsKey("TrackOrderId") || (int)TempData["TrackOrderId"] != id)
                    {
                        return RedirectToAction("TrackOrder");
                    }
                }

                // Chỉ cho phép hủy đơn hàng ở trạng thái "Chờ xác nhận"
                if (order.OrderStatus != "Chờ xác nhận")
                {
                    TempData["ErrorMessage"] = "Không thể hủy đơn hàng ở trạng thái hiện tại.";
                    return RedirectToAction("OrderDetails", new { id = order.OrderID });
                }

                // Cập nhật trạng thái đơn hàng
                order.OrderStatus = "Đã hủy";
                order.UpdatedAt = DateTime.Now;

                // Thêm trạng thái đơn hàng mới
                var orderStatus = new OrderStatus
                {
                    OrderID = order.OrderID,
                    Status = "Đã hủy",
                    Description = string.IsNullOrEmpty(cancelReason) ? "Đơn hàng đã bị hủy." : $"Đơn hàng đã bị hủy. Lý do: {cancelReason}",
                    CreatedAt = DateTime.Now
                };
                _context.OrderStatuses.Add(orderStatus);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đơn hàng đã được hủy thành công.";
                return RedirectToAction("OrderDetails", new { id = order.OrderID });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi hủy đơn hàng {OrderId}: {Message}", id, ex.Message);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi hủy đơn hàng. Vui lòng thử lại sau.";
                return RedirectToAction("OrderDetails", new { id = id });
            }
        }

        // Phương thức tạo mã đơn hàng
        private string GenerateOrderNumber()
        {
            // Tạo mã đơn hàng theo định dạng: ORD + năm + tháng + ngày + 6 số ngẫu nhiên
            var random = new Random();
            var now = DateTime.Now;
            return $"ORD{now:yyMMdd}{random.Next(100000, 999999)}";
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

    // Lớp để lưu thông tin khách vãng lai
    public class GuestOrderInfo
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
    }
}
