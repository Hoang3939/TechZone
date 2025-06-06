using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShopDienTu.Data;
using ShopDienTu.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ShopDienTu.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")] // Chỉ Admin được truy cập
    public class PromotionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PromotionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/AdminPromotions
        public async Task<IActionResult> Index()
        {
            var promotions = await _context.Promotions
                .Include(p => p.Product)
                .Include(p => p.Rank)
                .AsNoTracking()
                .ToListAsync();
            return View(promotions);
        }

        // GET: Admin/AdminPromotions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var promotion = await _context.Promotions
                .Include(p => p.Product)
                .Include(p => p.Rank)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.PromotionID == id);

            if (promotion == null)
            {
                return NotFound();
            }

            return View(promotion);
        }

        // GET: Admin/AdminPromotions/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Products = new SelectList(await _context.Products.ToListAsync(), "ProductID", "ProductName");
            ViewBag.Ranks = new SelectList(await _context.Ranks.ToListAsync(), "RankID", "RankName");
            return View();
        }

        // POST: Admin/AdminPromotions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PromotionName,Description,DiscountPercentage,StartDate,EndDate,IsActive,ProductID,PromoCode,RankID")] Promotion promotion)
        {
            if (ModelState.IsValid)
            {
                if (promotion.EndDate <= promotion.StartDate)
                {
                    ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu.");
                }
                else
                {
                    _context.Add(promotion);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Chương trình khuyến mãi đã được tạo thành công.";
                    return RedirectToAction(nameof(Index));
                }
            }
            ViewBag.Products = new SelectList(await _context.Products.ToListAsync(), "ProductID", "ProductName");
            ViewBag.Ranks = new SelectList(await _context.Ranks.ToListAsync(), "RankID", "RankName");
            return View(promotion);
        }

        // GET: Admin/AdminPromotions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var promotion = await _context.Promotions
                .Include(p => p.Product)
                .Include(p => p.Rank)
                .FirstOrDefaultAsync(m => m.PromotionID == id);

            if (promotion == null)
            {
                return NotFound();
            }

            ViewBag.Products = new SelectList(await _context.Products.ToListAsync(), "ProductID", "ProductName");
            ViewBag.Ranks = new SelectList(await _context.Ranks.ToListAsync(), "RankID", "RankName");
            return View(promotion);
        }

        // POST: Admin/AdminPromotions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PromotionID,PromotionName,Description,DiscountPercentage,StartDate,EndDate,IsActive,ProductID,PromoCode,RankID")] Promotion promotion)
        {
            if (id != promotion.PromotionID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                if (promotion.EndDate <= promotion.StartDate)
                {
                    ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu.");
                }
                else
                {
                    try
                    {
                        _context.Update(promotion);
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Chương trình khuyến mãi đã được cập nhật thành công.";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!PromotionExists(promotion.PromotionID))
                        {
                            return NotFound();
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Lỗi đồng bộ dữ liệu. Vui lòng thử lại.";
                        }
                    }
                }
            }
            ViewBag.Products = new SelectList(await _context.Products.ToListAsync(), "ProductID", "ProductName");
            ViewBag.Ranks = new SelectList(await _context.Ranks.ToListAsync(), "RankID", "RankName");
            return View(promotion);
        }

        // GET: Admin/AdminPromotions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var promotion = await _context.Promotions
                .Include(p => p.Product)
                .Include(p => p.Rank)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.PromotionID == id);

            if (promotion == null)
            {
                return NotFound();
            }

            return View(promotion);
        }

        // POST: Admin/AdminPromotions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
            {
                return NotFound();
            }

            _context.Promotions.Remove(promotion);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Chương trình khuyến mãi đã được xóa thành công.";
            return RedirectToAction(nameof(Index));
        }

        private bool PromotionExists(int id)
        {
            return _context.Promotions.Any(e => e.PromotionID == id);
        }
    }
}