using Qconcert.Models;
using System;
using System.Collections.Generic;

namespace Qconcert.Services
{
    public class TicketService
    {
        private readonly TicketBoxDb1Context _context;

        public TicketService(TicketBoxDb1Context context)
        {
            _context = context;
        }

        public void CreateTicket(Ticket ticket)
        {
            _context.Tickets.Add(ticket);
            _context.SaveChanges();
        }

        public void CreateTickets(Event @event, int numberOfTickets, decimal price, DateTime startTime, DateTime endTime, string ticketType)
        {
            var tickets = new List<Ticket>();

            for (int i = 0; i < numberOfTickets; i++)
            {
                tickets.Add(new Ticket
                {
                    EventId = @event.Id,
                    LoaiVe = ticketType,
                    SoLuongGhe = numberOfTickets,
                    SoVeToiThieuTrongMotDonHang = 1,
                    SoVeToiDaTrongMotDonHang = 10,
                    ThoiGianBatDauBanVe = startTime,
                    ThoiGianKetThucBanVe = endTime,
                    ThongTinVe = "Thông tin vé " + (i + 1),
                    HinhAnhVe = null, // Cần cập nhật nếu có hình ảnh
                    Price = price,
                    CreatedAt = DateTime.UtcNow
                });
            }

            _context.Tickets.AddRange(tickets);
            _context.SaveChanges();
        }
    }
}

