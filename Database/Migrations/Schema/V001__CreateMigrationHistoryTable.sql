-- Migration: Create Migration History Table
-- Author: System
-- Created: 2026-06-16
-- Description: Creates the migration history tracking table.
-- Mirrors the schema produced by `dbsh init` (PostgreSQL variant).

CREATE TABLE IF NOT EXISTS __migration_history (
    id                   UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    version              VARCHAR(50)  NOT NULL,
    name                 VARCHAR(255) NOT NULL,
    script_name          VARCHAR(500) NOT NULL,
    script_hash          VARCHAR(64)  NOT NULL,
    migration_type       VARCHAR(20)  NOT NULL,
    category             VARCHAR(50)  NOT NULL,
    executed_by          VARCHAR(255) NOT NULL,
    executed_at_utc      TIMESTAMP    NOT NULL DEFAULT NOW(),
    execution_time_ms    BIGINT       NOT NULL,
    environment          VARCHAR(50)  NOT NULL,
    status               VARCHAR(20)  NOT NULL,
    rollback_available   BOOLEAN      NOT NULL DEFAULT FALSE,
    rollback_script_name VARCHAR(500),
    error_message        TEXT,
    batch_number         INTEGER      NOT NULL DEFAULT 1,
    CONSTRAINT uk_migration_script_name UNIQUE (script_name, environment)
);

-- Version uniqueness applies only to versioned migrations. Repeatable scripts all
-- share version 'R', so they are identified by script_name instead (see also the
-- UNIQUE(script_name, environment) constraint above).
CREATE UNIQUE INDEX IF NOT EXISTS uk_migration_version_env
    ON __migration_history(version, environment) WHERE version <> 'R';

CREATE INDEX IF NOT EXISTS idx_migration_history_env      ON __migration_history(environment);
CREATE INDEX IF NOT EXISTS idx_migration_history_status   ON __migration_history(status);
CREATE INDEX IF NOT EXISTS idx_migration_history_executed ON __migration_history(executed_at_utc);
CREATE INDEX IF NOT EXISTS idx_migration_history_version  ON __migration_history(version);