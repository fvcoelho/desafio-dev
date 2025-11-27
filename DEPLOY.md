# Deployment Guide

## Prerequisites

- Docker and Docker Compose
- .NET 8.0 SDK (for running tests)

## Quick Start

### 1. Run Tests

```bash
dotnet test
```

### 2. Start Application

```bash
docker-compose up -d
```

This will start:
- **PostgreSQL Database** on port 5432
- **API** on port 5002
- **Web UI** on port 5001

### 3. Access

- **Web Interface**: http://localhost:5001
- **API Documentation (Swagger)**: http://localhost:5002

### 4. Stop Application

```bash
docker-compose down
```

## Usage

1. Open the Web Interface at http://localhost:5001
2. Upload a CNAB file (or download the sample file)
3. View transactions grouped by store with balances
4. Use the "API Docs" link in the menu to explore the API

## Troubleshooting

### Port Conflicts

If ports 5001, 5002, or 5432 are already in use, edit `docker-compose.yml` to change the port mappings.

### Rebuild Containers

After code changes:

```bash
docker-compose up -d --build
```

### View Logs

```bash
docker-compose logs -f
```
