# SoundManager Module

Audio system for BackpackAdventures. Handles background music (BGM) with priority-based crossfading and sound effects (SFX) with pooling.

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│  Game Layer (Assets/Scripts/)                                   │
│  GameSoundServices (facade) · BGMPriority enum                  │
│  Screen Presenters call Push/ChangeBgmPriority/PlaySound        │
├─────────────────────────────────────────────────────────────────┤
│  AudioManager (IAudioManager)                                   │
│  Facade over MusicPlaylistManager + SoundEffectManager          │
│  Owns SoundSetting user data (volume, mute)                     │
├─────────────────────────────────────────────────────────────────┤
│  MusicPlaylistManager (IMusicPlaylistManager, ITickable)         │
│  Tick-based state machine · Priority queue · Crossfade engine   │
├─────────────────────────────────────────────────────────────────┤
│  SoundEffectManager (ISoundEffectManager)                       │
│  Object-pooled one-shot and looping SFX                         │
├─────────────────────────────────────────────────────────────────┤
│  SoundInstaller                                                 │
│  Zenject bindings for all sound services                        │
└─────────────────────────────────────────────────────────────────┘
```

## File Overview

| File | Purpose |
|---|---|
| `MusicPlaylistManager.cs` | Core BGM engine — priority queue, crossfade state machine, ITickable |
| `AudioManager.cs` | High-level facade wrapping music + SFX managers. Owns `SoundSetting` data |
| `SoundEffectManager.cs` | Pooled one-shot and looping SFX, clip caching, duplicate cap |
| `SoundInstaller.cs` | Zenject installer — binds all sound services |
| `SoundSetting.cs` | User data model for volume/mute settings (`IUserData`) |

---

## MusicPlaylistManager — Technical Design Document

### Overview

`MusicPlaylistManager` manages background music using a **priority-based context queue** and a **tick-based crossfade state machine**. It replaces the previous async-loop architecture to eliminate race conditions, orphaned tasks, and `MissingReferenceException` crashes.

### Design Principles

1. **Deterministic** — Single `Tick()` per frame via Zenject `ITickable`. No per-frame async loops.
2. **No CancellationTokenSource** — State-driven logic. `disposed` flag guards all AudioSource access.
3. **Snap-then-start** — Rapid BGM switches are handled by snapping the current fade to completion, then starting the new fade. No orphaned transitions.
4. **baseBgm is never in the queue** — It's a permanent fallback. The queue only contains context BGMs.

### State Machine

```
    ┌──────────┐
    │   Idle   │ ← fadeMode == None, no volume changes
    └────┬─────┘
         │ CrossFadeTo() / StartFadeOut()
         ▼
    ┌──────────┐
    │  Fading  │ ← Tick() lerps volume(s) each frame
    │          │   FadeMode: FadeIn | CrossFade | FadeOut
    └────┬─────┘
         │ t >= 1.0 → CompleteFade()
         ▼
    ┌──────────┐
    │   Idle   │ ← Sources swapped if crossfade
    └──────────┘
```

**FadeMode values:**

| Mode | Active Source | Inactive Source | Completion |
|---|---|---|---|
| `FadeIn` | Volume lerps `from → to` | Unchanged | — |
| `CrossFade` | Volume lerps `current → 0` | Volume lerps `0 → target` | Swap sources |
| `FadeOut` | Volume lerps `current → 0` | Unchanged | Stop active |

### Priority Queue

BGM contexts are stored in a max-heap priority queue. The highest-priority entry determines what plays.

```
Queue state example:
  [0] TownBGM    (priority 40)  ← PLAYING (top)
  [1] BossBGM    (priority 30)
  [2] DungeonBGM (priority 10)

baseBgm: region1_bgm (priority 5) — NOT in queue, permanent fallback
```

**Key behaviors:**
- `PushContextBGM(id, clip, priority)` — Insert or update entry, re-evaluate top
- `RemoveContextBGM(id)` — Remove entry, crossfade to new top (or baseBgm)
- `AdjustContextPriority(id, 0)` — Treated as removal (priority ≤ 0 = "done")
- `SetBaseBGM(name)` — Set the fallback BGM, stored outside the queue

### BgmContextEntry

Each entry in the queue is a simple `BgmContextEntry` with:

| Field | Type | Purpose |
|---|---|---|
| `Id` | `string` | Unique key (usually the clip address) |
| `Priority` | `int` | Max-heap ordering — highest plays |
| `Clip` | `AudioClip` | The BGM clip to play |
| `Volume` | `float` | Per-entry volume scale (multiplied by globalVolume) |
| `FadeSeconds` | `float` | Crossfade duration |

### Rapid Switching

When `CrossFadeTo()` is called while a fade is in progress:

```
1. SnapCurrentFade()         → instantly complete current fade
2. CompleteFade()            → swap sources if needed, set mode to None
3. Start new CrossFade/FadeIn → begin fresh transition
```

This is instant and deterministic — no async, no cancellation tokens, no race conditions.

### Double AudioSource Pattern

Two `AudioSource` GameObjects (`DontDestroyOnLoad`) enable seamless crossfading:

```
activeSource   ← currently playing (or fading out)
inactiveSource ← prepared for next clip (fading in during crossfade)

