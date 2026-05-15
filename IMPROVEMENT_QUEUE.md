# GZCTF Continuous-Improvement Queue

Working branch: `claude/continuous-improvement`
Loop plan: `/root/.claude/plans/compiled-squishing-neumann.md`

The agentic `/loop` appends one entry per iteration here. Targets that don't appear below are still eligible; targets marked `done`, `skip-permanent`, or `skip-this-loop` are not re-attempted in this loop session.

## Schema

Each entry:

```
### YYYY-MM-DD HH:MM — <target slug>
- status: done | reverted | skipped | needs-human-review
- tier: 1 | 2 | 3
- files: <comma-separated paths>
- verification: <commands run>
- result: <pass/fail summary>
- commit: <SHA or "(reverted)" or "(no commit)">
- notes: <optional one-liner>
```

## Iterations

_(empty — first iteration will append here)_

## Targets the loop must NOT touch (skip-permanent)

- `src/GZCTF/Extensions/ContainerServiceExtension.cs` — custom IPortMapper FIXME (architectural)
- `src/GZCTF/Providers/Container/DockerProvider.cs` — "After Docker.DotNet.Enhanced 3.132.0" TODO (upstream dep)
- `src/GZCTF/Controllers/ExerciseController.cs` — exercise mode TODO (feature scope)
- `src/GZCTF/Migrations/**` — DB migrations need human review
- `docker-compose.yml`, `Dockerfile*`, `assets/**` — infrastructure
