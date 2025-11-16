# VIP Customer Story Events

The VIP system is driven entirely by story events so designers can schedule bespoke visitors like **BabyBoss** without writing code.

## 1. Prepare Spawn Anchors

1. In the restaurant scene, select each seat or placeholder the VIP is allowed to occupy (ex: `Customer1`, `Customer2`, `Customer3`).
2. Add the `VipCustomerSpawnAnchor` component (Component ▸ Story ▸ VIP Customer Spawn Anchor).
3. Give each anchor a unique `Anchor Id`. Using the existing customer slot names keeps things consistent with the default configuration.
4. Optionally assign a custom `Spawn Point` transform (to fine-tune position) and a `Look At Target` so the NPC will face a table or focal point after spawning.

## 2. Create a VIP Story Event Asset

1. In the Project window choose **Create ▸ Game Flow ▸ Story Events ▸ VIP Customer**.
2. Assign the prefab you want to spawn to **VIP Customer Prefab** (the BabyBoss prefab lives under `Assets/Prefabs`).
3. Fill out the remaining inspector fields:
   - **VIP Display Name** shows up on the nameplate (`BabyBoss` by default).
   - **Forced Meal Name** (optional) guarantees a specific recipe for narrative beats.
   - **Require Specific Shift / Target Shift Index** gate the event to the intended shift.
   - **Spawn Hour Window** controls the in-game clock range where the VIP can appear.
   - **Preferred Anchor Ids** restricts which anchors may be used. Leave as `Customer1/2/3` or add more ids. Enable *Fallback To Any Anchor* to ignore the list when none are free.
   - **Dialogue Hooks** (Spawn / Order Taken / Order Completed) accept standard `DialogueEventAsset`s, ensuring the requested barks fire automatically.
   - **Quest Hooks** optionally re-use the shift's quest id so that `ShiftCompletionStoryEventAsset` can detect whether the VIP requirement was met via the existing quest pipeline.
   - **Signals & Broadcasts** lets you fan out to other story events or HUD callouts without writing glue code.

3. Drop the new asset into an existing story sequence or trigger (for example, right after the shift start event for the relevant day).

## 3. Hook Into Shift Requirements

1. Open `ShiftData` and enable **Requires VIP Customer** on the shift that should be blocked until the VIP is satisfied.
2. Point that shift's **Quest Id** to the same quest id referenced by the VIP story event. The quest will auto-start when the VIP spawns and auto-complete once their order is served, so the existing shift completion logic now knows whether the VIP requirement was met.

## 4. Testing Tips

- Use the `spawnHourWindow` to pick a narrow band (e.g., 9.5–9.6) for fast iteration.
- The story event logs warnings if the wrong shift is active, no anchors are present, or the shift ends before the VIP is served.
- Because the VIP uses the regular `Customer` class, all systems that listen for `OrderManager` events (UI popups, broadcasts, etc.) continue to work without changes.
