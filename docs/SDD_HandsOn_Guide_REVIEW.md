# Review: Hands-On Guide — Spec-Driven Development with GitHub Spec Kit and Claude Code

**Reviewed by:** PhiLong-Ho
**Date:** 2026-07-05
**Status:** Draft — awaiting reviewer feedback and personal experience input

---

## Purpose of This Review

This document evaluates `SDD_HandsOn_Guide.md` against a structured set of criteria. Each section below states the criterion, what the guide does well, what it could improve, and where applicable, flags questions the guide leaves unanswered. The final section is reserved for personal experience and feedback.

---

## 1. Completeness

**Does the guide cover the full SDD lifecycle end-to-end?**

What works:
- All nine named phases (constitution → specify → clarify → checklist → plan → analyze → tasks → implement → PR/acceptance) are present and ordered.
- Feature 1 (Bookmark Management) is walked through in detail across every phase.
- Features 2–4 are described at the specify phase with enough context to follow the pattern.

What could improve:
- Features 2–4 are never revisited after the specify step. The reader is expected to repeat clarify/plan/tasks/implement on their own with no per-feature guidance.
- The "PR, Pipeline, and Acceptance" section (Step 12) is thin — CI setup is explicitly deferred, and acceptance testing is reduced to "run dotnet test and npm test."
- No post-implementation phase: there is no guidance on retrospectives, spec archival, or capturing lessons learned.

Unanswered questions:
- What does acceptance actually look like beyond passing tests? Who signs off?
- Is there a phase for spec cleanup or archival after a feature ships?

---

## 2. Clarity & Actionability

**Can a reader with no prior SDD knowledge follow every step?**

What works:
- Commands are concrete and copy-pasteable. File paths are explicit.
- The folder structure diagram (lines 34–48) is clear and well-annotated.
- The "What to commit" callout after each phase removes ambiguity about version control.
- The `feature.json` explanation (lines 221–227) is a good example of surfacing non-obvious mechanics.

What could improve:
- Some phase descriptions blend *what the tool does* with *what you should do as the human*. The "Plan" section says "You are doing review work here, not authoring" — this framing is useful but inconsistent across phases.
- Terminology drifts: sometimes "Spec Kit", sometimes "the Spec Kit", sometimes "it". Minor, but noticeable over a long read.
- The distinction between `/speckit.checklist` (optional) and `/speckit.analyze` (recommended) is fuzzy. Both seem to validate — what's the practical difference?

Unanswered questions:
- What does the reader see when a phase runs successfully vs. when it fails? No sample output is shown.
- How long should each phase take? The reader has no way to know if their 20-minute plan phase is normal or stuck.

---

## 3. Technical Accuracy

**Are tool versions, commands, and flags correct and internally consistent?**

What works:
- Installation commands for `uv`, the Spec Kit CLI, and Claude Code follow each tool's documented approach.
- The technology stack (.NET 8, React, EF Core In-Memory) is stated consistently across the constitution and plan steps.
- The Spec Kit command list (lines 136–144) matches the current `/speckit.*` namespace.

What could improve:
- The Spec Kit install command uses a `vX.Y.Z` placeholder (line 108). A concrete version plus a "replace with latest" note would be safer for first-time users.
- No platform-specific troubleshooting: Windows, macOS, and Linux each have different failure modes for `uv`, Node, and .NET installs. None are addressed.
- The guide assumes `npm install -g` works without permissions issues. Many users will hit EACCES on Linux/macOS and need `npx` or a prefix configuration.

Unanswered questions:
- What minimum versions of Node.js, .NET, and Python are actually enforced by the tools?
- Does the Spec Kit CLI work with Python 3.12+? (3.11 is specified, but users may have newer.)

---

## 4. Methodology Fidelity

**Does the guide faithfully represent spec-first principles?**

What works:
- The "correct the spec first, then the code" rule (lines 373–374) is stated forcefully and correctly.
- The clarify phase is positioned as non-negotiable (line 261: "Run clarify after specifying each feature, before moving to plan") — this is good methodology.
- The spec maintenance note (line 253) about updating Feature 3's spec when Feature 4 introduces favourites is a realistic touch that many guides omit.

