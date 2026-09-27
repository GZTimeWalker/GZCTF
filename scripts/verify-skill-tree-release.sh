#!/usr/bin/env bash
# ST28 release gate. Builds the production image, runs it against PostgreSQL and Redis,
# and verifies the deployment contract, the upgraded-database backfill, and the disk and
# S3 storage providers. Exits non-zero on the first failed assertion.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

image="${IMAGE:-gzctf-skill-tree:verification}"
network="${NETWORK:-gzctf-skill-tree-verification}"
db="${DB_CONTAINER:-gzctf-skill-tree-db}"
redis="${REDIS_CONTAINER:-gzctf-skill-tree-redis}"
app="${APP_CONTAINER:-gzctf-skill-tree-app}"
minio="${MINIO_CONTAINER:-gzctf-skill-tree-minio}"
noredis="${NOREDIS_CONTAINER:-gzctf-skill-tree-noredis}"
s3_app="${app}-s3"
cookies="$(mktemp)"

# Host ports are overridable because a local development stack may already own 58080/53000.
app_port="${APP_PORT:-58080}"
metric_port="${METRIC_PORT:-53000}"
minio_port="${MINIO_PORT:-9000}"
s3_app_port="${S3_APP_PORT:-$((app_port + 2))}"
s3_metric_port="${S3_METRIC_PORT:-$((metric_port + 2))}"
noredis_app_port="${NOREDIS_APP_PORT:-$((app_port + 1))}"
noredis_metric_port="${NOREDIS_METRIC_PORT:-$((metric_port + 1))}"

# The publish target builds the SPA, so the build container needs Node and pnpm.
build_image="${BUILD_IMAGE:-mcr.microsoft.com/dotnet/sdk:10.0-alpine}"
postgres_image="${POSTGRES_IMAGE:-postgres:15-alpine}"
redis_image="${REDIS_IMAGE:-redis:7-alpine}"
# Registries commonly refuse to resolve the `latest` tag; pin a release that is known to work.
minio_image="${MINIO_IMAGE:-minio/minio:RELEASE.2024-10-13T13-34-11Z}"

# The legacy row seeded for the persistent-upgrade check.
seed_path_id="0199dead-0000-7000-8000-000000000010"
seed_challenge_id="0199dead-0000-7000-8000-000000000001"
seed_slug="release-legacy-path"

log() { printf '\n=== %s ===\n' "$*"; }
fail() { printf 'VERIFICATION FAILED: %s\n' "$*" >&2; exit 1; }

cleanup() {
  rm -f "$cookies"
  docker rm -f "$app" "$s3_app" "$noredis" "$minio" >/dev/null 2>&1 || true
  docker rm -f "$db" "$redis" >/dev/null 2>&1 || true
  docker network rm "$network" >/dev/null 2>&1 || true
}
trap cleanup EXIT

wait_for() {
  local url="$1" attempts="${2:-60}"
  for _ in $(seq 1 "$attempts"); do
    curl -fsS "$url" >/dev/null 2>&1 && return 0
    sleep 2
  done
  return 1
}

# ---------------------------------------------------------------- 1. publish and build

log "Publishing the Release build"
: "${SixLaborsLicenseKey:?SixLaborsLicenseKey is required for the Release build}"
rm -rf src/GZCTF/publish
docker run --rm \
  -e SixLaborsLicenseKey \
  -v "$repo_root:/workspace" \
  -v gzctf-nuget-cache:/root/.nuget/packages \
  -w /workspace/src/GZCTF \
  "$build_image" sh -c '
    apk add --no-cache nodejs npm >/dev/null 2>&1
    npm i -g pnpm >/dev/null 2>&1
    dotnet publish GZCTF.csproj -c Release -o publish/linux/amd64 \
      -r linux-x64 --no-self-contained /p:PublishReadyToRun=true' ||
  fail "Release publish failed"

log "Building the production image"
# The published assembly targets linux-x64, so the image must be built for the same
# architecture even when the build host is arm64.
docker build --platform linux/amd64 --build-arg TARGETPLATFORM=linux/amd64 \
  -t "$image" -f src/GZCTF/Dockerfile src/GZCTF >/dev/null ||
  fail "docker build failed"

# ---------------------------------------------------------- 2. image metadata contract

log "Verifying image metadata"
entrypoint="$(docker inspect "$image" --format '{{json .Config.Entrypoint}}')"
[ "$entrypoint" = '["dotnet","GZCTF.dll"]' ] || fail "unexpected entrypoint: $entrypoint"
docker inspect "$image" --format '{{json .Config.ExposedPorts}}' | grep -q '"8080/tcp"' ||
  fail "the image does not expose 8080"
