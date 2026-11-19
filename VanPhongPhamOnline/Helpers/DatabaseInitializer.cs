using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using VanPhongPhamOnline.Data;

namespace VanPhongPhamOnline.Helpers
{
    public class DatabaseInitializer : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DatabaseInitializer> _logger;

        public DatabaseInitializer(IServiceProvider serviceProvider, ILogger<DatabaseInitializer> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MultiShopContext>();

            try
            {
                _logger.LogInformation("Kiem tra va cap nhat co so du lieu tu dong...");

                var connection = context.Database.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
                    await connection.OpenAsync(cancellationToken);

                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    IF NOT EXISTS (
                        SELECT * FROM sys.columns 
                        WHERE Name = N'SoLuongTon' 
                        AND Object_ID = Object_ID(N'HangHoa'))
                    BEGIN
                        ALTER TABLE HangHoa ADD SoLuongTon INT NOT NULL DEFAULT 0;
                        PRINT 'Da them cot SoLuongTon vao HangHoa';
                    END
                    ELSE
                    BEGIN
                        PRINT 'Cot SoLuongTon da ton tai';
                    END";

                await cmd.ExecuteNonQueryAsync(cancellationToken);

                // Cap nhat du lieu mac du cho cac san pham cu (co ton kho)
                cmd.CommandText = "UPDATE HangHoa SET SoLuongTon = 100 WHERE SoLuongTon = 0";
                await cmd.ExecuteNonQueryAsync(cancellationToken);

                // Them cot MaVoucher va GiamGiaVoucher vao HoaDon neu chua co
                cmd.CommandText = @"
                    IF NOT EXISTS (
                        SELECT * FROM sys.columns 
                        WHERE Name = N'MaVoucher' 
                        AND Object_ID = Object_ID(N'HoaDon'))
                    BEGIN
                        ALTER TABLE HoaDon ADD MaVoucher NVARCHAR(50);
                        PRINT 'Da them cot MaVoucher vao HoaDon';
                    END
                    ELSE
                    BEGIN
                        PRINT 'Cot MaVoucher da ton tai';
                    END";

                await cmd.ExecuteNonQueryAsync(cancellationToken);

                cmd.CommandText = @"
                    IF NOT EXISTS (
                        SELECT * FROM sys.columns 
                        WHERE Name = N'GiamGiaVoucher' 
                        AND Object_ID = Object_ID(N'HoaDon'))
                    BEGIN
                        ALTER TABLE HoaDon ADD GiamGiaVoucher FLOAT NOT NULL DEFAULT 0;
                        PRINT 'Da them cot GiamGiaVoucher vao HoaDon';
                    END
                    ELSE
                    BEGIN
                        PRINT 'Cot GiamGiaVoucher da ton tai';
                    END";

                await cmd.ExecuteNonQueryAsync(cancellationToken);

                // Tao bang Voucher neu chua co
                cmd.CommandText = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Voucher]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE Voucher (
                            MaVoucher NVARCHAR(50) NOT NULL PRIMARY KEY,
                            TenVoucher NVARCHAR(100) NOT NULL,
                            MaGiamGia NVARCHAR(20) NOT NULL,
                            LoaiGiamGia INT NOT NULL,
                            GiaTriGiam FLOAT NOT NULL,
                            NgayBatDau DATETIME NOT NULL,
                            NgayKetThuc DATETIME NOT NULL,
                            SoLuong INT NOT NULL,
                            DieuKienToiThieu FLOAT NOT NULL DEFAULT 0,
                            TrangThai BIT NOT NULL DEFAULT 1,
                            NgayTao DATETIME NOT NULL DEFAULT GETDATE()
                        );
                        PRINT 'Da bang tao Voucher';
                    END
                    ELSE
                    BEGIN
                        PRINT 'Bang Voucher da ton tai';
                    END";

                await cmd.ExecuteNonQueryAsync(cancellationToken);

                // Them cot Slung, MetaTitle, MetaDescription vao HangHoa neu chua co
                cmd.CommandText = @"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'Slug' AND Object_ID = Object_ID(N'HangHoa'))
                    BEGIN
                        ALTER TABLE HangHoa ADD Slug NVARCHAR(100);
                        PRINT 'Da them cot Slug vao HangHoa';
                    END
                    ELSE
                    BEGIN
                        PRINT 'Cot Slug da ton tai trong HangHoa';
                    END";

                await cmd.ExecuteNonQueryAsync(cancellationToken);

                cmd.CommandText = @"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'MetaTitle' AND Object_ID = Object_ID(N'HangHoa'))
                    BEGIN
                        ALTER TABLE HangHoa ADD MetaTitle NVARCHAR(200);
                        PRINT 'Da them cot MetaTitle vao HangHoa';
                    END
                    ELSE
                    BEGIN
                        PRINT 'Cot MetaTitle da ton tai trong HangHoa';
                    END";

                await cmd.ExecuteNonQueryAsync(cancellationToken);

                cmd.CommandText = @"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'MetaDescription' AND Object_ID = Object_ID(N'HangHoa'))
                    BEGIN
                        ALTER TABLE HangHoa ADD MetaDescription NVARCHAR(500);
                        PRINT 'Da them cot MetaDescription vao HangHoa';
                    END
                    ELSE
                    BEGIN
                        PRINT 'Cot MetaDescription da ton tai trong HangHoa';
                    END";

                await cmd.ExecuteNonQueryAsync(cancellationToken);

                // Them cot Slung, MetaTitle, MetaDescription vao Loai neu chua co
                cmd.CommandText = @"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'Slug' AND Object_ID = Object_ID(N'Loai'))
                    BEGIN
                        ALTER TABLE Loai ADD Slug NVARCHAR(100);
                        PRINT 'Da them cot Slug vao Loai';
                    END
                    ELSE
                    BEGIN
                        PRINT 'Cot Slug da ton tai trong Loai';
                    END";

                await cmd.ExecuteNonQueryAsync(cancellationToken);

                cmd.CommandText = @"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'MetaTitle' AND Object_ID = Object_ID(N'Loai'))
                    BEGIN
                        ALTER TABLE Loai ADD MetaTitle NVARCHAR(200);
                        PRINT 'Da them cot MetaTitle vao Loai';
                    END
                    ELSE
                    BEGIN
                        PRINT 'Cot MetaTitle da ton tai trong Loai';
                    END";

                await cmd.ExecuteNonQueryAsync(cancellationToken);

                cmd.CommandText = @"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'MetaDescription' AND Object_ID = Object_ID(N'Loai'))
                    BEGIN
                        ALTER TABLE Loai ADD MetaDescription NVARCHAR(500);
                        PRINT 'Da them cot MetaDescription vao Loai';
                    END
                    ELSE
                    BEGIN
                        PRINT 'Cot MetaDescription da ton tai trong Loai';
                    END";

                await cmd.ExecuteNonQueryAsync(cancellationToken);

                _logger.LogInformation("Khoi tao co so du lieu hoan tat");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi khi khoi tao co so du lieu: {Message}", ex.Message);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}