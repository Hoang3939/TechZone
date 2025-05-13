using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShopDienTu.Data;
using ShopDienTu.Models;

namespace ShopDienTu.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/AdminProducts
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Products.Include(p => p.SubCategory);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Admin/AdminProducts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.SubCategory)
                .FirstOrDefaultAsync(m => m.ProductID == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Admin/AdminProducts/Create
        public IActionResult Create()
        {
            var subCategories = _context.SubCategories.ToList();
            if (!subCategories.Any())
            {
                ModelState.AddModelError("", "Không có danh mục phụ nào trong cơ sở dữ liệu. Vui lòng thêm danh mục phụ trước.");
            }
            ViewData["SubCategoryID"] = new SelectList(subCategories, "SubCategoryID", "SubCategoryName");
            return View();
        }

        // POST: Admin/AdminProducts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductName,Description,Price,SubCategoryID,StockQuantity,IsActive")] Product product)
        {
            // Log dữ liệu đầu vào
            Console.WriteLine($"ProductName: {product.ProductName}, Price: {product.Price}, SubCategoryID: {product.SubCategoryID}, StockQuantity: {product.StockQuantity}, IsActive: {product.IsActive}");

            // Bỏ qua validation cho SubCategory
            ModelState.Remove("SubCategory");

            // Kiểm tra SubCategoryID
            if (product.SubCategoryID != 0 && !_context.SubCategories.Any(sc => sc.SubCategoryID == product.SubCategoryID))
            {
                ModelState.AddModelError("SubCategoryID", "Danh mục phụ không tồn tại.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    product.CreatedAt = DateTime.Now;
                    product.UpdatedAt = DateTime.Now;
                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Sản phẩm đã được thêm thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error saving product: " + ex.Message);
                    ModelState.AddModelError("", $"Lỗi khi lưu sản phẩm: {ex.Message}");
                }
            }
            else
            {
                // Log lỗi validation
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                Console.WriteLine("Validation errors: " + string.Join(", ", errors));
            }

            ViewBag.SubCategoryID = new SelectList(_context.SubCategories, "SubCategoryID", "SubCategoryName", product.SubCategoryID);
            return View(product);
        }

        // GET: Admin/AdminProducts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            ViewData["SubCategoryID"] = new SelectList(_context.SubCategories, "SubCategoryID", "SubCategoryName", product.SubCategoryID);
            return View(product);
        }

        // POST: Admin/AdminProducts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductID,ProductName,Description,Price,SubCategoryID,StockQuantity,CreatedAt,UpdatedAt,IsActive")] Product product)
        {
            if (id != product.ProductID)
            {
                return NotFound();
            }

            // Bỏ qua validation cho SubCategory
            ModelState.Remove("SubCategory");

            if (ModelState.IsValid)
            {
                try
                {
                    product.UpdatedAt = DateTime.Now; // Cập nhật thời gian chỉnh sửa
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Sản phẩm đã được cập nhật thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.ProductID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Lỗi khi cập nhật sản phẩm: {ex.Message}");
                }
            }

            // Log lỗi validation
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            Console.WriteLine("Validation errors: " + string.Join(", ", errors));

            ViewData["SubCategoryID"] = new SelectList(_context.SubCategories, "SubCategoryID", "SubCategoryName", product.SubCategoryID);
            return View(product);
        }

        // GET: Admin/AdminProducts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.SubCategory)
                .FirstOrDefaultAsync(m => m.ProductID == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            try
            {
                // Tìm và xóa các bản ghi trong OrderDetails liên quan đến ProductID
                var orderDetails = await _context.OrderDetails
                    .Where(od => od.ProductID == id)
                    .ToListAsync();
                if (orderDetails.Any())
                {
                    _context.OrderDetails.RemoveRange(orderDetails);
                }

                // Xóa sản phẩm
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine(ex.InnerException?.Message);
                ModelState.AddModelError("", "Không thể xóa sản phẩm vì nó đang được sử dụng trong các đơn hàng.");
                return View("Delete", product);
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductID == id);
        }
    }
}