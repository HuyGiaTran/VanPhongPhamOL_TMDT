using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VanPhongPhamOnline.Data;
using VanPhongPhamOnline.Helpers;

namespace VanPhongPhamOnline.Controllers.Admin
{
    [Authorize(Policy = AppAuth.AdminPolicy)]
    public class LoaiController : Controller
    {
        private const string ViewFolder = "~/Views/Admin/Loai/";

        private readonly MultiShopContext _context;

        public LoaiController(MultiShopContext context)
        {
            _context = context;
        }

        // GET: Loai
        public async Task<IActionResult> Index()
        {
            return await DanhSachLoai(null);
        }

        // GET: Loai/Search?query=...
        public async Task<IActionResult> Search(string? query)
        {
            return await DanhSachLoai(query);
        }

        private async Task<IActionResult> DanhSachLoai(string? query)
        {
            var loais = _context.Loais
                .Include(l => l.HangHoas)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                query = query.Trim();
                loais = loais.Where(p => p.TenLoai.Contains(query));
            }

            ViewBag.Query = query;

            return View(ViewFolder + "Index.cshtml", await loais.OrderBy(l => l.MaLoai).ToListAsync());
        }

        // GET: Loai/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var loai = await _context.Loais
                .Include(l => l.HangHoas)
                .FirstOrDefaultAsync(m => m.MaLoai == id);

            if (loai == null)
                return NotFound();

            return View(ViewFolder + "Details.cshtml", loai);
        }

        // GET: Loai/Create
        public IActionResult Create()
        {
            return View(ViewFolder + "Create.cshtml", new Loai());
        }

        // POST: Loai/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Loai model, IFormFile? HinhUpload)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra trùng tên loại
                if (await _context.Loais.AnyAsync(l => l.TenLoai == model.TenLoai))
                {
                    ModelState.AddModelError(nameof(model.TenLoai), "Tên danh mục đã tồn tại.");
                    return View(ViewFolder + "Create.cshtml", model);
                }

                if (HinhUpload != null && HinhUpload.Length > 0)
                {
                    model.Hinh = MyUlti.UploadHinh(HinhUpload, "Loai");
                }

                _context.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thêm danh mục thành công!";
                return RedirectToAction(nameof(Index));
            }

            return View(ViewFolder + "Create.cshtml", model);
        }

        // GET: Loai/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var loai = await _context.Loais.FindAsync(id);
            if (loai == null)
                return NotFound();

            return View(ViewFolder + "Edit.cshtml", loai);
        }

        // POST: Loai/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Loai model, IFormFile? HinhUpload)
        {
            if (id != model.MaLoai)
                return NotFound();

            if (ModelState.IsValid)
            {
                // Kiểm tra trùng tên loại (trừ chính nó)
                if (await _context.Loais.AnyAsync(l => l.TenLoai == model.TenLoai && l.MaLoai != model.MaLoai))
                {
                    ModelState.AddModelError(nameof(model.TenLoai), "Tên danh mục đã tồn tại.");
                    return View(ViewFolder + "Edit.cshtml", model);
                }

                var existingLoai = await _context.Loais.FindAsync(id);
                if (existingLoai == null)
                    return NotFound();

                existingLoai.TenLoai = model.TenLoai;
                existingLoai.TenLoaiAlias = model.TenLoaiAlias;
                existingLoai.MoTa = model.MoTa;

                if (HinhUpload != null && HinhUpload.Length > 0)
                {
                    // Xóa ảnh cũ nếu có
                    if (!string.IsNullOrEmpty(existingLoai.Hinh))
                    {
                        var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Hinh", "Loai", existingLoai.Hinh);
                        if (System.IO.File.Exists(oldPath))
                            System.IO.File.Delete(oldPath);
                    }
                    existingLoai.Hinh = MyUlti.UploadHinh(HinhUpload, "Loai");
                }

                try
                {
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật danh mục thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.GetBaseException().Message);
                }
            }

            return View(ViewFolder + "Edit.cshtml", model);
        }

        // GET: Loai/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var loai = await _context.Loais
                .Include(l => l.HangHoas)
                .FirstOrDefaultAsync(m => m.MaLoai == id);

            if (loai == null)
                return NotFound();

            return View(ViewFolder + "Delete.cshtml", loai);
        }

        // POST: Loai/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var loai = await _context.Loais
                .Include(l => l.HangHoas)
                .FirstOrDefaultAsync(l => l.MaLoai == id);

            if (loai == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy danh mục!";
                return RedirectToAction(nameof(Index));
            }

            // Không cho phép xóa nếu đang có sản phẩm thuộc danh mục này
            if (loai.HangHoas.Any())
            {
                TempData["ErrorMessage"] = "Không thể xóa danh mục này vì đang có sản phẩm sử dụng!";
                return RedirectToAction(nameof(Index));
            }

            // Xóa hình ảnh nếu có
            if (!string.IsNullOrEmpty(loai.Hinh))
            {
                var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Hinh", "Loai", loai.Hinh);
                if (System.IO.File.Exists(imagePath))
                    System.IO.File.Delete(imagePath);
            }

            _context.Loais.Remove(loai);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Xóa danh mục thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}