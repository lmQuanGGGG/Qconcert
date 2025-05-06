
using Qconcert.Models;
using Microsoft.EntityFrameworkCore;

namespace Qconcert.Services
{
    public class PaymentService
    {
        private readonly TicketBoxDb1Context _context;
        private readonly VietQRService _vietQRService;

        public PaymentService(TicketBoxDb1Context context, VietQRService vietQRService)
        {
            _context = context;
            _vietQRService = vietQRService;
        }

        public async Task<string> GeneratePaymentQR(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return null;

            string qrUrl = _vietQRService.GenerateQrUrl(order);
            
            // Lưu URL QR vào đơn hàng
            order.QrCodeUrl = qrUrl;
            await _context.SaveChangesAsync();

            return qrUrl;
        }

        public async Task<bool> UpdatePaymentStatus(int orderId, string bankTransferImage = null)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            // Cập nhật thông tin thanh toán
            order.PaymentStatus = "Đang xử lý";
            order.PaymentMethod = "Chuyển khoản ngân hàng";
            order.PaymentDate = DateTime.Now;
            order.BankTransferImage = bankTransferImage;
            order.TransactionId = $"TX{DateTime.Now.Ticks}";

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}