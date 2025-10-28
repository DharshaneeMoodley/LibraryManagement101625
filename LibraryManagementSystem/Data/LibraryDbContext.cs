using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Data
{
    public class LibraryDbContext : DbContext
    {

        public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options) { }

        public DbSet<Book> Books { get; set; }
        public DbSet<BookReview> BookReviews { get; set; }
        public DbSet<UploadedDocument> UploadedDocuments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Book>( entity => 
            {
                entity.ToTable("Books");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Author).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Category).HasMaxLength(100);
                entity.Property(e => e.ISBN).HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(int.MaxValue);

                entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

                entity.Property(e => e.SubmittedDate).HasDefaultValueSql("GETDATE()");

                entity.HasMany(e => e.Documents)
                .WithOne()
                .HasForeignKey("BookId")
                .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Reviews)
                .WithOne()
                .HasForeignKey(r => r.BookId)
                .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<BookReview>(entity => 
            {
                entity.ToTable("BookReviews");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Decision)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

                entity.Property(e => e.ReviewDate).IsRequired();
                
            });

            modelBuilder.Entity<UploadedDocument>(entity => 
            {
                entity.ToTable("UploadDocument");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.FileName).IsRequired().HasMaxLength(225);
                entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
                entity.Property(e => e.FileSize).HasDefaultValue(1);
                entity.Property(e => e.UploadedDate).HasDefaultValueSql("GETDATE()");

            });
        }
    }
}
