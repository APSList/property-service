using Microsoft.EntityFrameworkCore;
using property_service.Models.PropertyAmenityModels;
using property_service.Models.PropertyImageModels;
using property_service.Models.PropertyModels;

namespace property_service.Database;


public class PropertyDbContext : DbContext
{
    public PropertyDbContext(DbContextOptions<PropertyDbContext> options)
        : base(options)
    {
    }

    public DbSet<Property> Properties => Set<Property>();
    public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
    public DbSet<PropertyAmenity> PropertyAmenities => Set<PropertyAmenity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Property>()
        .Property(e => e.Status)
        .HasConversion<string>();

        modelBuilder.Entity<Property>()
        .Property(e => e.PropertyType)
        .HasConversion<string>();

        //Property amenity
        modelBuilder.Entity<PropertyAmenity>()
        .HasKey(pa => new { pa.PropertyId, pa.AmenityName });

        modelBuilder.Entity<PropertyAmenity>()
            .HasOne(pa => pa.Property)
            .WithMany(p => p.PropertyAmenities)
            .HasForeignKey(pa => pa.PropertyId);

        modelBuilder.Entity<PropertyAmenity>()
       .Property(e => e.AmenityName)
       .HasConversion<string>();
    }
}