using QRCoder; // Thư viện chính để tạo mã QR
using System;

public class QRCodeService
{
    public string GenerateQRCode(string content)
    {
        using (var qrGenerator = new QRCodeGenerator())
        {
            var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new Base64QRCode(qrCodeData); // Sử dụng Base64QRCode từ QRCoder
            return qrCode.GetGraphic(20); // Trả về chuỗi Base64 của mã QR
        }
    }
}