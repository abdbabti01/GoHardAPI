# Profile photo storage

Profile-photo **files** live in a configurable directory outside the application
publish output; the **URL contract is unchanged** (`/uploads/profiles/…`), so
existing `ProfilePhotoUrl` values and the mobile client keep working.

## Configuration

| Source | Key | Notes |
|---|---|---|
| Environment variable | `PROFILE_PHOTO_STORAGE_PATH` | Absolute path. **Wins** over config. |
| `appsettings*.json` | `ProfilePhotoStorage:Directory` | Absolute path. |
| (non-Production only) fallback | – | `%TEMP%/gohardapi/profile-photos` (or `/tmp/...`). A console warning is printed. |

**Production refuses to start** if neither the env var nor the config key is set
— it never silently uses ephemeral storage (`ProfilePhotoStorageResolver.Resolve`,
wired in `Program.cs`). Local development needs no configuration.

> A resolvable directory does **not** prove a persistent volume is mounted there.
> Verify the mount separately (see *Verify after a deploy* below).

## Serving

`Program.cs` registers a dedicated `UseStaticFiles` over the configured directory
at request path `/uploads/profiles`, **before** the existing `wwwroot`
`UseStaticFiles`:

* only `.jpg` / `.jpeg` / `.png` are mapped; `ServeUnknownFileTypes = false`, so
  no other file in the directory is served;
* `PhysicalFileProvider` + ASP.NET path normalisation block traversal;
* responses carry `Cache-Control: public,max-age=86400,immutable` (filenames are
  unique per upload) and `X-Content-Type-Options: nosniff`.

The `wwwroot` handler remains only as a **fallback** for the three legacy photo
files still committed under `GoHardAPI/wwwroot/uploads/profiles/` (users 13, 14,
16). A freshly uploaded photo always wins because its handler runs first.

## Write / replace / remove behaviour

* Upload validates (size ≤ 5 MB, then a real JPEG/PNG content-signature check —
  the client filename/extension is ignored) and writes a **new** file with
  exclusive creation (`FileMode.CreateNew`, random `user_{id}_{guid}.{ext}` name)
  — the previous file is untouched.
* The DB reference is published with a compare-and-set
  (`UPDATE Users SET ProfilePhotoUrl = @new WHERE Id = @id AND ProfilePhotoUrl = @previous`).
  Exactly one of two overlapping writes wins; the loser deletes only its own new
  file and gets `409`.
* Only **after** the new reference is committed is the old file deleted
  (best-effort — a cleanup failure does not fail the upload; it leaves a
  recoverable orphan).
* On upload validation failure → `400`; the previous reference **and file are
  intact** and no new file is written.
* If the compare-and-set call itself throws or is cancelled, the outcome is
  **not assumed**. Entering the `catch` proves only that *this request* stopped
  waiting for confirmation — it does **not** establish that the server `UPDATE`
  finished, failed, or rolled back; the write may still commit a moment later.
  The controller does one reconciliation read — the committed `ProfilePhotoUrl`
  on a **fresh context/connection**, with a token that is *not* the request-abort
  token — and acts on a single positive signal:
  * the row **already** holds the exact URL this request wrote → that name is
    unique to this request, so its `UPDATE` committed → treat as success (the old
    file is then cleaned up as usual);
  * **anything else** — a different URL, a `null`, or the read itself failing —
    is treated as *uncertain*. The controller deletes **neither** file and lets
    the failure propagate. A different observed value is **not** read as "our
    write rolled back", because the client-side exception is not evidence that
    the server operation finished.
  Keeping both files in the uncertain case is deliberate: an orphaned new file is
  recoverable and harmless; deleting a photo the row does (or is about to)
  reference is not. There are no locks, no retries, and no destructive cleanup
  job — a possible orphan is the accepted cost.
* Remove clears the DB reference first, then deletes the file (best-effort).

A process crash between the filesystem write and the DB commit leaves at worst a
**recoverable orphan file** — never a committed reference to a missing file. No
automatic destructive cleanup job is included; prune orphans manually if needed
(a file under the storage directory matching `user_<id>_<32hex>.<ext>` that no
`Users.ProfilePhotoUrl` references).

## Railway setup (do this once, in order — not performed by this change)

**Step 0 — preserve recoverable files first, before any Railway change.** Any
runtime-written photo that still exists lives only on the *current* deployment's
ephemeral filesystem; creating a Volume, changing env vars, or redeploying can
replace that container and lose it. Capture the files while the old deployment is
still running and untouched:

```bash
# From the currently running deployment (railway shell / railway run):
cd /app/wwwroot/uploads/profiles
tar -czf - . > /tmp/profile-photos-backup.tgz
# then copy that archive off the container (railway run ... > local file,
# or an object-storage put) so it survives the container being replaced.
```

The three photos committed under `GoHardAPI/wwwroot/uploads/profiles/` ship
inside the image and stay reachable via the `wwwroot` fallback regardless; the
backup matters for anything a previous ephemeral deploy wrote.