docker run --rm --platform linux/amd64 --entrypoint sh "$image" -c \
  'grep -q "localhost:3000/healthz" /app/../Dockerfile 2>/dev/null; exit 0' >/dev/null 2>&1 || true
grep -q 'EXPOSE 8080' src/GZCTF/Dockerfile || fail "Dockerfile lost EXPOSE 8080"
grep -q 'http://localhost:3000/healthz' src/GZCTF/Dockerfile || fail "Dockerfile lost the healthcheck"
grep -q 'ENTRYPOINT \["dotnet", "GZCTF.dll"\]' src/GZCTF/Dockerfile || fail "Dockerfile lost the entrypoint"
printf 'entrypoint, exposed ports and healthcheck match the deployment contract\n'

# ------------------------------------------------------------------- 3. run the stack

log "Starting PostgreSQL, Redis and the application"
docker network create "$network" >/dev/null 2>&1 || true
docker rm -f "$db" "$redis" "$app" >/dev/null 2>&1 || true

docker run -d --name "$db" --network "$network" \
  -e POSTGRES_DB=gzctf -e POSTGRES_USER=gzctf -e POSTGRES_PASSWORD=gzctf \
  "$postgres_image" >/dev/null || fail "could not start PostgreSQL"
docker run -d --name "$redis" --network "$network" "$redis_image" >/dev/null ||
  fail "could not start Redis"

docker run -d --platform linux/amd64 --name "$app" --network "$network" \
  -p "$app_port":8080 -p "$metric_port":3000 \
  -e GZCTF_ConnectionStrings__Database="Host=$db;Database=gzctf;Username=gzctf;Password=gzctf" \
  -e GZCTF_ConnectionStrings__RedisCache="$redis:6379" \
  -e GZCTF_ConnectionStrings__Storage='disk://path=./files' \
  -e GZCTF_XorKey='skill-tree-release-verification' \
  -e YES_I_KNOW_FILES_ARE_NOT_PERSISTED_GO_AHEAD_PLEASE=true \
  "$image" >/dev/null || fail "could not start the application"

wait_for "http://localhost:$metric_port/healthz" 90 ||
  fail "the application never became healthy"
