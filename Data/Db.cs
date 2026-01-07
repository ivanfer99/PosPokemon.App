using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using PosPokemon.App.Services;

namespace PosPokemon.App.Data;

public sealed class Db
{
    private readonly string _connectionString;

    public Db(string dbFile)
    {
        _connectionString = $"Data Source={dbFile}";
    }

    public IDbConnection OpenConnection() => new SqliteConnection(_connectionString);

    /// <summary>
    /// Inicializa el esquema de la base de datos desde Schema.sqlite.sql
    /// </summary>
    public void InitSchema()
    {
        var schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Schema.sqlite.sql");

        if (!File.Exists(schemaPath))
            throw new FileNotFoundException($"Schema file not found: {schemaPath}");

        var sql = File.ReadAllText(schemaPath);

        using var conn = OpenConnection();
        conn.Execute(sql);
    }

    /// <summary>
    /// Migración V2: Agrega columnas amount_received y change a la tabla sales.
    /// Es seguro ejecutar múltiples veces - solo aplica cambios si no existen.
    /// </summary>
    public void MigrateToV2()
    {
        using var conn = OpenConnection();

        // Verificar si las columnas ya existen
        const string sql = "PRAGMA table_info(sales);";
        var columns = conn.Query<dynamic>(sql)
                          .Select(x => (string)x.name)
                          .ToList();

        if (!columns.Contains("amount_received"))
        {
            // Agregar nuevas columnas
            conn.Execute("ALTER TABLE sales ADD COLUMN amount_received REAL NOT NULL DEFAULT 0;");
            conn.Execute("ALTER TABLE sales ADD COLUMN change REAL NOT NULL DEFAULT 0;");

            // Actualizar ventas existentes: amount_received = total (sin vuelto)
            conn.Execute("UPDATE sales SET amount_received = total, change = 0;");
        }
    }

    public void MigrateToV3()
    {
        using var conn = OpenConnection();

        // Verificar si las configuraciones ya existen
        const string checkSql = "SELECT COUNT(*) FROM app_settings WHERE key = 'store.name';";
        var exists = conn.ExecuteScalar<int>(checkSql);

        if (exists == 0)
        {
            var now = DateTime.UtcNow.ToString("O");

            const string insertSql = @"
INSERT INTO app_settings (key, value, updated_utc) 
VALUES (@Key, @Value, @UpdatedUtc);";

            // Insertar configuraciones por defecto
            conn.Execute(insertSql, new { Key = "store.name", Value = "POS POKÉMON TCG", UpdatedUtc = now });
            conn.Execute(insertSql, new { Key = "store.address", Value = "Lima, Perú", UpdatedUtc = now });
            conn.Execute(insertSql, new { Key = "store.phone", Value = "", UpdatedUtc = now });
            conn.Execute(insertSql, new { Key = "store.ruc", Value = "", UpdatedUtc = now });
            conn.Execute(insertSql, new { Key = "store.logo_path", Value = "", UpdatedUtc = now });
        }
    }

    public void MigrateToV4()
    {
        using var conn = OpenConnection();

        // Verificar si las tablas ya existen
        const string checkSql = @"
SELECT COUNT(*) 
FROM sqlite_master 
WHERE type='table' AND name='discount_campaigns';";

        var exists = conn.ExecuteScalar<int>(checkSql);

        if (exists == 0)
        {
            const string sql = @"
CREATE TABLE IF NOT EXISTS discount_campaigns (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  name TEXT NOT NULL,
  discount_percentage REAL NOT NULL,
  start_date TEXT NOT NULL,
  end_date TEXT NOT NULL,
  is_active INTEGER NOT NULL DEFAULT 1,
  created_by INTEGER NOT NULL,
  created_utc TEXT NOT NULL,
  updated_utc TEXT NOT NULL,
  FOREIGN KEY (created_by) REFERENCES users(id)
);

CREATE TABLE IF NOT EXISTS discount_campaign_products (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  campaign_id INTEGER NOT NULL,
  product_id INTEGER NOT NULL,
  FOREIGN KEY (campaign_id) REFERENCES discount_campaigns(id) ON DELETE CASCADE,
  FOREIGN KEY (product_id) REFERENCES products(id) ON DELETE CASCADE,
  UNIQUE(campaign_id, product_id)
);

CREATE INDEX IF NOT EXISTS idx_discount_campaigns_active ON discount_campaigns(is_active);
CREATE INDEX IF NOT EXISTS idx_discount_campaigns_dates ON discount_campaigns(start_date, end_date);
CREATE INDEX IF NOT EXISTS idx_discount_campaign_products_campaign ON discount_campaign_products(campaign_id);
CREATE INDEX IF NOT EXISTS idx_discount_campaign_products_product ON discount_campaign_products(product_id);
";
            conn.Execute(sql);
        }
    }

