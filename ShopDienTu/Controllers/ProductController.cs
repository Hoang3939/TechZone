using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShopDienTu.Data;
using ShopDienTu.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ShopDienTu.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.SubCategory)
                .ThenInclude(s => s.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.Reviews)
                .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(m => m.ProductID == id);

            if (product == null) return NotFound();

            var relatedProducts = await _context.Products
                .Include(p => p.ProductImages)
                .Where(p => p.SubCategoryID == product.SubCategoryID && p.ProductID != product.ProductID && p.IsActive)
                .Take(4)
                .ToListAsync();

            ViewBag.RelatedProducts = relatedProducts;
            return View(product);
        }

        public async Task<IActionResult> Search(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm)) return RedirectToAction("Index", "Home");

            var products = await _context.Products
                .Include(p => p.SubCategory)
                .ThenInclude(s => s.Category)
                .Include(p => p.ProductImages)
                .Where(p => p.IsActive && (p.ProductName.Contains(searchTerm) || p.Description.Contains(searchTerm)))
                .ToListAsync();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.ResultCount = products.Count;
            ViewBag.Categories = await _context.Categories.Include(c => c.SubCategories).ToListAsync();

            return View(products);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.SubCategoryList = _context.SubCategories
                .Select(s => new SelectListItem
                {
                    Value = s.SubCategoryID.ToString(),
                    Text = s.SubCategoryName
                }).ToList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, string MainImagePath)
        {
            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(MainImagePath))
                {
                    var productImage = new ProductImage
                    {
                        ProductID = product.ProductID,
                        ImagePath = MainImagePath,
                        IsMainImage = true
                    };
                    _context.ProductImages.Add(productImage);
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }

            ViewBag.SubCategoryList = _context.SubCategories
                .Select(s => new SelectListItem
                {
                    Value = s.SubCategoryID.ToString(),
                    Text = s.SubCategoryName
                }).ToList();

            return View(product);
        }


        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductID == id);
            if (product == null) return NotFound();

            ViewBag.SubCategoryList = new SelectList(_context.SubCategories.ToList(), "SubCategoryID", "SubCategoryName", product.SubCategoryID);

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, string MainImagePath)
        {
            if (id != product.ProductID) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();

                    if (!string.IsNullOrEmpty(MainImagePath))
                    {
                        var existingMainImage = _context.ProductImages.FirstOrDefault(p => p.ProductID == product.ProductID && p.IsMainImage);
                        if (existingMainImage != null)
                        {
                            existingMainImage.ImagePath = MainImagePath;
                        }
                        else
                        {
                            _context.ProductImages.Add(new ProductImage
                            {
                                ProductID = product.ProductID,
                                ImagePath = MainImagePath,
                                IsMainImage = true
                            });
                        }
                        await _context.SaveChangesAsync();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Products.Any(e => e.ProductID == id)) return NotFound();
                    else throw;
                }

                return RedirectToAction(nameof(Index));
            }

            ViewBag.SubCategoryList = new SelectList(_context.SubCategories.ToList(), "SubCategoryID", "SubCategoryName", product.SubCategoryID);
            return View(product);
        }


        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.SubCategory)
                .FirstOrDefaultAsync(p => p.ProductID == id);

            if (product == null) return NotFound();

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}