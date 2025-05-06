using Qconcert.Models;

namespace Qconcert.Services
{
    public class VietQRService
    {
        private readonly string _bankId = "970436"; // Mã ngân hàng Vietcombank
        private readonly string _accountNo = "1029950075"; // Thay bằng số tài khoản thật
        private readonly string _accountName = "CONG TY QCONCERT"; // Thay bằng tên tài khoản thật

        public string GenerateQrUrl(Order order)
        {
            string amount = ((int)order.TotalAmount).ToString();
            string description = $"Thanh toan don hang {order.Id}";

            // Format theo chuẩn VietQR
            string template = "https://img.vietqr.io/image/{0}-{1}-compact2.jpg?amount={2}&addInfo={3}&accountName={4}";
            
            string qrUrl = string.Format(template,
                _bankId,
                _accountNo,
                amount,
                description,
                _accountName);

            return qrUrl;
        }
    }
}