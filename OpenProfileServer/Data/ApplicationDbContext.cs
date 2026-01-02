using Microsoft.EntityFrameworkCore;
using OpenProfileServer.Models.Entities;
using OpenProfileServer.Models.Entities.Auth;
using OpenProfileServer.Models.Entities.Base;
using OpenProfileServer.Models.Entities.Details;
using OpenProfileServer.Models.Entities.Profiles;
using OpenProfileServer.Models.Entities.Settings;
using OpenProfileServer.Models.ValueObjects;

namespace OpenProfileServer.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // ==========================================
    // Core Accounts & Social
    // ==========================================
    public DbSet<Account> Accounts { get; set; }
    public DbSet<AccountFollower> AccountFollowers { get; set; }
    public DbSet<AccountBlock> AccountBlocks { get; set; } // Added
    public DbSet<AccountEmail> AccountEmails { get; set; }
    public DbSet<AccountCredential> AccountCredentials { get; set; }
    public DbSet<AccountSecurity> AccountSecurities { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<OrganizationMember> OrganizationMembers { get; set; }

    // ==========================================
    // Profiles (Polymorphic TPT)
    // ==========================================
    public DbSet<Profile> Profiles { get; set; }
    public DbSet<PersonalProfile> PersonalProfiles { get; set; }
    public DbSet<OrganizationProfile> OrganizationProfiles { get; set; }
    public DbSet<ApplicationProfile> ApplicationProfiles { get; set; }

    // ==========================================
    // Settings (Polymorphic TPT)
    // ==========================================
    public DbSet<AccountSettings> AccountSettings { get; set; }
    public DbSet<PersonalSettings> PersonalSettings { get; set; }
    public DbSet<OrganizationSettings> OrganizationSettings { get; set; }
    public DbSet<ApplicationSettings> ApplicationSettings { get; set; }
    public DbSet<SystemSetting> SystemSettings { get; set; }

    // ==========================================
    // Details & Collections
    // ==========================================
    public DbSet<Project> Projects { get; set; }
    public DbSet<GalleryItem> GalleryItems { get; set; }
    public DbSet<SponsorshipItem> SponsorshipItems { get; set; }
    public DbSet<Certificate> Certificates { get; set; }
    public DbSet<SocialLink> SocialLinks { get; set; }
    public DbSet<ContactMethod> ContactMethods { get; set; }
    
    // Personal Specific
    public DbSet<WorkExperience> WorkExperiences { get; set; }
    public DbSet<EducationExperience> EducationExperiences { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureAccounts(modelBuilder);
        ConfigureProfiles(modelBuilder);
        ConfigureSettings(modelBuilder);
        ConfigureDetails(modelBuilder);
        ConfigureSystemSettings(modelBuilder);
    }

    private static void ConfigureAccounts(ModelBuilder modelBuilder)
    {
        // Account Config
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasIndex(e => e.AccountName).IsUnique(); 
            
            // 1:1 Relationships
            entity.HasOne(a => a.Credential)
                .WithOne(c => c.Account)
                .HasForeignKey<AccountCredential>(c => c.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.Security)
                .WithOne(s => s.Account)
                .HasForeignKey<AccountSecurity>(s => s.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.Settings)
                .WithOne(s => s.Account)
                .HasForeignKey<AccountSettings>(s => s.Id)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.Profile)
                .WithOne(p => p.Account)
                .HasForeignKey<Profile>(p => p.Id) 
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Account Follower Config (Self-referencing Many-to-Many)
        modelBuilder.Entity<AccountFollower>(entity =>
        {
            entity.HasKey(f => new { f.FollowerId, f.FollowingId });

            entity.HasOne(f => f.Follower)
                .WithMany(a => a.Following)
                .HasForeignKey(f => f.FollowerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(f => f.Following)
                .WithMany(a => a.Followers)
                .HasForeignKey(f => f.FollowingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Account Block Config (Self-referencing Many-to-Many)
        modelBuilder.Entity<AccountBlock>(entity =>
        {
            entity.HasKey(b => new { b.BlockerId, b.BlockedId });

            entity.HasOne(b => b.Blocker)
                .WithMany(a => a.BlockedUsers)
                .HasForeignKey(b => b.BlockerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(b => b.Blocked)
                .WithMany(a => a.BlockedBy)
                .HasForeignKey(b => b.BlockedId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Email Config
        modelBuilder.Entity<AccountEmail>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Organization Member Config
        modelBuilder.Entity<OrganizationMember>(entity =>
        {
            entity.HasIndex(om => new { om.OrganizationId, om.AccountId }).IsUnique();

            entity.HasOne(om => om.Organization)
                .WithMany(op => op.Members)
                .HasForeignKey(om => om.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(om => om.Account)
                .WithMany(a => a.Memberships)
                .HasForeignKey(om => om.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureProfiles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Profile>().UseTptMappingStrategy();
        modelBuilder.Entity<PersonalProfile>().ToTable("PersonalProfiles");
        modelBuilder.Entity<OrganizationProfile>().ToTable("OrganizationProfiles");
        modelBuilder.Entity<ApplicationProfile>().ToTable("ApplicationProfiles");

        modelBuilder.Entity<Profile>(entity =>
        {
            entity.OwnsOne(p => p.Avatar, nav =>
            {
                nav.Property(p => p.Type).HasColumnName("Avatar_Type");
                nav.Property(p => p.Value).HasColumnName("Avatar_Value");
                nav.Property(p => p.Tag).HasColumnName("Avatar_Tag");
            });

            entity.OwnsOne(p => p.Background, nav =>
            {
                nav.Property(p => p.Type).HasColumnName("Background_Type");
                nav.Property(p => p.Value).HasColumnName("Background_Value");
                nav.Property(p => p.Tag).HasColumnName("Background_Tag");
            });
        });
    }

    private static void ConfigureSettings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountSettings>().UseTptMappingStrategy();
        modelBuilder.Entity<PersonalSettings>().ToTable("PersonalSettings");
        modelBuilder.Entity<OrganizationSettings>().ToTable("OrganizationSettings");
        modelBuilder.Entity<ApplicationSettings>().ToTable("ApplicationSettings");
    }

    private static void ConfigureSystemSettings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.HasIndex(s => s.Key).IsUnique();
        });
    }

    private static void ConfigureDetails(ModelBuilder modelBuilder)
    {
        // Sponsorship Item
        modelBuilder.Entity<SponsorshipItem>(entity =>
        {
            entity.OwnsOne(s => s.Icon, nav =>
            {
                nav.Property(p => p.Type).HasColumnName("Icon_Type");
                nav.Property(p => p.Value).HasColumnName("Icon_Value");
                nav.Property(p => p.Tag).HasColumnName("Icon_Tag");
            });
            
            entity.OwnsOne(s => s.QrCode, nav =>
            {
                nav.Property(p => p.Type).HasColumnName("QrCode_Type");
                nav.Property(p => p.Value).HasColumnName("QrCode_Value");
                nav.Property(p => p.Tag).HasColumnName("QrCode_Tag");
            });
        });

        // Contact Method
        modelBuilder.Entity<ContactMethod>(entity =>
        {
            entity.OwnsOne(c => c.Icon, nav =>
            {
                nav.Property(p => p.Type).HasColumnName("Icon_Type");
                nav.Property(p => p.Value).HasColumnName("Icon_Value");
                nav.Property(p => p.Tag).HasColumnName("Icon_Tag");
            });
            entity.OwnsOne(c => c.Image, nav =>
            {
                nav.Property(p => p.Type).HasColumnName("Image_Type");
                nav.Property(p => p.Value).HasColumnName("Image_Value");
                nav.Property(p => p.Tag).HasColumnName("Image_Tag");
            });
        });

        // Project
        modelBuilder.Entity<Project>(entity =>
        {
            entity.OwnsOne(p => p.Logo, nav =>
            {
                nav.Property(p => p.Type).HasColumnName("Logo_Type");
                nav.Property(p => p.Value).HasColumnName("Logo_Value");
                nav.Property(p => p.Tag).HasColumnName("Logo_Tag");
            });
        });

        // WorkExperience
        modelBuilder.Entity<WorkExperience>(entity =>
        {
            entity.OwnsOne(w => w.Logo, nav =>
            {
                nav.Property(p => p.Type).HasColumnName("Logo_Type");
                nav.Property(p => p.Value).HasColumnName("Logo_Value");
                nav.Property(p => p.Tag).HasColumnName("Logo_Tag");
            });
        });

        // EducationExperience
        modelBuilder.Entity<EducationExperience>(entity =>
        {
            entity.OwnsOne(e => e.Logo, nav =>
            {
                nav.Property(p => p.Type).HasColumnName("Logo_Type");
                nav.Property(p => p.Value).HasColumnName("Logo_Value");
                nav.Property(p => p.Tag).HasColumnName("Logo_Tag");
            });
        });
        
        // Gallery Item
        modelBuilder.Entity<GalleryItem>(entity =>
        {
            entity.OwnsOne(g => g.Image, nav =>
            {
                nav.Property(p => p.Type).HasColumnName("Image_Type");
                nav.Property(p => p.Value).HasColumnName("Image_Value");
                nav.Property(p => p.Tag).HasColumnName("Image_Tag");
            });
        });
        
        // Social Link
        modelBuilder.Entity<SocialLink>(entity =>
        {
            entity.OwnsOne(s => s.Icon, nav =>
            {
                nav.Property(p => p.Type).HasColumnName("Icon_Type");
                nav.Property(p => p.Value).HasColumnName("Icon_Value");
                nav.Property(p => p.Tag).HasColumnName("Icon_Tag");
            });
        });
    }
}
