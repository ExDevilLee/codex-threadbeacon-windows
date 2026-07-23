# Windows README Restructure Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebuild the Windows Chinese and English READMEs around the current macOS documentation structure without losing Windows-specific behavior or privacy boundaries.

**Architecture:** Treat the macOS README section order as the shared product-documentation contract and map each section to native Windows installation, automation, and distribution behavior. Keep detailed engineering history in existing supporting documents rather than the top-level README.

**Tech Stack:** GitHub-flavored Markdown, repository screenshots, PowerShell link validation, .NET repository-readiness tests

---

### Task 1: Restructure the Chinese README

**Files:**
- Modify: `README.md`

- [ ] Replace the chronological POC status narrative with the approved product-oriented section order.
- [ ] Preserve Windows quick-start, installation, privacy, automation, Hook, and limitation facts.
- [ ] Keep all six localized screenshots in a compact paired preview.

### Task 2: Restructure the English README

**Files:**
- Modify: `README-EN.md`

- [ ] Mirror the Chinese heading order and feature group coverage.
- [ ] Use natural English while preserving identical platform and privacy claims.
- [ ] Keep all six English screenshots in the corresponding preview positions.

### Task 3: Verify and publish the documentation

**Files:**
- Verify: `README.md`
- Verify: `README-EN.md`

- [ ] Resolve every relative Markdown link and image target from the repository root.
- [ ] Compare heading order and run the complete .NET test suite and Release build.
- [ ] Scan the staged diff for secrets and machine-specific paths.
- [ ] Commit the documentation change and push `main`.
