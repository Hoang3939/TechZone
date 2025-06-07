using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ShopDienTu.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using ShopDienTu.Models;
using System.ComponentModel.DataAnnotations;
using ShopDienTu.Services;
using Microsoft.AspNetCore.Authorization;

namespace ShopDienTu.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;
        private readonly IShoppingCartService _shoppingCartService;

        public AccountController(ApplicationDbContext context, ILogger<AccountController> logger, IShoppingCartService shoppingCartService)
        {
            _context = context;
            _logger = logger;
            _shoppingCartService = shoppingCartService;
        }

        [HttpGet]
        public async Task<IActionResult> Addresses()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var addresses = await _context.UserAddresses
                .Where(a => a.UserID == userId)
                .OrderByDescending(a => a.AddedAt)
                .ToListAsync();

            return View(addresses);
        }

        // GET: Account/Login
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.UserName == model.UserName || u.Email == model.UserName);

                    if (user != null && VerifyPassword(model.Password, user.Password))
                    {
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, user.UserName),
                            new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                            new Claim(ClaimTypes.Email, user.Email),
                            new Claim(ClaimTypes.Role, user.Role ?? "Customer"),
                            new Claim("Phone", user.Phone ?? "")
                        };

                        var claimsIdentity = new ClaimsIdentity(
                            claims, CookieAuthenticationDefaults.AuthenticationScheme);

                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = model.RememberMe,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                        };

                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity),
                            authProperties);

                        _logger.LogInformation("User {UserName} logged in at {Time}.", user.UserName, DateTime.UtcNow);

                        await _shoppingCartService.MergeAnonymousCartAsync(User, HttpContext.Session);

                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
                        return View(model);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during login for user {UserName}", model.UserName);
                    ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi trong quá trình đăng nhập. Vui lòng thử lại sau.");
                }
            }

            return View(model);
        }

        // GET: Account/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Models.RegisterViewModel model)
        {
            try
            {
                // Debug: Kiểm tra ModelState
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ModelState không hợp lệ: {Errors}",
                        string.Join(", ", ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)));

                    return View(model);
                }

                // Kiểm tra tên đăng nhập đã tồn tại
                var userNameExists = await _context.Users.AnyAsync(u => u.UserName == model.UserName);
                if (userNameExists)
                {
                    ModelState.AddModelError("UserName", "Tên đăng nhập đã tồn tại.");
                    return View(model);
                }

                // Kiểm tra email đã tồn tại
                var emailExists = await _context.Users.AnyAsync(u => u.Email == model.Email);
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "Email đã được sử dụng bởi tài khoản khác.");
                    return View(model);
                }

                // Lấy rank mặc định (rank có điểm thấp nhất)
                var defaultRank = await _context.Ranks
                    .OrderBy(r => r.MinimumPoints)
                    .FirstOrDefaultAsync();

                var user = new User
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    FullName = model.FullName,
                    Password = HashPassword(model.Password),
                    Phone = model.Phone,
                    Address = model.Address, // Có thể null
                    Role = "Customer", // Sửa từ "User" thành "Customer"
                    CreatedAt = DateTime.Now,
                    Points = 0,
                    RankID = defaultRank?.RankID // Gán rank mặc định
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserName} created a new account at {Time}.", user.UserName, DateTime.UtcNow);

                // Đăng nhập người dùng sau khi đăng ký
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("Phone", user.Phone ?? "")
                };

                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                await _shoppingCartService.MergeAnonymousCartAsync(User, HttpContext.Session);

                TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Chào mừng bạn đến với cửa hàng.";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for user {UserName}: {Message}", model.UserName, ex.Message);
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi trong quá trình đăng ký: " + ex.Message);
                return View(model);
            }
        }

        // GET: Account/Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["SuccessMessage"] = "Đăng xuất thành công!";
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/ForgotPassword
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                    if (user == null)
                    {
                        // Không tiết lộ thông tin về việc email có tồn tại hay không
                        TempData["SuccessMessage"] = "Nếu email của bạn tồn tại trong hệ thống, chúng tôi đã gửi hướng dẫn đặt lại mật khẩu.";
                        return RedirectToAction("Login");
                    }

                    // TODO: Gửi email đặt lại mật khẩu
                    // Trong môi trường thực tế, bạn sẽ tạo token đặt lại mật khẩu và gửi email

                    TempData["SuccessMessage"] = "Chúng tôi đã gửi hướng dẫn đặt lại mật khẩu đến email của bạn.";
                    return RedirectToAction("Login");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during forgot password for email {Email}", model.Email);
                    ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi. Vui lòng thử lại sau.");
                }
            }

            return View(model);
        }

        // GET: Account/Profile
        public async Task<IActionResult> Profile()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }

            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var user = await _context.Users
                    .Include(u => u.Rank)
                    .FirstOrDefaultAsync(u => u.UserID == userId);

                if (user == null)
                {
                    return NotFound();
                }

                int currentPoints = user.Points ?? 0;

                // Logic cập nhật Rank tự động dựa trên điểm
                var newRank = await _context.Ranks
                    .Where(r => r.MinimumPoints <= currentPoints)
                    .OrderByDescending(r => r.MinimumPoints)
                    .FirstOrDefaultAsync();

                if (newRank != null && (user.RankID == null || user.RankID != newRank.RankID))
                {
                    _logger.LogInformation("User {UserId} rank changed from {OldRankName} (ID: {OldRankId}) to {NewRankName} (ID: {NewRankId}) with {Points} points.",
                                           userId,
                                           user.Rank?.RankName ?? "No Rank",
                                           user.RankID,
                                           newRank.RankName,
                                           newRank.RankID,
                                           currentPoints);
                    user.RankID = newRank.RankID;
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                    user.Rank = newRank;
                }

                ViewBag.Points = currentPoints;
                ViewBag.RankName = user.Rank?.RankName ?? "Chưa có hạng";

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profile");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải thông tin tài khoản. Vui lòng thử lại sau.";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: Account/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(User model)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }

            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                if (model.UserID != userId)
                {
                    return Forbid();
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound();
                }

                // Kiểm tra xem email đã tồn tại chưa (nếu thay đổi)
                if (user.Email != model.Email)
                {
                    var emailExists = await _context.Users.AnyAsync(u => u.Email == model.Email && u.UserID != userId);
                    if (emailExists)
                    {
                        ModelState.AddModelError("Email", "Email này đã được sử dụng bởi tài khoản khác.");
                        return View("Profile", model);
                    }
                }

                // Cập nhật thông tin người dùng
                user.FullName = model.FullName;
                user.Email = model.Email;
                user.Phone = model.Phone;
                user.Address = model.Address;

                _context.Update(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cập nhật thông tin tài khoản thành công!";
                return RedirectToAction("Profile");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", model.UserID);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi cập nhật thông tin tài khoản. Vui lòng thử lại sau.";
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var currentUser = await _context.Users.Include(u => u.Rank).FirstOrDefaultAsync(u => u.UserID == userId);
                ViewBag.Points = currentUser?.Points ?? 0;
                ViewBag.RankName = currentUser?.Rank?.RankName ?? "Chưa có hạng";
                return View("Profile", model);
            }
        }

        // GET: Account/ChangePassword
        public async Task<IActionResult> ChangePassword()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users
                .Include(u => u.Rank)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login");
            }

            ViewBag.FullName = user.FullName;
            ViewBag.Email = user.Email;
            ViewBag.Points = user.Points ?? 0;
            ViewBag.RankName = user.Rank?.RankName ?? "Chưa có hạng";

            return View();
        }

        // POST: Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _context.Users.FindAsync(userId);

                    if (user == null)
                    {
                        return NotFound();
                    }

                    // Kiểm tra mật khẩu hiện tại
                    if (!VerifyPassword(model.CurrentPassword, user.Password))
                    {
                        ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng.");
                        await SetSidebarViewBagData(userId);
                        return View(model);
                    }

                    // Cập nhật mật khẩu mới
                    user.Password = HashPassword(model.NewPassword);
                    _context.Update(user);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                    return RedirectToAction("Profile");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error changing password");
                    ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi đổi mật khẩu. Vui lòng thử lại sau.");
                    await SetSidebarViewBagData(userId);
                }
            }
            else
            {
                await SetSidebarViewBagData(userId);
            }

            return View(model);
        }

        private async Task SetSidebarViewBagData(int userId)
        {
            var user = await _context.Users
                .Include(u => u.Rank)
                .FirstOrDefaultAsync(u => u.UserID == userId);


            if(user != null)
            {
                ViewBag.FullName = user.FullName;
                ViewBag.Email = user.Email;
                ViewBag.Points = user.Points ?? 0;
                ViewBag.RankName = user.Rank?.RankName ?? "Chưa có hạng";
            }
            else
            {
                ViewBag.FullName = "Người dùng";
                ViewBag.Email = "";
                ViewBag.Points = 0;
                ViewBag.RankName = "Chưa có hạng";
            }
        }

        // Phương thức mã hóa mật khẩu
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        // Phương thức xác minh mật khẩu
        private bool VerifyPassword(string password, string hashedPassword)
        {
            var hashedInput = HashPassword(password);
            return hashedInput == hashedPassword;
        }

        [Authorize]
        public async Task<IActionResult> Wishlist()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdStr, out var userId))
            {
                return Unauthorized();
            }
            var currentUser = await _context.Users
                .Include(u => u.Rank) // Bao gồm thông tin Rank
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (currentUser == null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login");
            }

            ViewBag.FullName = currentUser.FullName;
            ViewBag.Email = currentUser.Email;
            ViewBag.Points = currentUser.Points ?? 0;
            ViewBag.RankName = currentUser.Rank?.RankName ?? "Chưa có hạng";

            var wishlist = await _context.WishLists
                .Where(w => w.UserID == userId)
                .Include(w => w.Product)
                    .ThenInclude(p => p.ProductImages)
                .ToListAsync();

            return View(wishlist);
        }

        [Authorize]
        public async Task<IActionResult> Voucher()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(!int.TryParse(userIdStr, out var userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users.FindAsync(userId);
            if(user == null)
            {
                return NotFound("Không tìm thấy thông tin người dùng.");
            }

            var userRankId = user.RankID;

            var availableVouchers = await _context.Promotions
                .Where(p => p.ProductID == null && p.DiscountPercentage == 0m && p.IsActive && p.EndDate > DateTime.Now && (p.RankID == null || p.RankID == userRankId))
                .OrderByDescending(p => p.EndDate)
                .ToListAsync();

            ViewBag.FullName = user.FullName;
            ViewBag.Email = user.Email;
            ViewBag.Points = user.Points ?? 0;
            var rank = await _context.Ranks.FindAsync(user.RankID);
            ViewBag.RankName = rank?.RankName ?? "Chưa có hạng";

            return View(availableVouchers);
        }

        [Authorize]
        public async Task<IActionResult> Reviews()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized();

            var reviews = await _context.Reviews
                .Include(r => r.Product)
                    .ThenInclude(p => p.ProductImages)
                .Where(r => r.UserID == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(reviews);
        }
    }

    // View Models
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [Display(Name = "Tên đăng nhập")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu hiện tại")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [StringLength(100, ErrorMessage = "Mật khẩu phải có ít nhất {2} ký tự và tối đa {1} ký tự.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu mới")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu mới và xác nhận mật khẩu không khớp.")]
        public string ConfirmPassword { get; set; }
    }
}

namespace ShopDienTu.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [Display(Name = "Tên đăng nhập")]
        [StringLength(20, ErrorMessage = "Tên đăng nhập phải có ít nhất {2} ký tự và tối đa {1} ký tự.", MinimumLength = 3)]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ và tên")]
        [StringLength(100, ErrorMessage = "Họ tên phải có ít nhất {2} ký tự và tối đa {1} ký tự.", MinimumLength = 2)]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        [StringLength(100)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        [StringLength(15)]
        public string Phone { get; set; }

        [Display(Name = "Địa chỉ")]
        [StringLength(255)]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [StringLength(100, ErrorMessage = "Mật khẩu phải có ít nhất {2} ký tự và tối đa {1} ký tự.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp.")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "Tôi đồng ý với điều khoản dịch vụ")]
        [Required(ErrorMessage = "Bạn phải đồng ý với điều khoản dịch vụ để đăng ký.")]
        public bool AgreeTerms { get; set; }
    }
}