On CrossFade completion: (activeSource, inactiveSource) = (inactiveSource, activeSource)
```

### Lifecycle

```
Constructor → InitializeAsync() → spawns 2 AudioSource GameObjects
                                → sets initialized = true

Tick()      → called every frame by Zenject ITickable
            → short-circuits if disposed || !initialized || null sources
            → updates fade volumes

Dispose()   → sets disposed = true
            → stops and destroys AudioSource GameObjects
            → clears queue and baseBgm
```

---

## Usage Guide

### 1. Setting the Base (Fallback) BGM

Called once during game initialization. This is the permanent fallback when no context BGM is active.

```csharp
// In GameSoundServices constructor (or initialization):
await audioManager.SetBaseBGM(StaticValue.SoundString.Region1BGM, (int)BGMPriority.BaseBgm);
```

### 2. Pushing a Context BGM

Push a temporary BGM that overrides based on priority. Higher priority wins.

```csharp
// From a screen presenter or ECS system:
GameSoundServices.Instance.PushBackGroundMusic("BGM/boss_bgm.wav", (int)BGMPriority.BossBgm);
```

**Priority values** (defined in `BGMPriority` enum in `GameSoundServices.cs`):

| Enum | Value | Usage |
|---|---|---|
| `BaseBgm` | 5 | Region/world fallback BGM |
| `DungeonBgm` | 10 | Dungeon scenes |
| `StoryBgm` | 15 | Story FTUE sequences |
| `PoiBgm` | 20 | Point-of-interest encounters |
| `BossBgm` | 30 | Boss combat |
| `TownBgm` | 40 | Town/hub scenes |

### 3. Removing a Context BGM (Restoring Previous)

When a screen closes or a game state ends, remove the context BGM. The system automatically crossfades to the next highest priority or baseBgm.

```csharp
// Option A: Set priority to 0 (treated as removal)
GameSoundServices.Instance.ChangeBgmPriority(StaticValue.SoundString.BossBGM, 0);

// Option B: Direct removal via AudioManager (if needed)
audioManager.AdjustContextPriority("BGM/boss_bgm.wav", 0);
```

### 4. Playing Sound Effects

```csharp
// One-shot SFX by name (Addressable key)
GameSoundServices.Instance.PlaySound("SFX/click.wav");

// One-shot SFX by clip reference
GameSoundServices.Instance.PlaySound(myAudioClip);

// Looping SFX
audioManager.PlaySound("SFX/ambient_wind.wav", isLoop: true);
audioManager.StopSound("SFX/ambient_wind.wav");
```

**SoundEffectManager features:**
- **Clip caching** — AudioClips loaded by name are cached in a `Dictionary<string, AudioClip>`. Subsequent plays skip the async `LoadAssetAsync` call entirely.
- **Duplicate cap** — Max `4` concurrent instances of the same clip (configurable via `SoundEffectManager.MaxConcurrentSameClip`). Prevents audio distortion from rapid-fire sounds.
- **Pool source reset** — Recycled AudioSources have `pitch`, `loop`, and `volume` reset to defaults to prevent stale state.
- **Pitch-aware recycling** — Sources playing at non-1.0 pitch are recycled after `clip.length / |pitch|` seconds (not `clip.length`).

### 5. Volume Control

Volume is managed through `AudioManager` which reads from `SoundSetting` user data:

```csharp
audioManager.SetMusicValue(0.5f);  // 0.0 – 1.0
audioManager.SetSoundValue(0.8f);  // 0.0 – 1.0
```

**Mute flags** — `SoundSetting` has three boolean toggles that override volume:

| Property | Effect |
|---|---|
| `MasterVolume` | `false` → all audio returns 0 (both SFX and music) |
| `MuteMusic` | `true` → `MusicVolume` returns 0 |
| `MuteSound` | `true` → `SoundVolume` returns 0 |

Changes to mute flags are reactive (via UniRx subscription) and update music volume in real-time. SFX volume is read per-call, so mute takes effect immediately on the next `PlaySound`.

---

## Common Patterns

### Screen with BGM Push/Pop

The standard pattern for any screen that needs its own BGM:

```csharp
[ScreenInfo(nameof(MyScreenView))]
public class MyScreenPresenter : BaseScreenPresenter<MyScreenView>
{
    private const string MY_BGM = "BGM/my_screen_bgm.wav";
    private const int    MY_BGM_PRIORITY = 25;

