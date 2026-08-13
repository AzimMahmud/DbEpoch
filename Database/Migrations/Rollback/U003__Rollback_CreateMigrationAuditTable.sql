-- Rollback: Create Migration Audit Table
-- Author: System
-- Created: 2026-06-16
-- Description: Rolls back V003__CreateMigrationAuditTable.sql

DROP INDEX IF EXISTS idx_migration_audit_env;
DROP INDEX IF EXISTS idx_migration_audit_date;
DROP TABLE IF EXISTS __migration_audit;