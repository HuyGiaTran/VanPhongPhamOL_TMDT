using System;
using System.ComponentModel.DataAnnotations;

namespace VanPhongPhamOnline.Data
{
    public class Voucher
    {
        [Key]
        [MaxLength(50)]
        public string MaVoucher { get; set; } = null!;

        [Required(ErrorMessage = "Tên voucher là bắt buộc")]
        [MaxLength(100)]
        public string TenVoucher { get; set; } = null!;

        [Required(ErrorMessage = "Mã giảm giá là bắt buộc")]
        [MaxLength(20)]
        public string MaGiamGia { get; set; } = null!;

        [Required(ErrorMessage = "Loại giảm giá là bắt buộc")]
        public int LoaiGiamGia { get; set; } // 0 = Phần trăm (%), 1 = Tiền mặt (VNĐ)

        [Required(ErrorMessage = "Giá trị giảm là bắt buộc")]
        public double GiaTriGiam { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
        public DateTime NgayBatDau { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc là bắt buộc")]
        public DateTime NgayKetThuc { get; set; }

        [Required(ErrorMessage = "Số lượng là bắt buộc")]
        public int SoLuong { get; set; }

        public double DieuKienToiThieu { get; set; } = 0;

        public bool TrangThai { get; set; } = true;

        public DateTime NgayTao { get; set; } = DateTime.Now;
    }
}