// Services/IQrCodeService.cs
namespace ShopDienTu.Services
{
    public interface IQrCodeService
    {
        string GenerateVietQrCodeAsBase64(decimal amount, string orderInfo);
    }
}