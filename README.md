# KidzDev Unity Toast

Non-blocking, auto-dismissing notification banners (snackbars) for Unity uGUI. Fire-and-forget `Show()` — no
awaited result — with a capped stack, newest-evicts-oldest admission, slide+fade transition. No DOTween, no
Addressables dependency.

```csharp
Toast.Default.Show("Item added");

var handle = Toast.Default.Show("Connection lost", new ToastOptions { Duration = 0f });
// ...later, once reconnected:
handle.Dismiss();
```

## Why a separate package

A toast isn't a [popup](https://github.com/knabsiraphop/kidzdev-unity-popup) — nothing awaits its result — and
it isn't a [ui-overlay](https://github.com/knabsiraphop/kidzdev-unity-ui-overlay) loader either, since it never
blocks input or represents an in-flight operation. It's throwaway, timed feedback tied to a moment: "item
added," "connection lost," "achievement unlocked." So it ships standalone with its own small stack + eviction
model, and works fine alongside either package.

## Install

Add to `Packages/manifest.json` (UniTask comes from the OpenUPM scoped registry):

```json
{
  "scopedRegistries": [
    { "name": "OpenUPM", "url": "https://package.openupm.com", "scopes": ["com.cysharp.unitask"] }
  ],
  "dependencies": {
    "com.kidzdev.unity.toast": "https://github.com/knabsiraphop/kidzdev-unity-toast.git#v0.1.0"
  }
}
```

Requires Unity `6000.0`+. Dependencies: `com.cysharp.unitask`, `com.unity.ugui`, `com.unity.textmeshpro`.
Addressables is **not** required — the built-in view is constructed entirely in code, no bundled prefab assets.

## Core model

`Toast.Default.Show(message, options)` returns immediately. A `ToastManager` caps how many toasts are visible
at once (default 3); a new `Show()` beyond the cap **evicts the oldest immediately** rather than queueing —
toasts are feedback tied to the moment they were triggered, not precious content worth waiting to see. Each
shown toast slides/fades in, waits its `Duration` (or stays sticky if `<= 0`), then slides/fades out and
destroys itself.

```csharp
Toast.Default.Show("Quick message", new ToastOptions { Duration = 1f });   // shorter than the 3s default
Toast.Default.Show("Coins +100", new ToastOptions { Icon = coinSprite, BackgroundColor = Color.green });
var handle = Toast.Default.Show("Connection lost", new ToastOptions { Duration = 0f }); // sticky
handle.Dismiss(); // e.g. once reconnected
```

| Type | Role |
| --- | --- |
| `IToastService` / `Toast` / `ToastManager` | The seam, static facade (`.Default`), and default ref-capped stack manager. |
| `ToastOptions` | `Duration` (`<= 0` = sticky), `Icon`, `BackgroundColor`, `TextColor`, `TapToDismiss`, per-call `Transition` override. |
| `ToastHandle` | The value every `Show` returns; `IsActive` + `Dismiss()` for a sticky toast you dismiss later. |
| `ToastAnchor` | `Bottom` (default, newest nearest the bottom) or `Top` (newest nearest the top). |
| `IToastLayer` / `ToastLayer` | Lazily creates the overlay canvas (sort order 3000 — above `ui-overlay`'s default 2000) toasts stack under. |
| `IToastTransition` | `InstantToastTransition` / `SlideFadeToastTransition` (default) — no third-party animation dependency. |
| `IToastView` / `DefaultToastView` | The view contract; the built-in view is a capsule with an optional icon and wrapped label, built entirely in code. |

## A second, independently anchored manager

`Toast.Default` is one ambient `ToastManager` anchored at the bottom. Construct another for a different corner
or a separate cap/transition — e.g. a top-anchored system-message channel alongside the default bottom toasts:

```csharp
var systemToasts = new ToastManager(anchor: ToastAnchor.Top, maxVisible: 1);
systemToasts.Show("Server maintenance in 5 minutes");
```

## Tap to dismiss

```csharp
Toast.Default.Show("Tap to dismiss me", new ToastOptions { TapToDismiss = true });
```

Off by default — a toast floating over active gameplay UI shouldn't silently swallow an unrelated tap as a
dismissal.

## Writing a custom view

```csharp
public sealed class CardToastView : MonoBehaviour, IToastView
{
    public GameObject Root => gameObject;
    public RectTransform Content => contentRect;
    public void SetContent(in ToastContent content) { label.text = content.Message; icon.sprite = content.Icon; }
}
```

```csharp
Toast.Default = new ToastManager(viewFactory: () => Instantiate(cardPrefab).GetComponent<CardToastView>());
```

## Production notes

- **Main-thread only** — call every API from the Unity main thread, like the rest of the UI stack.
- **Eviction, not queueing** — a `Show()` beyond the cap plays the oldest toast's exit transition immediately;
  nothing queues up waiting for a slot.
- **Exception-safe teardown** — a failed view factory, a transition that throws, and a manager `Dispose()`
  mid-animation all clean up their instance; nothing leaks.
- **Domain-reload-off safe** — `Toast.Default` resets via `[RuntimeInitializeOnLoadMethod]` at each play
  session's start, so Enter Play Mode Options with domain reload disabled can't strand a stale manager
  referencing a destroyed scene.

## Samples

- **Demo** — buttons for a default toast, a short toast, an icon+color toast, a burst of 5 (visibly evicts
  against a cap of 3), a sticky "Connection lost" toast dismissed via its handle, and a second top-anchored
  manager.

## Authorship

Built with [Claude Code](https://claude.com/claude-code), Anthropic's AI coding agent: the design, direction, and review are human ([@knabsiraphop](https://github.com/knabsiraphop)); most of the implementation code was written by Claude under that direction. All code is original — nothing copied from or bundled with third-party sources.

## License

MIT — see [LICENSE.md](LICENSE.md).
