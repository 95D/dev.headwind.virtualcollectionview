# Virtual Collection View

RecyclerView-style virtualized collection view for Unity UI Toolkit, built on a
**platform-agnostic pure C# virtualization core**.

## Architecture

The dependency boundary is drawn so that the virtualizer never knows what a "view"
is — it consumes indices, sizes, and a scroll offset, and emits
realize/virtualize/move decisions:

```
┌─ Runtime/Core ────────────────────────────────────┐   noEngineReferences: true
│  CollectionVirtualizer                            │   (pure C#, netstandard2.1,
│   · viewport ∩ virtual rects → diff               │    usable from UGUI/Godot/...)
│   · ItemRealized / ItemVirtualized /              │
│     ItemMoved / ScrollOffsetChanged events        │
│  LayoutManager (placement rules, pure math)       │
│   · ColumnLayoutManager (vertical list)           │
│   · RowLayoutManager    (horizontal list)         │
│   · GridLayoutManager   (fixed span)              │
│  ISizeProvider · LayoutVector2 · ItemRect         │
│  AxisHelper (main/cross axis normalization)       │
└───────────────────────────────────────────────────┘
                    ▲ indices & rects only
┌─ Runtime/UIToolkit ───────────────────────────────┐
│  VirtualCollectionView<T> : VisualElement         │
│   · viewport: input (wheel/drag/fling)            │
│   · recycling pool keyed by holder Type           │
│   · content container translated by -offset       │
│  Adapter<T> / Adapter<TVH, T>                     │
│  ViewHolder<T> / ViewHolder<T, TSub>              │
└───────────────────────────────────────────────────┘
```

Key decisions:

- **Measurement contract**: item sizes are provided up front by the adapter
  (`GetItemSize`), keeping layout a pure, synchronous computation and making
  the total content size exact (no estimated scrollbars). This is the
  UICollectionView model rather than RecyclerView's anchor+fill model.
- **Transform-based placement**: items are `position: absolute`, placed once
  via `style.translate` (never `left/top`), with `UsageHints.DynamicTransform`.
  Scrolling updates a single content-container translate per frame.
- **Recycling**: pools are keyed by concrete `ViewHolder` `Type`
  (`Adapter.GetViewType`), so holders are only rebound to their own view type.
  Recycled elements stay attached with `display: none` — no attach/detach churn.
- **Orientation**: the virtualizer is 2D-generic; scrollable axes are declared by
  the `LayoutManager` (`CanScrollHorizontally/Vertically`). `AxisHelper`
  normalizes main/cross axes so each layout is written once for both
  orientations. Both-axes layouts are structurally possible.
- **Alignment is a layout-manager concern**, expressed axis-relatively and
  defined per layout so each manager only exposes values that are meaningful
  for its placement rule: shared `MainAlignment` (Start/Center/End) places the
  content block when it is shorter than the viewport;
  `LinearCrossAlignment` (Stretch/Start/Center/End) and
  `GridCrossAlignment` (Stretch/Start/Center/End/SpaceBetween) either fill the
  non-scrolling cross axis (default) or respect the items' preferred cross
  size and distribute the leftover — around the block, or into the lane
  gutters for the grid's SpaceBetween.
- **Multi view type**: heterogeneous items are supported within one model
  inheritance tree; `ViewHolder<T, TSub>` seals the single safe downcast so
  user code stays fully typed.
- **Granular data changes preserve identity**: `NotifyItemRangeInserted/
  Removed/Moved/Changed` index-shift surviving holders without rebinding
  (`ItemsShifted` batch), reposition them with exact from→to geometry
  (`ItemMoved`), and tag realize/virtualize events with a cause
  (`Inserted`/`Removed` vs `ScrolledIn`/`ScrolledOut`) so only data-level
  changes animate. `NotifyDataSetChanged` remains the identity-losing full
  invalidation.
- **Anchor policy: keep watching what you were watching.** Granular changes
  capture the first visible item before remeasuring and compensate the scroll
  offset afterwards, so changes outside the viewport are visually invisible
  (insert/remove/grow above just adjusts the offset) while changes inside push
  the following items away as expected. The virtualizer exposes the per-relayout
  `ScrollCompensation`; the view composes it with move events so
  anchor-preserved items resolve to zero visual motion instead of a
  jump-then-slide. A moved anchor is not chased — the anchor falls to the next
  visible item.
- **Animation = mechanics (view) + expression (animator)**: the virtualizer is
  timeless — events state the new truth, and the pluggable `IItemAnimator<T>`
  interpolates toward it (duration, easing). The view owns the invariants:
  cause routing, deferred recycling of exit-animating holders, interruption
  bookkeeping. `DefaultItemAnimator<T>` fades inserts in, fades removals out,
  and slides moves from their current visual state (smooth re-targeting).
  With no animator set, everything snaps as before. Layout changes are
  measured once to their final state; intermediate frames are the animator's
  fiction — the absolute-positioning model means an item's own size tween
  never disturbs its neighbors.

## Quick start

```csharp
var view = new VirtualCollectionView<ProfileModel>();
view.SetLayoutManager(new ColumnLayoutManager { Spacing = 8f, Padding = 12f });
view.SetAdapter(new ProfileCardAdapter(profiles));
root.Add(view);

adapter.NotifyDataSetChanged();  // full invalidation + rebind of visible range
view.ScrollToItem(500);
```

See the **Profile Cards** sample (Package Manager → Samples) for a complete
scene-ready example: 10,000 cards with column/row/grid switching, shuffle, and
jump-to-index over a single shared adapter.

## Tests

Tests are split by layer:

- `Tests/Core` — NUnit tests for the core (layout math, visibility queries,
  virtualizer realize/virtualize diffing, clamping, data invalidation). The
  assembly sets `noEngineReferences: true`, so these can never grow a Unity
  dependency and — like `Runtime/Core` itself — also compile and run on the
  plain .NET SDK.
- `Tests/UIToolkit` — EditMode tests for the view layer's panel-independent
  mechanics: adapter notification routing, ViewHolder contracts, and the
  realize/recycle/pool cycle (driven by setting the virtualizer's viewport
  directly). Input handling (wheel/drag/fling) and animator timing need a live
  panel and are intentionally untested.

Both run in the Unity Test Runner.

## Current limitations / future work

- DiffUtil-style automatic diffing and stable IDs are future work; data owners
  call the granular notify methods explicitly (mutate first, then notify).
- The anchor policy is fixed to "first visible item stays put"; alternative
  policies (bottom-pinned chat, no compensation) are future work.
- Change (crossfade) animations for in-place rebinds are future work —
  `NotifyItemRangeChanged` rebinds instantly.
- Item sizes must be known up front; measure-after-attach for dynamic content
  is intentionally out of scope for the core.
- No scrollbars yet; scroll input is wheel + pointer drag with inertial fling.
- Pool sharing across multiple views (RecycledViewPool) is not implemented.
