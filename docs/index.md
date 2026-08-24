---
layout: home

hero:
  name: DbEpoch
  text: Database migrations that ship
  tagline: A Flyway-style migration tool for PostgreSQL, SQL Server, MySQL, and SQLite. Beautiful CLI, zero magic, production-tested patterns.
  image:
    src: /logo.png
    alt: DbEpoch
  actions:
    - theme: brand
      text: Get Started
      link: /guide/installation
    - theme: alt
      text: Commands
      link: /commands/new
    - theme: alt
      text: GitHub
      link: https://github.com/AzimMahmud/DbEpoch

features:
  - icon: 📝
    title: SQL-first
    details: Plain .sql files. No embedded DSL, no XML, no surprises. You write SQL, DbEpoch tracks and applies it.
  - icon: 🗄️
    title: Multi-database
    details: One tool, four engines. PostgreSQL, SQL Server, MySQL, and SQLite. Switch providers without changing your workflow.
  - icon: 🔒
    title: Safe by design
    details: Distributed locks, approval gates, deployment windows, and audit trails. Production-grade safety built in.
  - icon: ⚡
    title: CI-friendly
    details: Every command supports --json output and deterministic exit codes. Plug it into any CI/CD pipeline.
  - icon: 📴
    details: Validate, plan, and scaffold without a database connection. Use --in-memory for offline workflows.
    title: Works offline
  - icon: 🔍
    title: Checksum integrity
    details: SHA-256 checksums detect when previously-applied scripts are edited. No silent drift in production.
---
