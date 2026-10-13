using DAL.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DAL.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Venue> Venues => Set<Venue>();
        public DbSet<VenueCategory> VenueCategories => Set<VenueCategory>();
        public DbSet<VenueTag> VenueTags => Set<VenueTag>();
        public DbSet<VenueTagLink> VenueTagLinks => Set<VenueTagLink>();
        public DbSet<Amenity> Amenities => Set<Amenity>();
        public DbSet<VenueAmenityLink> VenueAmenityLinks => Set<VenueAmenityLink>();
        public DbSet<VenueOpeningHour> VenueOpeningHours => Set<VenueOpeningHour>();
        public DbSet<VenueHoursException> VenueHoursExceptions => Set<VenueHoursException>();
        public DbSet<VenuePhoto> VenuePhotos => Set<VenuePhoto>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Venue>(entity =>
            {
                entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Description).HasMaxLength(2000);
                entity.Property(x => x.Address).HasMaxLength(500).IsRequired();
                entity.Property(x => x.Latitude).HasPrecision(9, 6);
                entity.Property(x => x.Longitude).HasPrecision(9, 6);
                entity.Property(x => x.PriceMin).HasPrecision(12, 2);
                entity.Property(x => x.PriceMax).HasPrecision(12, 2);
                entity.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
                entity.Property(x => x.GooglePlaceId).HasMaxLength(200);
                entity.Property(x => x.WebsiteUrl).HasMaxLength(1000);
                entity.Property(x => x.MenuUrl).HasMaxLength(1000);
                entity.Property(x => x.PhoneNumber).HasMaxLength(30);
                entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
                entity.Property(x => x.PriceBasis).HasConversion<string>().HasMaxLength(20);
                entity.HasIndex(x => x.Status);
                entity.HasIndex(x => x.CategoryId);
                entity.HasOne(x => x.Category).WithMany(x => x.Venues).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<AppUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
                entity.ToTable(t => t.HasCheckConstraint("CK_Venues_Latitude", "[Latitude] >= -90 AND [Latitude] <= 90"));
                entity.ToTable(t => t.HasCheckConstraint("CK_Venues_Longitude", "[Longitude] >= -180 AND [Longitude] <= 180"));
                entity.ToTable(t => t.HasCheckConstraint("CK_Venues_PriceRange", "[PriceMin] IS NULL OR [PriceMax] IS NULL OR [PriceMin] <= [PriceMax]"));
            });

            builder.Entity<VenueCategory>(entity =>
            {
                entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
                entity.HasIndex(x => x.Code).IsUnique();
            });

            builder.Entity<VenueTag>(entity =>
            {
                entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
                entity.Property(x => x.TagType).HasConversion<string>().HasMaxLength(20);
                entity.HasIndex(x => x.Code).IsUnique();
            });

            builder.Entity<VenueTagLink>().HasKey(x => new { x.VenueId, x.VenueTagId });
            builder.Entity<VenueTagLink>().HasOne(x => x.Venue).WithMany(x => x.Tags).HasForeignKey(x => x.VenueId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<VenueTagLink>().HasOne(x => x.Tag).WithMany(x => x.Venues).HasForeignKey(x => x.VenueTagId).OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Amenity>(entity =>
            {
                entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
                entity.HasIndex(x => x.Code).IsUnique();
            });

            builder.Entity<VenueAmenityLink>().HasKey(x => new { x.VenueId, x.AmenityId });
            builder.Entity<VenueAmenityLink>().Property(x => x.Availability).HasConversion<string>().HasMaxLength(20);
            builder.Entity<VenueAmenityLink>().Property(x => x.Note).HasMaxLength(500);
            builder.Entity<VenueAmenityLink>().HasOne(x => x.Venue).WithMany(x => x.Amenities).HasForeignKey(x => x.VenueId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<VenueAmenityLink>().HasOne(x => x.Amenity).WithMany(x => x.Venues).HasForeignKey(x => x.AmenityId).OnDelete(DeleteBehavior.Cascade);

            builder.Entity<VenueOpeningHour>(entity =>
            {
                entity.Property(x => x.Note).HasMaxLength(300);
                entity.HasIndex(x => new { x.VenueId, x.DayOfWeek });
                entity.HasOne(x => x.Venue).WithMany(x => x.OpeningHours).HasForeignKey(x => x.VenueId).OnDelete(DeleteBehavior.Cascade);
                entity.ToTable(t => t.HasCheckConstraint("CK_VenueOpeningHours_Times", "[IsClosed] = 1 OR ([OpenTime] IS NOT NULL AND [CloseTime] IS NOT NULL)"));
            });

            builder.Entity<VenueHoursException>(entity =>
            {
                entity.Property(x => x.Note).HasMaxLength(300);
                entity.HasIndex(x => new { x.VenueId, x.Date }).IsUnique();
                entity.HasOne(x => x.Venue).WithMany(x => x.HoursExceptions).HasForeignKey(x => x.VenueId).OnDelete(DeleteBehavior.Cascade);
                entity.ToTable(t => t.HasCheckConstraint("CK_VenueHoursExceptions_Times", "[IsClosed] = 1 OR ([OpenTime] IS NOT NULL AND [CloseTime] IS NOT NULL)"));
            });

            builder.Entity<VenuePhoto>(entity =>
            {
                entity.Property(x => x.ImageUrl).HasMaxLength(1000).IsRequired();
                entity.Property(x => x.PhotoType).HasConversion<string>().HasMaxLength(20);
                entity.Property(x => x.Caption).HasMaxLength(300);
                entity.Property(x => x.Source).HasMaxLength(500);
                entity.Property(x => x.UsagePermission).HasMaxLength(500);
                entity.HasIndex(x => new { x.VenueId, x.SortOrder });
                entity.HasOne(x => x.Venue).WithMany(x => x.Photos).HasForeignKey(x => x.VenueId).OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
