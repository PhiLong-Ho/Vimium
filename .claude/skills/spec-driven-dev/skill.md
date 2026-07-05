---
name: Spec-Driven Development
description: Develop features by iterating on a spec document — design → review → implement → verify — until all criteria are met.
---

## Spec-Driven Development

This skill encodes the project's development cycle. Every feature starts and ends with a spec file in `docs/features/<version>/<feature-name>.md`.

### The Cycle

```
┌──────────┐     ┌──────────┐     ┌──────────┐     ┌──────────┐
│ 1. SPEC  │ ──▶ │ 2. PLAN  │ ──▶ │ 3. BUILD │ ──▶ │ 4. CHECK │
│  draft   │     │  review  │     │ step by  │     │  verify  │
│          │ ◀── │ approve  │ ◀── │  step    │ ◀── │  vs spec │
└──────────┘     └──────────┘     └──────────┘     └──────────┘
     ▲                                                  │
     └──── new requirements → update spec ←─────────────┘
```

### 1. SPEC — Create or update the spec document

- Write specs in `docs/features/<version>/<name>.md` (e.g. `docs/features/v1.3/options-window-modernization.md`).
- Include sections: **Overview**, **Requirements** (numbered tables: `| # | Req | Details |`), **Design** (diagrams, palettes), **Implementation Plan** (phased steps), **Files Changed**.
- Keep requirements **testable** — each `#` should be verifiable.
- If a spec already exists, update it; don't create a duplicate.

### 2. PLAN — Review the spec with the user

- Present the spec and highlight **decisions needed** (open questions at the bottom).
- After user feedback, update the spec and **commit** it before any code.
- Commit message: `docs: add/update <feature> spec`

### 3. BUILD — Implement phase by phase

- Work through the spec's Implementation Plan **one phase at a time**.
- Each phase: make changes → build (0 errors) → commit.
- Commit messages: brief, prefixed with `feat:`, `fix:`, or `refactor:`.
- **Stop between phases** — let the user review before continuing.

### 4. CHECK — Verify against the spec

- After all phases, cross-check every requirement in the spec.
- For each `#` in the Requirements tables, confirm it works or note what's missing.
- Run the app to verify visually (`dotnet build` then launch).
- When all criteria are met, mark the spec **Status: Complete**.

### New Requirements Mid-Cycle

If the user asks for something not in the spec:
1. **Don't implement yet** — add it to the spec first.
2. Update the Requirements table and Implementation Plan.
3. Get user confirmation on the updated spec.
4. Then implement.

### Token Efficiency

- After code changes, build with `dotnet build src/Vimium.sln` and grep for `error|Build`.
- Use `get_minimal_context` before other graph tools.
- Commit often, messages brief — the commit history IS the log.
