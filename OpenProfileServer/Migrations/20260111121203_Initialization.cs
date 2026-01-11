using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenProfileServer.Migrations
{
    /// <inheritdoc />
    public partial class Initialization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    RecoveryStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastLogin = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SiteMetadata",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SiteName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    SiteDescription = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Copyright = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ContactEmail = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Logo_Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Logo_Value = table.Column<string>(type: "TEXT", nullable: true),
                    Logo_Tag = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Favicon_Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Favicon_Value = table.Column<string>(type: "TEXT", nullable: true),
                    Favicon_Tag = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteMetadata", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Asset_Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Asset_Value = table.Column<string>(type: "TEXT", nullable: true),
                    Asset_Tag = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    ValueType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "VerificationCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Identifier = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccountAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Asset_Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Asset_Value = table.Column<string>(type: "TEXT", nullable: true),
                    Asset_Tag = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountAssets_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccountBlocks",
                columns: table => new
                {
                    BlockerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BlockedId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountBlocks", x => new { x.BlockerId, x.BlockedId });
                    table.ForeignKey(
                        name: "FK_AccountBlocks_Accounts_BlockedId",
                        column: x => x.BlockedId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccountBlocks_Accounts_BlockerId",
                        column: x => x.BlockerId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccountCredentials",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    PasswordSalt = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    SecurityStamp = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountCredentials", x => x.AccountId);
                    table.ForeignKey(
                        name: "FK_AccountCredentials_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccountEmails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    IsPrimary = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsVerified = table.Column<bool>(type: "INTEGER", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountEmails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountEmails_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccountFollowers",
                columns: table => new
                {
                    FollowerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FollowingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountFollowers", x => new { x.FollowerId, x.FollowingId });
                    table.ForeignKey(
                        name: "FK_AccountFollowers_Accounts_FollowerId",
                        column: x => x.FollowerId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccountFollowers_Accounts_FollowingId",
                        column: x => x.FollowingId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccountSecurities",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsTwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    TotpSecret = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    BackupCodes = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountSecurities", x => x.AccountId);
                    table.ForeignKey(
                        name: "FK_AccountSecurities_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccountSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    DefaultVisibility = table.Column<int>(type: "INTEGER", nullable: false),
                    AllowFollowers = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShowFollowingList = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShowFollowersList = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountSettings_Accounts_Id",
                        column: x => x.Id,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RecipientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    Data = table.Column<string>(type: "TEXT", nullable: true),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Accounts_RecipientId",
                        column: x => x.RecipientId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Content = table.Column<string>(type: "TEXT", nullable: true),
                    Location = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    TimeZone = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Website = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Avatar_Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Avatar_Value = table.Column<string>(type: "TEXT", nullable: true),
                    Avatar_Tag = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Background_Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Background_Value = table.Column<string>(type: "TEXT", nullable: true),
                    Background_Tag = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Profiles_Accounts_Id",
                        column: x => x.Id,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Token = table.Column<string>(type: "TEXT", nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeviceInfo = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CallbackUrl = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    WebhookSecret = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    RateLimitQuota = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationSettings_AccountSettings_Id",
                        column: x => x.Id,
                        principalTable: "AccountSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DefaultMemberVisibility = table.Column<int>(type: "INTEGER", nullable: false),
                    AllowMemberInvite = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationSettings_AccountSettings_Id",
                        column: x => x.Id,
                        principalTable: "AccountSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonalSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ShowLocalTime = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonalSettings_AccountSettings_Id",
                        column: x => x.Id,
                        principalTable: "AccountSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeveloperAccountId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TermsOfServiceUrl = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationProfiles_Profiles_Id",
                        column: x => x.Id,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Certificates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Fingerprint = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Content = table.Column<string>(type: "TEXT", maxLength: 65536, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Certificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Certificates_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContactMethods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Icon_Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Icon_Value = table.Column<string>(type: "TEXT", nullable: true),
                    Icon_Tag = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Image_Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Image_Value = table.Column<string>(type: "TEXT", nullable: true),
                    Image_Tag = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactMethods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactMethods_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GalleryItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Image_Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Image_Value = table.Column<string>(type: "TEXT", nullable: true),
                    Image_Tag = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Caption = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ActionUrl = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GalleryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GalleryItems_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FoundedDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    TaxId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationProfiles_Profiles_Id",
                        column: x => x.Id,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonalProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Pronouns = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    Birthday = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    JobTitle = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    CurrentCompany = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    CurrentSchool = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonalProfiles_Profiles_Id",
                        column: x => x.Id,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Content = table.Column<string>(type: "TEXT", maxLength: 16384, nullable: true),
                    Url = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Logo_Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Logo_Value = table.Column<string>(type: "TEXT", nullable: true),
                    Logo_Tag = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SocialLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Platform = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Icon_Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Icon_Value = table.Column<string>(type: "TEXT", nullable: true),
                    Icon_Tag = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SocialLinks_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SponsorshipItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Platform = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Icon_Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Icon_Value = table.Column<string>(type: "TEXT", nullable: true),
                    Icon_Tag = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    QrCode_Type = table.Column<int>(type: "INTEGER", nullable: false),
                    QrCode_Value = table.Column<string>(type: "TEXT", nullable: true),
                    QrCode_Tag = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SponsorshipItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SponsorshipItems_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InviterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InviteeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationInvitations_Accounts_InviteeId",
                        column: x => x.InviteeId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrganizationInvitations_Accounts_InviterId",
                        column: x => x.InviterId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationInvitations_OrganizationProfiles_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "OrganizationProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationMembers_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrganizationMembers_OrganizationProfiles_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "OrganizationProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EducationExperiences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PersonalProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SchoolName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Degree = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Major = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Logo_Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Logo_Value = table.Column<string>(type: "TEXT", nullable: true),
                    Logo_Tag = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EducationExperiences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EducationExperiences_PersonalProfiles_PersonalProfileId",
                        column: x => x.PersonalProfileId,
                        principalTable: "PersonalProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkExperiences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PersonalProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CompanyName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Position = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Logo_Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Logo_Value = table.Column<string>(type: "TEXT", nullable: true),
                    Logo_Tag = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkExperiences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkExperiences_PersonalProfiles_PersonalProfileId",
                        column: x => x.PersonalProfileId,
                        principalTable: "PersonalProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountAssets_AccountId",
                table: "AccountAssets",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountAssets_Visibility",
                table: "AccountAssets",
                column: "Visibility");

            migrationBuilder.CreateIndex(
                name: "IX_AccountBlocks_BlockedId",
                table: "AccountBlocks",
                column: "BlockedId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountEmails_AccountId",
                table: "AccountEmails",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountEmails_Email",
                table: "AccountEmails",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_AccountFollowers_FollowingId",
                table: "AccountFollowers",
                column: "FollowingId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_AccountName",
                table: "Accounts",
                column: "AccountName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_ProfileId",
                table: "Certificates",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactMethods_ProfileId",
                table: "ContactMethods",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_EducationExperiences_PersonalProfileId",
                table: "EducationExperiences",
                column: "PersonalProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_GalleryItems_ProfileId",
                table: "GalleryItems",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedAt",
                table: "Notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IsRead",
                table: "Notifications",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientId",
                table: "Notifications",
                column: "RecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationInvitations_InviteeId",
                table: "OrganizationInvitations",
                column: "InviteeId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationInvitations_InviterId",
                table: "OrganizationInvitations",
                column: "InviterId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationInvitations_OrganizationId",
                table: "OrganizationInvitations",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMembers_AccountId",
                table: "OrganizationMembers",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMembers_OrganizationId_AccountId",
                table: "OrganizationMembers",
                columns: new[] { "OrganizationId", "AccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProfileId",
                table: "Projects",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_AccountId",
                table: "RefreshTokens",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialLinks_ProfileId",
                table: "SocialLinks",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorshipItems_ProfileId",
                table: "SponsorshipItems",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemAssets_Visibility",
                table: "SystemAssets",
                column: "Visibility");

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Key",
                table: "SystemSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VerificationCodes_ExpiresAt",
                table: "VerificationCodes",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationCodes_Identifier_Type_Code",
                table: "VerificationCodes",
                columns: new[] { "Identifier", "Type", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkExperiences_PersonalProfileId",
                table: "WorkExperiences",
                column: "PersonalProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountAssets");

            migrationBuilder.DropTable(
                name: "AccountBlocks");

            migrationBuilder.DropTable(
                name: "AccountCredentials");

            migrationBuilder.DropTable(
                name: "AccountEmails");

            migrationBuilder.DropTable(
                name: "AccountFollowers");

            migrationBuilder.DropTable(
                name: "AccountSecurities");

            migrationBuilder.DropTable(
                name: "ApplicationProfiles");

            migrationBuilder.DropTable(
                name: "ApplicationSettings");

            migrationBuilder.DropTable(
                name: "Certificates");

            migrationBuilder.DropTable(
                name: "ContactMethods");

            migrationBuilder.DropTable(
                name: "EducationExperiences");

            migrationBuilder.DropTable(
                name: "GalleryItems");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "OrganizationInvitations");

            migrationBuilder.DropTable(
                name: "OrganizationMembers");

            migrationBuilder.DropTable(
                name: "OrganizationSettings");

            migrationBuilder.DropTable(
                name: "PersonalSettings");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "SiteMetadata");

            migrationBuilder.DropTable(
                name: "SocialLinks");

            migrationBuilder.DropTable(
                name: "SponsorshipItems");

            migrationBuilder.DropTable(
                name: "SystemAssets");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "VerificationCodes");

            migrationBuilder.DropTable(
                name: "WorkExperiences");

            migrationBuilder.DropTable(
                name: "OrganizationProfiles");

            migrationBuilder.DropTable(
                name: "AccountSettings");

            migrationBuilder.DropTable(
                name: "PersonalProfiles");

            migrationBuilder.DropTable(
                name: "Profiles");

            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