1. **Create a Volume** on the API service and mount it, e.g. at `/data`.
2. **Set the environment variable** on the API service:
   `PROFILE_PHOTO_STORAGE_PATH=/data/profile-photos`
   (any absolute path under the mount; it need not pre-exist — the app creates it
   on boot).
3. **Import the preserved files into the new volume.** A Railway Volume is only
   mounted into a running deployment of its own service, so the old (volume-less)
   deployment cannot see it — this is an **export-then-import**, not a copy:
   1. keep the Step 0 archive off-container;
   2. deploy this change so a container with the volume mounted at `/data` is
      running;
   3. from a shell on that new deployment, unpack into the configured directory
      *before* announcing the photo feature as live:
      ```bash
      mkdir -p "$PROFILE_PHOTO_STORAGE_PATH"
      tar -xzf /path/to/profile-photos-backup.tgz -C "$PROFILE_PHOTO_STORAGE_PATH"
      ```
   4. spot-check a known `ProfilePhotoUrl` resolves (see *Verify* below).
4. **Filesystem permissions:** the container runs as `root`
   (`mcr.microsoft.com/dotnet/aspnet:8.0`, no `USER` in the Dockerfile), and
   Railway volumes are writable by the container user, so no `chmod` is required.
   If the image is ever switched to a non-root user, ensure that uid can
   read/write the mount (`0700` owned by that uid is enough).
5. **Deploy** this change (if not already deployed for the import in step 3). On
   boot the app resolves the directory, creates it if absent, and refuses to
   start if the variable is missing in Production.

### Replica constraint

A Railway Volume attaches to a single service and Railway does **not** offer a
shared volume across horizontally scaled replicas, so the service backing it must
run **exactly one replica**.

The `Dockerfile` `CMD` runs a single `dotnet GoHardAPI.dll` process and the repo
has no scaling configuration — but that only describes one *container*. It does
**not** prove how many replicas Railway is running: replica count is a
service-level dashboard/`railway.json` setting that is **not verifiable from this
repo**. Before relying on the volume, confirm in the Railway dashboard
(API service → Settings → number of replicas / regions) that it is 1, and keep it
there.

If the API is ever scaled to multiple replicas, profile photos must move to
object storage (S3 / Cloudflare R2); that is out of scope here and requires new
infrastructure/credentials.

## Verify a photo survives a subsequent deploy

1. `POST /api/v1/profile/photo` with a JPEG; note the returned `photoUrl`.
2. `GET https://<host><photoUrl>` → `200`, `Content-Type: image/jpeg`, correct
   bytes.
3. Redeploy using **Railway's own redeploy action** — the service's *Redeploy*
   button in the dashboard, or `railway redeploy` / `railway up`. Do **not** push
   an empty Git commit; an unrelated commit is not needed to test persistence and
   pollutes history.
4. After the deploy: `GET https://<host><photoUrl>` again → still `200`, same
   bytes; `GET /api/v1/profile` still returns the same `profilePhotoUrl`.

If step 4 fails, do not assume "the mount is missing" is the only possibility.
Check, in order:

* the Volume is attached to **this** service and mounted (Railway dashboard →
  service → Volumes) — and the deployment that failed the check is the one with
  the mount;
* `PROFILE_PHOTO_STORAGE_PATH` points **inside** the mount path (a typo puts
  files back on the ephemeral layer, which still "works" until the next deploy);
* the container user can read/write the mount (permissions, if the base image was
  changed to non-root);
* the files were actually imported into the volume (Step 0/3) — `ls` the
  directory on the running deployment;
* the serving configuration still matches: the dedicated `/uploads/profiles`
  `UseStaticFiles` over the configured directory runs **before** the `wwwroot`
  handler, and only `.jpg/.jpeg/.png` are mapped.

## Existing data / limitations

* No database references are rewritten and no files are deleted by this change.
* Legacy `ProfilePhotoUrl` values keep resolving: new-directory first, then the
  `wwwroot` fallback (durable only for the three committed files; anything a
  previous ephemeral deploy wrote and that is now gone **cannot be recovered by
  this code change**).
* When one of the three legacy users (13, 14, 16) replaces or removes their
  photo, the new-directory logic takes over but the old committed
  `wwwroot/uploads/profiles/user_<id>_<timestamp>.jpg` file is left in place
  (its name does not match this service's `user_<id>_<32hex>` pattern, so
  cleanup skips it). This is at most three harmless, unreferenced files; remove
  them from `wwwroot` in a later commit if desired.
* Non-JPEG/PNG uploads (incl. HEIC) are rejected by the API with a clear message
  instead of being stored as an unrenderable file — the decision is made from the
  file's content signature, not its name or `Content-Type`.
* The Flutter client picks images with `image_picker`. Its `imageQuality` /
  resize options **often** cause a re-encode to JPEG, but that is **not
  guaranteed** for every iPhone HEIC selection across every iOS version / picker
  path. Confirming the client always sends JPEG (or converting explicitly before
  upload) is **pending Flutter/device verification on a physical iPhone**. Until
  then, a HEIC that does reach the API is safely rejected, not stored broken.
