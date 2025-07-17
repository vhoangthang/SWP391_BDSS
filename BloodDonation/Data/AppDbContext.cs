using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Drawing;
using BloodDonation.Models;

namespace BloodDonation.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        //DB information here
        public DbSet<BloodBank> BloodBanks { get; set; }
        public DbSet<MedicalCenter> MedicalCenters { get; set; }
        public DbSet<BloodType> BloodTypes { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Donor> Donors { get; set; }
        public DbSet<BloodRequest> BloodRequests { get; set; }
        public DbSet<DonorBloodRequest> DonorBloodRequests { get; set; }
        public DbSet<DonationAppointment> DonationAppointments { get; set; }
        public DbSet<DonationCertificate> DonationCertificates { get; set; }
        public DbSet<BloodInventory> BloodInventories { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<HealthSurvey> HealthSurveys { get; set; }

        public DbSet<News> News { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Optional: Fluent API configs can go here if needed
            modelBuilder.Entity<Account>()
                .HasOne(a => a.Donor)
                .WithOne(d => d.Account)
                .HasForeignKey<Donor>(d => d.AccountID);

            modelBuilder.Entity<DonationAppointment>()
                .HasOne(a => a.DonationCertificate)
                .WithOne(c => c.Appointment)
                .HasForeignKey<DonationCertificate>(c => c.AppointmentID);
        }

    }
}
