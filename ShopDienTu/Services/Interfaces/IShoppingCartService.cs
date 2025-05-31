using ShopDienTu.Models; // Để tham chiếu ShoppingCart (View Model)
using Microsoft.AspNetCore.Http; // Để sử dụng ISession
using System.Security.Claims; // Để sử dụng ClaimsPrincipal (User)
using System.Threading.Tasks;

namespace ShopDienTu.Services
{
    public interface IShoppingCartService
    {
        Task<ShoppingCart> GetCartAsync(ClaimsPrincipal user, ISession session);
        Task AddItemAsync(ClaimsPrincipal user, ISession session, int productId, int quantity);
        Task RemoveItemAsync(ClaimsPrincipal user, ISession session, int productId);
        Task UpdateQuantityAsync(ClaimsPrincipal user, ISession session, int productId, int quantity);
        Task ClearCartAsync(ClaimsPrincipal user, ISession session);
        Task MergeAnonymousCartAsync(ClaimsPrincipal user, ISession session); // Để hợp nhất giỏ hàng ẩn danh khi đăng nhập
    }
}