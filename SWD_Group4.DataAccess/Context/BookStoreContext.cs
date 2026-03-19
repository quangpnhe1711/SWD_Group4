using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SWD_Group4.DataAccess.Models;
using System;
using System.Collections.Generic;

namespace SWD_Group4.DataAccess.Context;

public partial class BookStoreContext : DbContext
{
    public BookStoreContext()
    {
    }

    public BookStoreContext(DbContextOptions<BookStoreContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Book> Books { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<Promotion> Promotions { get; set; }

    public virtual DbSet<RefundRequest> RefundRequests { get; set; }

    public virtual DbSet<VerificationRequest> VerificationRequests { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
        {
            return;
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DBDefault");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            optionsBuilder.UseSqlServer(connectionString);
        }
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Book__3213E83FF83007B2");

            entity.ToTable("Book");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Author)
                .HasMaxLength(255)
                .HasColumnName("author");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.SellerId).HasColumnName("seller_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.StockQuantity).HasColumnName("stock_quantity");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");

            entity.HasOne(d => d.Seller).WithMany(p => p.Books)
                .HasForeignKey(d => d.SellerId)
                .HasConstraintName("FK_Book_User");

            entity.HasMany(d => d.Categories).WithMany(p => p.Books)
                .UsingEntity<Dictionary<string, object>>(
                    "BookCategory",
                    r => r.HasOne<Category>().WithMany()
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__BookCateg__categ__5629CD9C"),
                    l => l.HasOne<Book>().WithMany()
                        .HasForeignKey("BookId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__BookCateg__book___5535A963"),
                    j =>
                    {
                        j.HasKey("BookId", "CategoryId").HasName("PK__BookCate__1459F47A95544E69");
                        j.ToTable("BookCategory");
                        j.IndexerProperty<int>("BookId").HasColumnName("book_id");
                        j.IndexerProperty<int>("CategoryId").HasColumnName("category_id");
                    });
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Category__3213E83F72B33A90");

            entity.ToTable("Category");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Order__3213E83F9CCB3F9D");

            entity.ToTable("Order");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(500)
                .HasColumnName("address");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.PaymentDate)
                .HasColumnType("datetime")
                .HasColumnName("payment_date");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(20)
                .HasColumnName("payment_method");
            entity.Property(e => e.PaymentTransactionId)
                .HasMaxLength(255)
                .HasColumnName("payment_transaction_id");
            entity.Property(e => e.PromotionId).HasColumnName("promotion_id");
            entity.Property(e => e.SellerId).HasColumnName("seller_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.TotalPrice).HasColumnName("total_price");

            entity.HasOne(d => d.Customer).WithMany(p => p.OrderCustomers)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK__Order__customer___60A75C0F");

            entity.HasOne(d => d.Promotion).WithMany(p => p.Orders)
                .HasForeignKey(d => d.PromotionId)
                .HasConstraintName("FK__Order__promotion__628FA481");

            entity.HasOne(d => d.Seller).WithMany(p => p.OrderSellers)
                .HasForeignKey(d => d.SellerId)
                .HasConstraintName("FK__Order__seller_id__619B8048");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__OrderIte__3213E83F32576894");

            entity.ToTable("OrderItem");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BookId).HasColumnName("book_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.Quantity).HasColumnName("quantity");

            entity.HasOne(d => d.Book).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.BookId)
                .HasConstraintName("FK__OrderItem__book___66603565");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__OrderItem__order__656C112C");
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Promotio__3213E83F07DD9AC2");

            entity.ToTable("Promotion");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.DiscountType)
                .HasMaxLength(20)
                .HasColumnName("discount_type");
            entity.Property(e => e.DiscountValue).HasColumnName("discount_value");
            entity.Property(e => e.EndDate)
                .HasColumnType("datetime")
                .HasColumnName("end_date");
            entity.Property(e => e.PromotionName)
                .HasMaxLength(255)
                .HasColumnName("promotion_name");
            entity.Property(e => e.PromotionScope)
                .HasMaxLength(20)
                .HasColumnName("promotion_scope");
            entity.Property(e => e.StartDate)
                .HasColumnType("datetime")
                .HasColumnName("start_date");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Promotions)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK__Promotion__creat__5BE2A6F2");
        });

        modelBuilder.Entity<RefundRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RefundRe__3213E83FE12A5A87");

            entity.ToTable("RefundRequest");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Reason)
                .HasMaxLength(500)
                .HasColumnName("reason");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");

            entity.HasOne(d => d.Order).WithMany(p => p.RefundRequests)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__RefundReq__order__6A30C649");
        });

        modelBuilder.Entity<VerificationRequest>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.ToTable("VerificationRequest");

            entity.HasIndex(e => e.UserId, "IX_VerificationRequest_user_id");
            entity.HasIndex(e => e.Status, "IX_VerificationRequest_status");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");

            entity.Property(e => e.Url)
                .HasMaxLength(500)
                .HasColumnName("url");
            entity.Property(e => e.CitizenId)
                .HasMaxLength(50)
                .HasColumnName("citizen_id");
            entity.Property(e => e.BankAccount)
                .HasMaxLength(50)
                .HasColumnName("bank_account");
            entity.Property(e => e.BankName)
                .HasMaxLength(255)
                .HasColumnName("bank_name");
            entity.Property(e => e.CitizenImage)
                .HasMaxLength(1000)
                .HasColumnName("citizen_image");
            entity.Property(e => e.CitizenImageBack)
                .HasMaxLength(1000)
                .HasColumnName("citizen_image_back");
            entity.Property(e => e.BankCardImage)
                .HasMaxLength(1000)
                .HasColumnName("bank_card_image");

            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.Approved).HasColumnName("approved");
            entity.Property(e => e.Reason)
                .HasMaxLength(500)
                .HasColumnName("reason");

            entity.HasOne(d => d.User).WithMany(p => p.VerificationRequests)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_VerificationRequest_User");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__User__3213E83F16440526");

            entity.ToTable("User");

            entity.HasIndex(e => e.Email, "UQ_User_email").IsUnique();
            entity.HasIndex(e => e.Name, "UQ_User_name").IsUnique();
            entity.HasIndex(e => e.CitizenId, "UX_User_citizen_id")
                .IsUnique()
                .HasFilter("[citizen_id] IS NOT NULL");

            entity.HasIndex(e => e.BankAccount, "UX_User_bank_account")
                .IsUnique()
                .HasFilter("[bank_account] IS NOT NULL");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .HasColumnName("role");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");

            entity.Property(e => e.Url)
                .HasMaxLength(500)
                .HasColumnName("url");
            entity.Property(e => e.CitizenId)
                .HasMaxLength(50)
                .HasColumnName("citizen_id");
            entity.Property(e => e.BankAccount)
                .HasMaxLength(50)
                .HasColumnName("bank_account");
            entity.Property(e => e.BankName)
                .HasMaxLength(255)
                .HasColumnName("bank_name");
            entity.Property(e => e.CitizenImage)
                .HasMaxLength(1000)
                .HasColumnName("citizen_image");
            entity.Property(e => e.CitizenImageBack)
                .HasMaxLength(1000)
                .HasColumnName("citizen_image_back");

            entity.Property(e => e.SuspensionEndAt)
                .HasColumnType("datetime")
                .HasColumnName("suspension_end_at");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
