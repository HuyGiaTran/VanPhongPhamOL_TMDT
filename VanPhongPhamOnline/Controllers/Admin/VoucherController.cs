using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VanPhongPhamOnline.Data;
using VanPhongPhamOnline.Helpers;

namespace VanPhongPhamOnline.Controllers.Admin
{
    [Authorize(Policy = AppAuth.AdminPolicy)]
    public class VoucherController : Controller
    {
        private const string ViewFolder = "~/Views/Admin/Voucher/";

        private readonly MultiShopContext _context;

        public VoucherController(MultiShopContext context)
        {
            _context = context;
        }

        // GET: Voucher
        public async Task<IActionResult> Index()
        {
            return await DanhSachVoucher(null);
        }

        // GET: Voucher/Search?query=...
        public async Task<IActionResult> Search(string? query)
        {
            return await DanhSachVoucher(query);
        }

        private async Task<IActionResult> DanhSachVoucher(string? query)
        {
            var vouchers = _context.Vouchers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                query = query.Trim();
                vouchers = vouchers.Where(p => p.TenVoucher.Contains(query) || p.MaGiamGia.Contains(query));
            }

            ViewBag.Query = query;

            return View(ViewFolder + "Index.cshtml", await vouchers.OrderByDescending(v => v.NgayTao).ToListAsync());
        }

        // GET: Voucher/Details/5
        public async Task<IActionResult> Details(string? id)
        {
            if (id == null)
                return NotFound();

            var voucher = await _context.Vouchers.FirstOrDefaultAsync(m => m.MaVoucher == id);
            if (voucher == null)
                return NotFound();

            return View(ViewFolder + "Details.cshtml", voucher);
        }

        // GET: Voucher/Create
        public IActionResult Create()
        {
            return View(ViewFolder + "Create.cshtml", new Voucher
            {
                NgayBatDau = DateTime.Today,
                NgayKetThuc = DateTime.Today.AddDays(30),
                TrangThai = true
            });
        }

        // POST: Voucher/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Voucher model)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Vouchers.AnyAsync(v => v.MaVoucher == model.MaVoucher))
                {
                    ModelState.AddModelError(nameof(model.MaVoucher), "Mã voucher đã tồn tại.");
                    return View(ViewFolder + "Create.cshtml", model);
                }

                if (await _context.Vouchers.AnyAsync(v => v.MaGiamGia == model.MaGiamGia))
                {
                    ModelState.AddModelError(nameof(model.MaGiamGia), "Mã giảm giá đã tồn tại.");
                    return View(ViewFolder + "Create.cshtml", model);
                }

                if (model.NgayKetThuc <= model.NgayBatDau)
                {
                    ModelState.AddModelError(nameof(model.NgayKetThuc), "Ngày kết thúc phải sau ngày bắt đầu.");
                    return View(ViewFolder + "Create.cshtml", model);
                }

                _context.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thêm voucher thành công!";
                return RedirectToAction(nameof(Index));
            }

            return View(ViewFolder + "Create.cshtml", model);
        }

        // GET: Voucher/Edit/5
        public async Task<IActionResult> Edit(string? id)
        {
            if (id == null)
                return NotFound();

            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null)
                return NotFound();

            return View(ViewFolder + "Edit.cshtml", voucher);
        }

        // POST: Voucher/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Voucher model)
        {
            if (id != model.MaVoucher)
                return NotFound();

            if (ModelState.IsValid)
            {
                if (await _context.Vouchers.AnyAsync(v => v.MaGiamGia == model.MaGiamGia && v.MaVoucher != model.MaVoucher))
                {
                    ModelState.AddModelError(nameof(model.MaGiamGia), "Mã giảm giá đã tồn tại.");
                    return View(ViewFolder + "Edit.cshtml", model);
                }

                if (model.NgayKetThuc <= model.NgayBatDau)
                {
                    ModelState.AddModelError(nameof(model.NgayKetThuc), "Ngày kết thúc phải sau ngày bắt đầu.");
                    return View(ViewFolder + "Edit.cshtml", model);
                }

                var existingVoucher = await _context.Vouchers.FindAsync(id);
                if (existingVoucher == null)
                    return NotFound();

                existingVoucher.TenVoucher = model.TenVoucher;
                existingVoucher.MaGiamGia = model.MaGiamGia;
                existingVoucher.LoaiGiamGia = model.LoaiGiamGia;
                existingVoucher.GiaTriGiam = model.GiaTriGiam;
                existingVoucher.NgayBatDau = model.NgayBatDau;
                existingVoucher.NgayKetThuc = model.NgayKetThuc;
                existingVoucher.SoLuong = model.SoLuong;
                existingVoucher.DieuKienToiThieu = model.DieuKienToiThieu;
                existingVoucher.TrangThai = model.TrangThai;

                try
                {
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật voucher thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.GetBaseException().Message);
                }
            }

            return View(ViewFolder + "Edit.cshtml", model);
        }

        // GET: Voucher/Delete/5
        public async Task<IActionResult> Delete(string? id)
        {
            if (id == null)
                return NotFound();

            var voucher = await _context.Vouchers.FirstOrDefaultAsync(m => m.MaVoucher == id);
            if (voucher == null)
                return NotFound();

            return View(ViewFolder + "Delete.cshtml", voucher);
        }

        // POST: Voucher/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy voucher!";
                return RedirectToAction(nameof(Index));
            }

            _context.Vouchers.Remove(voucher);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Xóa voucher thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}