What could improve:
- The guide says "Review the specification. Good behavioural specs use precise language: must, should, must not, out of scope" (line 211) but doesn't show a before/after example. A concrete contrast would anchor this advice.
- The checklist phase is marked optional. If SDD is taken seriously, quality gates should not be optional — or the guide should explain when it's safe to skip.
- The implement phase (Step 11) is the shortest section despite being where most methodology violations happen. There's no guidance on how to keep the agent honest to the spec during code generation.

Unanswered questions:
- What distinguishes a "spec-first" project from a "spec-annotated" one? A concrete comparison would help.
- How does the methodology handle specs that are intentionally vague (spikes, research stories, UX exploration)?

---

## 5. Real-World Practicality

**Does the guide address failure modes, cost, and realistic scope?**

What works:
- The "What to watch out for" callout about `specify init --here --force` overwriting the constitution (lines 52–56) is exactly the kind of practical warning guides need.
- The scope is deliberately simple (a Bookmark Manager) — this is the right call for a hands-on tutorial.
- The four-feature structure is realistic: it's enough to show cross-feature dynamics without being overwhelming.

What could improve:
- **Cost is never mentioned.** Running all phases with Claude Code across four features will consume tokens. A rough estimate (even a range) would set expectations and prevent surprise bills.
- **Context window limits are acknowledged once** (line 371: "implement in stages rather than all at once") but no concrete splitting strategy is given. When should you stop and commit? At task boundaries? At file boundaries? At test boundaries?
- **No unhappy paths are demonstrated.** Every phase "works." What does the reader do when `/speckit.plan` produces nonsense? When `/speckit.implement` generates code that doesn't compile? The guide shows only the golden path.
- **Time investment is not estimated.** Is this a 2-hour exercise? A full day? A weekend? The reader has no way to plan.

Unanswered questions:
- How much does this cost in Claude Code API tokens, roughly?
- What's the expected wall-clock time to complete all four features?
- Which phases can be done offline, and which require network access?
- What do you do when a phase produces unusable output — re-prompt? Edit manually? Start over?

---

## 6. Team & Collaboration

**Does the guide cover multi-developer workflows?**

What works:
- The `feature.json` commit rationale (lines 221–227) shows awareness of team context — "any teammate who pulls your branch immediately has the correct feature context."
- `CLAUDE.md` is described as "the first file a new team member should read" (line 152) — correct framing.

What could improve:
- **The entire guide is single-developer.** There is no discussion of:
  - Two developers specifying different features in parallel and how their specs interact.
  - Merge conflicts on `spec.md`, `plan.md`, or `tasks.md`.
  - How code review works when the reviewer needs to validate against a spec they didn't write.
  - Who "owns" the spec — the author, the team, or the person who runs `/speckit.specify`?
- The branching model is implicit: one branch per feature, numbered sequentially. But numbering implies a global ordering — what happens when two developers both create "003-something" on different machines?

Unanswered questions:
- How are merge conflicts on spec artifacts resolved? Do you re-run phases or hand-edit?
- How does a new developer onboard into an in-flight SDD project?
- What does a code review look like when the reviewer must verify implementation against a spec they didn't author?
- Is there a recommended branch naming convention, or is the `NNN-feature-name` pattern required by Spec Kit?

---

## 7. Verification & Quality

**Are there clear exit criteria, testing strategy, and consistency checks?**

What works:
- The analyze phase (`/speckit.analyze`) as a cross-artifact consistency check is a good concept — catching constitution/spec/plan contradictions before code is cheaper.
- The checklist phase provides a spec-quality gate, even if optional.
- "Review the PR against the specification, not just against the diff" (line 399) is a concise, powerful instruction.

What could improve:
- **Testing strategy is absent.** The guide says "run the tests" and "run dotnet test for the backend and npm test for the frontend" but never discusses:
  - What kinds of tests to write (unit, integration, contract, end-to-end).
  - How tests should trace back to spec requirements.
  - Whether tests should be generated by Claude Code or written manually.
  - At what phase test authoring happens (during implement? during plan?).
