# Deferred Items — Phase 02

Out-of-scope discoveries logged during execution, per executor scope-boundary rules
(not fixed — pre-existing issues in files not touched by the current task/plan).

## Plan 02-04, Task 2

- **`apps/web/components/card-form/pix-section.tsx:70,78`** — `react-hooks/set-state-in-effect`
  ESLint error (`setDialogOpen` called synchronously inside `useEffect`). Pre-existing,
  introduced in an earlier plan (not touched by 02-04). Causes
  `npx eslint components/card-form` to exit non-zero even though 02-04's own new files
  (`qr-section.tsx`, `qr-preview.tsx`) are clean. Out of scope for 02-04 per Scope
  Boundary rule — not fixed here.
- **`apps/web/components/card-form/slug-field.tsx:44`** — same
  `react-hooks/set-state-in-effect` rule, same reason, same disposition (deferred, not
  fixed).