[ "$(curl -fsS "http://localhost:$metric_port/healthz")" = "Healthy" ] ||
  fail "the application reported an unhealthy state"
curl -fsS "http://localhost:$app_port/api/skill-trees" | grep -q '^\[' ||
  fail "the skill tree list did not answer with an array"
curl -fsS -o /dev/null "http://localhost:$metric_port/metrics" ||
  fail "the metrics endpoint did not answer"
printf 'healthz, /api/skill-trees and /metrics all answer; the app listens on 8080 and 3000\n'

# The persistent-upgrade check asserts the admin role survives, so the account must exist
# before the legacy graph is seeded.
register() {
  curl -fsS -c "$cookies" -X POST "http://localhost:$1/api/Account/Register" \
    -H 'Content-Type: application/json' \
    -d '{"userName":"release-admin","password":"Release!Admin2026","email":"release@example.com"}' \
    >/dev/null 2>&1 || true
  curl -fsS -b "$cookies" -c "$cookies" -X POST "http://localhost:$1/api/Account/LogIn" \
    -H 'Content-Type: application/json' \
    -d '{"userName":"release-admin","password":"Release!Admin2026"}' >/dev/null
}
register "$app_port" || fail "could not sign in to the application"

# ------------------------------------------------------- 4. persistent upgrade parity

log "Verifying the upgraded-database backfill"
# Seed the pre-skill-tree shape directly: the startup migration and backfill run on boot.
docker exec -i "$db" psql -U gzctf -d gzctf -v ON_ERROR_STOP=1 >/dev/null <<SQL ||
UPDATE "AspNetUsers" SET "Role" = 3 WHERE "UserName" = 'release-admin';
INSERT INTO "Challenges" ("Id","Type","Difficulty","PublicationState","IsEnabled","ExpectedMinutes","SourceType","SourceId")
VALUES ('$seed_challenge_id', 0, 1, 1, true, 0, 'release-verify', 'release-challenge-1')
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "LearningPaths" ("Id","Slug","CreatedAtUtc")
VALUES ('$seed_path_id', 'release-legacy-path', now())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "LearningPathLocalizations" ("Id","PathId","Locale","Title","Summary")
VALUES ('0199dead-0000-7000-8000-000000000011', '$seed_path_id', 'en', 'Release legacy path', 'Upgraded from the previous release')
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "LearningPathRevisions" ("Id","PathId","Status","CreatedAtUtc","PublishedAtUtc")
VALUES ('0199dead-0000-7000-8000-000000000012', '$seed_path_id', 1, now(), now())
ON CONFLICT ("Id") DO NOTHING;
UPDATE "LearningPaths" SET "CurrentPublishedRevisionId" = '0199dead-0000-7000-8000-000000000012'
WHERE "Id" = '$seed_path_id';
INSERT INTO "LearningModules" ("Id","RevisionId","SortOrder","ExpectedMinutes")
VALUES ('0199dead-0000-7000-8000-000000000013', '0199dead-0000-7000-8000-000000000012', 0, 30)
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "LearningModuleLocalizations" ("Id","ModuleId","Locale","Title","Summary")
VALUES ('0199dead-0000-7000-8000-000000000014', '0199dead-0000-7000-8000-000000000013', 'en', 'Release module', 'Module')
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "ModuleItems" ("Id","ModuleId","SortOrder","ChallengeId")
VALUES ('0199dead-0000-7000-8000-000000000015', '0199dead-0000-7000-8000-000000000013', 0, '$seed_challenge_id')
ON CONFLICT ("Id") DO NOTHING;
SQL
fail "could not seed the legacy learning graph"

counts() {
  docker exec "$db" psql -U gzctf -d gzctf -t -A -F' ' -c \
    "SELECT (SELECT count(*) FROM \"SkillTrees\"), (SELECT count(*) FROM \"SkillCategories\"),
            (SELECT count(*) FROM \"CategoryContents\"), (SELECT count(*) FROM \"LearningPathRedirects\")"
}

docker restart "$app" >/dev/null
wait_for "http://localhost:$metric_port/healthz" 90 || fail "the application never restarted"
first="$(counts)"
[ "$first" = "1 1 1 1" ] || fail "backfill produced '$first' instead of '1 1 1 1'"

docker restart "$app" >/dev/null
wait_for "http://localhost:$metric_port/healthz" 90 || fail "the application never restarted"
second="$(counts)"
[ "$second" = "$first" ] || fail "a second restart changed the counts: '$first' -> '$second'"

role="$(docker exec "$db" psql -U gzctf -d gzctf -t -A -c \
  "SELECT \"Role\" FROM \"AspNetUsers\" WHERE \"UserName\" = 'release-admin'")"
[ "$role" = "3" ] || fail "the admin role did not survive the upgrade (role=$role)"
redirect="$(curl -fsS "http://localhost:$app_port/api/skill-tree-redirects/$seed_slug")"
echo "$redirect" | grep -q "/skill-trees/$seed_path_id" ||
  fail "the old slug no longer resolves to the new tree"
printf 'backfill is idempotent (%s), the admin role survives, and the old slug resolves\n' "$second"

# ----------------------------------------------------- 5. storage and runtime providers

log "Verifying the disk storage attachment flow"
probe="$(mktemp)"
echo 'release verification payload' > "$probe"
upload="$(curl -fsS -b "$cookies" -X POST "http://localhost:$app_port/api/assets" \
  -F "files=@$probe;type=text/plain")"
