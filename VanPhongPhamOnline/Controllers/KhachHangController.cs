using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VanPhongPhamOnline.Data;
using VanPhongPhamOnline.Helpers;
using VanPhongPhamOnline.ViewModels;

namespace VanPhongPhamOnline.Controllers
{
    public class KhachHangController : Controller
    {
        private readonly MultiShopContext db;
        private readonly IMapper _mapper;
        private readonly MyUlti _ulti;
        public KhachHangController(MultiShopContext context, IMapper mapper, MyUlti ulti)
        {
            db = context;
            _mapper = mapper;
            _ulti = ulti;
        }

        #region Register
        [HttpGet]
        public IActionResult DangKy()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DangKy(RegisterVM model, IFormFile? Hinh)
        {
            if (!ModelState.IsValid)
            {
                // Thu thập tất cả lỗi validation để hiển thị
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                TempData["RegisterError"] = string.Join("; ", errors);
                return View(model);
            }

            if (db.KhachHangs.Any(kh => kh.MaKh == model.MaKh))
            {
                ModelState.AddModelError(nameof(model.MaKh), "Tên đăng nhập đã tồn tại.");
                TempData["RegisterError"] = "Tên đăng nhập đã tồn tại.";
                return View(model);
            }

            if (db.KhachHangs.Any(kh => kh.EmailKh == model.EmailKh))
            {
                ModelState.AddModelError(nameof(model.EmailKh), "Email đã được sử dụng.");
                TempData["RegisterError"] = "Email đã được sử dụng.";
                return View(model);
            }

            try
            {
                var khachHang = _mapper.Map<KhachHang>(model);
                khachHang.MaKh = model.MaKh;
                khachHang.RandomKeyKh = MyUlti.GenerateRandomKey();
                khachHang.MatKhauKh = BcryptHelper.HashPassword(model.MatKhauKh!);

                if (Hinh != null)
                {
                    khachHang.HinhKh = MyUlti.UploadHinh(Hinh, "KhachHang", khachHang.MaKh);
                }
                db.Add(khachHang);
                db.SaveChanges();

                TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("DangNhap");
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                ModelState.AddModelError("", "Lỗi khi lưu dữ liệu: " + errorMessage);
                TempData["RegisterError"] = "Lỗi khi lưu dữ liệu: " + errorMessage;
                return View(model);
            }
        }
        #endregion

        #region Login
        [HttpGet]
        public IActionResult DangNhap(string? ReturnUrl)
        {
            ViewBag.ReturnUrl = ReturnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DangNhap(LoginVM model, string? ReturnUrl)
        {
            ViewBag.ReturnUrl = ReturnUrl;

            if (!ModelState.IsValid)
            {
                return View();
            }

            var khachHang = db.KhachHangs.SingleOrDefault(kh => kh.EmailKh == model.UserName && !kh.IsDeleted);
            if (khachHang == null)
            {
                ModelState.AddModelError("", "Sai thông tin đăng nhập!");
                return View();
            }

            // Kiểm tra mật khẩu: hỗ trợ bcrypt (mới) và MD5 (cũ cho dữ liệu chưa migrate)
            bool passwordValid = false;
            if (!string.IsNullOrEmpty(khachHang.MatKhauKh))
            {
                // Nếu là hash bcrypt (bắt đầu bằng $2)
                if (khachHang.MatKhauKh.StartsWith("$2"))
                {
                    passwordValid = BCrypt.Net.BCrypt.Verify(model.Password, khachHang.MatKhauKh);
                }
                else
                {
                    // Hash MD5 cũ - kiểm tra tương thích
                    passwordValid = khachHang.MatKhauKh == model.Password.ToMd5Hash(khachHang.RandomKeyKh);
                    // Nếu đăng nhập thành công với MD5 cũ, tự động nâng cấp lên bcrypt
                    if (passwordValid)
                    {
                        khachHang.MatKhauKh = BcryptHelper.HashPassword(model.Password);
                        db.SaveChanges();
                    }
                }
            }

            if (!passwordValid)
            {
                ModelState.AddModelError("", "Sai thông tin đăng nhập!");
                return View();
            }

            // Tạo claims cho người dùng
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, khachHang.EmailKh),
                new Claim(ClaimTypes.Name, khachHang.HoTenKh),
                new Claim("MaKH", khachHang.MaKh),
                new Claim("HinhKH", khachHang.HinhKh ?? string.Empty),
                new Claim("DienThoaiKH", khachHang.DienThoaiKh ?? string.Empty),
                new Claim("DiaChi", khachHang.DiaChiKh ?? string.Empty),
                new Claim("GioiTinh", khachHang.GioiTinhKh ? "Nam" : "Nữ"),
                new Claim("NgaySinh", khachHang.NgaySinhKh.ToString("yyyy-MM-dd")),
                new Claim(ClaimTypes.Role, "Customer")
            };

