-- V6: Agregar tabla de Expansiones y campos adicionales a productos

-- ============================================
-- TABLA DE EXPANSIONES
-- ============================================
CREATE TABLE IF NOT EXISTS expansions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE,
    code TEXT,
    release_date TEXT,
    is_active INTEGER NOT NULL DEFAULT 1,
    created_utc TEXT NOT NULL,
    updated_utc TEXT NOT NULL
);

-- Índices para expansions
CREATE INDEX IF NOT EXISTS idx_expansions_name ON expansions(name);
CREATE INDEX IF NOT EXISTS idx_expansions_active ON expansions(is_active);

-- ============================================
-- INSERTAR EXPANSIONES INICIALES
-- ============================================
INSERT OR IGNORE INTO expansions (name, code, release_date, is_active, created_utc, updated_utc)
VALUES 
    ('Escarlata y Púrpura', 'SV01', '2023-03-31', 1, datetime('now'), datetime('now')),
    ('151', 'SV3.5', '2023-09-22', 1, datetime('now'), datetime('now')),
    ('Obsidiana Flamígera', 'SV02', '2023-06-16', 1, datetime('now'), datetime('now')),
    ('Paradoja Temporal', 'SV04', '2023-11-03', 1, datetime('now'), datetime('now')),
    ('Fuerzas Temporales', 'SV05', '2024-03-22', 1, datetime('now'), datetime('now'));

-- ============================================
-- AGREGAR CAMPOS ADICIONALES A PRODUCTS
-- ============================================

-- Módulo del producto
ALTER TABLE products ADD COLUMN module TEXT;

-- Si es promoción especial
ALTER TABLE products ADD COLUMN is_promo_special INTEGER NOT NULL DEFAULT 0;

-- Relación con expansión
ALTER TABLE products ADD COLUMN expansion_id INTEGER REFERENCES expansions(id);

-- Idioma
ALTER TABLE products ADD COLUMN language TEXT;

-- Rareza
ALTER TABLE products ADD COLUMN rarity TEXT;

-- Acabado
ALTER TABLE products ADD COLUMN finish TEXT;

-- Precio de venta sugerido
ALTER TABLE products ADD COLUMN sale_price REAL;

-- Índices para los nuevos campos
CREATE INDEX IF NOT EXISTS idx_products_expansion ON products(expansion_id);
CREATE INDEX IF NOT EXISTS idx_products_module ON products(module);
CREATE INDEX IF NOT EXISTS idx_products_promo ON products(is_promo_special);
CREATE INDEX IF NOT EXISTS idx_products_language ON products(language);