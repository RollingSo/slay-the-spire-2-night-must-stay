# Repository handoff

## Project identity

- Formal project name: **Slay the Spire 2 : Night Must Stay**
- Suggested repository slug: `slay-the-spire-2-night-must-stay`
- Default branch: `main`

The colon and spaces are valid in the display name but are not suitable for a
portable Git hosting slug. Keep the formal name in `manifest.json`,
`project.godot`, and documentation; use the suggested slug (or an equivalent
provider-safe slug) for GitHub/GitLab/Codeberg.

## Publish an authenticated clone

The local repository is already initialized and has a clean bootstrap commit.
Once an authenticated remote repository has been created, run from the project
root:

```powershell
git remote add origin https://github.com/<account>/slay-the-spire-2-night-must-stay.git
git push -u origin main
```

For SSH, use the equivalent `git@github.com:<account>/...git` URL. Do not put
access tokens in the remote URL or commit them to this repository.

## Release handoff

Before publishing a release, run:

```powershell
pwsh -File .\tools\export_guardian_mod.ps1
```

The export script synchronizes Guardian power icons, imports assets, builds the
PCK/release output, installs the mod when configured, and verifies SHA-256
hashes. Commit source and documentation; keep generated build/install output
ignored by `.gitignore`.

## Authentication and permissions

The repository cannot create a hosting account or authenticate a Git provider
by itself. A maintainer must create the empty remote and either sign in with a
provider credential helper or provide the remote URL after authentication. The
remote should be private or public according to the maintainer's licensing and
asset-distribution decision.