- **Exit criteria are implicit.** Each phase says "review and commit." But review against what? There's no rubric for "this spec is good enough to proceed."
- The PR review section says "Review the PR against the specification" but doesn't explain *how* — do you open both side by side? Is there a checklist? A tool?

Unanswered questions:
- What does a good spec look like vs. a bad one? A before/after example would make the advice actionable.
- At what phase are tests written? Plan? Tasks? Implement?
- How do you know when a spec phase is "done" and you can safely move to the next?
- How does CI verify spec compliance automatically? (Alluded to in the conclusion but never explained.)

---

## 8. Maintenance & Evolution

**Does the guide address how specs evolve over time?**

What works:
- The spec maintenance note (line 253) acknowledges that Feature 4 requires updating Feature 3's spec — this is realistic and honest.
- The "correct the specification first, then correct the code" discipline (lines 373–374) establishes the right priority.

What could improve:
- **The mid-implementation change workflow is stated as a principle but never demonstrated.** The guide says to update the spec before the code, but doesn't answer: after updating the spec, do you re-run clarify? Re-run plan? Re-run tasks? Or just hand-edit the downstream artifacts?
- **Cross-feature cascading changes are under-specified.** Feature 4 modifies Feature 3's spec. Does Feature 3 need to be re-planned? Re-implemented? Re-tested? What's the actual workflow?
- **Constitution evolution is not addressed.** The constitution is created in Step 4 and never revisited. What if the team later decides to switch from In-Memory to PostgreSQL? Does that require a constitution amendment? Do existing specs need to be re-validated?
- **Spec deprecation and archiving are not mentioned.** After four features ship, do the specs stay in `specs/` forever? Are they ever cleaned up?

Unanswered questions:
- What is the exact process for a mid-implementation requirement change?
- Can the constitution be amended after initial creation? What's the process?
- How are cascading spec changes across already-merged features handled?
- What happens to spec artifacts when a feature is deprecated or removed?
- What if a feature is reverted after merge — do you keep or delete the spec?

---

## 9. Tooling & Environment

**Is the setup complete, versioned, and troubleshootable?**

What works:
- The three-tool installation sequence (Claude Code → uv → Spec Kit) is logically ordered.
- The verification step for each tool (`claude`, `uv --version`, `specify version`, `/speckit` command list) is good practice.
- The `--integration claude` flag on `specify init` is explicitly shown, which prevents a common mistake.

What could improve:
- No troubleshooting section. Common issues — `specify` not found in PATH, Claude Code not recognizing `/speckit` commands, Python version conflicts, Node.js EACCES errors — are not addressed.
- The Spec Kit install uses `git+https://` which requires Git to be configured. Users behind corporate proxies or with SSL inspection will hit issues. No proxy/air-gapped guidance.
- The guide requires Python 3.11, Node.js 18, and .NET 8 — three separate runtime ecosystems. Version managers (`nvm`, `pyenv`, `asdf`) are not mentioned, but many developers use them and may encounter path conflicts.

Unanswered questions:
- What do you do if `/speckit` commands don't appear in Claude Code after `specify init`?
- How do you upgrade Spec Kit without losing your constitution?
- Does this workflow work behind a corporate proxy?
- What's the minimum set of tools needed if you only want to *read* specs (not generate them)?

---

## 10. Scope, Maturity, and Next Steps

**Does the guide clearly define its boundaries and the path forward?**

What works:
- The intro (line 4) is honest: "The application itself is simple by design. The point is to experience the SDD workflow end to end, not to build a complex product."
- The "Where to Go Next" section points to external resources and introduces the maturity-level concept.

What could improve:
- **The three maturity levels are name-dropped but not explained.** "Spec-first" is the guide's level. "Spec-anchored" is mentioned as the next step with a one-line description. The third level is never named. If maturity levels are a framework, explain all three so the reader knows the full trajectory.
- **No concrete next step for spec-anchored.** What would a minimal spec-compliance CI check look like? A 10-line GitHub Actions snippet would make this tangible.
- The guide references Birgitta Böckeler's tool comparison and Hari Krishnan's enterprise adoption analysis but doesn't cite specific URLs, article titles, or key takeaways. The reader has to go searching.

