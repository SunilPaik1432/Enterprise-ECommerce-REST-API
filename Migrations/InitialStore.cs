using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

[DbContext(typeof(StoreDb))]
[Migration("20261119090000_InitialStore")]
public sealed class InitialStore : Migration
{
    protected override void Up(MigrationBuilder migration)
    {
        migration.Sql(@"
CREATE TABLE [Products] ([Id] int IDENTITY PRIMARY KEY, [Name] nvarchar(max) NOT NULL, [Price] decimal(18,2) NOT NULL, [Stock] int NOT NULL);
CREATE TABLE [Accounts] ([Id] int IDENTITY PRIMARY KEY, [Email] nvarchar(200) NOT NULL, [PasswordHash] nvarchar(max) NOT NULL, [Role] nvarchar(max) NOT NULL);
CREATE UNIQUE INDEX [IX_Accounts_Email] ON [Accounts]([Email]);
CREATE TABLE [Cart] ([AccountId] int NOT NULL, [ProductId] int NOT NULL REFERENCES [Products]([Id]) ON DELETE CASCADE,
    [Quantity] int NOT NULL, PRIMARY KEY ([AccountId],[ProductId]));
CREATE INDEX [IX_Cart_ProductId] ON [Cart]([ProductId]);
CREATE TABLE [Orders] ([Id] int IDENTITY PRIMARY KEY, [AccountId] int NOT NULL, [CreatedUtc] datetime2 NOT NULL, [Total] decimal(18,2) NOT NULL);
CREATE INDEX [IX_Orders_AccountId_CreatedUtc] ON [Orders]([AccountId],[CreatedUtc]);
CREATE TABLE [PurchaseLine] ([Id] int IDENTITY PRIMARY KEY, [ProductId] int NOT NULL, [ProductName] nvarchar(max) NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL, [Quantity] int NOT NULL, [PurchaseId] int NULL REFERENCES [Orders]([Id]));
CREATE INDEX [IX_PurchaseLine_PurchaseId] ON [PurchaseLine]([PurchaseId]);");
    }
    protected override void Down(MigrationBuilder migration) =>
        migration.Sql("DROP TABLE [PurchaseLine], [Cart], [Orders], [Accounts], [Products];");
}