-- Rollback: Create Migration History Table
-- Author: System
-- Created: 2026-06-16
-- Description: Rolls back V001__CreateMigrationHistoryTable.sql

DROP INDEX IF EXISTS uk_migration_version_env;
DROP INDEX IF EXISTS idx_migration_history_version;
DROP INDEX IF EXISTS idx_migration_history_executed;
DROP INDEX IF EXISTS idx_migration_history_status;
DROP INDEX IF EXISTS idx_migration_history_env;
DROP TABLE IF EXISTS __migration_history;