-- ================================================
-- CNAB Parser API - Drop Schema
-- PostgreSQL 17
-- ================================================
-- WARNING: This will permanently delete all data!
-- ================================================

-- Drop tables in correct order (child tables first due to foreign keys)
DROP TABLE IF EXISTS transactions CASCADE;
DROP TABLE IF EXISTS stores CASCADE;

-- Verify tables are dropped
SELECT
    'Schema dropped successfully!' AS status,
    COUNT(*) FILTER (WHERE table_name = 'stores') AS stores_remaining,
    COUNT(*) FILTER (WHERE table_name = 'transactions') AS transactions_remaining
FROM information_schema.tables
WHERE table_schema = 'public'
  AND table_name IN ('stores', 'transactions');
