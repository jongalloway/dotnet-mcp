# Releasing

This repo uses **tag-driven releases**.

- The publish workflow runs from a version tag (for example: `v1.0.2`).
- The most common release mistake is tagging the wrong commit (often from a stale GitHub tab or a non-main branch).

## Golden Rule

Create the tag from the exact commit you intend to ship (usually the merge commit on `main`).

If you only remember one verification step, use this one:

```bash
git branch -r --contains v1.0.2
```

You should see `origin/main` in the output.

## Release notes

Recommended approach: keep the **authoritative release notes in the GitHub Release body**.

- This avoids having to commit versioned markdown files just to cut a release.
- A Git tag is the immutable “what code shipped” pointer; a GitHub Release is the editable “what changed” narrative.

There are two good workflows:

### Option A: Generate, then edit (fastest)

1) Tag + push.
2) Create a **draft** GitHub Release with generated notes.
3) Edit the notes in the GitHub UI, then publish.

Example:

```bash
git tag -a v1.0.2 -m "v1.0.2"
git push origin v1.0.2
gh release create v1.0.2 --draft --generate-notes
```

### Option B: Curate locally, but don’t commit (more control)

If you like writing notes in your editor, keep a local file that is **not committed** (for example under `temp/` or `artifacts-local/`) and pass it to `gh`.

Example:

```bash
gh release create v1.0.2 --draft --notes-file temp/release-notes/v1.0.2.md
```

If you do want a long-running, in-repo history, use [CHANGELOG.md](../CHANGELOG.md) as the curated archive and keep the GitHub Release body as the “announcement” view.

## Checklist (Command Line - recommended)

### 1) Preflight

- `git fetch --tags origin`
- `git switch main`
- `git pull --ff-only`
- `git status -sb` (must be clean)
- Confirm the commit you plan to release:
  - `git log -1 --oneline`

Optional but recommended:

- `dotnet build --project DotNetMcp/DotNetMcp.csproj`
- `dotnet test --solution DotNetMcp.slnx`

### 2) Create an annotated tag

Annotated tags add useful audit info (who/when/message).

```bash
git tag -a v1.0.2 -m "v1.0.2"
```

### 3) Verify the tag points to the right place

```bash
git show -s --format="%H %s" v1.0.2
git branch -r --contains v1.0.2
```

### 4) Push the tag

```bash
git push origin v1.0.2
```

### 5) Create the GitHub Release (notes)

Create the release after the tag exists on GitHub.

```bash
gh release create v1.0.2 --generate-notes
```

Or, if you have a notes file:

```bash
gh release create v1.0.2 --notes-file artifacts/release-notes/v1.0.2.md
```

### 6) Watch the publish workflow

- GitHub Actions should run on the tag.
- Confirm the publish run is using the tagged SHA.

## Checklist (GitHub Web UI)

The web UI is convenient, but it’s easier to accidentally tag the wrong commit (especially if the page was opened days ago).

### 1) Avoid stale UI state

- Do a hard refresh right before creating the release: `Ctrl+F5`

### 2) Explicitly select the target branch

- When creating the release, explicitly set **Target** to `main`.
- Do not trust whatever value is pre-selected.

### 3) Confirm the commit preview

- The “recent commits” preview should show the merge commit you expect.
- If it mentions a Dependabot branch or any non-main branch name, stop.

### 4) Create the tag + release

- Prefer creating a **draft release** first.
- Double-check: **Tag**, **Target**, and **commit preview**.
- Publish.

### 5) Sanity check after publish

- NuGet package exists for the version.
- MCP registry shows the new version.
- Install docs/badges still point at the intended floating major (`@1.*`).

## PowerShell helper (optional)

See `scripts/cut-release.ps1` for a safe “tag + verify + push (+ optional GitHub release)” helper.

Examples:

```powershell
# Dry-run (no changes), validates preflight checks
./scripts/cut-release.ps1 -Version 1.0.2 -WhatIf

# Create + push annotated tag
./scripts/cut-release.ps1 -Version 1.0.2

# Create + push tag, then create a draft GitHub Release with generated notes
./scripts/cut-release.ps1 -Version 1.0.2 -CreateGitHubRelease -Draft -GenerateNotes
```

It’s designed to be run from the repo root and intentionally refuses to proceed if:

- your working tree is dirty
- you’re not on `main`
- `main` can’t be fast-forwarded
- the tag already exists