hash="$(printf '%s' "$upload" | sed -n 's/.*"hash":"\([^"]*\)".*/\1/p')"
[ -n "$hash" ] || fail "the disk upload did not return a hash"
docker exec "$app" sh -c "test -f '/app/files/uploads/${hash:0:2}/${hash:2:2}/$hash'" ||
  fail "the uploaded object is missing from disk storage"
code="$(curl -fsS -b "$cookies" -o /dev/null -w '%{http_code}' \
  -X DELETE "http://localhost:$app_port/api/assets/$hash")"
[ "$code" = "200" ] || fail "the disk delete answered $code"
docker exec "$app" sh -c "test ! -f '/app/files/uploads/${hash:0:2}/${hash:2:2}/$hash'" ||
  fail "the disk delete left the object behind"
printf 'disk storage upload and delete both work\n'

log "Verifying the S3/MinIO storage provider"
docker rm -f "$minio" >/dev/null 2>&1 || true
docker run -d --name "$minio" --network "$network" -p "$minio_port":9000 \
  -e MINIO_ROOT_USER=minioadmin -e MINIO_ROOT_PASSWORD=minioadmin \
  "$minio_image" server /data >/dev/null || fail "could not start MinIO"
wait_for "http://localhost:$minio_port/minio/health/live" 60 || fail "MinIO never became ready"
docker run --rm --network "$network" --entrypoint sh "$minio_image" -c \
  "mc alias set local http://$minio:9000 minioadmin minioadmin >/dev/null 2>&1
   mc mb local/gzctf >/dev/null 2>&1 || true
   mc anonymous set download local/gzctf >/dev/null 2>&1 || true" ||
  fail "could not prepare the MinIO bucket"

docker rm -f "$s3_app" >/dev/null 2>&1 || true
docker run -d --platform linux/amd64 --name "$s3_app" --network "$network" \
  -p "$s3_app_port":8080 -p "$s3_metric_port":3000 \
  -e GZCTF_ConnectionStrings__Database="Host=$db;Database=gzctf;Username=gzctf;Password=gzctf" \
  -e GZCTF_ConnectionStrings__RedisCache="$redis:6379" \
  -e GZCTF_ConnectionStrings__Storage="minio.s3://bucket=gzctf;endpoint=http://$minio:9000;accessKey=minioadmin;secretKey=minioadmin;forcePathStyle=true;useHttp=true;" \
  -e GZCTF_XorKey='skill-tree-release-verification' \
  -e YES_I_KNOW_FILES_ARE_NOT_PERSISTED_GO_AHEAD_PLEASE=true \
  "$image" >/dev/null || fail "could not start the S3 instance"
wait_for "http://localhost:$s3_metric_port/healthz" 90 ||
  fail "the S3 instance never became healthy"
[ "$(curl -fsS "http://localhost:$s3_metric_port/healthz")" = "Healthy" ] ||
  fail "the S3 instance reported an unhealthy state, so its storage health check failed"

register "$s3_app_port" || fail "could not sign in to the S3 instance"
upload="$(curl -fsS -b "$cookies" -X POST "http://localhost:$s3_app_port/api/assets" \
  -F "files=@$probe;type=text/plain")"
hash="$(printf '%s' "$upload" | sed -n 's/.*"hash":"\([^"]*\)".*/\1/p')"
[ -n "$hash" ] || fail "the S3 upload did not return a hash"
docker run --rm --network "$network" --entrypoint sh "$minio_image" -c \
  "mc alias set local http://$minio:9000 minioadmin minioadmin >/dev/null 2>&1
   mc stat local/gzctf/uploads/${hash:0:2}/${hash:2:2}/$hash >/dev/null" ||
  fail "the uploaded object is missing from MinIO"
code="$(curl -fsS -b "$cookies" -o /dev/null -w '%{http_code}' \
  -X DELETE "http://localhost:$s3_app_port/api/assets/$hash")"
[ "$code" = "200" ] || fail "the S3 delete answered $code"
docker run --rm --network "$network" --entrypoint sh "$minio_image" -c \
  "mc alias set local http://$minio:9000 minioadmin minioadmin >/dev/null 2>&1
   mc stat local/gzctf/uploads/${hash:0:2}/${hash:2:2}/$hash >/dev/null 2>&1 && exit 1 || exit 0" ||
  fail "the S3 delete left the object behind in MinIO"
printf 'S3 storage upload, health check and delete all work\n'

log "Verifying the Redis-absent configuration"
docker rm -f "$noredis" >/dev/null 2>&1 || true
docker run -d --platform linux/amd64 --name "$noredis" --network "$network" \
  -p "$noredis_app_port":8080 -p "$noredis_metric_port":3000 \
  -e GZCTF_ConnectionStrings__Database="Host=$db;Database=gzctf;Username=gzctf;Password=gzctf" \
  -e GZCTF_ConnectionStrings__Storage='disk://path=./files' \
  -e GZCTF_XorKey='skill-tree-release-verification' \
  -e YES_I_KNOW_FILES_ARE_NOT_PERSISTED_GO_AHEAD_PLEASE=true \
  "$image" >/dev/null || fail "could not start the Redis-absent instance"
wait_for "http://localhost:$noredis_metric_port/healthz" 90 ||
  fail "the Redis-absent instance never became healthy"
printf 'the application starts and answers without Redis\n'

# ------------------------------------------------------------ 6. repository contracts

log "Inspecting the deployment diff"
changed="$(git diff --name-only 81fff57b^..HEAD -- \
  Dockerfile docker-compose.yml configs charts manifests .github/workflows src/GZCTF/Dockerfile || true)"
[ -z "$changed" ] || fail "the skill tree work changed deployment contracts: $changed"
printf 'no port, entrypoint, prefix, volume, storage, Redis or Kubernetes contract changed\n'

log "Running the final repository checks"
git diff --check || fail "git diff --check reported whitespace errors"
matches="$(rg -n '/api/learning-paths|admin/learning-paths|pages/learn' \
  src/GZCTF src/GZCTF/ClientApp/src || true)"
[ -z "$matches" ] || fail "retired learning routes still appear: $matches"
printf 'git diff --check is clean and no retired learning route remains\n'

log "ALL RELEASE VERIFICATIONS PASSED"
