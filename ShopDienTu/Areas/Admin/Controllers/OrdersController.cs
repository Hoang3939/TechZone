using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.X509;
using ShopDienTu.Data;
using ShopDienTu.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ShopDienTu.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")] // Chỉ Admin được truy cập
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AdminOrders
        public async Task<IActionResult> Index(string searchString, string statusFilter)
        {
            var orders = _context.Orders
                .Include(o => o.User)
                .Include(o => o.PaymentMethod)
                .AsNoTracking();

            if (!string.IsNullOrEmpty(searchString))
            {
                orders = orders.Where(o => o.OrderNumber.Contains(searchString) ||
                                          (o.User != null && (o.User.FullName.Contains(searchString) || o.User.Email.Contains(searchString))));
                ViewBag.SearchString = searchString;
            }

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
            {
                orders = orders.Where(o => o.OrderStatus == statusFilter);
                ViewBag.StatusFilter = statusFilter;
            }

            ViewBag.StatusList = new[] { "All", "Chờ xác nhận", "Đang xử lý", "Đang giao", "Hoàn thành", "Hủy" };
            return View(await orders.ToListAsync());
        }

        // GET: AdminOrders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.PaymentMethod)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .Include(o => o.OrderStatuses)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.OrderID == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Admin/Orders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.PaymentMethod)
                .FirstOrDefaultAsync(m => m.OrderID == id);

            if (order == null)
            {
                return NotFound();
            }

            ViewBag.PaymentMethods = await _context.PaymentMethods
                .Where(pm => pm.IsActive)
                .Select(pm => new { pm.PaymentMethodID, pm.MethodName })
                .ToListAsync();
            ViewBag.StatusList = new[] { "Chờ xác nhận", "Đang xử lý", "Đang giao", "Hoàn thành", "Hủy" };
            return View(order);
        }

        // POST: Admin/Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("OrderID,OrderStatus,PaymentMethodID,ShippingAddress,Notes")] Order order)
        {
            if (id != order.OrderID)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["ErrorMessage"] = "Lỗi xác thực: " + string.Join("; ", errors);
            }
            else
            {
                try
                {
                    var existingOrder = await _context.Orders.FindAsync(id);
                    if (existingOrder == null)
                    {
                        return NotFound();
                    }

                    // Xử lý PaymentMethodID: Nếu null, giữ nguyên giá trị cũ
                    if (!order.PaymentMethodID.HasValue)
                    {
                        order.PaymentMethodID = existingOrder.PaymentMethodID;
                    }
                    else if (!await _context.PaymentMethods.AnyAsync(pm => pm.PaymentMethodID == order.PaymentMethodID))
                    {
                        ModelState.AddModelError("PaymentMethodID", "Phương thức thanh toán không hợp lệ.");
                    }

                    if (ModelState.IsValid)
                    {
                        existingOrder.OrderStatus = order.OrderStatus;
                        existingOrder.ShippingAddress = order.ShippingAddress;
                        existingOrder.Notes = order.Notes;
                        existingOrder.PaymentMethodID = order.PaymentMethodID;
                        existingOrder.UpdatedAt = DateTime.Now;

                        var originalOrder = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.OrderID == id);
                        if (originalOrder?.OrderStatus != order.OrderStatus)
                        {
                            var orderStatus = new OrderStatus
                            {
                                OrderID = order.OrderID,
                                Status = order.OrderStatus,
                                Description = $"Cập nhật trạng thái bởi admin lúc {DateTime.Now:dd/MM/yyyy HH:mm}",
                                CreatedAt = DateTime.Now
                            };
                            _context.OrderStatuses.Add(orderStatus);
                        }

                        _context.Update(existingOrder);
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Đơn hàng đã được cập nhật thành công.";
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.OrderID))
                    {
                        return NotFound();
                    }
                    TempData["ErrorMessage"] = "Lỗi đồng bộ dữ liệu. Vui lòng thử lại.";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Lỗi khi cập nhật đơn hàng: {ex.Message}";
                }
            }

            ViewBag.PaymentMethods = await _context.PaymentMethods
                .Where(pm => pm.IsActive)
                .Select(pm => new { pm.PaymentMethodID, pm.MethodName })
                .ToListAsync();
            ViewBag.StatusList = new[] { "Chờ xác nhận", "Đang xử lý", "Đang giao", "Hoàn thành", "Hủy" };
            return View(order);
        }


        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.OrderID == id);
        }


        // GET: AdminOrders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.PaymentMethod)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.OrderID == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: AdminOrders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .Include(o => o.OrderStatuses)
                    .FirstOrDefaultAsync(o => o.OrderID == id);

                if (order != null)
                {
                    _context.OrderDetails.RemoveRange(order.OrderDetails);
                    _context.OrderStatuses.RemoveRange(order.OrderStatuses);
                    _context.Orders.Remove(order);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Đơn hàng đã được xóa thành công.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng để xóa.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi xóa đơn hàng: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }


    }
}