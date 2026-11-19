using System.ComponentModel.DataAnnotations;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

[ApiController, Route("api/auth")]
public class AuthController : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(Credentials input, [FromServices] StoreDb db, [FromServices] IPasswordHasher<Account> hasher)
    {
        var account = new Account { Email = input.Email.Trim().ToUpperInvariant() };
        account.PasswordHash = hasher.HashPassword(account, input.Password);
        db.Accounts.Add(account);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        { return Conflict(new { message = "Email already registered." }); }
        return StatusCode(201, new { account.Id, account.Role });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(Credentials input, [FromServices] StoreDb db,
        [FromServices] IPasswordHasher<Account> hasher, [FromServices] IConfiguration configuration)
    {
        var email = input.Email.Trim().ToUpperInvariant();
        var account = await db.Accounts.SingleOrDefaultAsync(x => x.Email == email);
        if (account is null || hasher.VerifyHashedPassword(account, account.PasswordHash, input.Password) == PasswordVerificationResult.Failed)
            return Unauthorized();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()), new Claim(ClaimTypes.Role, account.Role) };
        var token = new JwtSecurityToken("EnterpriseECommerce", "EnterpriseECommerce", claims,
            expires: DateTime.UtcNow.AddHours(1), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
    }
}

[ApiController, Route("api/products")]
public class ProductsController : ControllerBase
{
    [HttpGet]
    public Task<List<Product>> List([FromServices] ICatalog catalog, string? query, [Range(1, 10000)] int page = 1) => catalog.List(query, page);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Product>> Find(int id, [FromServices] ICatalog catalog) =>
        await catalog.Find(id) is { } product ? Ok(product) : NotFound();

    [Authorize(Roles = "Admin"), HttpPost]
    public async Task<IActionResult> Create(ProductInput input, [FromServices] StoreDb db)
    {
        var product = new Product { Name = input.Name.Trim(), Price = input.Price, Stock = input.Stock };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Find), new { id = product.Id }, product);
    }

    [Authorize(Roles = "Admin"), HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ProductInput input, [FromServices] StoreDb db)
    {
        var changed = await db.Products.Where(x => x.Id == id).ExecuteUpdateAsync(set => set
            .SetProperty(x => x.Name, input.Name.Trim()).SetProperty(x => x.Price, input.Price).SetProperty(x => x.Stock, input.Stock));
        return changed == 0 ? NotFound() : NoContent();
    }
}

[Authorize(Roles = "Customer"), ApiController, Route("api")]
public class ShoppingController : ControllerBase
{
    private int AccountId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("cart")]
    public async Task<IActionResult> Cart([FromServices] StoreDb db) => Ok(await db.Cart.AsNoTracking()
        .Where(x => x.AccountId == AccountId).Select(x => new { x.ProductId, x.Product.Name, x.Product.Price, x.Quantity }).ToListAsync());

    [HttpPut("cart/{productId:int}")]
    public async Task<IActionResult> SetCart(int productId, CartInput input, [FromServices] StoreDb db)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var product = await db.Products.FindAsync(productId);
        if (product is null) return NotFound();
        if (input.Quantity > product.Stock) return Conflict(new { message = "Insufficient stock." });
        var line = await db.Cart.FindAsync(AccountId, productId);
        if (input.Quantity == 0)
        {
            if (line is not null) db.Cart.Remove(line);
        }
        else if (line is null)
            db.Cart.Add(new CartLine { AccountId = AccountId, ProductId = productId, Quantity = input.Quantity });
        else
            line.Quantity = input.Quantity;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return NoContent();
    }

    [HttpGet("orders")]
    public async Task<IActionResult> Orders([FromServices] StoreDb db) => Ok(await db.Orders.AsNoTracking()
        .Include(x => x.Lines).Where(x => x.AccountId == AccountId).OrderByDescending(x => x.Id).ToListAsync());

    [HttpPost("orders")]
    public async Task<IActionResult> Checkout([FromServices] StoreDb db)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var cart = await db.Cart.Where(x => x.AccountId == AccountId).Include(x => x.Product).OrderBy(x => x.ProductId).ToListAsync();
        if (cart.Count == 0) return BadRequest(new { message = "Your cart is empty." });
        if (cart.Any(x => x.Quantity > x.Product.Stock)) return Conflict(new { message = "Stock changed. Update your cart." });
        var order = new Purchase { AccountId = AccountId };
        foreach (var item in cart)
        {
            item.Product.Stock -= item.Quantity;
            order.Lines.Add(new PurchaseLine { ProductId = item.ProductId, ProductName = item.Product.Name,
                UnitPrice = item.Product.Price, Quantity = item.Quantity });
            order.Total += item.Quantity * item.Product.Price;
        }
        db.Orders.Add(order);
        db.Cart.RemoveRange(cart);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return StatusCode(201, order);
    }
}