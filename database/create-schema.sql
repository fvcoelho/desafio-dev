-- ================================================
-- CNAB Parser API - Database Schema
-- PostgreSQL 17
-- ================================================

-- Drop existing tables if they exist (for clean re-creation)
DROP TABLE IF EXISTS transactions CASCADE;
DROP TABLE IF EXISTS stores CASCADE;

-- ================================================
-- Stores Table
-- ================================================
CREATE TABLE stores (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    owner_name VARCHAR(255) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Create unique index on store name and owner combination
CREATE UNIQUE INDEX idx_stores_name_owner ON stores(name, owner_name);

-- Create index for name lookups
CREATE INDEX idx_stores_name ON stores(name);

-- ================================================
-- Transactions Table
-- ================================================
CREATE TABLE transactions (
    id SERIAL PRIMARY KEY,
    type INTEGER NOT NULL CHECK (type BETWEEN 1 AND 9),
    date DATE NOT NULL,
    time TIME NOT NULL,
    value DECIMAL(18, 2) NOT NULL CHECK (value >= 0),
    cpf VARCHAR(11) NOT NULL,
    card_number VARCHAR(50) NOT NULL,
    store_name VARCHAR(255) NOT NULL,
    store_owner VARCHAR(255) NOT NULL,
    store_id INTEGER NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,

    -- Foreign key constraint
    CONSTRAINT fk_transactions_store
        FOREIGN KEY (store_id)
        REFERENCES stores(id)
        ON DELETE CASCADE
);

-- Create indexes for common queries
CREATE INDEX idx_transactions_store_id ON transactions(store_id);
CREATE INDEX idx_transactions_date ON transactions(date);
CREATE INDEX idx_transactions_type ON transactions(type);
CREATE INDEX idx_transactions_store_name ON transactions(store_name);

-- Composite index for date range queries by store
CREATE INDEX idx_transactions_store_date ON transactions(store_id, date DESC);

-- ================================================
-- Comments
-- ================================================
COMMENT ON TABLE stores IS 'Stores that process CNAB transactions';
COMMENT ON COLUMN stores.id IS 'Unique identifier for the store';
COMMENT ON COLUMN stores.name IS 'Store name (e.g., "BAR DO JOÃO")';
COMMENT ON COLUMN stores.owner_name IS 'Name of the store owner/representative';

COMMENT ON TABLE transactions IS 'Financial transactions from CNAB files';
COMMENT ON COLUMN transactions.id IS 'Unique identifier for the transaction';
COMMENT ON COLUMN transactions.type IS 'Transaction type: 1=Debit, 2=Boleto, 3=Financing, 4=Credit, 5=LoanReceipt, 6=Sales, 7=TedReceipt, 8=DocReceipt, 9=Rent';
COMMENT ON COLUMN transactions.date IS 'Date when the transaction occurred';
COMMENT ON COLUMN transactions.time IS 'Time when the transaction occurred (UTC-3)';
COMMENT ON COLUMN transactions.value IS 'Transaction value (normalized - already divided by 100)';
COMMENT ON COLUMN transactions.cpf IS 'CPF of the beneficiary';
COMMENT ON COLUMN transactions.card_number IS 'Card number used in the transaction (may be masked)';
COMMENT ON COLUMN transactions.store_name IS 'Store name from CNAB file (for reference)';
COMMENT ON COLUMN transactions.store_owner IS 'Store owner name from CNAB file (for reference)';
COMMENT ON COLUMN transactions.store_id IS 'Foreign key to the stores table';

-- ================================================
-- Database Statistics
-- ================================================
-- Analyze tables for query optimization
ANALYZE stores;
ANALYZE transactions;

-- ================================================
-- Summary
-- ================================================
-- Display table information
SELECT
    'Schema created successfully!' AS status,
    COUNT(*) FILTER (WHERE table_name = 'stores') AS stores_table,
    COUNT(*) FILTER (WHERE table_name = 'transactions') AS transactions_table
FROM information_schema.tables
WHERE table_schema = 'public'
  AND table_name IN ('stores', 'transactions');
