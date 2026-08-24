# info

Show configuration, provider, environments, and paths. Alias: `config`.

**Database required:** No

## Usage

```bash
DbEpoch info
DbEpoch info --json
```

Also accepts [global options](/reference/global-options).

## Example output

```
 DbEpoch Configuration

  Provider:         PostgreSQL
  Base path:        /home/user/myapp
  Config file:      /home/user/myapp/Database/Config/migration.json
  Scripts path:     /home/user/myapp/Database/Migrations
  Tracking schema:  public

  Environments:
    - local
    - development
    - staging
    - production
```

## When to use

- Verify your configuration is loaded correctly
- Check which environments are available
- Confirm the resolved provider and paths
- Debug connection string or config issues
