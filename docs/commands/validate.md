# validate

Check scripts for naming conventions, syntax, duplicates, and dependency errors.

**Database required:** No

## Usage

```bash
dbshift validate
dbshift validate --environment local
dbshift validate --json    # for CI
```

## What it checks

- **Naming**: every file must match the `V/R/U` + version + `__` + name convention
- **Syntax**: scripts must not be empty or comment-only
- **Duplicates**: no two versioned scripts can share the same version number
- **Dependencies**: every `-- Depends:` reference must point to an existing file
- **Parse errors**: malformed filenames are flagged

## Example output

```
 Validating migration scripts...

  Scripts checked:    16
  Errors:             0
  Warnings:           0

  Validation passed.
```

## JSON output

```bash
dbshift validate --json
```

```json
{
  "success": true,
  "scriptsChecked": 16,
  "errors": [],
  "warnings": []
}
```

## When to run

- Before `plan` or `migrate` — catches issues early
- In CI — fast, no database needed
- After editing existing scripts — detects checksum drift potential
