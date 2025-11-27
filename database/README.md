# Database Scripts

This directory contains SQL scripts for setting up and managing the PostgreSQL database for the CNAB Parser API.

## Prerequisites

- PostgreSQL 17 (or compatible version)
- Docker (if using containerized PostgreSQL)
- Access to the database with user credentials

## Database Connection

### Using Docker Container

```bash
# Connect to the PostgreSQL container
docker exec -it desafio-dev-postgres psql -U user -d db
```

### Using psql directly

```bash
psql "postgresql://user:desafio@localhost:5432/db"
```

## Scripts

### `create-schema.sql`

Creates the database schema including:
- `stores` table - Stores that process CNAB transactions
- `transactions` table - Financial transactions from CNAB files
- Indexes for query optimization
- Foreign key constraints
- Comments and documentation

**Run this script to:**
- Initialize the database for the first time
- Reset the database to a clean state (drops existing tables first)

### `drop-schema.sql`

Drops all tables and data.

**Warning:** This permanently deletes all data!

## Running the Scripts

### Option 1: Using Docker exec

```bash
# Create schema
docker exec -i desafio-dev-postgres psql -U user -d db < database/create-schema.sql

# Drop schema (WARNING: deletes all data)
docker exec -i desafio-dev-postgres psql -U user -d db < database/drop-schema.sql
```

### Option 2: Using psql directly

```bash
# Create schema
psql "postgresql://user:desafio@localhost:5432/db" -f database/create-schema.sql

# Drop schema (WARNING: deletes all data)
psql "postgresql://user:desafio@localhost:5432/db" -f database/drop-schema.sql
```

### Option 3: From within psql

```sql
-- Connect to database first
\c db

-- Run create schema
\i database/create-schema.sql

-- Run drop schema (WARNING: deletes all data)
\i database/drop-schema.sql
```

## Database Schema

### Stores Table

| Column     | Type         | Description                    |
|------------|--------------|--------------------------------|
| id         | SERIAL       | Primary key                    |
| name       | VARCHAR(255) | Store name                     |
| owner_name | VARCHAR(255) | Store owner name               |
| created_at | TIMESTAMP    | Record creation timestamp      |
| updated_at | TIMESTAMP    | Record last update timestamp   |

**Indexes:**
- PRIMARY KEY on `id`
- UNIQUE INDEX on `(name, owner_name)`
- INDEX on `name`

### Transactions Table

| Column      | Type           | Description                                      |
|-------------|----------------|--------------------------------------------------|
| id          | SERIAL         | Primary key                                      |
| type        | INTEGER        | Transaction type (1-9)                           |
| date        | DATE           | Transaction date                                 |
| time        | TIME           | Transaction time (UTC-3)                         |
| value       | DECIMAL(18,2)  | Transaction value (normalized)                   |
| cpf         | VARCHAR(11)    | CPF of beneficiary                               |
| card_number | VARCHAR(50)    | Card number (may be masked)                      |
| store_name  | VARCHAR(255)   | Store name from CNAB file                        |
| store_owner | VARCHAR(255)   | Store owner from CNAB file                       |
| store_id    | INTEGER        | Foreign key to stores table                      |
| created_at  | TIMESTAMP      | Record creation timestamp                        |

**Indexes:**
- PRIMARY KEY on `id`
- INDEX on `store_id`
- INDEX on `date`
- INDEX on `type`
- INDEX on `store_name`
- COMPOSITE INDEX on `(store_id, date DESC)`

**Constraints:**
- FOREIGN KEY `store_id` REFERENCES `stores(id)` ON DELETE CASCADE
- CHECK `type` BETWEEN 1 AND 9
- CHECK `value` >= 0

## Transaction Types

| Value | Type              | Description                    | Income/Expense |
|-------|-------------------|--------------------------------|----------------|
| 1     | Debit             | Débito                         | Income (+)     |
| 2     | Boleto            | Boleto                         | Expense (-)    |
| 3     | Financing         | Financiamento                  | Expense (-)    |
| 4     | Credit            | Crédito                        | Income (+)     |
| 5     | LoanReceipt       | Recebimento Empréstimo         | Income (+)     |
| 6     | Sales             | Vendas                         | Income (+)     |
| 7     | TedReceipt        | Recebimento TED                | Income (+)     |
| 8     | DocReceipt        | Recebimento DOC                | Income (+)     |
| 9     | Rent              | Aluguel                        | Expense (-)    |

## Useful Queries

### Check table counts

```sql
SELECT
    (SELECT COUNT(*) FROM stores) AS store_count,
    (SELECT COUNT(*) FROM transactions) AS transaction_count;
```

### View stores with transaction counts and balances

```sql
SELECT
    s.id,
    s.name,
    s.owner_name,
    COUNT(t.id) AS transaction_count,
    SUM(CASE
        WHEN t.type IN (1, 4, 5, 6, 7, 8) THEN t.value  -- Income types
        ELSE -t.value  -- Expense types
    END) AS balance
FROM stores s
LEFT JOIN transactions t ON s.id = t.store_id
GROUP BY s.id, s.name, s.owner_name
ORDER BY s.name;
```

### View recent transactions

```sql
SELECT
    t.id,
    t.date,
    t.time,
    t.type,
    t.value,
    s.name AS store_name,
    s.owner_name
FROM transactions t
JOIN stores s ON t.store_id = s.id
ORDER BY t.date DESC, t.time DESC
LIMIT 20;
```

## Maintenance

### Vacuum and analyze tables

```sql
VACUUM ANALYZE stores;
VACUUM ANALYZE transactions;
```

### Reindex tables

```sql
REINDEX TABLE stores;
REINDEX TABLE transactions;
```
