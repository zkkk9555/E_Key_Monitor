# Domain Docs

How the engineering skills should consume this repo's domain documentation when exploring the codebase.

## Before exploring, read these

- **`AGENTS.md`** at the repo root (project fact source; also carries the `## Agent skills` block).
- **`docs/agents/issue-tracker.md`** and **`docs/agents/triage-labels.md`** for tracker conventions.
- **`docs/adr/`**: read ADRs that touch the area you're about to work in.

If any of these files don't exist, **proceed silently**. Don't flag their absence; don't suggest creating them upfront. The `/domain-modeling` skill (reached via `/grill-with-docs` and `/improve-codebase-architecture`) creates them lazily when terms or decisions actually get resolved.

## Project glossary pointer

Domain vocabulary for this repo lives in `AGENTS.md` (project purpose, safety boundaries, run modes) and the spec Glossary of the active `.scratch/<slug>/spec.md`. Use those terms; don't drift to synonyms.

## Flag ADR conflicts

If your output contradicts an existing ADR, surface it explicitly rather than silently overriding:

> _Contradicts ADR-0007 (event-sourced orders), but worth reopening because…_
