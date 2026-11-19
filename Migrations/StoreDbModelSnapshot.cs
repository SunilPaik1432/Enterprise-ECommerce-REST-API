using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

[DbContext(typeof(StoreDb))]
public sealed class StoreDbModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder model)
    {
        model.HasAnnotation("ProductVersion","7.0.20");
        model.UseIdentityColumns();
        model.Entity("Product", product =>
        {
            product.Property<int>("Id").ValueGeneratedOnAdd();
            product.Property<string>("Name").IsRequired();
            product.Property<decimal>("Price").HasPrecision(18,2);
            product.Property<int>("Stock");
            product.HasKey("Id");
            product.ToTable("Products");
        });
        model.Entity("Account", account =>
        {
            account.Property<int>("Id").ValueGeneratedOnAdd();
            account.Property<string>("Email").IsRequired().HasMaxLength(200);
            account.Property<string>("PasswordHash").IsRequired();
            account.Property<string>("Role").IsRequired();
            account.HasKey("Id");
            account.HasIndex("Email").IsUnique();
            account.ToTable("Accounts");
        });
        model.Entity("CartLine", cart =>
        {
            cart.Property<int>("AccountId");
            cart.Property<int>("ProductId");
            cart.Property<int>("Quantity");
            cart.HasKey("AccountId","ProductId");
            cart.HasIndex("ProductId");
            cart.ToTable("Cart");
            cart.HasOne("Product","Product").WithMany().HasForeignKey("ProductId").OnDelete(DeleteBehavior.Cascade).IsRequired();
        });
        model.Entity("Purchase", purchase =>
        {
            purchase.Property<int>("Id").ValueGeneratedOnAdd();
            purchase.Property<int>("AccountId");
            purchase.Property<DateTime>("CreatedUtc");
            purchase.Property<decimal>("Total").HasPrecision(18,2);
            purchase.HasKey("Id");
            purchase.HasIndex("AccountId","CreatedUtc");
            purchase.ToTable("Orders");
        });
        model.Entity("PurchaseLine", line =>
        {
            line.Property<int>("Id").ValueGeneratedOnAdd();
            line.Property<int>("ProductId");
            line.Property<string>("ProductName").IsRequired();
            line.Property<decimal>("UnitPrice").HasPrecision(18,2);
            line.Property<int>("Quantity");
            line.Property<int?>("PurchaseId");
            line.HasKey("Id");
            line.HasIndex("PurchaseId");
            line.ToTable("PurchaseLine");
            line.HasOne("Purchase",null).WithMany("Lines").HasForeignKey("PurchaseId");
        });
    }
}