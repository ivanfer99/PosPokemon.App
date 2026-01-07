PRAGMA foreign_keys = ON;

--------------------------------------------------
-- USERS
--------------------------------------------------
CREATE TABLE IF NOT EXISTS users (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  username TEXT NOT NULL UNIQUE,
  password_hash TEXT NOT NULL,
  role TEXT NOT NULL,
  is_active INTEGER NOT NULL DEFAULT 1,
  created_utc TEXT NOT NULL
);

--------------------------------------------------
-- PRODUCTS
--------------------------------------------------
CREATE TABLE IF NOT EXISTS products (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  sku TEXT NOT NULL UNIQUE,
  name TEXT NOT NULL,
  category TEXT NOT NULL,
  tcg TEXT NOT NULL,
  set_name TEXT,
  rarity TEXT,
  language TEXT,
  cost REAL NOT NULL DEFAULT 0,
  price REAL NOT NULL DEFAULT 0,
  stock INTEGER NOT NULL DEFAULT 0,
  created_utc TEXT NOT NULL,
  updated_utc TEXT NOT NULL
);

--------------------------------------------------
-- CUSTOMERS (CLIENTES)
--------------------------------------------------
CREATE TABLE IF NOT EXISTS customers (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  document_type TEXT NOT NULL DEFAULT 'DNI',  -- DNI, RUC, CE, PASSPORT
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

--------------------------------------------------
-- SALES (VENTAS)
--------------------------------------------------
CREATE TABLE IF NOT EXISTS sales (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  sale_number TEXT NOT NULL UNIQUE,
  user_id INTEGER NOT NULL,
  customer_id INTEGER,
  subtotal REAL NOT NULL,
  discount REAL NOT NULL DEFAULT 0,
  total REAL NOT NULL,
  payment_method TEXT NOT NULL,
  amount_received REAL NOT NULL DEFAULT 0,
  change REAL NOT NULL DEFAULT 0,
  note TEXT,
  created_utc TEXT NOT NULL,
  updated_utc TEXT NOT NULL,
  FOREIGN KEY (user_id) REFERENCES users(id),
  FOREIGN KEY (customer_id) REFERENCES customers(id)
);

--------------------------------------------------
-- SALE ITEMS (DETALLE DE VENTA)
--------------------------------------------------
CREATE TABLE IF NOT EXISTS sale_items (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  sale_id INTEGER NOT NULL,
  product_id INTEGER NOT NULL,
  qty INTEGER NOT NULL,
  unit_price REAL NOT NULL,
  FOREIGN KEY (sale_id) REFERENCES sales(id) ON DELETE CASCADE,
  FOREIGN KEY (product_id) REFERENCES products(id)
);

--------------------------------------------------
-- STOCK MOVEMENTS (KARDEX)
--------------------------------------------------
CREATE TABLE IF NOT EXISTS stock_movements (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  product_id INTEGER NOT NULL,
  type TEXT NOT NULL,              -- IN / OUT / ADJUST
  qty INTEGER NOT NULL,
  reason TEXT,
  created_utc TEXT NOT NULL,
  FOREIGN KEY (product_id) REFERENCES products(id)
);

--------------------------------------------------
-- APP SETTINGS (CONFIGURACIÓN)
--------------------------------------------------
CREATE TABLE IF NOT EXISTS app_settings (
  key TEXT PRIMARY KEY,
  value TEXT NOT NULL,
  updated_utc TEXT NOT NULL
);

--------------------------------------------------
-- DISCOUNT CAMPAIGNS (CAMPAÑAS DE DESCUENTO)
--------------------------------------------------
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

--------------------------------------------------
-- DISCOUNT CAMPAIGN PRODUCTS (PRODUCTOS EN DESCUENTO)
--------------------------------------------------
CREATE TABLE IF NOT EXISTS discount_campaign_products (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  campaign_id INTEGER NOT NULL,
  product_id INTEGER NOT NULL,
  FOREIGN KEY (campaign_id) REFERENCES discount_campaigns(id) ON DELETE CASCADE,
  FOREIGN KEY (product_id) REFERENCES products(id) ON DELETE CASCADE,
  UNIQUE(campaign_id, product_id)
);

--------------------------------------------------
-- INDEXES (PERFORMANCE)
--------------------------------------------------
-- Products
CREATE INDEX IF NOT EXISTS idx_products_name ON products(name);
CREATE INDEX IF NOT EXISTS idx_products_sku ON products(sku);

-- Customers
CREATE INDEX IF NOT EXISTS idx_customers_document ON customers(document_number);
CREATE INDEX IF NOT EXISTS idx_customers_name ON customers(name);
CREATE INDEX IF NOT EXISTS idx_customers_active ON customers(is_active);

-- Sales
CREATE INDEX IF NOT EXISTS idx_sales_number ON sales(sale_number);
CREATE INDEX IF NOT EXISTS idx_sales_date ON sales(created_utc);
CREATE INDEX IF NOT EXISTS idx_sales_user ON sales(user_id);
CREATE INDEX IF NOT EXISTS idx_sales_customer ON sales(customer_id);
CREATE INDEX IF NOT EXISTS idx_sales_payment ON sales(payment_method);

-- Sale Items
CREATE INDEX IF NOT EXISTS idx_sale_items_sale_id ON sale_items(sale_id);
CREATE INDEX IF NOT EXISTS idx_sale_items_product_id ON sale_items(product_id);

-- Stock Movements
CREATE INDEX IF NOT EXISTS idx_stock_movements_product ON stock_movements(product_id);
CREATE INDEX IF NOT EXISTS idx_stock_movements_date ON stock_movements(created_utc);

-- Discount Campaigns
CREATE INDEX IF NOT EXISTS idx_discount_campaigns_active ON discount_campaigns(is_active);
CREATE INDEX IF NOT EXISTS idx_discount_campaigns_dates ON discount_campaigns(start_date, end_date);
CREATE INDEX IF NOT EXISTS idx_discount_campaign_products_campaign ON discount_campaign_products(campaign_id);
CREATE INDEX IF NOT EXISTS idx_discount_campaign_products_product ON discount_campaign_products(product_id);

--------------------------------------------------
-- CONFIGURACIONES DE TIENDA (SEED INICIAL)
--------------------------------------------------
INSERT OR IGNORE INTO app_settings (key, value, updated_utc) VALUES 
('store.name', 'POS POKÉMON TCG', datetime('now'));

INSERT OR IGNORE INTO app_settings (key, value, updated_utc) VALUES 
('store.address', 'Lima, Perú', datetime('now'));

INSERT OR IGNORE INTO app_settings (key, value, updated_utc) VALUES 
('store.phone', '', datetime('now'));

INSERT OR IGNORE INTO app_settings (key, value, updated_utc) VALUES 
('store.ruc', '', datetime('now'));

INSERT OR IGNORE INTO app_settings (key, value, updated_utc) VALUES 
('store.logo_path', '', datetime('now'));
