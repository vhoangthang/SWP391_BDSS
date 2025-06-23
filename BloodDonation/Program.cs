using Microsoft.EntityFrameworkCore;
using BloodDonation.Data; // namespace chứa lớp MyDbContext
using BloodDonation.Repositories.Interfaces;
using BloodDonation.Repositories;

namespace BloodDonation
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ✅ Kết nối DbContext
            builder.Services.AddDbContext<MyDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // ✅ Đăng ký Repository
            builder.Services.AddScoped<IDonorRepository, DonorRepository>();

            // ✅ Thêm Session và cache
            builder.Services.AddDistributedMemoryCache(); // Bắt buộc cho Session
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // session timeout
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Thêm MVC
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Middleware xử lý lỗi
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // ✅ Kích hoạt session middleware (rất quan trọng)
            app.UseSession();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
