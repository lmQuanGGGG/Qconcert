using System.ComponentModel.DataAnnotations;

namespace Qconcert.Models
{
    public class Ticket
    {
        public int Id { get; set; } // Mã vé
        public int EventId { get; set; } // Mã sự kiện
        [Required(ErrorMessage = "Tổng số vé là bắt buộc.")]
        [Range(1, int.MaxValue, ErrorMessage = "Tổng số vé phải lớn hơn 0.")]
        public int SoLuongGhe { get; set; } // Tổng số lượng vé
        [Required(ErrorMessage = "Số vé tối thiểu là bắt buộc.")]
        [Range(1, int.MaxValue, ErrorMessage = "Số vé tối thiểu phải lớn hơn 0.")]
        public int SoVeToiThieuTrongMotDonHang { get; set; } // Số vé tối thiểu trong một đơn hàng
        [Required(ErrorMessage = "Số vé tối đa là bắt buộc.")]
        [Range(1, int.MaxValue, ErrorMessage = "Số vé tối đa phải lớn hơn 0.")]
        public int SoVeToiDaTrongMotDonHang { get; set; } // Số vé tối đa trong một đơn hàng
        [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc.")]
        public DateTime ThoiGianBatDauBanVe { get; set; } // Thời gian bắt đầu bán vé
        [Required(ErrorMessage = "Thời gian kết thúc là bắt buộc.")]
        [Compare(nameof(ThoiGianBatDauBanVe), ErrorMessage = "Thời gian kết thúc phải lớn hơn thời gian bắt đầu.")]
        public DateTime ThoiGianKetThucBanVe { get; set; } // Thời gian kết thúc bán vé
        [Required(ErrorMessage = "Thông tin vé là bắt buộc.")]
        public string ThongTinVe { get; set; } = null!; // Thông tin vé
        [Required(ErrorMessage = "Hình ảnh vé là bắt buộc.")]
        public byte[] HinhAnhVe { get; set; } = null!; // Hình ảnh vé
        [Required(ErrorMessage = "Giá vé là bắt buộc.")]
        [Range(1, double.MaxValue, ErrorMessage = "Giá vé phải lớn hơn 0.")]
        public decimal Price { get; set; } // Giá vé
        [Required(ErrorMessage = "Loại vé là bắt buộc.")]
        public string LoaiVe { get; set; } = null!; // Loại vé

        public DateTime? CreatedAt { get; set; }
    }
}
