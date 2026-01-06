PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS products (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  sku TEXT NOT NULL UNIQUE,
  name TEXT NOT NULL,
  category TEXT NOT NULL,
  tcg TEXT NOT NULL,                 -- Pokemon, OnePiece, Yugioh, etc
  set_name TEXT,
  rarity TEXT,
  language TEXT,                     -- ES, EN, JP
  cost REAL NOT NULL DEFAULT 0,
  price REAL NOT NULL DEFAULT 0,
  stock INTEGER NOT NULL DEFAULT 0,
  created_utc TEXT NOT NULL,
  updated_utc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS sales (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  sale_number TEXT NOT NULL UNIQUE,  -- ej: CF-20260105-0001
  created_utc TEXT NOT NULL,
  subtotal REAL NOT NULL,
  discount REAL NOT NULL,
  total REAL NOT NULL,
  payment_method TEXT NOT NULL,      -- CASH, CARD, YAPE, PLIN, TRANSFER
  note TEXT
);

CREATE TABLE IF NOT EXISTS sale_items (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  sale_id INTEGER NOT NULL,
  product_id INTEGER NOT NULL,
  qty INTEGER NOT NULL,
  unit_price REAL NOT NULL,
  line_total REAL NOT NULL,
  FOREIGN KEY (sale_id) REFERENCES sales(id) ON DELETE CASCADE,
  FOREIGN KEY (product_id) REFERENCES products(id)
);

CREATE TABLE IF NOT EXISTS stock_movements (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  product_id INTEGER NOT NULL,
  type TEXT NOT NULL,                -- IN, OUT, ADJUST
  qty INTEGER NOT NULL,
  reason TEXT,
  created_utc TEXT NOT NULL,
  FOREIGN KEY (product_id) REFERENCES products(id)
);

CREATE INDEX IF NOT EXISTS idx_products_name ON products(name);
CREATE INDEX IF NOT EXISTS idx_sale_items_sale_id ON sale_items(sale_id);

CREATE TABLE IF NOT EXISTS users (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  username TEXT NOT NULL UNIQUE,
  password_hash TEXT NOT NULL,
  role TEXT NOT NULL,            -- ADMIN / SELLER
  is_active INTEGER NOT NULL DEFAULT 1,
  created_utc TEXT NOT NULL
);

-- usuario admin por defecto (si no existe)
-- NOTA: el hash lo insertaremos desde código (seed) para no hardcodear aquí.
