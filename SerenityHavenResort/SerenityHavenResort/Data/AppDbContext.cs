using SerenityHavenResort.Models;
using SerenityHavenResort.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace SerenityHavenResort.Data
{
    public class AppDbContext : IdentityDbContext<User>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Entity DbSets - ONLY entities should be here
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Amenities> Amenities { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<CustomerPreference> CustomerPreferences { get; set; }
        public DbSet<CustomerNote> CustomerNotes { get; set; }
        public DbSet<Employee> Employees { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // EXPLICITLY IGNORE SERVICE CLASSES 
            modelBuilder.Ignore<BookingService>();
            modelBuilder.Ignore<RoomService>();
            modelBuilder.Ignore<PaymentService>();
            modelBuilder.Ignore<EmailSender>();
            modelBuilder.Ignore<CustomerService>();
            modelBuilder.Ignore<AmenityService>();
            modelBuilder.Ignore<PricingService>();
            modelBuilder.Ignore<TokenService>();
            modelBuilder.Ignore<SmsSenderService>();

            // Ignore any other service classes you might have


            // Configure ONLY your entity types explicitly
            ConfigureEntityTypes(modelBuilder);
        }

        private void ConfigureEntityTypes(ModelBuilder modelBuilder)
        {
            // Configure table names
            modelBuilder.Entity<Customer>().ToTable("Customers");
            modelBuilder.Entity<Room>().ToTable("Rooms");
            modelBuilder.Entity<Booking>().ToTable("Bookings");
            modelBuilder.Entity<Payment>().ToTable("Payments");
            modelBuilder.Entity<Amenities>().ToTable("Amenities");
            modelBuilder.Entity<Image>().ToTable("Images");
            modelBuilder.Entity<UserProfile>().ToTable("UserProfiles");
            modelBuilder.Entity<RefreshToken>().ToTable("RefreshTokens");

            // Many-to-Many: Room <-> Amenity
            modelBuilder.Entity<Room>()
                .HasMany(r => r.Amenities)
                .WithMany(a => a.Rooms)
                .UsingEntity<Dictionary<string, object>>(
                    "RoomAmenity",
                    j => j.HasOne<Amenities>().WithMany().HasForeignKey("AmenitiesID"),
                    j => j.HasOne<Room>().WithMany().HasForeignKey("RoomID"));

            // One-to-Many: Room -> Image
            modelBuilder.Entity<Room>()
                .HasMany(r => r.Images)
                .WithOne(i => i.Room)
                .HasForeignKey(i => i.RoomID)
                .OnDelete(DeleteBehavior.Restrict);

            // One-to-Many: Booking -> Payment
            modelBuilder.Entity<Booking>()
                .HasMany(b => b.Payments)
                .WithOne(p => p.Booking)
                .HasForeignKey(p => p.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            // One-to-Many: Customer -> Booking
            modelBuilder.Entity<Customer>()
                .HasMany(c => c.Bookings)
                .WithOne(b => b.Customer)
                .HasForeignKey(b => b.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // One-to-Many: Room -> Booking
            modelBuilder.Entity<Room>()
                .HasMany(r => r.Bookings)
                .WithOne(res => res.Room)
                .HasForeignKey(res => res.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            // User-UserProfile one-to-one
            modelBuilder.Entity<User>()
                .HasOne(u => u.UserProfile)
                .WithOne(up => up.User)
                .HasForeignKey<UserProfile>(up => up.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserProfile>()
                .HasIndex(up => up.UserID)
                .IsUnique();

            // User-Customer one-to-one
            modelBuilder.Entity<User>()
                .HasOne(u => u.Customer)
                .WithOne(c => c.User)
                .HasForeignKey<Customer>(c => c.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.UserID)
                .IsUnique();

            // User-Employee one-to-one
            modelBuilder.Entity<User>()
                .HasOne(u => u.Employee)
                .WithOne(e => e.User)
                .HasForeignKey<Employee>(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.UserId)
                .IsUnique();

            // Employee unique constraints
            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.EmployeeNumber)
                .IsUnique();

            // Customer-CustomerPreference relationship
            modelBuilder.Entity<CustomerPreference>()
                .HasOne(cp => cp.Customer)
                .WithMany(c => c.Preferences)
                .HasForeignKey(cp => cp.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Customer-CustomerNote relationship
            modelBuilder.Entity<CustomerNote>()
                 .HasOne(cn => cn.Customer)
                 .WithMany(c => c.Notes)
                 .HasForeignKey(cn => cn.CustomerId)
                 .OnDelete(DeleteBehavior.Restrict);

            // Employee-CustomerNote relationship (optional)
            modelBuilder.Entity<CustomerNote>()
                 .HasOne(cn => cn.Employee)
                 .WithMany(e => e.CreatedNotes)
                 .HasForeignKey(cn => cn.EmployeeId)
                 .OnDelete(DeleteBehavior.SetNull);


            // Configure optional relationships
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.CreatedByUser)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            //modelBuilder.Entity<Booking>()
            //    .HasOne(b => b.CreatedByUserProfile)
            //    .WithMany()
            //    .HasForeignKey(b => b.UserProfileID)
            //    .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.ProcessedByEmployee)
                .WithMany()
                .HasForeignKey(b => b.ProcessedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Auto-update timestamps
            modelBuilder.Entity<UserProfile>()
                .Property(up => up.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate();

            modelBuilder.Entity<Customer>()
                .Property(c => c.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate();

            modelBuilder.Entity<Employee>()
                .Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate();

            modelBuilder.Entity<CustomerPreference>()
                .Property(cp => cp.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate();

            // Enum configurations
            modelBuilder.Entity<User>()
                .Property(u => u.UserType)
                .HasConversion<int>();

            modelBuilder.Entity<CustomerPreference>()
                .Property(cp => cp.Priority)
                .HasConversion<int>();

            modelBuilder.Entity<CustomerNote>()
                .Property(cn => cn.NoteType)
                .HasConversion<int>();

            modelBuilder.Entity<CustomerNote>()
                .Property(cn => cn.Priority)
                .HasConversion<int>();

            // Indexes
            modelBuilder.Entity<User>()
                .HasIndex(u => u.UserType);

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.IsVip);

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.LoyaltyTier);

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.LoyaltyMembershipNumber);

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.IsBlacklisted);

            modelBuilder.Entity<CustomerNote>()
                .HasIndex(cn => cn.NoteType);

            modelBuilder.Entity<CustomerNote>()
                .HasIndex(cn => cn.Priority);

            modelBuilder.Entity<CustomerNote>()
                .HasIndex(cn => cn.RequiresAction);

            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.Department);

            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.Position);

            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.IsActive);

            modelBuilder.Entity<CustomerPreference>()
                .HasIndex(cp => cp.PreferenceCategory);

            modelBuilder.Entity<CustomerPreference>()
                .HasIndex(cp => cp.Priority);

            // RefreshToken configuration
            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                 .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
               .OnDelete(DeleteBehavior.Restrict);




            // Decimal precision configurations
            modelBuilder.Entity<Room>()
                .Property(r => r.BasePrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Room>()
                .Property(r => r.DynamicPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Room>()
                .Property(r => r.PricePerNight)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Booking>()
                .Property(b => b.TotalPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Booking>()
               .Property(b => b.RefundPercentage)
               .HasPrecision(5, 2);

            // Default values
            modelBuilder.Entity<Booking>()
                .Property(b => b.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Payment>()
                .Property(p => p.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            // Check constraints
            modelBuilder.Entity<Room>()
                .HasCheckConstraint("CK_Room_BasePrice", "BasePrice > 0");

            modelBuilder.Entity<Room>()
                .HasCheckConstraint("CK_Room_Capacity", "Capacity > 0");

            modelBuilder.Entity<Payment>()
                .HasCheckConstraint("CK_Payment_Amount", "Amount > 0");

            // Configure enum conversions
            modelBuilder.Entity<Booking>()
                .Property(e => e.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Payment>()
                .Property(e => e.Status)
                .HasConversion<string>();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Configure to only scan for entities in specific assemblies/namespaces
            if (!optionsBuilder.IsConfigured)
            {
                // Add any additional configuration here if needed
            }
        }
    }
}
