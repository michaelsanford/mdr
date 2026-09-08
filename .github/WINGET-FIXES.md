# Pending fixes for the winget workflows

Five defects in `winget.yml` and `winget-track.yml`, found while porting this machinery
to [`michaelsanford/wtop`](https://github.com/michaelsanford/wtop) in August 2026. The
port fixed all of them, so **the wtop versions of these two files are the reference
implementation** — the fastest route is to diff against them rather than work from this
list alone.

Nothing in the numbered list below is currently breaking a release. They are latent:
three depend on the repo staying public, on timing, or on someone reading a mangled
comment. Ordered by how much damage they do when they finally bite.

---

## 0. YAML parse error in `winget-track.yml` — FIXED 2026-09-07 (#34)

**Severity: was critical.** Not one of the five above; found separately, and the only one
that was actively breaking things.

`winget-track.yml` was created broken by f4e361f (2026-07-12). The continuation of a
multi-line shell string sat at **column 0** inside the `run: |` block, which ends a block
scalar — so YAML tried to read it as a new top-level key and the file never parsed:

```
could not find expected ':'
line 99: 🚀 **Downstream PR checks passed:** Marking microsoft/winget-pkgs#${PR_NUM} ...
```

Two consequences, both silent for nearly two months:

1. **The hourly reconciler never ran.** Every push produced only GitHub's generic
   "This run likely failed because of a workflow file issue", with no logs.
2. **Dependabot's `github_actions` updates failed weekly.** That updater parses every
   workflow file to find `uses:` refs, so one unparseable file fails the whole job. The
   correlation is exact — last green 2026-07-11, first failure 2026-07-18, the first
   weekly run after f4e361f. `nuget` updates succeeded throughout, since they do not read
   workflow files.

Fixed by holding the text in `READY_COMMENT` and joining with `printf`, the way wtop does
it (`winget-track.yml:128-130`) — no line needs to start at column zero. That also removed
a duplicated literal, since the string had been written twice with nothing keeping the two
copies in step.

**Lesson for the five below:** `actionlint` or a plain `yaml.safe_load` over
`.github/workflows/` would have caught this the day it was introduced. Step 1 of the
verification recipe at the bottom of this file is not optional.

---

## 1. The draft guarantee has a hole — a late PR is never drafted

**Severity: high.** This defeats the point of the feature.

`winget.yml` polls for the downstream PR 10 times at 30 s (`winget.yml:40-54`) and drafts
it inside that window. `winget-track.yml` only ever promotes drafts to ready
(`winget-track.yml:94-103`) — it never drafts anything.

So if `microsoft/winget-pkgs` is slow and the PR does not appear within five minutes, it
is **never drafted at all**, and moderators see it mid-validation. That is precisely the
situation the feature exists to prevent, and it fails silently.

**Fix.** Add an `elif` branch to the hourly reconciler: if the PR is not a draft and does
*not* carry both `Azure-Pipeline-Passed` and `Validation-Completed`, convert it with
`gh pr ready --undo`.

The guard matters. Do not re-draft a PR that *we* promoted — otherwise the job fights a
deliberate change every hour. Record a fixed marker string in the tracker comment when
promoting, and check for it before re-drafting:

```bash
elif [ "$DRAFT" = "false" ] && [ -z "$VALIDATED" ]; then
  if echo "$EXISTING_COMMENTS" | grep -qF "$READY_MARKER"; then
    echo "PR #$PR_NUM was promoted deliberately, leaving ready."
  else
    GH_TOKEN="$WINGET_TOKEN" gh pr ready "$PR_NUM" --undo --repo "$WINGET_REPO"
  fi
fi
```

Hoist that marker to a workflow-level `env` (wtop calls it `READY_MARKER`) so the string
posted when promoting and the string matched when re-drafting cannot drift apart. If they
drift, the guard silently stops working and the job starts undoing your own promotions.

---

## 2. Job-level `permissions` replace the workflow block, they do not merge

**Severity: medium — latent, and it fails the day the repo goes private.**

`winget.yml:8-9` sets `contents: read` at workflow level. The `track` job then declares
(`winget.yml:26-27`):

```yaml
    permissions:
      issues: write
```

A job-level `permissions` block **replaces** the workflow-level one outright. It does not
merge. So inside `track`, `contents` is `none` — while the job calls
`repos/{repo}/releases` and `repos/{repo}/compare/...` to derive contributors.

It works today only because the repo is public and those endpoints are readable by an
otherwise-unprivileged token. Flip the repo private, or tighten default token scopes, and
the contributor block starts failing.

**Fix.** Declare both explicitly:

```yaml
    permissions:
      contents: read   # release compare API
      issues: write    # tracking issue
```

`winget-track.yml:8-9` has the same shape at workflow level (`issues: write` only). It
only reads public `microsoft/winget-pkgs` data, so it is less exposed, but make it
explicit for the same reason.

---

## 3. Forwarded validation comments render as code blocks

**Severity: low, but it is the one you actually notice.**

`winget-track.yml:126-128`:

```yaml
                      gh issue comment "$NUM" --repo "$REPO" --body "⚠️ **Downstream validation reported an issue:** (Comment #$CID)

                      $C_BODY"
```

The YAML block's indentation is inside the string. Markdown treats indented lines as
preformatted, so the entire forwarded body renders as a code block — no links, no
formatting, and long lines scroll sideways. Exactly when you are trying to read a
validation failure.

**Fix.** Build the body with `printf` at column zero:

```bash
FAILURE_COMMENT=$(printf '%s\n\n%s\n' \
  "⚠️ **Downstream validation reported an issue** (Comment #${CID}):" \
  "$C_BODY")
gh issue comment "$NUM" --repo "$REPO" --body "$FAILURE_COMMENT"
```

---

## 4. The package identifier is hardcoded in seven places

**Severity: low — a maintenance trap, not a live bug.**

`michaelsanford.mdr` appears at `winget.yml:18,43,47,89` and
`winget-track.yml:44,49,57`. The search string, the draft call and the tracker body must
all agree; nothing enforces that. Rename the package and the workflows keep running while
quietly matching nothing.

**Fix.** One workflow-level `env` in each file, referenced everywhere including the
action input:

```yaml
env:
  PACKAGE_ID: michaelsanford.mdr
  WINGET_REPO: microsoft/winget-pkgs
```

```yaml
        with:
          identifier: ${{ env.PACKAGE_ID }}
```

---

## 5. The backfill `sed` matches a fragile literal

**Severity: low.**

`winget-track.yml:57` rewrites the "PR not found" line by matching the entire sentence
back — em dash, full URL, query string and all:

```bash
NEW_BODY=$(echo "$BODY" | sed "s|Downstream PR could not be located automatically — check https://github.com/microsoft/winget-pkgs/pulls?q=michaelsanford.mdr|Downstream PR: ...|g")
```

Any edit to the phrasing in `winget.yml:89` — including the em dash surviving a copy-paste
— breaks the backfill silently, and the tracker keeps re-searching forever.

**Fix.** Anchor on the stable prefix and let `.*` absorb the rest:

```bash
NEW_BODY=$(echo "$BODY" | sed "s|Downstream PR could not be located automatically.*|Downstream PR: https://github.com/${WINGET_REPO}/pull/${PR_NUM}|")
```

---

## Also worth doing while you are in there

- **Feed the main loop from process substitution, not a pipe.** `gh issue list … | while
  read` (`winget-track.yml:24`) runs the loop in a subshell. It happens to work, but the
  nested `echo "$LABELS" | while read` at line 107 is a *second* subshell, so the
  `EXISTING_COMMENTS` accumulation at lines 98-99 does not propagate out of it. Using
  `done < <(…)` for both keeps everything in one shell and makes the idempotency
  bookkeeping actually reliable.
- **The tracker issue body is the only state store.** `winget.yml` writes the
  `Downstream PR:` line and the `Contributors to this release:` line; `winget-track.yml`
  greps both back out. That coupling is invisible from either file alone — worth a comment
  in both.

## Verifying a fix without cutting a release

Every submission opens a real PR against `microsoft/winget-pkgs`, so do not test by
publishing a throwaway release. What worked for the wtop port:

1. `actionlint` over `.github/workflows/`.
2. Extract each `run:` block, dedent, and `bash -n` it.
3. Replay the PR lookup and contributor derivation against an *already merged* release —
   read-only, real data, real API shapes.
4. Drive the reconciler's branches with a stubbed `gh` on `PATH` that passes `api` and
   `issue view` through to the real CLI but logs and skips every mutation. Feed synthetic
   tracker rows in via the loop's input. That exercises merged, closed-unmerged,
   draft+validated, undrafted+unvalidated, and the idempotency guards with zero side
   effects.
