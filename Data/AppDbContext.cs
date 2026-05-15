using ECommerceApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.Data
{
    public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<int>, int>
    {
        // Constructor accepting DbContextOptions
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        //public virtual DbSet<Customer> TbCustomers { get; set; }
        public virtual DbSet<Address> TbAddresses { get; set; }
        public virtual DbSet<Status> TbStatuses { get; set; }
        public virtual DbSet<Category> TbCategories { get; set; }
        public virtual DbSet<Product> TbProducts { get; set; }
        public virtual DbSet<Order> TbOrders { get; set; }
        public virtual DbSet<OrderItem> TbOrderItems { get; set; }

        //public virtual DbSet<Payment> TbPayments { get; set; }
        //public virtual DbSet<Cancellation> TbCancellations { get; set; }
        //public virtual DbSet<Refund> TbRefunds { get; set; }
        
        public virtual DbSet<Cart> TbCarts { get; set; }
        public virtual DbSet<CartItem> TbCartItems { get; set; }

        //public virtual DbSet<Feedback> TbFeedbacks { get; set; }

        // Configuring model properties and relationships
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Order -> BillingAddress 
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Order>()
                .HasOne(o => o.BillingAddress)
                .WithMany()
                .HasForeignKey(o => o.BillingAddressId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascading delete
            // Order -> ShippingAddress 
            modelBuilder.Entity<Order>()
                .HasOne(o => o.ShippingAddress)
                .WithMany()
                .HasForeignKey(o => o.ShippingAddressId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascading delete

            modelBuilder.Entity<Cart>()
                .HasOne(c => c.Customer)
                .WithMany(c => c.Carts)
                .HasForeignKey(c =>  c.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Initial Seed Data
            // Seed OrderStatusEntity with initial data
            modelBuilder.Entity<Status>().HasData(
                //Order Statuses
                new Status { Id = 1, Name = "Pending" }, //Can be used to with Order, Paymeny, Cancellation, and Refund
                new Status { Id = 2, Name = "Processing" },
                new Status { Id = 3, Name = "Shipped" },
                new Status { Id = 4, Name = "Delivered" },
                new Status { Id = 5, Name = "Canceled" }
            );

            // Seed Categories
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Electronics", Description = "Electronic devices and accessories", IsActive = true },
                new Category { Id = 2, Name = "Books", Description = "Books and magazines", IsActive = true }
            );

            // Seed Products
            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = 1,
                    Name = "Smartphone",
                    Description = "Latest model smartphone with advanced features.",
                    Price = 699.99m,
                    StockQuantity = 50,
                    ImageUrl = "https://example.com/images/smartphone.jpg",
                    DiscountPercentage = 10,
                    CategoryId = 1,
                    IsAvailable = true
                },
                new Product
                {
                    Id = 2,
                    Name = "Laptop",
                    Description = "High-performance laptop suitable for all your needs.",
                    Price = 999.99m,
                    StockQuantity = 30,
                    ImageUrl = "https://example.com/images/laptop.jpg",
                    DiscountPercentage = 15,
                    CategoryId = 1,
                    IsAvailable = true
                },
                new Product
                {
                    Id = 3,
                    Name = "Science Fiction Novel",
                    Description = "A thrilling science fiction novel set in the future.",
                    Price = 19.99m,
                    StockQuantity = 100,
                    ImageUrl = "https://example.com/images/scifi-novel.jpg",
                    DiscountPercentage = 5,
                    CategoryId = 2,
                    IsAvailable = true
                }
            );
        }
    }
}
