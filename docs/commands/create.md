# create

Scaffold a new migration script from a template.

**Database required:** No

## Usage

```bash
# Schema migration with timestamp version
DbEpoch create --name CreateUsersTable --type schema --author jane

# Schema migration with sequence version (001, 002...)
DbEpoch create --name AddEmailColumn --type schema --sequence

# Data migration
DbEpoch create --name SeedRoles --type data --author jane

# Patch migration
DbEpoch create --name FixIndexes --type patch

# Rollback script
DbEpoch create --name CreateUsersTable --type rollback

# Repeatable script
DbEpoch create --name RefreshUserView --type repeatable

# With metadata
DbEpoch create --name CreateOrders --type schema --author jane --description "Orders and order_items tables"
```

## Options

| Option | Short | Description |
|--------|-------|-------------|
| `--name` | `-n` | **(Required)** Migration name in PascalCase |
| `--type` | `-t` | Migration type: `schema` (default), `data`, `patch`, `rollback`, `repeatable` |
| `--author` | `-a` | Author name for the metadata header |
| `--description` | `-d` | Short description for the metadata header |
| `--output` | `-o` | Override output directory |
| `--sequence` | `-s` | Use sequence version (`001`, `002`) instead of timestamp |

## Generated filenames

| Type | Pattern | Example |
|------|---------|---------|
| schema/data/patch | `V<version>__<Name>.sql` | `V20260617120000__CreateUsers.sql` |
| schema (sequence) | `V<seq>__<Name>.sql` | `V001__CreateUsers.sql` |
| rollback | `U<version>__Rollback_<Name>.sql` | `U001__Rollback_CreateUsers.sql` |
| repeatable | `R__<Name>.sql` | `R__RefreshUserView.sql` |

## Generated file content

```sql
-- Migration: CreateOrders
-- Author: jane
-- Created: 2026-06-17
-- Description: Orders and order_items tables

-- TODO: Add your SQL migration here
```
