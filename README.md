# Enterprise E-Commerce REST API

A C# and ASP.NET Core API for product catalogs, carts and order checkout. It uses EF Core Code First, SQL Server, JWT roles and a product repository injected through ASP.NET Core DI. Checkout snapshots item prices and updates inventory within one database transaction.

## Run locally

Use the .NET 7 SDK and SQL Server. The default database is `EnterpriseECommerce` on SQL Server LocalDB. Override the connection with `ConnectionStrings__Default` as needed.

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$keyBytes = New-Object byte[] 32
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
try { $rng.GetBytes($keyBytes) } finally { $rng.Dispose() }
$env:Jwt__Key = [Convert]::ToBase64String($keyBytes)
$env:Admin__Email = 'admin@example.test'
$env:Admin__Password = Read-Host 'Set a local admin password of at least 12 characters'
dotnet run -- --init-db
Remove-Item Env:Admin__Password
dotnet run -- --urls http://localhost:5201
```

Open [Swagger UI](http://localhost:5201/swagger) or import the [Postman collection](docs/ECommerce.postman_collection.json). Sign in as admin and create a product, then register and sign in as a customer to add it to the cart and place an order. Use the correct token for each role. Local admin seeding is opt-in through environment variables.

This portfolio demo targets the installed .NET 7 SDK. Upgrade to a supported runtime before public hosting.
