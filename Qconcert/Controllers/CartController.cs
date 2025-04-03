using Microsoft.AspNetCore.Mvc;
using Qconcert.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Qconcert.Controllers
{
    public class CartController : Controller
    {
        private readonly TicketBoxDb1Context _context;

        public CartController(TicketBoxDb1Context context)
        {
            _context = context;
        }

        // Hi?n th? gi? h�ng
        [Authorize]
        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }
        [Authorize]
        [HttpPost]
        public IActionResult AddToCart([FromBody] CartItemDto model)
        {
            var ticket = _context.Tickets.FirstOrDefault(t => t.Id == model.TicketId);
            if (ticket == null)
            {
                return NotFound(new { message = "Vé không tồn tại" });
            }

            var cart = GetCart();
            var cartItem = cart.FirstOrDefault(c => c.Ticket.Id == model.TicketId);

            if (cartItem == null && model.Quantity > 0)
            {
                cart.Add(new CartItem { Ticket = ticket, Quantity = model.Quantity });
            }
            else if (cartItem != null)
            {
                if (model.Quantity > 0)
                {
                    cartItem.Quantity = model.Quantity; // Cập nhật số lượng mới
                }
                else
                {
                    cart.Remove(cartItem); // Xóa vé nếu số lượng là 0
                }
            }

            SaveCart(cart);

            return Ok(cart.Select(c => new
            {
                ticketId = c.Ticket.Id,
                ticketName = c.Ticket.LoaiVe,
                quantity = c.Quantity,
                price = c.Ticket.Price
            }));
        }


        public class CartItemDto
        {
            public int TicketId { get; set; }
            public int Quantity { get; set; }
        }

        [Authorize]
        [HttpPost]
        public IActionResult RemoveFromCart([FromBody] CartRequest request)
        {
            var cart = GetCart();
            var cartItem = cart.FirstOrDefault(c => c.Ticket.Id == request.TicketId);

            if (cartItem != null)
            {
                cart.Remove(cartItem);
                SaveCart(cart);
            }

            // Lọc bỏ vé null trước khi trả về
            var updatedCart = cart.Where(c => c != null).ToList();

            return Json(new
            {
                message = "Vé đã được xóa khỏi giỏ hàng",
                updatedCart = updatedCart
            });
        }



        [Authorize]
        [HttpPost]
        public IActionResult Update([FromBody] CartRequest request)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.Ticket.Id == request.TicketId);

            if (item != null)
            {
                if (request.Quantity == 0)
                {
                    cart.Remove(item); // Xóa vé nếu số lượng là 0
                }
                else
                {
                    item.Quantity = request.Quantity; // Cập nhật số lượng
                }
            }
            else if (request.Quantity > 0)
            {
                var ticket = _context.Tickets.FirstOrDefault(t => t.Id == request.TicketId);
                if (ticket != null)
                {
                    cart.Add(new CartItem
                    {
                        Ticket = ticket,
                        Quantity = request.Quantity
                    });
                }
            }

            SaveCart(cart);

            // Trả về giỏ hàng dưới dạng JSON
            return Json(cart.Select(c => new
            {
                ticketId = c.Ticket.Id,
                ticketName = c.Ticket.LoaiVe,
                quantity = c.Quantity,
                price = c.Ticket.Price
            }));
        }

        [Authorize]
        [HttpPost]
        public IActionResult Checkout(string userId, string email)
        {
            // Lấy giỏ hàng từ session
            var cart = GetCart();

            if (cart == null || !cart.Any())
            {
                return BadRequest(new { message = "Giỏ hàng trống" });
            }

            // Lấy thông tin sự kiện từ vé đầu tiên trong giỏ hàng
            var eventId = cart.First().Ticket.EventId;
            var @event = _context.Events.FirstOrDefault(e => e.Id == eventId);

            if (@event == null)
            {
                return NotFound(new { message = "Sự kiện không tồn tại" });
            }

            // Tính tổng tiền
            decimal totalAmount = cart.Sum(c => c.Ticket.Price * c.Quantity);

            // Tạo đơn hàng
            var order = new Order
            {
                UserId = userId,
                EventId = eventId.ToString(),
                OrderDate = DateTime.Now,
                Email = email,
                TotalAmount = totalAmount,
                PaymentMethod = "Thanh toán momo", // Gán giá trị mặc định cho phương thức thanh toán
                PaymentStatus = "Chưa thanh toán" // Gán giá trị mặc định cho trạng thái thanh toán
            };

            _context.Orders.Add(order);
            _context.SaveChanges();

            // Lưu chi tiết đơn hàng
            foreach (var cartItem in cart)
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = order.Id,
                    TicketId = cartItem.Ticket.Id,
                    Quantity = cartItem.Quantity,
                    Price = cartItem.Ticket.Price
                };

                _context.OrderDetails.Add(orderDetail);
            }

            _context.SaveChanges();

            // Xóa giỏ hàng sau khi thanh toán
            SaveCart(new List<CartItem>());

            // Chuyển hướng đến trang xác nhận
            return RedirectToAction("Confirmation", "Orders", new { id = order.Id });
        }

        private List<CartItem> GetCart()
        {
            return HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
        }
        [Authorize]
        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetObjectAsJson("Cart", cart);
        }

        public class CartRequest
        {
            public int TicketId { get; set; }
            public int Quantity { get; set; }
        }

        [Authorize]
        public IActionResult Details(int id)
        {
            var @event = _context.Events
                .Include(e => e.Tickets) // Bao gồm danh sách vé
                .FirstOrDefault(e => e.Id == id);

            if (@event == null)
            {
                return NotFound();
            }

            return View(@event); // Trả về View 'Details.cshtml'
        }

        public class CartItem
        {
            public Ticket Ticket { get; set; }
            public int Quantity { get; set; }
        }
    }
}
