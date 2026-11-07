
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ThptA.Models;

namespace ThptA.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(
            DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // Các bảng quản lý hồ sơ học sinh
        public DbSet<AcademicYear> AcademicYears
            => Set<AcademicYear>();

        public DbSet<SchoolClass> SchoolClasses
            => Set<SchoolClass>();

        public DbSet<Student> Students
            => Set<Student>();

        public DbSet<PrimaryContact> PrimaryContacts
            => Set<PrimaryContact>();

        public DbSet<AuditLog> AuditLogs
            => Set<AuditLog>();

        protected override void OnModelCreating(
            ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 1. NĂM HỌC
            builder.Entity<AcademicYear>()
                .HasIndex(x => x.Code)
                .IsUnique();

            builder.Entity<AcademicYear>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Year_Dates",
                    "[EndDate] > [StartDate]"));

            // 2. LỚP HỌC
            builder.Entity<SchoolClass>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Class_Grade",
                    "[Grade] IN (10,11,12)"));

            builder.Entity<SchoolClass>()
                .HasIndex(x => new
                {
                    x.AcademicYearId,
                    x.Name
                })
                .IsUnique();

            // Một GVCN tối đa một lớp trong một năm học
            builder.Entity<SchoolClass>()
                .HasIndex(x => new
                {
                    x.AcademicYearId,
                    x.TeacherId
                })
                .IsUnique()
                .HasFilter("[TeacherId] IS NOT NULL");

            builder.Entity<SchoolClass>()
                .HasOne(x => x.AcademicYear)
                .WithMany()
                .HasForeignKey(x => x.AcademicYearId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SchoolClass>()
                .HasOne(x => x.Teacher)
                .WithMany()
                .HasForeignKey(x => x.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SchoolClass>()
                .Property(x => x.RowVersion)
                .IsRowVersion();

            // 3. HỒ SƠ HỌC SINH
            builder.Entity<Student>()
                .HasIndex(x => x.Code)
                .IsUnique();

            builder.Entity<Student>()
                .ToTable(t =>
                {
                    t.HasCheckConstraint(
                        "CK_Student_Gender",
                        "[Gender] IN ('M','F','O')");

                    t.HasCheckConstraint(
                        "CK_Student_Status",
                        "[Status] IN ('Active','Inactive')");
                });

            builder.Entity<Student>()
                .HasOne(x => x.SchoolClass)
                .WithMany(x => x.Students)
                .HasForeignKey(x => x.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Student>()
                .Property(x => x.RowVersion)
                .IsRowVersion();

            // 4. NGƯỜI LIÊN HỆ CHÍNH
            builder.Entity<PrimaryContact>()
                .HasKey(x => x.StudentId);

            builder.Entity<PrimaryContact>()
                .HasOne(x => x.Student)
                .WithOne(x => x.PrimaryContact)
                .HasForeignKey<PrimaryContact>(
                    x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // 5. NHẬT KÝ THAO TÁC
            builder.Entity<AuditLog>()
                .HasOne(x => x.Actor)
                .WithMany()
                .HasForeignKey(x => x.ActorId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
