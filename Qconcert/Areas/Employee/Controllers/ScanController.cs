using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qconcert.Controllers;
using Qconcert.Models;
using Qconcert.Services;
using Microsoft.EntityFrameworkCore;

namespace Qconcert.Areas.Employee.Controllers
{
    [Area("Employee")]
    [Authorize(Roles = "Employee")]
    public class ScanController : Controller
    {
        private readonly TicketBoxDb1Context _context;
        private readonly ILogger<ScanController> _logger;
        public ScanController(
            TicketBoxDb1Context context,
            ILogger<ScanController> logger)
        {
            _context = context;
            _logger = logger;
        }
        public IActionResult Index()
        {
            return View();
        }
        [Authorize]
        [HttpPost("api/verify-qr-code")]
        public async Task<IActionResult> VerifyQrCode([FromBody] VerifyQrCodeRequest request)
        {
            var qrCodeParts = request.QrCodeText.Split(", ");
            var tokenPart = qrCodeParts.FirstOrDefault(p => p.StartsWith("Token:"));
            if (tokenPart == null)
            {
                return NotFound(new { message = "Mã QR không hợp lệ" });
            }

            var token = tokenPart.Split(": ")[1];
            var orderDetail = await _context.OrderDetails
                .Include(od => od.Order)
                .FirstOrDefaultAsync(od => od.QrCodeToken == token);

            if (orderDetail == null)
            {
                return NotFound(new { message = "Mã QR không hợp lệ" });
            }

            if (orderDetail.IsUsed)
            {
                return BadRequest(new { message = "Mã QR đã được sử dụng" });
            }

            // Đánh dấu mã QR là đã sử dụng
            orderDetail.IsUsed = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Mã QR hợp lệ. Mời vào!!" });
        }

        public class VerifyQrCodeRequest
        {
            public string QrCodeText { get; set; }
        }

        [Authorize(Roles = "Employee")]
        public IActionResult ScanQrCode()
        {
            return View();
        }
    }
}
