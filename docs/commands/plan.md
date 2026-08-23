# plan

Compute and display the pending execution plan. Alias: `dry-run`.

**Database required:** No

## Usage

```bash
dbshift plan
dbshift plan --environment production
dbshift plan --json
```

## What it shows

- Which migrations are pending
- Their type (Schema / Data / Patch)
- Whether a rollback script is available
- Execution order

## Example output

```
 Execution Plan

  Pending migrations: 3

  #  Type     Script                                    Rollback
  ─  ───────  ────────────────────────────────────────  ────────
  1  Schema   V002__AddOrders.sql                       yes
  2  Schema   V003__AddOrderItems.sql                   yes
  3  Data     V004__SeedOrderStatuses.sql               no
```

## How it works

`plan` uses in-memory tracking — it scans your migration files, compares them against what would be in the tracking table, and computes the diff. No database connection is needed.

This makes it safe to run anywhere: your local machine, a CI pipeline, a code review.
