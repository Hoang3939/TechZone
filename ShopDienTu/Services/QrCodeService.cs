// Services/QrCodeService.cs
using Microsoft.Extensions.Options;
using QRCoder;
using ShopDienTu.Settings;
using System;
using System.Text;

namespace ShopDienTu.Services
{
    public class QrCodeService : IQrCodeService
    {
        private readonly BankInfoSettings _bankInfo;

        public QrCodeService(IOptions<BankInfoSettings> bankInfoOptions)
        {
            _bankInfo = bankInfoOptions.Value;
        }

        public string GenerateVietQrCodeAsBase64(decimal amount, string orderInfo)
        {
            // Chuẩn hóa nội dung chuyển khoản để không có dấu và ký tự đặc biệt
            string sanitizedOrderInfo = SanitizeString(orderInfo);

            // Tạo payload theo chuẩn VietQR
            // Tham khảo: https://vietqr.net/portal-service/download-document
            var payload = new StringBuilder();
            payload.Append("000201"); // Version
            payload.Append("010212"); // Init method
            payload.Append("38" + (_bankInfo.BankId.Length + _bankInfo.AccountNumber.Length + 25).ToString("D2")); // Merchant Account Information
            payload.Append("0010A000000727"); // VietQR service
            payload.Append("01" + _bankInfo.BankId.Length.ToString("D2") + _bankInfo.BankId); // Bank BIN
            payload.Append("02" + _bankInfo.AccountNumber.Length.ToString("D2") + _bankInfo.AccountNumber); // Account Number
            payload.Append("5303704"); // Currency (VND)
            payload.Append("54" + amount.ToString("F0").Length.ToString("D2") + amount.ToString("F0")); // Amount
            payload.Append("5802VN"); // Country Code
            payload.Append("62" + (sanitizedOrderInfo.Length + 4).ToString("D2")); // Additional Data
            payload.Append("08" + sanitizedOrderInfo.Length.ToString("D2") + sanitizedOrderInfo); // Bill Number / Order Info

            // Thêm CRC16 checksum
            string payloadToHash = payload.ToString() + "6304";
            string crc = CalculateCrc16(payloadToHash);
            payload.Append("6304" + crc);

            // Tạo mã QR
            using (var qrGenerator = new QRCodeGenerator())
            using (var qrCodeData = qrGenerator.CreateQrCode(payload.ToString(), QRCodeGenerator.ECCLevel.Q))
            using (var qrCode = new Base64QRCode(qrCodeData))
            {
                return "data:image/png;base64," + qrCode.GetGraphic(20);
            }
        }

        // Hàm tính CRC16-CCITT
        private string CalculateCrc16(string data)
        {
            ushort crc = 0xFFFF;
            foreach (byte b in Encoding.ASCII.GetBytes(data))
            {
                crc ^= (ushort)(b << 8);
                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x8000) != 0)
                        crc = (ushort)((crc << 1) ^ 0x1021);
                    else
                        crc <<= 1;
                }
            }
            return crc.ToString("X4");
        }

        private string SanitizeString(string input)
        {
            // Thay thế các ký tự đặc biệt bằng khoảng trắng và loại bỏ dấu
            var normalizedString = input.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();
            foreach (var c in normalizedString)
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }
            return stringBuilder.ToString().Normalize(NormalizationForm.FormC).ToUpper().Replace(" ", "");
        }
    }
}