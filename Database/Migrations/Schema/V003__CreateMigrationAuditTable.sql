-- Migration: Create Migration Audit Table
-- Author: System
-- Created: 2026-06-16
-- Description: Creates the migration audit trail table.
-- Mirrors the schema produced by `dbsh init` (PostgreSQL variant).

CREATE TABLE IF NOT EXISTS __migration_audit (
    id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    action           VARCHAR(50)  NOT NULL,
    performed_by     VARCHAR(255) NOT NULL,
    performed_at_utc TIMESTAMP    NOT NULL DEFAULT NOW(),
    environment      VARCHAR(50)  NOT NULL,
    details          TEXT
);

CREATE INDEX IF NOT EXISTS idx_migration_audit_date ON __migration_audit(performed_at_utc);
CREATE INDEX IF NOT EXISTS idx_migration_audit_env  ON __migration_audit(environment);