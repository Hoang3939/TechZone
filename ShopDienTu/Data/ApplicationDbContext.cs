using ShopDienTu.Models;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.EntityFrameworkCore;
using System;

namespace ShopDienTu.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<SubCategory> SubCategories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<OrderStatus> OrderStatuses { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<Rank> Ranks { get; set; }
        public DbSet<UserAddress> UserAddresses { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }


        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    base.OnModelCreating(modelBuilder);

        //    // --- Cấu hình mối quan hệ cho Cart và CartItem (DB Entity) ---
        //    modelBuilder.Entity<CartItem>()
        //        .HasOne(ci => ci.Cart)          // Một CartItem thuộc về một Cart
        //        .WithMany(c => c.CartItems)   // Một Cart có nhiều CartItem
        //        .HasForeignKey(ci => ci.CartID)
        //        .OnDelete(DeleteBehavior.Cascade);

        //    modelBuilder.Entity<CartItem>()
        //        .HasOne(ci => ci.Product)
        //        .WithMany()
        //        .HasForeignKey(ci => ci.ProductID)
        //        .OnDelete(DeleteBehavior.Cascade);

        //    // --- Cấu hình các mối quan hệ khác dựa trên SQL của bạn ---

        //    // Users - Ranks
        //    modelBuilder.Entity<User>()
        //        .HasOne(u => u.Rank)
        //        .WithMany()
        //        .HasForeignKey(u => u.RankID)
        //        .OnDelete(DeleteBehavior.SetNull); // RankID là nullable

        //    // UserAddress - Users
        //    modelBuilder.Entity<UserAddress>()
        //        .HasOne(ua => ua.User)
        //        .WithMany()
        //        .HasForeignKey(ua => ua.UserID)
        //        .OnDelete(DeleteBehavior.Cascade);

        //    // SubCategories - Categories
        //    modelBuilder.Entity<SubCategory>()
        //        .HasOne(sc => sc.Category)
        //        .WithMany()
        //        .HasForeignKey(sc => sc.CategoryID)
        //        .OnDelete(DeleteBehavior.Cascade);

        //    // Products - SubCategories
        //    modelBuilder.Entity<Product>()
        //        .HasOne(p => p.SubCategory)
        //        .WithMany()
        //        .HasForeignKey(p => p.SubCategoryID)
        //        .OnDelete(DeleteBehavior.SetNull);

        //    // ProductImages - Products
        //    modelBuilder.Entity<ProductImage>()
        //        .HasOne(pi => pi.Product)
        //        .WithMany(p => p.ProductImages) // Giả định Product có ICollection<ProductImage> ProductImages
        //        .HasForeignKey(pi => pi.ProductID)
        //        .OnDelete(DeleteBehavior.Cascade);

        //    // Orders - Users
        //    modelBuilder.Entity<Order>()
        //        .HasOne(o => o.User)
        //        .WithMany()
        //        .HasForeignKey(o => o.UserID)
        //        .OnDelete(DeleteBehavior.SetNull); // UserID là nullable

        //    // Orders - PaymentMethods
        //    modelBuilder.Entity<Order>()
        //        .HasOne(o => o.PaymentMethod)
        //        .WithMany()
        //        .HasForeignKey(o => o.PaymentMethodID)
        //        .OnDelete(DeleteBehavior.SetNull); // PaymentMethodID là nullable

        //    // OrderDetails - Orders
        //    modelBuilder.Entity<OrderDetail>()
        //        .HasOne(od => od.Order)
        //        .WithMany(o => o.OrderDetails) // Giả định Order có ICollection<OrderDetail> OrderDetails
        //        .HasForeignKey(od => od.OrderID)
        //        .OnDelete(DeleteBehavior.Cascade);

        //    // OrderDetails - Products
        //    modelBuilder.Entity<OrderDetail>()
        //        .HasOne(od => od.Product)
        //        .WithMany()
        //        .HasForeignKey(od => od.ProductID)
        //        .OnDelete(DeleteBehavior.Restrict); // Mặc định hoặc theo SQL

        //    // OrderStatuses - Orders
        //    modelBuilder.Entity<OrderStatus>()
        //        .HasOne(os => os.Order)
        //        .WithMany()
        //        .HasForeignKey(os => os.OrderID)
        //        .OnDelete(DeleteBehavior.Cascade);

        //    // Reviews - Products
        //    modelBuilder.Entity<Review>()
        //        .HasOne(r => r.Product)
        //        .WithMany()
        //        .HasForeignKey(r => r.ProductID)
        //        .OnDelete(DeleteBehavior.Cascade);

        //    // Reviews - Users
        //    modelBuilder.Entity<Review>()
        //        .HasOne(r => r.User)
        //        .WithMany()
        //        .HasForeignKey(r => r.UserID)
        //        .OnDelete(DeleteBehavior.Cascade);

        //    // Promotions - Products
        //    modelBuilder.Entity<Promotion>()
        //        .HasOne(p => p.Product)
        //        .WithMany()
        //        .HasForeignKey(p => p.ProductID)
        //        .OnDelete(DeleteBehavior.SetNull);

        //    // Promotions - Ranks
        //    modelBuilder.Entity<Promotion>()
        //        .HasOne(p => p.Rank)
        //        .WithMany()
        //        .HasForeignKey(p => p.RankID)
        //        .OnDelete(DeleteBehavior.SetNull);

        //    //// WishlistItems - Users
        //    //modelBuilder.Entity<WishlistItem>()
        //    //    .HasOne(wli => wli.User)
        //    //    .WithMany()
        //    //    .HasForeignKey(wli => wli.UserID)
        //    //    .OnDelete(DeleteBehavior.Cascade);

        //    //// WishlistItems - Products
        //    //modelBuilder.Entity<WishlistItem>()
        //    //    .HasOne(wli => wli.Product)
        //    //    .WithMany()
        //    //    .HasForeignKey(wli => wli.ProductID)
        //    //    .OnDelete(DeleteBehavior.Cascade);
        //}
    }
}