Unanswered questions:
- What are all three SDD maturity levels, and what distinguishes each?
- What would a minimal spec-compliance CI check look like in practice?
- What are the key takeaways from the cited external references?
- After completing this guide, what's the recommended first real-world project size to attempt?

---

## Summary Matrix

| Dimension | Strengths | Gaps |
|-----------|-----------|------|
| Completeness | Full lifecycle covered; Feature 1 detailed end-to-end | Features 2–4 not re-visited after specify; acceptance phase is thin |
| Clarity | Copy-pasteable commands; explicit file paths; good folder diagram | No sample output shown; phase duration unstated; terminology drifts |
| Technical accuracy | Correct install sequences; consistent stack references | Placeholder version (`vX.Y.Z`); no platform-specific troubleshooting |
| Methodology fidelity | Strong spec-first discipline; clarify is mandatory; spec maintenance note is realistic | No before/after spec example; checklist marked optional without rationale |
| Practicality | Honest scope; good `feature.json` and `--force` warnings | Cost and time unstated; no unhappy paths demonstrated; context-window splitting vague |
| Team workflow | `feature.json` rationale; `CLAUDE.md` as onboarding doc | Entirely single-developer; no merge-conflict or parallel-feature guidance |
| Verification | Analyze phase concept; "review against the spec" instruction | No testing strategy; no exit criteria per phase; no before/after spec example |
| Maintenance | Spec-update-before-code rule; cross-feature awareness | No mid-implementation change workflow; no constitution evolution; no spec archival |
| Tooling | Verification steps; explicit `--integration` flag | No troubleshooting; no proxy/air-gapped guidance; three runtimes, no version-manager notes |
| Scope & next steps | Honest about simplicity; external references provided | Maturity levels unexplained; no concrete CI example; external refs lack specifics |

---

## Unanswered Questions — Consolidated

1. How do multiple developers work on different features in parallel?
2. How are merge conflicts on spec artifacts resolved?
3. How does a new developer onboard into an in-flight SDD project?
4. What do you do when a phase produces bad or unusable output?
5. What is the exact process when requirements change mid-implementation?
6. How are cascading spec changes across already-merged features handled?
7. What tests should be written, at what level, and how do they trace to spec requirements?
8. What are the objective exit criteria for each phase?
9. Can the constitution be amended mid-project? What's the process?
10. How do you introduce SDD to an existing (non-greenfield) codebase?
11. How does CI verify spec compliance automatically?
12. What's the rough token/cost footprint of running all phases?
13. Where do non-functional requirements (performance, security, accessibility) live?
14. What are all three SDD maturity levels, and what distinguishes each?
15. What makes a spec requirement "testable"? A rubric or examples would help.
16. How do you decide what belongs in one feature vs. multiple?
17. What happens to spec artifacts when a feature is deprecated, reverted, or removed?
18. How do you upgrade Spec Kit without losing your constitution and existing specs?
19. Does this workflow work behind a corporate proxy or in air-gapped environments?
20. What's the recommended first real-world project size after completing this guide?
21. What do code reviewers look for when verifying implementation against a spec?

---

## Personal Experience & Feedback

<!-- 
Add your own experience here. Some prompts to get started:

- Which phases matched your real-world experience, and which didn't?
- What surprised you — positively or negatively — when applying this workflow?
- Where did you deviate from the guide, and why?
- What would you add, remove, or change based on your own SDD practice?
- How did the tooling hold up in practice? Any reliability issues?
- Did the spec-first discipline produce measurably better outcomes? In what way?
- What did your teammates or reviewers think of the SDD artifacts?
- If you were to onboard a junior developer to SDD, what would you teach differently from this guide?
- What's the biggest risk you see in adopting this workflow at scale?
-->

---

## Reviewer Notes

*Add your observations, agreements, disagreements, and edits to the analysis above. This is a working document — mark sections as confirmed, amended, or rejected as you review.*
