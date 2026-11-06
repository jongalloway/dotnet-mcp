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

## Release preparation and publication

Preparation and publication are separate gates. [#546](https://github.com/jongalloway/dotnet-mcp/issues/546) prepares compatibility evidence and the curated [Unreleased changelog](../CHANGELOG.md); [#562](https://github.com/jongalloway/dotnet-mcp/issues/562) tracks publication and validation of the published package.

The provisional candidate is **1.2.0-rc.1**, retaining `@1.*` install links. The server is supported as an MCP application, not as a library: an MCP SDK dependency major bump alone does not require a server major bump. Compare supported tool inputs/results and runtime requirements with 1.1.2 before confirming the version. Existing ordinary legacy calls must continue working. Additional structured data, corrected error flags, concise human-readable build output, and client-dependent confirmation are called out in the changelog; byte-for-byte CLI text is not a structured result contract. An intentional incompatible supported-contract change requires an explicit major-version decision and corresponding installation-link migration, not an unexplained minor release.

For preparation, pack a local candidate with `-p:MinVerVersionOverride=1.2.0-rc.1` if necessary; do not hardcode a project version or create a release tag just to test. Record the source SHA and whether local changes were included: an informational version containing a SHA does not establish a clean source tree. Run the release workflow against the exact committed candidate ref, preserve job outcomes and package SHA-256/provenance, and distinguish source tests from packaged execution.

The publisher must confirm that the approved changes are on `main`, verify the exact tag commit, and publish via the existing tag-triggered workflows. Create a GitHub **prerelease**, not a stable/latest release, using curated notes:

```powershell
gh release create v1.2.0-rc.1 --verify-tag --prerelease --latest=false --notes-file C:\release-notes\candidate.md
```

This command assumes the annotated tag already exists remotely; pushing that tag triggers NuGet publication independently of whether a GitHub Release is still a draft. The PowerShell helper does not currently set the GitHub prerelease flag, so do not use its `-CreateGitHubRelease` option for a candidate without separately ensuring the correct designation.

After publication, install the **exact published version** in isolated consumer environments and verify MCP behavior through the installed tool and both dnx entry points. Confirm registry publication/status explicitly, including any prerelease restrictions. Local-feed package tests alone do not validate the public artifact. Retain the published package hash, source commit, workflow URLs, and SDK/runtime identities.

Use official SDK release metadata to decide when GA execution can run. Replay the preserved RC3 package with `rc3_run_id` on GA without rebuilding it, and identify separately which checks exercise the actual published package. An unavailable GA SDK is a pending gate, not a passing test; fixtures only validate selection policy. Do not claim product-specific Apps rendering or full-catalog governance readiness from wire/safe-subset tests.

### Release note authoring

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

- `dotnet build "$PWD/DotNetMcp/DotNetMcp.csproj"` (uses the RC3 minimum or compatible installed GA SDK and builds both targets)
- `dotnet test --solution DotNetMcp.slnx`

Before tagging, run `release-scenarios.yml` with `os=all` against the intended source ref and confirm its SHA and every job outcome. It covers both target frameworks, Windows/Linux source scenarios, and the packaged-tool matrix without publishing. See [Testing](testing.md#release-gate-scenario-tests-manual--workflow_dispatch) for commands and evidence artifacts.

Build and publish setup automatically adopts the stable .NET 11.0.1xx SDK once official release metadata announces it; until then it uses the exact RC3 baseline. Exact-RC3 regression lanes remain, and the packaged compatibility artifact is always built with RC3. Retain `rc3-tool-package` (including provenance and hash) before its 90-day expiry to verify that same package on GA later with `rc3_run_id`. GA runtime roll-forward requires GA to be installed; it does not install runtimes automatically. Do not treat pre-GA fixture checks as proof of GA execution.

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
