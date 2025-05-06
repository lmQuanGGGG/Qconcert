using Microsoft.AspNetCore.Mvc;
using Qconcert.Models;
using Qconcert.Services;
using System.IO;
using System.Threading.Tasks;

namespace Qconcert.Controllers
{
    public class TicketController : Controller
    {
        private readonly TicketBoxDb1Context _context;
        private readonly TicketService _ticketService;

        public TicketController(TicketBoxDb1Context context, TicketService ticketService)
        {
            _context = context;
            _ticketService = ticketService;
        }

       public IActionResult Create(int eventId)
{
    var model = new Ticket
    {
        EventId = eventId,
        CreatedAt = DateTime.Now, // Gán thời gian tạo mặc định
        ThoiGianBatDauBanVe = DateTime.Now, // Cần cập nhật
        ThoiGianKetThucBanVe = DateTime.Now, // Cần cập nhật
    };

    ViewBag.Tickets = _context.Tickets.Where(t => t.EventId == eventId).ToList();
    return View(model);
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(Ticket model, IFormFile HinhAnhVe)
{
    var @event = _context.Events.Find(model.EventId);
    if (@event == null)
    {
        return NotFound();
    }

    if (HinhAnhVe != null && HinhAnhVe.Length > 0)
    {
        using (var memoryStream = new MemoryStream())
        {
            await HinhAnhVe.CopyToAsync(memoryStream);
            model.HinhAnhVe = memoryStream.ToArray();
        }
    }
    model.SoLuongConLai = model.SoLuongGhe; // Gán số lượng còn lại bằng số lượng ghế
    model.CreatedAt = DateTime.Now; // Thiết lập thời gian tạo mặc định

    // Thêm vé vào database
    _ticketService.CreateTicket(model);

    TempData["SuccessMessage"] = "Tạo vé thành công.";
    return RedirectToAction("Create", new { eventId = model.EventId });
}

public IActionResult Edit(int id)
{
            var ticket = _context.Tickets.Find(id);
            if (ticket == null)
            {
                return NotFound();
            }
            return PartialView("_EditTicketPartial", ticket);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Ticket model, IFormFile HinhAnhVe)
        {
       
            {
                var ticket = await _context.Tickets.FindAsync(model.Id);
                if (ticket == null)
                {
                    return NotFound();
                }

                ticket.LoaiVe = model.LoaiVe;
                ticket.Price = model.Price;
                ticket.SoLuongGhe = model.SoLuongGhe;
                ticket.SoVeToiThieuTrongMotDonHang = model.SoVeToiThieuTrongMotDonHang;
                ticket.SoVeToiDaTrongMotDonHang = model.SoVeToiDaTrongMotDonHang;
                ticket.ThoiGianBatDauBanVe = model.ThoiGianBatDauBanVe;
                ticket.ThoiGianKetThucBanVe = model.ThoiGianKetThucBanVe;
                ticket.ThongTinVe = model.ThongTinVe;

                if (HinhAnhVe != null && HinhAnhVe.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await HinhAnhVe.CopyToAsync(memoryStream);
                        ticket.HinhAnhVe = memoryStream.ToArray();
                    }
                }

                _context.Update(ticket);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cập nhật vé thành công.";
                return RedirectToAction("Create", new { eventId = ticket.EventId });
            }

            return PartialView("_EditTicketPartial", model);
        }


        public IActionResult Delete(int id)
{
    var ticket = _context.Tickets.Find(id);
    if (ticket == null)
    {
        return NotFound();
    }
    _context.Tickets.Remove(ticket);
    _context.SaveChanges();

    TempData["SuccessMessage"] = "Xóa vé thành công.";
    return RedirectToAction("Create", new { eventId = ticket.EventId });
}


    }
}

