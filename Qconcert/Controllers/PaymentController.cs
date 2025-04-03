using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qconcert.Models;
using System.Threading.Tasks;

namespace Qconcert.Controllers
{
    public class PaymentController : Controller
    {
        private readonly TicketBoxDb1Context _context;

        public PaymentController(TicketBoxDb1Context context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult CreatePaymentInfo(int eventId)
        {
            var existingPaymentInfo = _context.PaymentInfos.FirstOrDefault(p => p.EventId == eventId);

            if (existingPaymentInfo != null)
            {
                return View(existingPaymentInfo); // Nếu đã có dữ liệu, hiển thị nó
            }

            var paymentInfo = new PaymentInfo { EventId = eventId };
            return View(paymentInfo);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePaymentInfo(PaymentInfo paymentInfo)
        {

            var existingPaymentInfo = await _context.PaymentInfos
     .FirstOrDefaultAsync(p => p.EventId == paymentInfo.EventId);

            if (existingPaymentInfo != null)
            {
                // Cập nhật thông tin thanh toán thay vì thêm mới
                existingPaymentInfo.AccountNumber = paymentInfo.AccountNumber;
                existingPaymentInfo.BankName = paymentInfo.BankName;
                existingPaymentInfo.AccountHolder = paymentInfo.AccountHolder;
                existingPaymentInfo.Branch = paymentInfo.Branch;
                _context.PaymentInfos.Update(existingPaymentInfo);
            }
            else
            {
                // Nếu chưa có, thêm mới vào DB
                _context.PaymentInfos.Add(paymentInfo);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Success()
        {
            return View();
        }
    }
}
