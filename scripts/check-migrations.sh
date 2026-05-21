#!/usr/bin/env bash
#
# Lint EF Core migrations for the "AddColumn without backfill" pattern
# that has bitten us twice already (AllowUserSubmissions, IntervalSeconds).
#
# When a migration adds a NOT NULL column with a `defaultValue:` other
# than 0, EF on PostgreSQL applies it only to the column default — old
# rows stay at the type default (0 / false / NULL). Every such
# migration needs an explicit `migrationBuilder.Sql("UPDATE ... SET ...")`
# call to backfill, or the running app sees nonsensical zeros in the
# admin UI until something else writes the row.
#
# Usage: ./scripts/check-migrations.sh
#   exit 0 if clean, 1 if a migration is missing a backfill.
#
# Heuristic: for every migration file under src/GZCTF/Migrations/, scan
# every `AddColumn<...>(` block; if the block contains a
# `defaultValue:` that isn't `(byte)0`, `0`, `false`, or `null`, require
# a `migrationBuilder.Sql(...)` somewhere in the same file. Best-effort
# — false positives are possible (e.g., the new col is genuinely
# allowed to start at its declared default), but a one-line override
# comment can silence the check.

set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
MIG_DIR="$ROOT/src/GZCTF/Migrations"

if [[ ! -d "$MIG_DIR" ]]; then
  echo "check-migrations: $MIG_DIR not found" >&2
  exit 2
fi

fail=0
for f in "$MIG_DIR"/*.cs; do
  [[ "$f" == *.Designer.cs ]] && continue
  [[ "$(basename "$f")" == "AppDbContextModelSnapshot.cs" ]] && continue

  # Extract every defaultValue: line *inside an AddColumn(...)* block
  # that isn't one of the trivial defaults. CreateTable() defaultValues
  # don't need a backfill — there are no existing rows on first apply.
  defaults_in_addcolumn="$(awk '
    /migrationBuilder\.AddColumn</ { in_addcol = 1 }
    in_addcol { print NR": "$0 }
    in_addcol && /\);/ { in_addcol = 0 }
  ' "$f")"

  nontrivial="$(echo "$defaults_in_addcolumn" | grep -E 'defaultValue:\s*' \
    | grep -vE 'defaultValue:\s*(\(byte\)|\(short\)|\(int\)|\(uint\)|\(long\)|\(ulong\))?\s*0[uUlLmMdDfF]?\s*[,)]' \
    | grep -vE 'defaultValue:\s*false\s*[,)]' \
    | grep -vE 'defaultValue:\s*null\s*[,)]' \
    | grep -vE 'defaultValue:\s*""\s*[,)]' \
    | grep -vE 'defaultValue:\s*new Guid\("00000000-0000-0000-0000-000000000000"\)\s*[,)]' \
    || true)"

  if [[ -n "$nontrivial" ]]; then
    if ! grep -q 'migrationBuilder\.Sql(' "$f"; then
      if grep -q '// check-migrations: backfill-not-needed' "$f"; then
        continue
      fi
      echo "::error file=$f::Migration has a non-trivial defaultValue but no explicit backfill SQL. Add a migrationBuilder.Sql() that updates existing rows, or annotate with '// check-migrations: backfill-not-needed' to silence."
      echo "  Offending lines:"
      echo "$nontrivial" | sed 's/^/    /'
      fail=1
    fi
  fi
done

if [[ $fail -ne 0 ]]; then
  echo
  echo "check-migrations: $fail file(s) failed. See https://github.com/dimasma0305/GZCTF#migrations for the convention."
  exit 1
fi

echo "check-migrations: all migrations OK ($(ls "$MIG_DIR"/*.cs | grep -v Designer | grep -v ModelSnapshot | wc -l) files scanned)"
