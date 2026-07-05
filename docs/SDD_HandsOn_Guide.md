# Hands-On Guide: Spec-Driven Development with GitHub Spec Kit and Claude Code

This guide walks you through the same build described in the article, step by step. You will build a Personal Bookmark Manager using Spec-Driven Development. The application itself is simple by design. The point is to experience the SDD workflow end to end, not to build a complex product.

By the end you will have gone through every SDD phase: constitution, specify, clarify, plan, tasks, implement, PR, pipeline, acceptance, and iteration.

> **Note:** Spec Kit is an open source project maintained by GitHub at [github/spec-kit](https://github.com/github/spec-kit). The official documentation is at [github.github.com/spec-kit](https://github.github.com/spec-kit/) and is the authoritative source for anything that may have changed since this guide was written.

---

## What You Need Before You Start

**Accounts and access**
- A GitHub account
- An Anthropic account with access to Claude Code

**Tools installed on your machine**
- Claude Code (`npm install -g @anthropic-ai/claude-code`)
- Git
- Python 3.11 or later and [uv](https://docs.astral.sh/uv/) (for installing the Spec Kit CLI — install commands in Step 3)
- Node.js 18 or later
- .NET 8 SDK

**Familiarity assumed**
- You can run commands in a terminal
- You are comfortable with Git branches and pull requests

---

## What the Spec Kit Creates on Disk

Before you start, it helps to understand the folder structure `specify init` produces, so you know what you are looking at and what needs to go into Git.

```
your-project/
  CLAUDE.md                          ← Auto-generated. Review and commit it.
  .specify/
    memory/
      constitution.md                ← Your customised governance doc. Commit it.
    scripts/                         ← Spec Kit tooling. Commit it so teammates can run the workflow.
    templates/                       ← Spec Kit templates. Commit alongside scripts.
  specs/
    001-bookmark-management/         ← Created per feature, on a Git branch of the same name.
      spec.md                        ← Feature specification. Commit it.
      plan.md                        ← Implementation plan. Commit it.
      tasks.md                       ← Task list. Commit it.
      data-model.md                  ← If generated. Commit it.
```

**What to commit:** `CLAUDE.md`, everything under `.specify/memory/`, and everything under `specs/`. The `specs/` folder is where all your authored SDD artifacts live and is the most important thing to keep in version control.

**What to watch out for:** If you ever run `specify init --here --force` to upgrade Spec Kit, it will overwrite `.specify/memory/constitution.md` with the default template. Back it up first:

```bash
cp .specify/memory/constitution.md .specify/memory/constitution-backup.md
```

---

## Step 1: Install Claude Code

```bash
npm install -g @anthropic-ai/claude-code
claude login
```

Confirm it is working by navigating to any directory and running `claude`. You should see the interactive prompt. Type `exit` to leave.

---

## Step 2: Create Your Repository

1. Create a new GitHub repository called `bookmark-manager`. Initialise it with a README.
2. Clone it and enter the directory:

```bash
git clone https://github.com/your-username/bookmark-manager.git
cd bookmark-manager
```

---

## Step 3: Install uv, the Spec Kit, and Initialise for Claude Code

### Install uv

`uv` is a Python package manager used to install the Spec Kit CLI. If you do not have it:

```bash
# macOS and Linux
curl -LsSf https://astral.sh/uv/install.sh | sh

# Windows
powershell -ExecutionPolicy ByPass -c "irm https://astral.sh/uv/install.ps1 | iex"
```

Restart your terminal after installing, then confirm it works:

```bash
uv --version
```

### Install the Spec Kit CLI

Replace `vX.Y.Z` with the latest release tag from the [Spec Kit releases page](https://github.com/github/spec-kit/releases):

```bash
uv tool install specify-cli --from git+https://github.com/github/spec-kit.git@vX.Y.Z
```

Confirm the install:

```bash
specify version
```

### Initialise the project for Claude Code

```bash
specify init . --integration claude
```

This creates the full `.specify/` folder structure, installs the Spec Kit slash commands into Claude Code's skill directory (`.claude/skills/`), and generates `CLAUDE.md` at the repository root.

### Verify the installation

Open Claude Code in the project directory:

```bash
claude
```

Type `/speckit` and confirm you see the following core commands listed:

```
/speckit.constitution
/speckit.specify
/speckit.clarify
/speckit.checklist
/speckit.plan
/speckit.analyze
/speckit.tasks
/speckit.implement
```

If the commands do not appear, check that the Claude integration installed correctly by reviewing the `.claude/skills/` directory. Type `exit` to leave Claude Code for now.

### Review and customise `CLAUDE.md`

`specify init` generates `CLAUDE.md` at the repository root. This file tells Claude Code about the project and its SDD workflow. Open it now and add any project-specific conventions — your branching strategy, commit message format, or technology constraints — that are not already there.

When a new team member joins, `CLAUDE.md` is the first file they should read. It is the operating manual for how the team works, not documentation about the application.

### Commit everything

```bash
git add .
git commit -m "SDD: Initialise Spec Kit with Claude Code integration"
git push
```

---

## Step 4: Phase 1 — Constitution

The constitution is the governing document for the entire project. It defines non-negotiable architectural principles that every downstream phase — plan, tasks, and implementation — must honour. It lives at `.specify/memory/constitution.md` and is automatically referenced by every Spec Kit command.

Open Claude Code:

```bash
claude
```

Run:

```
/speckit.constitution This project is a Personal Bookmark Manager. Users save URLs with a title, tags, notes, and a read or unread status. They can filter bookmarks and view a summary dashboard. Single user, no authentication required. Backend: .NET 8 Web API. Frontend: React. Data: Entity Framework Core In-Memory. Repository pattern for data access. Clean separation between controllers and business logic. RESTful API design.
```

Claude Code will generate the constitution and write it to `.specify/memory/constitution.md`.

Review the output carefully:
- Does it reflect the architectural decisions you actually want?
- Has it surfaced any implicit preferences you had not stated explicitly?
- Is anything missing that should govern every future feature?

Edit `.specify/memory/constitution.md` directly until it accurately reflects your intentions. Then commit:

```bash
git add .specify/memory/constitution.md
git commit -m "SDD: Constitution"
git push
```

---

## Step 5: Phase 2 — Specify the First Feature

Each feature gets its own Git branch and its own folder under `specs/`. The Spec Kit creates both automatically when you run `/speckit.specify`.

Start with Bookmark Management. Tagging is included here rather than as a separate feature — tags are a property of a bookmark, not a standalone capability, and they belong in the same specify/plan/implement cycle as the core CRUD operations.

In Claude Code, run:

```
/speckit.specify Build the core bookmark management feature. A user can create a bookmark with a URL, title, tags, notes, and a read or unread status. A user can update any of these fields. A user can delete a bookmark. The system must prevent saving a duplicate URL and must return a clear error when a duplicate is attempted. Tags are short text labels attached to a bookmark. A bookmark can have zero or more tags. Tags must be trimmed of leading and trailing whitespace before saving. Tags are case-insensitive for filtering purposes.
```

Spec Kit creates a new branch (`001-bookmark-management`) and writes the specification to `specs/001-bookmark-management/spec.md`.

Review the specification. Good behavioural specs use precise language: must, should, must not, out of scope. A spec that says "the user can manage bookmarks" is not a specification. A spec that says "the system must prevent saving a URL that has already been saved and return a clear duplicate error" is. Correct anything vague.

Commit both the spec artifacts and `.specify/feature.json`:

```bash
git add specs/ .specify/feature.json
git commit -m "SDD: Specify bookmark management"
git push
```

> **Why commit `feature.json`?** Spec Kit writes `.specify/feature.json` when `/speckit.specify` runs. It contains a single pointer — the active feature directory — and looks like this:
> ```json
> {
>   "feature_directory": "specs/001-bookmark-management"
> }
> ```
> Committing it means any teammate who pulls your branch immediately has the correct feature context. Without it, Spec Kit does not know which `specs/` folder to read and subsequent phase commands will fail or prompt unexpectedly. `feature.json` only changes when a new feature is specified, so subsequent phase commits (clarify, plan, tasks) only need `git add specs/`.

### Specifying Features 2, 3, and 4

Each feature follows the same pattern: return to `main`, then run `/speckit.specify` and Spec Kit creates the next numbered branch and folder automatically.

```bash
# Return to main before each new feature
git checkout main
git pull
```

Then open Claude Code and run `/speckit.specify` with the feature description. Repeat the review and commit after each one, including `.specify/feature.json` each time:

```bash
git add specs/ .specify/feature.json
git commit -m "SDD: Specify [feature name]"
git push
```

**Feature 2 — Filtering and Search** (`specs/002-filtering-and-search`): A user can filter bookmarks by tag, by read status, or by a text search across title and notes. Filters can be combined. No results returns an empty list, not an error.

**Feature 3 — Summary Dashboard** (`specs/003-summary-dashboard`): Shows total bookmarks, total unread, and a breakdown by tag. Reflects current data without a page reload.

**Feature 4 — Favourites** (`specs/004-favourites`): A user can mark any bookmark as a favourite. Favourites appear in a dedicated section at the top of the bookmark list. The summary dashboard shows a count of favourite bookmarks. Removing a favourite flag does not delete the bookmark.

> **Spec maintenance note:** Before specifying Favourites, update `specs/003-summary-dashboard/spec.md` to mention the favourites count. The dashboard spec was written before Favourites existed — keep it consistent. This is real work and should be budgeted.

> **Note:** Spec Kit detects the active feature from your current Git branch. Always confirm you are on the correct branch before running any phase command.

---

## Step 6: Phase 3 — Clarify

Run clarify after specifying each feature, before moving to plan. This is where Claude Code reads your spec and asks about the cases you did not address.

Switch to the feature branch you are working on, open Claude Code, and run:

```
/speckit.clarify
```

Or focus it on a specific concern:

```
/speckit.clarify Focus on edge cases in tag handling and duplicate URL detection.
```

For the Bookmark Manager, expect questions like:
- What happens if a tag is only whitespace after trimming — reject it or ignore it?
- Is the duplicate URL check case-sensitive?
- What should the dashboard show when there are no bookmarks yet?
- Is text search a substring match or exact match?

For each question, update the specification. Do not answer in conversation and leave `spec.md` unchanged. The spec is the record.

Commit after clarify:

```bash
git add specs/
git commit -m "SDD: Clarify bookmark management"
git push
```

---

## Step 7: Phase 4 — Checklist (Optional but Recommended)

The checklist command validates your specification quality before you move to planning. For production features, treat it as a required gate.

```
/speckit.checklist
```

Review any items flagged as incomplete or unclear. Fix them in `spec.md` before continuing.

---

## Step 8: Phase 5 — Plan

```
/speckit.plan The application uses .NET 8 Web API for the backend and React for the frontend. Entity Framework Core In-Memory for data persistence. REST API with clean controller and service separation following the Repository pattern.
```

Spec Kit writes the plan to the `specs/` folder for the currently active branch — for Feature 1 this is `specs/001-bookmark-management/plan.md`. For subsequent features, the path will reflect their branch name.

Review the plan against your constitution. Check for:
- Architectural decisions that conflict with the constitution
- Gaps the spec implies but the plan does not address

You are doing review work here, not authoring. Correct where needed, then commit:

```bash
git add specs/
git commit -m "SDD: Plan bookmark management"
git push
```

---

## Step 9: Phase 6 — Analyze

Before generating tasks, run the cross-artifact consistency check:

```
/speckit.analyze
```

This checks for contradictions between your constitution, specification, and plan before implementation starts. Any conflict flagged here is significantly cheaper to fix than after code has been generated. Address any issues raised, then commit:

```bash
git add specs/
git commit -m "SDD: Analyze bookmark management"
git push
```

---

## Step 10: Phase 7 — Tasks

```
/speckit.tasks
```

Spec Kit decomposes the plan into a dependency-ordered task list and writes it to `tasks.md` in the active feature's `specs/` folder.

Review the task list. Tasks should be granular: each one should produce a small, reviewable unit of code. If any task feels too large, ask Claude Code to decompose it further.

Commit:

```bash
git add specs/
git commit -m "SDD: Tasks bookmark management"
git push
```

---

## Step 11: Phase 8 — Implement

```
/speckit.implement
```

For complex features, implement in stages rather than all at once — the Spec Kit documentation recommends this explicitly to avoid overwhelming the agent's context window. Start with core functionality, validate it works, then continue.

During implementation, apply this discipline without exception: when the generated code diverges from the specification, correct the specification first, then correct or regenerate the code. Never fix the code without updating the spec. If you only fix the code, the spec becomes unreliable and every future generation may reproduce the original error.

Run the tests after each stage before continuing.

When implementation is complete, commit:

```bash
git add .
git commit -m "Implement: bookmark management"
git push
```

---

## Step 12: PR, Pipeline, and Acceptance

Push your feature branch and open a pull request against `main`:

```bash
git push origin 001-bookmark-management
```

Open the pull request on GitHub.

**CI pipeline:** If you have a GitHub Actions workflow configured, it will run your tests automatically on the PR. If you do not have one set up yet, this step becomes a manual test run — run `dotnet test` for the backend and `npm test` for the frontend locally before merging. Setting up a full CI pipeline is outside the scope of this guide; the [GitHub Actions quickstart for .NET](https://docs.github.com/en/actions/use-cases-and-examples/building-and-testing/building-and-testing-net) is a good starting point when you are ready.

Review the PR against the specification, not just against the diff. Merge when tests pass and the review is complete.

After merging, return to `main`:

```bash
git checkout main
git pull
```

---

## What to Check When You Are Done

By the end you should have:

- `CLAUDE.md` at the repository root, reviewed and customised
- `.specify/memory/constitution.md` accurately describing the system's architecture and constraints
- `specs/001-bookmark-management/`, `specs/002-filtering-and-search/`, `specs/003-summary-dashboard/`, and `specs/004-favourites/` each containing `spec.md`, `plan.md`, and `tasks.md`
- A `.gitignore` excluding build artefacts but keeping all SDD artifacts committed
- A Git history showing one commit per SDD phase across all four features
- A working Personal Bookmark Manager application
- All spec artifacts versioned alongside the code

If anything is missing, trace back to the phase where the gap occurred. The most common gaps are a constitution that was too thin, specifications that used vague language, and a clarify phase that was skipped or rushed.

---

## Where to Go Next

This build is spec-first — the first of three SDD maturity levels. The next level, spec-anchored, adds structured CI validation that mechanically checks implementation compliance against the specification.

For deeper reading on the methodology, start with Birgitta Böckeler's tool comparison on Martin Fowler's site and Hari Krishnan's enterprise adoption analysis on InfoQ. For the Spec Kit itself, the official documentation is at [github.github.com/spec-kit](https://github.github.com/spec-kit/) and is the authoritative source for anything that may have changed since this guide was written.