            var claimsIdentity = new ClaimsIdentity(claims, AppAuth.CustomerScheme);
            await HttpContext.SignInAsync(AppAuth.CustomerScheme, new ClaimsPrincipal(claimsIdentity));

            // Điều hướng về URL cũ hoặc về trang chủ
            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }
            return RedirectToAction("Index", "HangHoa");
        }

        public IActionResult AccessDenied()
        {
            return View("~/Views/Shared/AccessDenied.cshtml");
        }
        #endregion

        #region Profile
        [Authorize]
        public IActionResult Profile()
        {
            var maKH = User.FindFirst("MaKH")?.Value;
            var kh = db.KhachHangs.FirstOrDefault(k => k.MaKh == maKH);
            if (kh == null) return NotFound();

            var profile = new
            {
                kh.HoTenKh,
                kh.EmailKh,
                kh.DienThoaiKh,
                kh.DiaChiKh,
                GioiTinhKh = kh.GioiTinhKh ? "Nam" : "Nữ",
                NgaySinhKh = kh.NgaySinhKh == DateTime.MinValue ? "Chưa cập nhật" : kh.NgaySinhKh.ToString("dd/MM/yyyy"),
                HinhKh = kh.HinhKh ?? string.Empty,
                MaKH = kh.MaKh
            };

            ViewBag.Profile = profile;
            return View();
        }

        [Authorize]
        [HttpGet]
        public IActionResult EditProfile()
        {
            var maKH = User.FindFirst("MaKH")?.Value;
            var kh = db.KhachHangs.FirstOrDefault(k => k.MaKh == maKH);
            if (kh == null) return NotFound();

            var model = new EditProfileVM
            {
                HoTen = kh.HoTenKh,
                DiaChi = kh.DiaChiKh,
                DienThoai = kh.DienThoaiKh,
                GioiTinh = kh.GioiTinhKh,
                NgaySinh = kh.NgaySinhKh,
                Email = kh.EmailKh
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditProfile(EditProfileVM model)
        {
            var maKH = User.FindFirst("MaKH")?.Value;
            var kh = db.KhachHangs.FirstOrDefault(k => k.MaKh == maKH);
            if (kh == null) return NotFound();

            if (!string.IsNullOrEmpty(model.Email) &&
                db.KhachHangs.Any(x => x.EmailKh == model.Email && x.MaKh != kh.MaKh))
            {
                ModelState.AddModelError(nameof(model.Email), "Email đã được sử dụng bởi tài khoản khác.");
            }

            if (ModelState.IsValid)
            {
                kh.HoTenKh = model.HoTen;
                kh.DiaChiKh = model.DiaChi;
                kh.DienThoaiKh = model.DienThoai;
                kh.GioiTinhKh = model.GioiTinh;
                kh.NgaySinhKh = model.NgaySinh ?? DateTime.MinValue;
                if (!string.IsNullOrEmpty(model.Email))
                {
                    kh.EmailKh = model.Email;
                }

                // Đổi mật khẩu nếu người dùng nhập mật khẩu mới
                if (!string.IsNullOrWhiteSpace(model.MatKhau))
                {
                    kh.MatKhauKh = BcryptHelper.HashPassword(model.MatKhau);
                }

                if (model.Hinh != null)
                {
                    kh.HinhKh = MyUlti.UploadHinh(model.Hinh, "KhachHang", kh.MaKh);
                }

                db.SaveChanges();
                return RedirectToAction("Profile");
            }

            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> DangXuat()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(AppAuth.CustomerScheme);
            return Redirect("/");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaTaiKhoan()
        {
            var maKH = User.FindFirst("MaKH")?.Value;
            var kh = db.KhachHangs.FirstOrDefault(x => x.MaKh == maKH);

            if (kh != null)
            {
                // Xóa hình đại diện đã upload
                if (!string.IsNullOrEmpty(kh.HinhKh))
                {
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Hinh", "KhachHang", kh.HinhKh);
                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }
                    kh.HinhKh = null;
                }

                kh.IsDeleted = true;
                db.SaveChanges();
            }

            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(AppAuth.CustomerScheme);
            return Redirect("/");
        }
        #endregion

        #region Forgot password
        [HttpGet]
        public IActionResult QuenMatKhau()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult QuenMatKhau(QuenMatKhauVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var kh = db.KhachHangs.FirstOrDefault(x => x.EmailKh == model.Email && !x.IsDeleted);
            if (kh == null)
            {
                ModelState.AddModelError("", "Email này chưa được đăng ký.");
                return View(model);
            }

            var otp = OtpHelper.Generate(HttpContext.Session, model.Email);

            var subject = "Xác nhận đặt lại mật khẩu - MultiShop";
            var content = $@"Xin chào {kh.HoTenKh}, mã OTP đặt lại mật khẩu của bạn là: <b>{otp}</b>. Mã có hiệu lực trong {OtpHelper.ExpireMinutes} phút. Đừng chia sẻ mã này cho bất kỳ ai.";
            _ulti.SendMail(kh.EmailKh, subject, content);

            TempData["Email"] = model.Email;
            return RedirectToAction("XacNhanMa");
        }

        [HttpGet]
        public IActionResult XacNhanMa()
        {
            var email = TempData["Email"]?.ToString();
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("QuenMatKhau");

            return View(new XacNhanMaVM { Email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XacNhanMa(XacNhanMaVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var error = OtpHelper.Verify(HttpContext.Session, model.Email, model.MaXacNhan);
            if (error != null)
            {
                ModelState.AddModelError("", error);
                return View(model);
            }

            TempData["Email"] = model.Email;
            return RedirectToAction("DatLaiMatKhau");
        }

        [HttpGet]
        public IActionResult DatLaiMatKhau()
        {
            var email = TempData["Email"]?.ToString();
            if (string.IsNullOrEmpty(email) || !OtpHelper.IsVerified(HttpContext.Session, email))
                return RedirectToAction("QuenMatKhau");

            return View(new DatLaiMatKhauVM { Email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DatLaiMatKhau(DatLaiMatKhauVM model)
        {
            // Chỉ cho phép đặt lại mật khẩu khi email đã xác nhận OTP thành công
            if (!OtpHelper.IsVerified(HttpContext.Session, model.Email))
                return RedirectToAction("QuenMatKhau");

            if (!ModelState.IsValid)
                return View(model);

            var kh = db.KhachHangs.FirstOrDefault(x => x.EmailKh == model.Email);
            if (kh == null)
            {
                ModelState.AddModelError("", "Không tìm thấy tài khoản.");
                return View(model);
            }

            kh.MatKhauKh = BcryptHelper.HashPassword(model.MatKhauMoi);
            db.SaveChanges();

            OtpHelper.Clear(HttpContext.Session);

            TempData["Success"] = "Mật khẩu đã được cập nhật. Vui lòng đăng nhập lại.";
            return RedirectToAction("DangNhap");
        }
        #endregion

        #region Order history
        #region Hủy đơn hàng
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HuyDon(int id)
        {
            var maKH = User.FindFirst("MaKH")?.Value;
            if (string.IsNullOrEmpty(maKH)) return Unauthorized();

            var hoaDon = db.HoaDons
                .Include(h => h.ChiTietHds)
                .FirstOrDefault(h => h.MaHd == id && h.MaKh == maKH);
            if (hoaDon == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng hoặc bạn không có quyền hủy đơn này.";
                return RedirectToAction("Details");
            }

            // Chỉ cho phép hủy khi trạng thái là "Chưa xác nhận" (MaTrangThai = 1)
            if (hoaDon.MaTrangThai != 1)
            {
                TempData["Error"] = "Chỉ có thể hủy đơn hàng khi trạng thái là 'Chưa xác nhận'.";
                return RedirectToAction("Details");
            }

            // Hoàn trả tồn kho cho từng sản phẩm trong đơn
            foreach (var ct in hoaDon.ChiTietHds)
            {
                var hangHoa = db.HangHoas.SingleOrDefault(p => p.MaHh == ct.MaHh);
                if (hangHoa != null)
                {
                    hangHoa.SoLuongTon += ct.SoLuong;
                }
            }

            // Cập nhật trạng thái đơn hàng thành "Đã hủy" (giả sử mã trạng thái 4 = Đã hủy, nếu chưa có sẽ dùng 3)
            // Tạm thời dùng MaTrangThai = 3 (cần thêm trạng thái "Đã hủy" vào CSDL)
            hoaDon.MaTrangThai = 3;

            db.SaveChanges();

            TempData["Success"] = $"Đã hủy đơn hàng #{hoaDon.MaHd} thành công. Tồn kho đã được hoàn trả.";
            return RedirectToAction("Details");
        }
        #endregion

        [Authorize]
        public IActionResult Details(int pageNumber = 1, int pageSize = 4)
        {
            var maKH = User.FindFirst("MaKH")?.Value;
            if (string.IsNullOrEmpty(maKH)) return Unauthorized();

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 4;

            var query = db.HoaDons
                .Include(h => h.ChiTietHds)
                    .ThenInclude(ct => ct.MaHhNavigation)
                .Include(h => h.MaTrangThaiNavigation)
                .Include(h => h.MaNvNavigation)
                .Where(h => h.MaKh == maKH)
                .OrderByDescending(h => h.NgayDat);

            // tổng số hóa đơn
            int totalRecords = query.Count();

            // lấy dữ liệu phân trang
            var hoaDons = query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var result = hoaDons.Select(h => new BillVM
            {
                MaHd = h.MaHd,
                MaKh = h.MaKh,
                TenKhachHang = User.Identity?.Name,
                NgayDat = h.NgayDat,
                NgayCan = h.NgayCan,
                NgayGiao = h.NgayGiao,
                HoTenNguoiNhan = h.HoTenNguoiNhan,
                DiaChi = h.DiaChi,
                CachThanhToan = h.CachThanhToan,
                CachVanChuyen = h.CachVanChuyen,
                PhiVanChuyen = h.PhiVanChuyen,
                TenTrangThai = h.MaTrangThaiNavigation?.TenTrangThai,
                MaTrangThai = h.MaTrangThai,
                TenNhanVien = h.MaNvNavigation?.HoTenNv,
                GhiChu = h.GhiChu,
                TongTien = h.TongTien,
                ChiTietHds = h.ChiTietHds.Select(ct => new BillDetailVM
                {
                    MaHh = ct.MaHh,
                    TenHh = ct.MaHhNavigation?.TenHh ?? "Không rõ",
                    DonGia = ct.DonGia,
                    SoLuong = ct.SoLuong
                }).ToList()
            }).ToList();

            // truyền thêm thông tin phân trang qua ViewBag
            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalRecords = totalRecords;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            return View("DetailsBill", result);
        }
        #endregion
    }
}
