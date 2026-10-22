# Domain Docs

How the engineering skills should consume this repo's domain documentation when exploring the codebase.

## Before exploring, read these

- **`AGENTS.md`** at the repo root (project fact source; also carries the `## Agent skills` block).
- **`docs/agents/issue-tracker.md`** and **`docs/agents/triage-labels.md`** for tracker conventions.
- **`docs/adr/`**: read the ADRs that touch the area you're about to work in.

If any of these files don't exist, **proceed silently**. Don't flag their absence; don't suggest creating them upfront. The `/domain-modeling` skill (reached via `/grill-with-docs` and `/improve-codebase-architecture`) creates them lazily when terms or decisions actually get resolved.

## Project glossary pointer

Domain vocabulary for this repo lives in `AGENTS.md` (project purpose, safety boundaries, run modes, configuration fields) and the spec Glossary of the active `.scratch/<slug>/spec.md`. Use those terms; don't drift to synonyms.

Terminology lives in one place only: project vocabulary in `AGENTS.md`, this effort's vocabulary in the active spec's Glossary. Don't restate it here — a third copy is a third thing to keep in sync.

## Decisions

| ADR | 决定 |
| --- | --- |
| `docs/adr/0001-observe-only-boundary.md` | 只观察，不干预输入 |
| `docs/adr/0002-single-file-self-contained-portable.md` | 便携包用单文件 self-contained 发布 |
| `docs/adr/0003-config-next-to-executable.md` | 配置跟着 EXE 放，不放用户目录 |
| `docs/adr/0004-screen-relative-dot-and-user-run-autostart.md` | 圆点按屏幕比例定尺寸；开机自启用当前用户 Run 键 |

New decisions that are hard to reverse, surprising without context, and the result of a real trade-off go here: `docs/adr/NNNN-<slug>.md`, one file per decision.

## Flag ADR conflicts

If your output contradicts an existing ADR, surface it explicitly rather than silently overriding:

> _Contradicts ADR-0001 (observe only, never touch input), but worth reopening because…_
