using Microsoft.EntityFrameworkCore;
using FitTime.Models;

namespace FitTime.Data;

public class FitTimeDbContext : DbContext
{
    public FitTimeDbContext(DbContextOptions<FitTimeDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<MembershipType> MembershipTypes => Set<MembershipType>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<ClassType> ClassTypes => Set<ClassType>();
    public DbSet<Class> Classes => Set<Class>();
    public DbSet<ClassEnrollment> ClassEnrollments => Set<ClassEnrollment>();
    public DbSet<Attendance> Attendances => Set<Attendance>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(e =>
        {
            e.ToTable("roles");
            e.Property(r => r.Id).HasColumnName("id");
            e.Property(r => r.Name).HasColumnName("name");
            e.Property(r => r.Description).HasColumnName("description");
        });

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.Property(u => u.Id).HasColumnName("id");
            e.Property(u => u.Login).HasColumnName("login");
            e.Property(u => u.PasswordHash).HasColumnName("password_hash");
            e.Property(u => u.RoleId).HasColumnName("role_id");
            e.Property(u => u.FirstName).HasColumnName("first_name");
            e.Property(u => u.LastName).HasColumnName("last_name");
            e.Property(u => u.Patronymic).HasColumnName("patronymic");
            e.Property(u => u.Phone).HasColumnName("phone");
            e.Property(u => u.Email).HasColumnName("email");
            e.Property(u => u.Specialization).HasColumnName("specialization");
            e.Property(u => u.IsActive).HasColumnName("is_active");
            e.Property(u => u.FailedAttempts).HasColumnName("failed_attempts");
            e.Property(u => u.LastLoginAt).HasColumnName("last_login_at");
            e.Property(u => u.CreatedAt).HasColumnName("created_at");
            e.HasOne(u => u.Role).WithMany(r => r.Users).HasForeignKey(u => u.RoleId);
            e.Ignore(u => u.FullName);
            e.Ignore(u => u.ShortName);
        });

        modelBuilder.Entity<Client>(e =>
        {
            e.ToTable("clients");
            e.Property(c => c.Id).HasColumnName("id");
            e.Property(c => c.FirstName).HasColumnName("first_name");
            e.Property(c => c.LastName).HasColumnName("last_name");
            e.Property(c => c.Patronymic).HasColumnName("patronymic");
            e.Property(c => c.BirthDate).HasColumnName("birth_date");
            e.Property(c => c.Phone).HasColumnName("phone");
            e.Property(c => c.Email).HasColumnName("email");
            e.Property(c => c.Notes).HasColumnName("notes");
            e.Property(c => c.IsActive).HasColumnName("is_active");
            e.Property(c => c.CreatedAt).HasColumnName("created_at");
            e.Property(c => c.UpdatedAt).HasColumnName("updated_at");
            e.Ignore(c => c.FullName);
            e.Ignore(c => c.ShortName);
            e.Ignore(c => c.Initials);
        });

        modelBuilder.Entity<MembershipType>(e =>
        {
            e.ToTable("membership_types");
            e.Property(mt => mt.Id).HasColumnName("id");
            e.Property(mt => mt.Name).HasColumnName("name");
            e.Property(mt => mt.DurationDays).HasColumnName("duration_days");
            e.Property(mt => mt.IsUnlimited).HasColumnName("is_unlimited");
            e.Property(mt => mt.VisitCount).HasColumnName("visit_count");
            e.Property(mt => mt.Price).HasColumnName("price");
            e.Property(mt => mt.Description).HasColumnName("description");
            e.Property(mt => mt.IsArchived).HasColumnName("is_archived");
            e.Property(mt => mt.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<Membership>(e =>
        {
            e.ToTable("memberships");
            e.Property(m => m.Id).HasColumnName("id");
            e.Property(m => m.ClientId).HasColumnName("client_id");
            e.Property(m => m.MembershipTypeId).HasColumnName("membership_type_id");
            e.Property(m => m.StartDate).HasColumnName("start_date");
            e.Property(m => m.EndDate).HasColumnName("end_date");
            e.Property(m => m.IsUnlimited).HasColumnName("is_unlimited");
            e.Property(m => m.VisitsRemaining).HasColumnName("visits_remaining");
            e.Property(m => m.IsActive).HasColumnName("is_active");
            e.Property(m => m.SoldByUserId).HasColumnName("sold_by_user_id");
            e.Property(m => m.Price).HasColumnName("price");
            e.Property(m => m.Notes).HasColumnName("notes");
            e.Property(m => m.CreatedAt).HasColumnName("created_at");
            e.HasOne(m => m.Client).WithMany(c => c.Memberships).HasForeignKey(m => m.ClientId);
            e.HasOne(m => m.MembershipType).WithMany(mt => mt.Memberships).HasForeignKey(m => m.MembershipTypeId);
            e.HasOne(m => m.SoldByUser).WithMany().HasForeignKey(m => m.SoldByUserId);
        });

        modelBuilder.Entity<ClassType>(e =>
        {
            e.ToTable("class_types");
            e.Property(ct => ct.Id).HasColumnName("id");
            e.Property(ct => ct.Name).HasColumnName("name");
            e.Property(ct => ct.Description).HasColumnName("description");
            e.Property(ct => ct.Color).HasColumnName("color");
            e.Property(ct => ct.IsActive).HasColumnName("is_active");
        });

        modelBuilder.Entity<Class>(e =>
        {
            e.ToTable("classes");
            e.Property(c => c.Id).HasColumnName("id");
            e.Property(c => c.ClassTypeId).HasColumnName("class_type_id");
            e.Property(c => c.TrainerId).HasColumnName("trainer_id");
            e.Property(c => c.Room).HasColumnName("room");
            e.Property(c => c.StartTime).HasColumnName("start_time");
            e.Property(c => c.EndTime).HasColumnName("end_time");
            e.Property(c => c.MaxParticipants).HasColumnName("max_participants");
            e.Property(c => c.Status).HasColumnName("status");
            e.Property(c => c.Notes).HasColumnName("notes");
            e.Property(c => c.CreatedByUserId).HasColumnName("created_by_user_id");
            e.Property(c => c.CreatedAt).HasColumnName("created_at");
            e.HasOne(c => c.ClassType).WithMany(ct => ct.Classes).HasForeignKey(c => c.ClassTypeId);
            e.HasOne(c => c.Trainer).WithMany().HasForeignKey(c => c.TrainerId);
            e.HasOne(c => c.CreatedByUser).WithMany().HasForeignKey(c => c.CreatedByUserId);
        });

        modelBuilder.Entity<ClassEnrollment>(e =>
        {
            e.ToTable("class_enrollments");
            e.Property(ce => ce.Id).HasColumnName("id");
            e.Property(ce => ce.ClassId).HasColumnName("class_id");
            e.Property(ce => ce.ClientId).HasColumnName("client_id");
            e.Property(ce => ce.MembershipId).HasColumnName("membership_id");
            e.Property(ce => ce.EnrolledAt).HasColumnName("enrolled_at");
            e.Property(ce => ce.Status).HasColumnName("status");
            e.HasOne(ce => ce.Class).WithMany(c => c.Enrollments).HasForeignKey(ce => ce.ClassId);
            e.HasOne(ce => ce.Client).WithMany(c => c.Enrollments).HasForeignKey(ce => ce.ClientId);
            e.HasOne(ce => ce.Membership).WithMany().HasForeignKey(ce => ce.MembershipId);
            e.HasIndex(ce => new { ce.ClassId, ce.ClientId }).IsUnique();
        });

        modelBuilder.Entity<Attendance>(e =>
        {
            e.ToTable("attendance");
            e.Property(a => a.Id).HasColumnName("id");
            e.Property(a => a.ClassId).HasColumnName("class_id");
            e.Property(a => a.ClientId).HasColumnName("client_id");
            e.Property(a => a.MembershipId).HasColumnName("membership_id");
            e.Property(a => a.CheckedInAt).HasColumnName("checked_in_at");
            e.Property(a => a.CheckedInByUserId).HasColumnName("checked_in_by_user_id");
            e.Property(a => a.Status).HasColumnName("status");
            e.Property(a => a.Notes).HasColumnName("notes");
            e.HasOne(a => a.Class).WithMany(c => c.Attendances).HasForeignKey(a => a.ClassId);
            e.HasOne(a => a.Client).WithMany(c => c.Attendances).HasForeignKey(a => a.ClientId);
            e.HasOne(a => a.Membership).WithMany().HasForeignKey(a => a.MembershipId);
            e.HasOne(a => a.CheckedInByUser).WithMany().HasForeignKey(a => a.CheckedInByUserId);
        });
    }
}
