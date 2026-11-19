using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration.GetConnectionString("Default") ??
    @"Server=(localdb)\MSSQLLocalDB;Database=EnterpriseECommerce;Integrated Security=true;TrustServerCertificate=true";
var signingKey = builder.Configuration["Jwt:Key"];
if (signingKey is null || Encoding.UTF8.GetByteCount(signingKey) < 32)
    throw new InvalidOperationException("Set Jwt__Key to a random secret of at least 32 bytes.");
builder.Services.AddDbContext<StoreDb>(options => options.UseSqlServer(connection));
builder.Services.AddScoped<ICatalog, Catalog>();
builder.Services.AddScoped<IPasswordHasher<Account>, PasswordHasher<Account>>();
builder.Services.AddControllers();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = "EnterpriseECommerce",
        ValidateAudience = true, ValidAudience = "EnterpriseECommerce",
        ValidateLifetime = true, ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))
    });
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Enterprise E-Commerce REST API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>()
    });
});
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
if (args.Contains("--init-db"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<StoreDb>();
    await db.Database.MigrateAsync();
    var email = builder.Configuration["Admin:Email"];
    var password = builder.Configuration["Admin:Password"];
    if (!string.IsNullOrWhiteSpace(email) && password is { Length: >= 12 })
    {
        email = email.Trim().ToUpperInvariant();
        var existing = await db.Accounts.SingleOrDefaultAsync(x => x.Email == email);
        if (existing is null)
        {
            var admin = new Account { Email = email, Role = "Admin" };
            admin.PasswordHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Account>>().HashPassword(admin, password);
            db.Add(admin);
            await db.SaveChangesAsync();
        }
        else if (args.Contains("--reset-demo-admin"))
        {
            if (email != "ADMIN@EXAMPLE.TEST" || existing.Role != "Admin")
                throw new InvalidOperationException("Password reset is limited to the local demo admin.");
            existing.PasswordHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Account>>()
                .HashPassword(existing, password);
            await db.SaveChangesAsync();
            Console.WriteLine("Demo admin password reset.");
        }
    }
    return;
}
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers();
app.Run();
