using ShopDienTu.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace ShopDienTu.ViewComponents
{
    public class CartSummaryViewComponent : ViewComponent
    {
        private const string CartSessionKey = "Cart";

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var cartJson = HttpContext.Session.GetString(CartSessionKey);
            if (string.IsNullOrEmpty(cartJson))
            {
                return Content("0");
            }

            var cart = JsonConvert.DeserializeObject<ShoppingCart>(cartJson);
            return Content(cart.GetTotalItems().ToString());
        }
    }
}
