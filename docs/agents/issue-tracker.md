# Issue tracker: GitHub

Issues and PRDs for this repository live as GitHub Issues. Use the `gh` CLI for all operations.

## Conventions

- Create: `gh issue create --title "..." --body "..."`
- Read: `gh issue view <number> --comments`
- List: `gh issue list --state open --json number,title,body,labels,comments`
- Comment: `gh issue comment <number> --body "..."`
- Apply or remove labels: `gh issue edit <number> --add-label "..."` / `--remove-label "..."`
- Close: `gh issue close <number> --comment "..."`

Infer the repository from `git remote -v`.

## Pull requests as a triage surface

**PRs as a request surface: no.**

GitHub Issues are the request and triage surface. Pull requests are implementation and review surfaces.

## Skill operations

- “Publish to the issue tracker” means creating a GitHub Issue.
- “Fetch the relevant ticket” means running `gh issue view <number> --comments`.
- Use GitHub issue dependencies for blocking relationships when available.
- A ticket is ready when all blocking issues are closed.
- Claim work with `gh issue edit <number> --add-assignee @me`.
