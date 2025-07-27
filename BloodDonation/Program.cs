using Microsoft.EntityFrameworkCore;
using BloodDonation.Data;
using BloodDonation.Repositories.Interfaces;
using BloodDonation.Repositories;
using BloodDonation.Models;

namespace BloodDonation
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Connect DbContext
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Register Repository
            builder.Services.AddScoped<IDonorRepository, DonorRepository>();

            // Add Session & Cache
            builder.Services.AddDistributedMemoryCache(); // Must have Session
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // Session timeout
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Add SMTP settings manually
            builder.Services.Configure<SmtpSettings>(options =>
            {
                options.Host = "smtp.gmail.com";
                options.Port = 587;
                options.EnableSsl = true;
                options.UserName = "vohoangthang2004@gmail.com";
                options.Password = "lwvgdgghfyorgabk";
                options.DisplayName = "BloodDonation";
            });

            // Add MVC
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Middleware fix error
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // Activate session middleware
            app.UseSession();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
