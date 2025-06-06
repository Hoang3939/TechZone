using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopDienTu.Models;
using ShopDienTu.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace ShopDienTu.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")] // Chỉ Admin được truy cập
    public class RanksController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RanksController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/AdminRank
        public IActionResult Index(string searchString)
        {
            var ranks = _context.Ranks.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
                ranks = ranks.Where(r => r.RankName.Contains(searchString) || r.Description.Contains(searchString));

            ViewBag.SearchString = searchString;

            return View(ranks.ToList());
        }

        // GET: Admin/AdminRank/Create
        public IActionResult Create()
        {
            return View(new Rank());
        }

        // POST: Admin/AdminRank/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Rank model)
        {
            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.Now;
                _context.Ranks.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm hạng thành viên thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // GET: Admin/AdminRank/Edit/5
        public IActionResult Edit(int id)
        {
            var rank = _context.Ranks.FirstOrDefault(r => r.RankID == id);
            if (rank == null) return NotFound();
            return View(rank);
        }

        // POST: Admin/AdminRank/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Rank model)
        {
            if (ModelState.IsValid)
            {
                var rank = _context.Ranks.FirstOrDefault(r => r.RankID == model.RankID);
                if (rank == null) return NotFound();

                rank.RankName = model.RankName;
                rank.Description = model.Description;
                rank.MinimumPoints = model.MinimumPoints;
                rank.DiscountPercentage = model.DiscountPercentage;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật hạng thành viên thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // POST: Admin/AdminRank/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var rank = _context.Ranks.FirstOrDefault(r => r.RankID == id);
            if (rank == null) return NotFound();

            _context.Ranks.Remove(rank);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa hạng thành viên thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}