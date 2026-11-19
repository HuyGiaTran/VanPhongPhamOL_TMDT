using BCrypt.Net;

namespace VanPhongPhamOnline.Helpers
{
    public static class BcryptHelper
    {
        /// <summary>
        /// Băm mật khẩu bằng bcrypt (khuyên dùng cho mật khẩu mới).
        /// </summary>
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        /// <summary>
        /// Kiểm tra mật khẩu nhập vào có khớp với hash đã lưu hay không.
        /// Tự động nhận diện bcrypt (bắt đầu bằng $2a$, $2b$, $2c$) hoặc MD5 (dữ liệu cũ).
        /// </summary>
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
                return false;

            // Nếu là hash bcrypt (bắt đầu bằng $2a$, $2b$, $2c...)
            if (hashedPassword.StartsWith("$2"))
            {
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }

            // Ngược lại: là hash MD5 cũ (dữ liệu chưa migrate)
            // Sử dụng ToMd5Hash cũ với RandomKey (cần truyền saltKey riêng nếu biết)
            // Ở đây ta trả về false để bắt buộc người dùng phải reset mật khẩu
            // hoặc có thể gọi ToMd5Hash nếu có sẵn RandomKey
            return false;
        }

        /// <summary>
        /// Kiểm tra xem một chuỗi hash có phải là bcrypt hay không.
        /// </summary>
        public static bool IsBcryptHash(string hashedPassword)
        {
            return !string.IsNullOrEmpty(hashedPassword) && hashedPassword.StartsWith("$2");
        }
    }
}