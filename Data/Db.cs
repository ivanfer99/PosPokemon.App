using Dapper;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Math;
using Microsoft.Data.Sqlite;
using PosPokemon.App.Services;
using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IOPath = System.IO.Path;

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
        var schemaPath = System.IO.Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "Data",
        "Schema.sqlite.sql"
    );

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


    public void MigrateToV6()
    {
        var currentVersion = GetDatabaseVersion();
        if (currentVersion >= 6) return;

        using var cnn = OpenConnection();
        cnn.Open();  // ✅ NECESARIO: Abrir antes de BeginTransaction()

        using var txn = cnn.BeginTransaction();

        try
        {
            // 1. Crear tabla de expansiones (si no existe)
            cnn.Execute(@"
            CREATE TABLE IF NOT EXISTS expansions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL UNIQUE,
                code TEXT,
                release_date TEXT,
                is_active INTEGER NOT NULL DEFAULT 1,
                created_utc TEXT NOT NULL,
                updated_utc TEXT NOT NULL
            );
        ", transaction: txn);

            // 2. Crear tabla de categorías (si no existe)
            cnn.Execute(@"
            CREATE TABLE IF NOT EXISTS categories (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL UNIQUE,
                description TEXT,
                is_active INTEGER NOT NULL DEFAULT 1,
                created_utc TEXT NOT NULL,
                updated_utc TEXT NOT NULL
            );
        ", transaction: txn);

            // 3. Verificar si la tabla products ya tiene la nueva estructura
            var columns = cnn.Query<string>(
                "SELECT name FROM pragma_table_info('products')",
                transaction: txn
            ).ToList();

            bool needsRestructure = !columns.Contains("code") ||
                                    !columns.Contains("category_id") ||
                                    !columns.Contains("module");

            if (needsRestructure)
            {
                // 3a. Renombrar tabla antigua
                cnn.Execute("ALTER TABLE products RENAME TO products_old;", transaction: txn);

                // 3b. Crear nueva tabla products
                cnn.Execute(@"
                CREATE TABLE products (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    code TEXT NOT NULL UNIQUE,
                    name TEXT NOT NULL,
                    category_id INTEGER NOT NULL,
                    module TEXT,
                    is_promo_special INTEGER NOT NULL DEFAULT 0,
                    expansion_id INTEGER,
                    language TEXT,
                    rarity TEXT,
                    finish TEXT,
                    price REAL NOT NULL,
                    sale_price REAL,
                    stock INTEGER NOT NULL DEFAULT 0,
                    min_stock INTEGER NOT NULL DEFAULT 0,
                    description TEXT,
                    is_active INTEGER NOT NULL DEFAULT 1,
                    created_utc TEXT NOT NULL,
                    updated_utc TEXT NOT NULL,
                    FOREIGN KEY (category_id) REFERENCES categories(id),
                    FOREIGN KEY (expansion_id) REFERENCES expansions(id)
                );
            ", transaction: txn);

                // 3c. Verificar si products_old tiene datos
                var oldCount = cnn.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM products_old",
                    transaction: txn
                );

                if (oldCount > 0)
                {
                    // 3d. Insertar categoría por defecto si no existe
                    cnn.Execute(@"
                    INSERT OR IGNORE INTO categories (id, name, description, is_active, created_utc, updated_utc)
                    VALUES (1, 'General', 'Categoría por defecto', 1, datetime('now'), datetime('now'));
                ", transaction: txn);

                    // 3e. Migrar datos antiguos
                    var oldColumns = cnn.Query<string>(
                        "SELECT name FROM pragma_table_info('products_old')",
                        transaction: txn
                    ).ToList();

                    // Determinar qué columnas usar de la tabla antigua
                    string skuColumn = oldColumns.Contains("sku") ? "sku" :
                                       oldColumns.Contains("code") ? "code" : "'LEGACY-' || id";

                    cnn.Execute($@"
                    INSERT INTO products (
                        code, name, category_id, price, stock, 
                        description, is_active, created_utc, updated_utc
                    )
                    SELECT 
                        {skuColumn} as code,
                        name,
                        1 as category_id,
                        price,
                        stock,
                        {(oldColumns.Contains("description") ? "description" : "NULL")} as description,
                        1 as is_active,
                        {(oldColumns.Contains("created_utc") ? "created_utc" : "datetime('now')")} as created_utc,
                        {(oldColumns.Contains("updated_utc") ? "updated_utc" : "datetime('now')")} as updated_utc
                    FROM products_old;
                ", transaction: txn);
                }

                // 3f. Eliminar tabla antigua
                cnn.Execute("DROP TABLE IF EXISTS products_old;", transaction: txn);
            }

            // 4. Crear índices
            cnn.Execute(@"
            CREATE INDEX IF NOT EXISTS idx_products_code ON products(code);
            CREATE INDEX IF NOT EXISTS idx_products_name ON products(name);
            CREATE INDEX IF NOT EXISTS idx_products_category ON products(category_id);
            CREATE INDEX IF NOT EXISTS idx_products_expansion ON products(expansion_id);
            CREATE INDEX IF NOT EXISTS idx_products_active ON products(is_active);
            
            CREATE INDEX IF NOT EXISTS idx_expansions_name ON expansions(name);
            CREATE INDEX IF NOT EXISTS idx_expansions_active ON expansions(is_active);
            
            CREATE INDEX IF NOT EXISTS idx_categories_name ON categories(name);
            CREATE INDEX IF NOT EXISTS idx_categories_active ON categories(is_active);
        ", transaction: txn);

            // 5. Insertar categorías por defecto
            cnn.Execute(@"
            INSERT OR IGNORE INTO categories (name, description, is_active, created_utc, updated_utc)
            VALUES 
                ('Single', 'Cartas individuales', 1, datetime('now'), datetime('now')),
                ('Sealed', 'Productos sellados (sobres, cajas)', 1, datetime('now'), datetime('now')),
                ('Accesorio', 'Accesorios (fundas, carpetas, dados)', 1, datetime('now'), datetime('now'));
        ", transaction: txn);

            // 6. Actualizar versión
            SetDatabaseVersion(6);

            txn.Commit();
        }
        catch (Exception ex)
        {
            txn.Rollback();
            throw new Exception($"Error en migración V6: {ex.Message}", ex);
        }
    }
    /// <summary>
    /// Obtiene la versión actual de la base de datos
    /// </summary>
    private int GetDatabaseVersion()
    {
        using var cnn = OpenConnection();
        // ❌ ELIMINAR: cnn.Open();  // NO es necesario, OpenConnection() ya lo hace

        // Crear tabla de versiones si no existe
        cnn.Execute(@"
        CREATE TABLE IF NOT EXISTS database_version (
            id INTEGER PRIMARY KEY CHECK (id = 1),
            version INTEGER NOT NULL,
            updated_utc TEXT NOT NULL
        );
    ");

        // Obtener versión actual
        var version = cnn.QueryFirstOrDefault<int?>(
            "SELECT version FROM database_version WHERE id = 1"
        );

        // Si no existe registro, insertar versión 0
        if (version == null)
        {
            cnn.Execute(@"
            INSERT INTO database_version (id, version, updated_utc)
            VALUES (1, 0, datetime('now'));
        ");
            return 0;
        }

        return version.Value;
    }

    /// <summary>
    /// Establece la versión actual de la base de datos
    /// </summary>

    /// <summary>
    /// Establece la versión actual de la base de datos
    /// </summary>
    private void SetDatabaseVersion(int version)
    {
        using var cnn = OpenConnection();
        // ❌ ELIMINAR: cnn.Open();  // NO es necesario, OpenConnection() ya lo hace

        cnn.Execute(@"
        INSERT OR REPLACE INTO database_version (id, version, updated_utc)
        VALUES (1, @Version, datetime('now'));
    ", new { Version = version });
    }

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