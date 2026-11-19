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