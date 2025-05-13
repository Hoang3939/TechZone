using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShopDienTu.Data; // Đảm bảo đúng namespace cho DbContext
using ShopDienTu.Models; // Đảm bảo đúng namespace cho Model

namespace ShopDienTu.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context; // Đảm bảo tên DbContext đúng

        public OrdersController(ApplicationDbContext context) // Đảm bảo tên DbContext đúng
        {
            _context = context;
        }

        // GET: Admin/Orders
        public async Task<IActionResult> Index()
        {
            // Thêm Include cho OrderDetails nếu cần hiển thị thông tin chi tiết
            var applicationDbContext = _context.Orders
                                                .Include(o => o.PaymentMethod)
                                                .Include(o => o.User);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Admin/Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.PaymentMethod)
                .Include(o => o.User)
                // Include OrderDetails để xem chi tiết sản phẩm trong đơn hàng
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product) // Lấy thông tin sản phẩm từ chi tiết đơn hàng
                .FirstOrDefaultAsync(m => m.OrderID == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order); // View Details cần được cập nhật để hiển thị OrderDetails
        }

        // GET: Admin/Orders/Create
        public IActionResult Create()
        {
            ViewData["PaymentMethodID"] = new SelectList(_context.PaymentMethods.Where(pm => pm.IsActive), "PaymentMethodID", "MethodName");
            ViewData["UserID"] = new SelectList(_context.Users, "UserID", "UserName"); // Hiển thị UserName thay vì Email có thể thân thiện hơn
            return View();
        }

        // POST: Admin/Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Thêm Notes, OrderNumber, ShippingAddress vào Bind
        public async Task<IActionResult> Create([Bind("UserID,TotalAmount,PaymentMethodID,OrderStatus,Notes,OrderNumber,ShippingAddress")] Order order)
        {
            // Xóa OrderID và các trường tự động (CreatedAt, UpdatedAt) khỏi Bind
            order.CreatedAt = DateTime.Now; // Set thời gian tạo
            // ModelState bây giờ sẽ hợp lệ hơn vì các trường không bind sẽ không bị kiểm tra
            ModelState.Remove("OrderID");
            ModelState.Remove("User"); // Remove navigation properties from validation
            ModelState.Remove("PaymentMethod");
            ModelState.Remove("OrderDetails"); // Nếu có thêm OrderDetails vào Model Order

            if (ModelState.IsValid)
            {
                _context.Add(order);
                await _context.SaveChangesAsync();
                // Có thể thêm thông báo thành công (TempData)
                return RedirectToAction(nameof(Index));
            }
            // Log lỗi ModelState nếu có
            foreach (var modelStateKey in ModelState.Keys)
            {
                var value = ModelState[modelStateKey];
                foreach (var error in value.Errors)
                {
                    // Ghi log lỗi này lại (ví dụ: Console.WriteLine hoặc dùng logging framework)
                    Console.WriteLine($"Error in {modelStateKey}: {error.ErrorMessage}");
                }
            }

            ViewData["PaymentMethodID"] = new SelectList(_context.PaymentMethods.Where(pm => pm.IsActive), "PaymentMethodID", "MethodName", order.PaymentMethodID);
            ViewData["UserID"] = new SelectList(_context.Users, "UserID", "UserName", order.UserID);
            return View(order);
        }


        // GET: Admin/Orders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            ViewData["PaymentMethodID"] = new SelectList(_context.PaymentMethods.Where(pm => pm.IsActive), "PaymentMethodID", "MethodName", order.PaymentMethodID);
            ViewData["UserID"] = new SelectList(_context.Users, "UserID", "UserName", order.UserID); // Dùng UserName
                                                                                                     // Tạo SelectList cho OrderStatus
            var orderStatusList = new List<SelectListItem>
            {
                new SelectListItem { Value = "Pending", Text = "Pending" },
                new SelectListItem { Value = "Confirmed", Text = "Confirmed" },
                new SelectListItem { Value = "Shipping", Text = "Shipping" }, // Thêm các trạng thái khác nếu cần
                new SelectListItem { Value = "Completed", Text = "Completed" },
                new SelectListItem { Value = "Cancelled", Text = "Cancelled" }
            };
            ViewData["OrderStatusList"] = new SelectList(orderStatusList, "Value", "Text", order.OrderStatus);
            return View(order);
        }

        // POST: Admin/Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        // **QUAN TRỌNG:** Cập nhật Bind để bao gồm các trường cần sửa và loại bỏ các trường không cần/không nên sửa
        public async Task<IActionResult> Edit(int id, [Bind("OrderID,UserID,TotalAmount,PaymentMethodID,OrderStatus,Notes,OrderNumber,ShippingAddress,CreatedAt")] Order order)
        // Giữ lại CreatedAt trong Bind để nó không bị mất khi update, nhưng không hiển thị để sửa
        {
            if (id != order.OrderID)
            {
                return NotFound();
            }

            // Chỉ định rõ các trường cần cập nhật để tránh lỗi concurrency hoặc cập nhật ngoài ý muốn
            var orderToUpdate = await _context.Orders.FindAsync(id);
            if (orderToUpdate == null)
            {
                return NotFound();
            }

            // Xóa các navigation property khỏi ModelState validation
            ModelState.Remove("User");
            ModelState.Remove("PaymentMethod");
            ModelState.Remove("OrderDetails");

            if (ModelState.IsValid)
            {
                try
                {
                    // Cập nhật các trường cần thiết từ model được bind
                    orderToUpdate.UserID = order.UserID;
                    orderToUpdate.TotalAmount = order.TotalAmount; // Cẩn thận khi cho sửa TotalAmount
                    orderToUpdate.PaymentMethodID = order.PaymentMethodID;
                    orderToUpdate.OrderStatus = order.OrderStatus;
                    orderToUpdate.Notes = order.Notes;
                    orderToUpdate.OrderNumber = order.OrderNumber;
                    orderToUpdate.ShippingAddress = order.ShippingAddress;
                    // **QUAN TRỌNG:** Cập nhật thời gian UpdatedAt
                    orderToUpdate.UpdatedAt = DateTime.Now;
                    // CreatedAt không thay đổi, giữ nguyên giá trị từ orderToUpdate (hoặc từ order bind nếu bạn giữ nó)
                    // _context.Entry(orderToUpdate).Property(o => o.CreatedAt).IsModified = false; // Cách khác để EF không update CreatedAt

                    _context.Update(orderToUpdate); // Chỉ update đối tượng đã lấy từ DB
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.OrderID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        // Thêm log lỗi hoặc thông báo lỗi cụ thể hơn
                        ModelState.AddModelError("", "Lỗi xảy ra khi cập nhật dữ liệu. Dữ liệu có thể đã bị thay đổi bởi người khác.");
                        // Reload dữ liệu để xử lý concurrency conflict nếu cần
                    }
                }
                // Có thể thêm thông báo thành công (TempData)
                return RedirectToAction(nameof(Index));
            }

            // Nếu ModelState không hợp lệ, log lỗi ra
            foreach (var modelStateKey in ModelState.Keys)
            {
                var value = ModelState[modelStateKey];
                foreach (var error in value.Errors)
                {
                    Console.WriteLine($"Validation Error in {modelStateKey}: {error.ErrorMessage}");
                }
            }

            // Repopulate ViewData nếu quay lại View
            ViewData["PaymentMethodID"] = new SelectList(_context.PaymentMethods.Where(pm => pm.IsActive), "PaymentMethodID", "MethodName", order.PaymentMethodID);
            ViewData["UserID"] = new SelectList(_context.Users, "UserID", "UserName", order.UserID);
            // Tạo lại SelectList cho OrderStatus
            var orderStatusList = new List<SelectListItem>
            {
                new SelectListItem { Value = "Pending", Text = "Pending" },
                new SelectListItem { Value = "Confirmed", Text = "Confirmed" },
                new SelectListItem { Value = "Shipping", Text = "Shipping" },
                new SelectListItem { Value = "Completed", Text = "Completed" },
                new SelectListItem { Value = "Cancelled", Text = "Cancelled" }
            };
            ViewData["OrderStatusList"] = new SelectList(orderStatusList, "Value", "Text", order.OrderStatus);

            return View(order); // Trả về model ban đầu (có thể chứa lỗi validation)
        }

        // GET: Admin/Orders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.PaymentMethod)
                .Include(o => o.User)
                .FirstOrDefaultAsync(m => m.OrderID == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Admin/Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Cần kiểm tra xem có OrderDetails liên quan không và xử lý logic (ví dụ: không cho xóa nếu có chi tiết)
            // Hoặc đảm bảo Foreign Key đã được thiết lập đúng để xử lý (ví dụ: CASCADE DELETE hoặc SET NULL cho ProductID trong OrderDetails nếu Product bị xóa)

            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                // Cân nhắc: Có nên thực sự xóa đơn hàng khỏi DB hay chỉ đánh dấu là đã hủy/đã xóa?
                // Ví dụ: Thêm cột IsDeleted BIT và chỉ cập nhật nó
                // order.IsDeleted = true;
                // _context.Update(order);

                _context.Orders.Remove(order); // Xóa vật lý
            }

            await _context.SaveChangesAsync();
            // Có thể thêm thông báo thành công (TempData)
            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.OrderID == id);
        }
    }
}