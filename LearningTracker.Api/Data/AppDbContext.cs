using LearningTracker.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LearningTracker.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Book> Books { get; set; }
    public DbSet<BookUnit> BookUnits { get; set; }
    public DbSet<Goal> Goals { get; set; }
    public DbSet<GoalBook> GoalBooks { get; set; }
    public DbSet<ProgressEntry> ProgressEntries { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<GroupMember> GroupMembers { get; set; }
    public DbSet<GroupGoal> GroupGoals { get; set; }
    public DbSet<GroupGoalBook> GroupGoalBooks { get; set; }
    public DbSet<GroupGoalMember> GroupGoalMembers { get; set; }
    public DbSet<GroupProgressEntry> GroupProgressEntries { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users", "dbo");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(512);
            entity.Property(x => x.FullName).HasMaxLength(256).IsRequired();
            entity.Property(x => x.GoogleId).HasMaxLength(256);
            entity.Property(x => x.ProfilePicture).HasMaxLength(1024);
            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories", "dbo");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
            entity.Property(x => x.L1Name).HasMaxLength(128).IsRequired();
            entity.Property(x => x.L2Name).HasMaxLength(128).IsRequired();
            entity.Property(x => x.UnitName).HasMaxLength(128).IsRequired();
            entity.HasOne(x => x.CreatedBy)
                  .WithMany(u => u.CreatedCategories)
                  .HasForeignKey(x => x.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Book>(entity =>
        {
            entity.ToTable("Books", "dbo");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
            entity.Property(x => x.SeriesName).HasMaxLength(256);
            entity.HasOne(x => x.Category)
                  .WithMany(c => c.Books)
                  .HasForeignKey(x => x.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedBy)
                  .WithMany(u => u.CreatedBooks)
                  .HasForeignKey(x => x.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BookUnit>(entity =>
        {
            entity.ToTable("BookUnits", "dbo");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.L1Label).HasMaxLength(128).IsRequired();
            entity.Property(x => x.UnitLabel).HasMaxLength(128).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(256).IsRequired();
            entity.HasOne(x => x.Book)
                  .WithMany(b => b.Units)
                  .HasForeignKey(x => x.BookId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Goal>(entity =>
        {
            entity.ToTable("Goals", "dbo");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(512).IsRequired();
            entity.Property(x => x.DailyPace).HasColumnType("decimal(10,2)");
            entity.HasOne(x => x.User)
                  .WithMany(u => u.Goals)
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Category)
                  .WithMany(c => c.Goals)
                  .HasForeignKey(x => x.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired(false);
            entity.HasOne(x => x.StartUnit)
                  .WithMany(u => u.GoalsAsStart)
                  .HasForeignKey(x => x.StartUnitId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired(false);
        });

        modelBuilder.Entity<GoalBook>(entity =>
        {
            entity.ToTable("GoalBooks", "dbo");
            entity.HasKey(x => new { x.GoalId, x.BookId });
            entity.HasOne(x => x.Goal)
                  .WithMany(g => g.GoalBooks)
                  .HasForeignKey(x => x.GoalId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Book)
                  .WithMany(b => b.GoalBooks)
                  .HasForeignKey(x => x.BookId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProgressEntry>(entity =>
        {
            entity.ToTable("ProgressEntries", "dbo");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Note).HasMaxLength(1024);
            entity.HasOne(x => x.Goal)
                  .WithMany(g => g.ProgressEntries)
                  .HasForeignKey(x => x.GoalId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User)
                  .WithMany(u => u.ProgressEntries)
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Book)
                  .WithMany(b => b.ProgressEntries)
                  .HasForeignKey(x => x.BookId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.FromUnit)
                  .WithMany(u => u.ProgressEntriesAsFrom)
                  .HasForeignKey(x => x.FromUnitId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ToUnit)
                  .WithMany(u => u.ProgressEntriesAsTo)
                  .HasForeignKey(x => x.ToUnitId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.ToTable("Groups", "dbo");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1024);
            entity.Property(x => x.ProfilePicture).HasMaxLength(1024);
            entity.Property(x => x.InviteCode).HasMaxLength(32).IsRequired();
            entity.HasIndex(x => x.InviteCode).IsUnique();
            entity.HasOne(x => x.CreatedBy)
                  .WithMany(u => u.CreatedGroups)
                  .HasForeignKey(x => x.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GroupMember>(entity =>
        {
            entity.ToTable("GroupMembers", "dbo");
            entity.HasKey(x => new { x.GroupId, x.UserId });
            entity.Property(x => x.Role).HasMaxLength(32).IsRequired();
            entity.HasOne(x => x.Group)
                  .WithMany(g => g.Members)
                  .HasForeignKey(x => x.GroupId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User)
                  .WithMany(u => u.GroupMembers)
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GroupGoal>(entity =>
        {
            entity.ToTable("GroupGoals", "dbo");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(512).IsRequired();
            entity.HasOne(x => x.Group)
                  .WithMany(g => g.GroupGoals)
                  .HasForeignKey(x => x.GroupId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Category)
                  .WithMany(c => c.GroupGoals)
                  .HasForeignKey(x => x.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired(false);
            entity.HasOne(x => x.CollectiveTargetUnit)
                  .WithMany(u => u.GroupGoalsAsCollectiveTarget)
                  .HasForeignKey(x => x.CollectiveTargetUnitId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired(false);
            entity.HasOne(x => x.StartUnit)
                  .WithMany()
                  .HasForeignKey(x => x.StartUnitId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired(false);
            entity.HasOne(x => x.CreatedBy)
                  .WithMany(u => u.CreatedGroupGoals)
                  .HasForeignKey(x => x.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GroupGoalBook>(entity =>
        {
            entity.ToTable("GroupGoalBooks", "dbo");
            entity.HasKey(x => new { x.GroupGoalId, x.BookId });
            entity.HasOne(x => x.GroupGoal)
                  .WithMany(gg => gg.GroupGoalBooks)
                  .HasForeignKey(x => x.GroupGoalId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Book)
                  .WithMany(b => b.GroupGoalBooks)
                  .HasForeignKey(x => x.BookId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GroupGoalMember>(entity =>
        {
            entity.ToTable("GroupGoalMembers", "dbo");
            entity.HasKey(x => new { x.GroupGoalId, x.UserId });
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.HasOne(x => x.GroupGoal)
                  .WithMany(gg => gg.GroupGoalMembers)
                  .HasForeignKey(x => x.GroupGoalId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User)
                  .WithMany(u => u.GroupGoalMembers)
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens", "dbo");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Token).HasMaxLength(256).IsRequired();
            entity.HasIndex(x => x.Token).IsUnique();
            entity.HasOne(x => x.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.ToTable("PasswordResetTokens", "dbo");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasOne(x => x.User)
                  .WithMany()
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GroupProgressEntry>(entity =>
        {
            entity.ToTable("GroupProgressEntries", "dbo");
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.GroupGoal)
                  .WithMany(gg => gg.ProgressEntries)
                  .HasForeignKey(x => x.GroupGoalId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User)
                  .WithMany(u => u.GroupProgressEntries)
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Book)
                  .WithMany(b => b.GroupProgressEntries)
                  .HasForeignKey(x => x.BookId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Unit)
                  .WithMany(u => u.GroupProgressEntries)
                  .HasForeignKey(x => x.UnitId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
