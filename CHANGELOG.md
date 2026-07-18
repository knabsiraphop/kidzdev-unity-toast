# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-07-19

### Added
- `IToastService` / `Toast` (static facade `.Default`) / `ToastManager` — fire-and-forget `Show(message, options)` returning a `ToastHandle`, capped stack (default 3) with newest-evicts-oldest admission.
- `ToastOptions` — `Duration` (`<= 0` makes a toast sticky until dismissed), `Icon`, `BackgroundColor`, `TextColor`, `TapToDismiss`, per-call `Transition` override.
- `ToastHandle` — `IsActive` + `Dismiss()`, for sticky toasts (e.g. "Connection lost" → dismissed once reconnected).
- `ToastAnchor` — `Bottom` (default) / `Top`; independent `ToastManager` instances can run side by side with different anchors, caps, or transitions.
- `IToastLayer` / `ToastLayer` — lazily created overlay canvas (sort order 3000, above `ui-overlay`'s default 2000) toasts stack under.
- `IToastTransition` — `InstantToastTransition` / `SlideFadeToastTransition` (default, slide + `CanvasGroup` fade), no third-party animation dependency.
- `IToastView` / `DefaultToastView` — the view contract plus a built-in capsule view (optional icon, wrapped label) constructed entirely in code, no bundled prefab assets.
- `ToastTapToDismiss` — optional tap-to-dismiss forwarding component, off by default.
- Domain-reload-off safety: `Toast.Default` resets via `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`.
- EditMode test suite: active-count tracking, bottom/top sibling ordering, capacity eviction (oldest not newest, skips an already-exiting entry), handle dismiss idempotence, `HideAll`, per-call transition override, tap-to-dismiss wiring, view-factory-returns-null diagnosable exception.
- PlayMode test suite: slide/fade transition enter/exit end-states, short-duration auto-dismiss timing, duration timer runs under `Time.timeScale = 0`, `Dispose` cancels an in-flight long transition, real-click tap-to-dismiss via `EventSystem` raycast.
- Demo sample: default/short/icon+color/burst-of-5-against-a-cap-of-3/sticky-with-handle toasts, plus a second top-anchored manager.
