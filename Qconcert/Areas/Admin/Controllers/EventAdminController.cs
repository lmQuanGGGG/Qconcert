using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qconcert.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using iTextSharp.text.pdf;
using iTextSharp.text;

namespace Qconcert.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class EventAdminController : Controller
    {
        private readonly TicketBoxDb1Context _context;

        public EventAdminController(TicketBoxDb1Context context)
        {
            _context = context;
        }

        public async Task<IActionResult> PaymentReport()
        {
            var expiredEvents = await _context.Events
                .Where(e => e.Date < DateTime.Now && !e.IsPaid)
                .Include(e => e.Tickets)
                    .ThenInclude(t => t.OrderDetails)
                        .ThenInclude(od => od.Order)
                .Include(e => e.PaymentInfos) // đảm bảo navigation property này có trong Event
                .ToListAsync();

            var result = expiredEvents.Select(e =>
            {
                var successfulOrders = e.Tickets
                    .SelectMany(t => t.OrderDetails)
                    .Where(od => od.Order != null && od.Order.PaymentStatus == "Thanh toán thành công")
                    .ToList();

                var totalRevenue = successfulOrders.Sum(od => od.Price * od.Quantity);
                var paymentDue = totalRevenue * 0.9m;

                var paymentInfo = e.PaymentInfos.FirstOrDefault();

                return new
                {
                    e.Id,
                    e.Name,
                    e.Date,
                    e.OrganizerName,
                    TotalRevenue = totalRevenue,
                    PaymentDue = paymentDue,
                    AccountHolder = paymentInfo?.AccountHolder ?? "Chưa cập nhật",
                    AccountNumber = paymentInfo?.AccountNumber ?? "Chưa cập nhật",
                    BankName = paymentInfo?.BankName ?? "Chưa cập nhật",
                    Branch = paymentInfo?.Branch ?? "Chưa cập nhật"
                };
            }).ToList();

            return View(result);
        }


        [HttpPost]
        public async Task<IActionResult> ExportPaymentReportToPdf(int eventId)
        {
            var eventEntity = await _context.Events
                .Where(e => e.Id == eventId)
                .Include(e => e.Tickets)
                    .ThenInclude(t => t.OrderDetails)
                        .ThenInclude(od => od.Order)
                .Include(e => e.PaymentInfos)
                .Include(e => e.Creator)
                .FirstOrDefaultAsync();

            if (eventEntity == null) return NotFound();

            var successfulOrders = eventEntity.Tickets
                .SelectMany(t => t.OrderDetails)
                .Where(od => od.Order != null && od.Order.PaymentStatus == "Thanh toán thành công");
            var totalRevenue = successfulOrders.Sum(od => od.Price * od.Quantity);
            var paymentDue = totalRevenue * 0.9m; // 90%

            var paymentInfo = eventEntity.PaymentInfos.FirstOrDefault();
            var creatorEmail = eventEntity.Creator?.Email ?? "Không có email";
            string fontPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "arialuni.ttf");
            BaseFont bf = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
            var normalFont = new iTextSharp.text.Font(bf, 10, iTextSharp.text.Font.NORMAL);
            var boldFont = new iTextSharp.text.Font(bf, 12, iTextSharp.text.Font.BOLD);
            var headerFont = new iTextSharp.text.Font(bf, 16, iTextSharp.text.Font.BOLD);

            using (var stream = new MemoryStream())
            {
                var doc = new Document(PageSize.A4.Rotate(), 10, 10, 10, 10);
                PdfWriter.GetInstance(doc, stream);
                doc.Open();

                doc.Add(new Paragraph("CÔNG TY TNHH QCONCERT", headerFont) { Alignment = Element.ALIGN_CENTER });
                doc.Add(new Paragraph("\n"));

                doc.Add(new Paragraph($"BÁO CÁO THANH TOÁN SỰ KIỆN: {eventEntity.Name}", boldFont) { Alignment = Element.ALIGN_CENTER });
                doc.Add(new Paragraph($"Ngày tổ chức: {eventEntity.Date:dd/MM/yyyy}", normalFont) { Alignment = Element.ALIGN_CENTER });
                doc.Add(new Paragraph($"Email nhà tổ chức: {creatorEmail}", normalFont) { Alignment = Element.ALIGN_CENTER });
                doc.Add(new Paragraph($"Ngày tạo báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm}", normalFont));
                doc.Add(new Paragraph("\n"));

                var table = new PdfPTable(9) { WidthPercentage = 100 };
                table.SetWidths(new float[] { 2.5f, 2f, 2f, 2.5f, 2.5f, 2.5f, 2.5f, 2.5f, 2.5f });

                string[] headers = {
            "Tên Sự Kiện", "Ngày Tổ Chức", "Nhà Tổ Chức", "Doanh Thu",
            "Số Tiền Thanh Toán (90%)", "Chủ Tài Khoản", "Số Tài Khoản", "Ngân Hàng", "Chi Nhánh"
        };
                foreach (var h in headers)
                    table.AddCell(new Phrase(h, boldFont));

                table.AddCell(new Phrase(eventEntity.Name, normalFont));
                table.AddCell(new Phrase(eventEntity.Date.ToString("dd/MM/yyyy"), normalFont));
                table.AddCell(new Phrase(eventEntity.OrganizerName, normalFont));
                table.AddCell(new Phrase(totalRevenue.ToString("C0", new System.Globalization.CultureInfo("vi-VN")), normalFont));
                table.AddCell(new Phrase(paymentDue.ToString("C0", new System.Globalization.CultureInfo("vi-VN")), normalFont));
                table.AddCell(new Phrase(paymentInfo?.AccountHolder ?? "Chưa cập nhật", normalFont));
                table.AddCell(new Phrase(paymentInfo?.AccountNumber ?? "Chưa cập nhật", normalFont));
                table.AddCell(new Phrase(paymentInfo?.BankName ?? "Chưa cập nhật", normalFont));
                table.AddCell(new Phrase(paymentInfo?.Branch ?? "Chưa cập nhật", normalFont));

                doc.Add(table);
                doc.Add(new Paragraph("\n"));
                var termss = new Paragraph(
    "\nĐiều khoản và điều kiện:\n" +
    "- Báo cáo này được lập dựa trên các giao dịch đã hoàn tất tính đến thời điểm tạo.\n" +
    "- Số tiền thanh toán tương ứng 90% tổng doanh thu từ vé sau khi đã trừ các khoản phí dịch vụ.\n" +
    "- Mọi sai sót nếu có, vui lòng liên hệ với công ty Qconcert trong vòng 7 ngày kể từ ngày lập báo cáo.\n",
    normalFont
);
                termss.SpacingBefore = 10f;
                termss.SpacingAfter = 20f;
                doc.Add(termss);
                var terms = new Paragraph("Lưu ý: Khi đã ký vào đây, bạn xác nhận rằng việc chuyển khoản đã được thực hiện thành công, "
                    + "email xác nhận thanh toán đã được gửi cho khách hàng và bạn đã nhận được phản hồi từ phía khách. "
                    + "Trong trường hợp có sai sót, bạn hoàn toàn chịu trách nhiệm về số tiền đã thanh toán.", normalFont);
                terms.SpacingBefore = 10f;
                terms.SpacingAfter = 20f;
                doc.Add(terms);

                var sigTable = new PdfPTable(2) { WidthPercentage = 100 };
                sigTable.SetWidths(new float[] { 50, 50 });

                var cellDirector = new PdfPCell();
                cellDirector.Border = Rectangle.NO_BORDER;
                cellDirector.AddElement(new Paragraph("Giám đốc", boldFont));
                cellDirector.AddElement(new Paragraph("\n\n\n"));
                cellDirector.AddElement(new Paragraph("(Ký và ghi rõ họ tên)", normalFont));
                cellDirector.AddElement(new Paragraph("Lê Minh Quang", normalFont));
                sigTable.AddCell(cellDirector);

                var cellConfirmer = new PdfPCell();
                cellConfirmer.Border = Rectangle.NO_BORDER;
                cellConfirmer.AddElement(new Paragraph("Người xác nhận", boldFont));
                cellConfirmer.AddElement(new Paragraph("\n\n\n"));
                cellConfirmer.AddElement(new Paragraph("(Ký và ghi rõ họ tên)", normalFont));
                cellConfirmer.AddElement(new Paragraph("...", normalFont));
                sigTable.AddCell(cellConfirmer);

                doc.Add(sigTable);
                doc.Close();

                return File(stream.ToArray(),
                            "application/pdf",
                            $"BaoCaoThanhToan_{eventEntity.Name}.pdf");
            }
        }






        [HttpPost]
        public async Task<IActionResult> MarkAsPaid(int eventId)
        {
            var e = await _context.Events.FindAsync(eventId);
            if (e == null) return NotFound();

            // Đánh dấu sự kiện là đã thanh toán
            e.IsPaid = true;
            await _context.SaveChangesAsync();

            // Lưu thông báo thành công vào TempData
            TempData["SuccessMessage"] = "Thanh toán đã được xác nhận thành công.";

            // Chuyển hướng về trang báo cáo
            return RedirectToAction("PaymentReport");
        }




    }

}