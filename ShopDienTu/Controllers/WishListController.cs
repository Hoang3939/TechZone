using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopDienTu.Data;
using ShopDienTu.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ShopDienTu.Controllers
{
    [Authorize]
    public class WishListController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WishListController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: /WishList/Add
        [HttpPost]
        public async Task<IActionResult> Add(int productId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized(); // Không lấy được userID
            }

            var exists = await _context.WishLists
                .AnyAsync(w => w.UserID == userId && w.ProductID == productId);

            if (!exists)
            {
                var newItem = new WishList
                {
                    UserID = userId,
                    ProductID = productId,
                    AddedAt = DateTime.UtcNow
                };

                _context.WishLists.Add(newItem);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Wishlist", "Account"); // Chuyển sang trang danh sách yêu thích
        }

        // POST: /WishList/DeleteById
        [HttpPost]
        public async Task<IActionResult> DeleteById(int wishlistItemId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return Unauthorized();
            }

            var item = await _context.WishLists.FindAsync(wishlistItemId);

            if (item != null && item.UserID == userId)
            {
                _context.WishLists.Remove(item);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Wishlist", "Account");
        }
    }
}
