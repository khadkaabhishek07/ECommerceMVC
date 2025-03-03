using ECommerce.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<OrderHeader> OrderHeaders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "In house product", DisplayOrder = 1 },
                new Category { Id = 2, Name = "Eco-friendly product", DisplayOrder = 2 },
                new Category { Id = 3, Name = "100% Natural", DisplayOrder = 3 },
                new Category { Id = 4, Name = "Made from natural wool", DisplayOrder = 4 },
                new Category { Id = 5, Name = "Handmade product", DisplayOrder = 5 },
                new Category { Id = 6, Name = "Footwear", DisplayOrder = 6 },
                new Category { Id = 7, Name = "Decoration", DisplayOrder = 7 },
                new Category { Id = 8, Name = "Pet product", DisplayOrder = 8 },
                new Category { Id = 9, Name = "Felt craft", DisplayOrder = 9 },
                new Category { Id = 10, Name = "Lifestyle", DisplayOrder = 10 },
                new Category { Id = 11, Name = "Festival product", DisplayOrder = 11 },
                new Category { Id = 12, Name = "Pashminas", DisplayOrder = 12 }
            );


            modelBuilder.Entity<Company>().HasData(
              new Company
              {
                  Id = 1,
                  Name = "Tech Solution",
                  StreetAddress = "123 Tech St",
                  City = "Tech City",
                  PostalCode = "12121",
                  State = "IL",
                  PhoneNumber = "6669990000"
              },
              new Company
              {
                  Id = 2,
                  Name = "Vivid Books",
                  StreetAddress = "999 Vid St",
                  City = "Vid City",
                  PostalCode = "66666",
                  State = "IL",
                  PhoneNumber = "7779990000"
              },
              new Company
              {
                  Id = 3,
                  Name = "Readers Club",
                  StreetAddress = "999 Main St",
                  City = "Lala land",
                  PostalCode = "99999",
                  State = "NY",
                  PhoneNumber = "1113335555"
              }
              );

            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = 1,
                    Title = "Muffler",
                    Author = "200*300cm",
                    Description = "Praesent vitae sodales libero. Praesent molestie orci augue, vitae euismod velit sollicitudin ac. Praesent vestibulum facilisis nibh ut ultricies.\r\n\r\nNunc malesuada viverra ipsum sit amet tincidunt. ",
                    ISBN = "PRO01",
                    ListPrice = 17000,                   
                    CategoryId = 12
                },
                new Product
                {
                    Id = 2,
                    Title = "Spiderman Mask",
                    Author = "Cosplay",
                    Description = "Praesent vitae sodales libero. Praesent molestie orci augue, vitae euismod velit sollicitudin ac. Praesent vestibulum facilisis nibh ut ultricies.\r\n\r\nNunc malesuada viverra ipsum sit amet tincidunt. ",
                    ISBN = "PRO02",
                    ListPrice = 4000,                    
                    CategoryId = 11
                }
                ); ;
        }
    }
}
