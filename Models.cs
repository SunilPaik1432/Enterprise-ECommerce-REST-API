using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    [Precision(18, 2)] public decimal Price { get; set; }
    public int Stock { get; set; }
}
public class Account
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "Customer";
}
public class CartLine
{
    public int AccountId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public Product Product { get; set; } = null!;
}
public class Purchase
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    [Precision(18, 2)] public decimal Total { get; set; }
    public List<PurchaseLine> Lines { get; set; } = new();
}
public class PurchaseLine
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    [Precision(18, 2)] public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}
public record Credentials([Required, EmailAddress, StringLength(200)] string Email,
    [Required, StringLength(128, MinimumLength = 10)] string Password);
public record ProductInput([Required, StringLength(120)] string Name,
    [Range(typeof(decimal), "0.01", "10000000")] decimal Price,
    [Range(0, 1000000)] int Stock);
public record CartInput([Range(0, 1000)] int Quantity);
public class StoreDb : DbContext
{
    public StoreDb(DbContextOptions<StoreDb> options) : base(options) { }
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<CartLine> Cart => Set<CartLine>();
    public DbSet<Purchase> Orders => Set<Purchase>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Account>().Property(x => x.Email).HasMaxLength(200);
        builder.Entity<Account>().HasIndex(x => x.Email).IsUnique();
        builder.Entity<CartLine>().HasKey(x => new { x.AccountId, x.ProductId });
        builder.Entity<Purchase>().HasIndex(x => new { x.AccountId, x.CreatedUtc });
    }
}
public interface ICatalog
{
    Task<List<Product>> List(string? query, int page);
    Task<Product?> Find(int id);
}
public class Catalog : ICatalog
{
    private readonly StoreDb db;
    public Catalog(StoreDb db) => this.db = db;
    public Task<List<Product>> List(string? query, int page) => db.Products.AsNoTracking()
        .Where(x => query == null || x.Name.Contains(query)).OrderBy(x => x.Id)
        .Skip((page - 1) * 20).Take(20).ToListAsync();
    public Task<Product?> Find(int id) => db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
}