    /// <summary>
    /// Migración V5: Agrega tabla de clientes y relación con ventas.
    /// Es seguro ejecutar múltiples veces.
    /// </summary>
    public void MigrateToV5()
    {
        using var conn = OpenConnection();  // ✅ CAMBIO: GetConnection() → OpenConnection()

        // Verificar si la tabla customers existe
        const string checkCustomersTable = @"
        SELECT name FROM sqlite_master 
        WHERE type='table' AND name='customers';
    ";

        var customersExists = conn.ExecuteScalar<string>(checkCustomersTable) != null;

        if (!customersExists)
        {
            // Crear tabla customers
            const string createCustomers = @"
            CREATE TABLE customers (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                document_type TEXT NOT NULL DEFAULT 'DNI',
                document_number TEXT NOT NULL UNIQUE,
                name TEXT NOT NULL,
                phone TEXT,
                email TEXT,
                address TEXT,
                notes TEXT,
                is_active INTEGER NOT NULL DEFAULT 1,
                created_utc TEXT NOT NULL,
                updated_utc TEXT NOT NULL
            );
        ";
            conn.Execute(createCustomers);

            // Crear índices
            const string createIndexes = @"
            CREATE INDEX idx_customers_document ON customers(document_number);
            CREATE INDEX idx_customers_name ON customers(name);
            CREATE INDEX idx_customers_active ON customers(is_active);
        ";
            conn.Execute(createIndexes);
        }

        // Verificar si la columna customer_id existe en sales
        const string checkColumn = @"
        SELECT COUNT(*) 
        FROM pragma_table_info('sales') 
        WHERE name='customer_id';
    ";

        var columnExists = conn.ExecuteScalar<int>(checkColumn) > 0;

        if (!columnExists)
        {
            // Agregar columna customer_id a sales
            const string addColumn = @"
            ALTER TABLE sales 
            ADD COLUMN customer_id INTEGER;
        ";
            conn.Execute(addColumn);

            // Crear índice
            const string createIndex = @"
            CREATE INDEX idx_sales_customer ON sales(customer_id);
        ";
            conn.Execute(createIndex);
        }
    }

    /// <summary>
    /// Crea usuarios por defecto (admin y seller) si no existen
    /// </summary>
    public async Task SeedAsync()
    {
        using var conn = OpenConnection();

        const string checkSql = "SELECT COUNT(*) FROM users WHERE username = 'admin'";
        var exists = await conn.ExecuteScalarAsync<int>(checkSql);

        if (exists == 0)
        {
            var passwordHasher = new PasswordHasher();
            var adminHash = passwordHasher.Hash("admin");
            var sellerHash = passwordHasher.Hash("seller");

            const string insertSql = @"
INSERT INTO users (username, password_hash, role, is_active, created_utc) 
VALUES (@Username, @PasswordHash, @Role, @IsActive, @CreatedUtc);";

            await conn.ExecuteAsync(insertSql, new
            {
                Username = "admin",
                PasswordHash = adminHash,
                Role = "ADMIN",
                IsActive = 1,
                CreatedUtc = DateTime.UtcNow.ToString("O")
            });

            await conn.ExecuteAsync(insertSql, new
            {
                Username = "seller",
                PasswordHash = sellerHash,
                Role = "SELLER",
                IsActive = 1,
                CreatedUtc = DateTime.UtcNow.ToString("O")
            });
        }
    }
}