    private System.Action _onViewClosed;

    protected override void OnViewReady()
    {
        base.OnViewReady();
        // Cache the delegate so -= matches +=
        _onViewClosed = () => GameSoundServices.Instance.ChangeBgmPriority(MY_BGM, 0);
        this.View.ViewDidClose += _onViewClosed;
    }

    protected override void OnViewDestroyed()
    {
        this.View.ViewDidClose -= _onViewClosed;
    }

    public override UniTask BindData()
    {
        GameSoundServices.Instance.PushBackGroundMusic(MY_BGM, MY_BGM_PRIORITY).Forget();
        return UniTask.CompletedTask;
    }
}
```

> ⚠️ **Lambda Event Handler Bug**: Never use inline lambdas with `+=`/`-=` for event cleanup.
> `this.View.ViewDidClose += () => ...` and `this.View.ViewDidClose -= () => ...` creates two different delegate instances — the `-=` never matches and cleanup silently fails.
> **Always cache the delegate in a field.**

### Screen with BGM Cleanup in Dispose

If the screen doesn't use `ViewDidClose`, clean up in `Dispose()`:

```csharp
public override void Dispose()
{
    base.Dispose();
    GameSoundServices.Instance.ChangeBgmPriority("BGM/story_bgm.wav", 0);
}
```

### ECS System BGM Push

For DOTS systems, push BGM from managed systems:

```csharp
// In SceneReadySystem.OnUpdate (dungeon entry):
GameSoundServices.Instance.PushBackGroundMusic(
    dungeonBgmName,
    (int)BGMPriority.DungeonBgm
).Forget();
```

---

## Gotchas

1. **Lambda `+=`/`-=` is broken** — Always cache the `Action` in a field. See "Common Patterns" above.
2. **`ChangeBgmPriority(id, 0)` = removal** — Priority ≤ 0 triggers a full `RemoveContextBGM`. This is by design.
3. **baseBgm is NOT in the queue** — It's a separate fallback. Don't try to adjust its priority via `AdjustContextPriority`.
4. **UniTask is only for asset loading** — `PushContextBGM` and `SetBaseBGM` use `async UniTask` solely for `WaitInit()` and `LoadAssetAsync`. The actual crossfade is synchronous (state machine).
5. **Crossfade snapping** — If you rapidly push multiple BGMs (e.g., enter town → boss fight within 0.5s), each intermediate fade is snapped to completion instantly. The final BGM always plays correctly.
6. **Dispose is safe** — `Dispose()` sets `disposed = true` before touching AudioSources. `Tick()` short-circuits immediately. No `MissingReferenceException` possible.
7. **SoundInstaller binding** — `BindInterfacesAndSelfTo` is used for both `SoundEffectManager` and `MusicPlaylistManager`, which auto-discovers `ISoundEffectManager`, `IMusicPlaylistManager`, `IDisposable`, and `ITickable`. No manual interface binding needed.
8. **Volume is single-responsibility** — `AudioManager` applies `SoundVolume` to SFX calls. `MusicPlaylistManager` applies `globalVolume` to BGM in `CrossFadeTo()`. Never multiply music volume in both places.
9. **Fades use `Time.unscaledDeltaTime`** — BGM crossfades continue even when `timeScale = 0` (pause menus, slow-mo). This is intentional.
10. **SFX duplicate cap** — Max 4 concurrent instances of the same clip. Excess plays are silently dropped. Adjust via `SoundEffectManager.MaxConcurrentSameClip`.

---

## Zenject Binding

```csharp
// In SoundInstaller:
Container.BindInterfacesAndSelfTo<SoundEffectManager>().AsCached().NonLazy();
Container.BindInterfacesAndSelfTo<MusicPlaylistManager>().AsCached().NonLazy();
Container.BindInterfacesTo<AudioManager>().AsSingle().NonLazy();

// AudioManager is also a BaseDataManager<SoundSetting> — it gets user volume data automatically.
```

---

## Assembly

`GameFoundation.SoundManager` — references:
- `UniTask`
- `Zenject`
- `GameFoundation.AssetLibrary`
- `GameFoundation.Utilities`
