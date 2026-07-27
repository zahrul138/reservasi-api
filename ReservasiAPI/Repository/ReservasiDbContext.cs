using Microsoft.EntityFrameworkCore;
using ReservasiAPI.Repository.Models;

namespace ReservasiAPI.Repository
{
    public partial class ReservasiDbContext : DbContext
    {
        public ReservasiDbContext(DbContextOptions<ReservasiDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Booking> Bookings { get; set; }
        public virtual DbSet<Room> Rooms { get; set; }
        public virtual DbSet<Review> Reviews { get; set; }
        public virtual DbSet<Feedback> Feedbacks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Createtime)
                      .HasDefaultValueSql("(getdate())");
            });

            modelBuilder.Entity<Booking>(entity =>
            {
                entity.ToTable("booking");
                entity.HasKey(e => e.Id);

                entity.HasOne(b => b.User)
                      .WithMany()
                      .HasForeignKey(b => b.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("(getdate())");
            });

            modelBuilder.Entity<Room>(entity =>
            {
                entity.ToTable("rooms");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(100).IsRequired();
                entity.Property(e => e.ShortDescription).HasColumnName("shortdescription").HasMaxLength(255);
                entity.Property(e => e.FullDescription).HasColumnName("fulldescription").HasMaxLength(1000);
                entity.Property(e => e.Price).HasColumnName("price").HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.Size).HasColumnName("size").HasMaxLength(50);
                entity.Property(e => e.Occupancy).HasColumnName("occupancy").HasMaxLength(50);
                entity.Property(e => e.Bed).HasColumnName("bed").HasMaxLength(50);
                entity.Property(e => e.RoomView).HasColumnName("roomview").HasMaxLength(100);
                entity.Property(e => e.Image1).HasColumnName("image1").HasMaxLength(255);
                entity.Property(e => e.Image2).HasColumnName("image2").HasMaxLength(255);
                entity.Property(e => e.Image3).HasColumnName("image3").HasMaxLength(255);
                entity.Property(e => e.Features).HasColumnName("features").HasMaxLength(1000);
                entity.Property(e => e.Amenities).HasColumnName("amenities").HasMaxLength(1000);
                entity.Property(e => e.Policies).HasColumnName("policies").HasMaxLength(1000);
                entity.Property(e => e.Quantity).HasColumnName("quantity").IsRequired();
                entity.Property(e => e.CreatedAt).HasColumnName("createdat").HasDefaultValueSql("(getdate())");
            });

            modelBuilder.Entity<Review>(entity =>
            {
                entity.ToTable("reviews");
                entity.HasKey(e => e.Id);

                entity.HasOne(r => r.Booking)
                      .WithMany()
                      .HasForeignKey(r => r.BookingId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.Room)
                      .WithMany()
                      .HasForeignKey(r => r.RoomId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.User)
                      .WithMany()
                      .HasForeignKey(r => r.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.Status)
                      .HasDefaultValue("pending");

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("(getdate())");
            });

            modelBuilder.Entity<Feedback>(entity =>
            {
                entity.ToTable("feedbacks");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Status)
                      .HasDefaultValue("new");

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("(getdate())");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
