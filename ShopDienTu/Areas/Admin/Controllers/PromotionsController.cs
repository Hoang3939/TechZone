using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShopDienTu.Data; // Namespace DbContext
using ShopDienTu.Models; // Namespace Model

namespace ShopDienTu.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class PromotionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PromotionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Hàm trợ giúp tạo SelectList cho ProductID (bao gồm option NULL)
        private async Task PopulateProductsDropDownList(object selectedProduct = null)
        {
            var productsQuery = from p in _context.Products
                                orderby p.ProductName
                                select p;

            var items = await productsQuery.Select(x => new SelectListItem
            {
                Value = x.ProductID.ToString(),
                Text = x.ProductName
            }).ToListAsync();

            // Thêm lựa chọn "Không chọn" (null) vào đầu danh sách
            items.Insert(0, new SelectListItem { Value = "", Text = "--- Không chọn (Khuyến mãi chung) ---" });

            ViewBag.ProductID = new SelectList(items, "Value", "Text", selectedProduct);
        }

        // Hàm trợ giúp tạo SelectList cho IsActive
        private void PopulateActiveStatusDropDownList(object selectedStatus = null)
        {
            var statusList = new List<SelectListItem>
            {
                new SelectListItem { Value = "true", Text = "Kích hoạt" },
                new SelectListItem { Value = "false", Text = "Vô hiệu hóa" }
            };
            ViewBag.IsActiveStatus = new SelectList(statusList, "Value", "Text", selectedStatus ?? true); // Mặc định là true nếu null
        }


        // GET: Admin/Promotions
        public async Task<IActionResult> Index()
        {
            // Include Product để hiển thị tên sản phẩm nếu có
            var applicationDbContext = _context.Promotions.Include(p => p.Product);
            return View(await applicationDbContext.ToListAsync());
            // *** Cần tạo View Index.cshtml ***
        }

        // GET: Admin/Promotions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var promotion = await _context.Promotions
                .Include(p => p.Product) // Include Product để hiển thị tên
                .FirstOrDefaultAsync(m => m.PromotionID == id);
            if (promotion == null)
            {
                return NotFound();
            }

            return View(promotion);
        }

        // GET: Admin/Promotions/Create
        public async Task<IActionResult> Create()
        {
            await PopulateProductsDropDownList(); // Load danh sách sản phẩm (có null)
            PopulateActiveStatusDropDownList(true); // Mặc định là true
            return View();
        }

        // POST: Admin/Promotions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Cập nhật Bind để bao gồm các trường mới
        public async Task<IActionResult> Create([Bind("PromotionName,ProductID,DiscountPercentage,IsActive,PromoCode,StartDate,EndDate,Description")] Promotion promotion)
        {
            // Xóa các navigation property khỏi validation
            ModelState.Remove("Product");

            // Validate EndDate > StartDate
            if (promotion.EndDate <= promotion.StartDate)
            {
                ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu.");
            }

            // Xử lý ProductID nếu người dùng chọn "Không chọn"
            if (promotion.ProductID == 0) // Hoặc kiểm tra giá trị "" nếu SelectList Value là string rỗng
            {
                promotion.ProductID = null;
            }


            if (ModelState.IsValid)
            {
                _context.Add(promotion);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Tạo chương trình khuyến mãi thành công!";
                return RedirectToAction(nameof(Index));
            }

            // Nếu lỗi, load lại dropdown lists
            await PopulateProductsDropDownList(promotion.ProductID);
            PopulateActiveStatusDropDownList(promotion.IsActive);
            return View(promotion);
        }

        // GET: Admin/Promotions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
            {
                return NotFound();
            }
            // Load dropdown lists với giá trị hiện tại
            await PopulateProductsDropDownList(promotion.ProductID);
            PopulateActiveStatusDropDownList(promotion.IsActive);
            return View(promotion);
        }

        // POST: Admin/Promotions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Cập nhật Bind
        public async Task<IActionResult> Edit(int id, [Bind("PromotionID,PromotionName,ProductID,DiscountPercentage,IsActive,PromoCode,StartDate,EndDate,Description")] Promotion promotionViewModel)
        {
            if (id != promotionViewModel.PromotionID)
            {
                return NotFound();
            }

            // Lấy đối tượng gốc từ DB
            var promotionToUpdate = await _context.Promotions.FindAsync(id);
            if (promotionToUpdate == null)
            {
                return NotFound();
            }

            // Xóa các navigation property khỏi validation
            ModelState.Remove("Product");

            // Validate EndDate > StartDate
            if (promotionViewModel.EndDate <= promotionViewModel.StartDate)
            {
                ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu.");
            }

            // Xử lý ProductID nếu người dùng chọn "Không chọn"
            int? selectedProductId = promotionViewModel.ProductID;
            if (selectedProductId == 0)
            {
                selectedProductId = null;
            }


            if (ModelState.IsValid)
            {
                try
                {
                    // **Cập nhật các thuộc tính từ ViewModel vào đối tượng lấy từ DB**
                    promotionToUpdate.PromotionName = promotionViewModel.PromotionName;
                    promotionToUpdate.ProductID = selectedProductId; // Cập nhật ProductID đã xử lý null
                    promotionToUpdate.DiscountPercentage = promotionViewModel.DiscountPercentage;
                    promotionToUpdate.IsActive = promotionViewModel.IsActive;
                    promotionToUpdate.PromoCode = promotionViewModel.PromoCode;
                    promotionToUpdate.StartDate = promotionViewModel.StartDate;
                    promotionToUpdate.EndDate = promotionViewModel.EndDate;
                    promotionToUpdate.Description = promotionViewModel.Description;

                    // _context.Update(promotionToUpdate); // Không cần dòng này nếu đã lấy từ context và thay đổi property
                    _context.Entry(promotionToUpdate).State = EntityState.Modified; // Hoặc chỉ định rõ trạng thái
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật chương trình khuyến mãi thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PromotionExists(promotionViewModel.PromotionID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        ModelState.AddModelError("", "Lỗi xảy ra khi cập nhật dữ liệu. Dữ liệu có thể đã bị thay đổi bởi người khác.");
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            // Nếu lỗi, load lại dropdown lists
            await PopulateProductsDropDownList(promotionViewModel.ProductID);
            PopulateActiveStatusDropDownList(promotionViewModel.IsActive);
            // Trả về ViewModel để hiển thị lỗi và giữ lại giá trị người dùng đã nhập
            return View(promotionViewModel);
        }


        // GET: Admin/Promotions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var promotion = await _context.Promotions
                .Include(p => p.Product) // Include để hiển thị tên sản phẩm
                .FirstOrDefaultAsync(m => m.PromotionID == id);
            if (promotion == null)
            {
                return NotFound();
            }

            return View(promotion); // View Delete cần được cập nhật để hiển thị đủ thông tin
        }

        // POST: Admin/Promotions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion != null)
            {
                _context.Promotions.Remove(promotion);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa chương trình khuyến mãi thành công!";
            return RedirectToAction(nameof(Index));
        }

        private bool PromotionExists(int id)
        {
            return _context.Promotions.Any(e => e.PromotionID == id);
        }
    }
}