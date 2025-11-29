# RimWorld 1.6 Vat Integration Notes

## Key Vanilla Types

- `Building_GrowthVat`
  - `TryAcceptPawn(Pawn pawn)` – entry point when a pawn is queued for vat insertion.
  - `EjectContents()` and `TryEjectPawn()` – handles pawn removal.
  - `Tick()` – performs gestation progression.
  - `EmbryoGestationTicksRemaining` (property) – remaining ticks until birth.
  - `PawnInside` (property) – current occupant reference.

- `CompVatGrower` (Component on `Building_GrowthVat`)
  - `CompTick()` – drives occupant growth and nutrition consumption.
  - `Notify_StartGrowing(Pawn pawn)` / `Notify_ContentsEjected()` – lifecycle notifications.
  - `PawnBeingGrown` (property) – occupant accessor used by other systems.

- `JobDriver_EnterGrowthVat`
  - `MakeNewToils()` – hands pawn off to the vat; provides a hook to register start events.

- `JobDriver_UseGrowthVat`
  - Manages adult pawn usage of vats; secondary hook for registration when pawns are set for skill training.

- `LordToil_GrowingVatBirth`
  - Finalizes birth sequence; good place to intercept for automatic ejection logic if default behavior changes.

- `GrowthVatUtility`
  - Contains helper methods such as `PawnCanOccupyVatNow`, providing shared logic for eligibility checks.

## Proposed Hook Points

1. **Registration** – Patch `CompVatGrower.Notify_StartGrowing` to call `VatSentinelWorldComponent.RegisterPawn`.
2. **Unregistration** – Patch `CompVatGrower.Notify_ContentsEjected` and `Building_GrowthVat.EjectContents` to remove records.
3. **Tick Monitoring** – Patch `CompVatGrower.CompTick` (postfix) to evaluate biological age thresholds and schedule ejections.
4. **Birth Events** – Observe `LordToil_GrowingVatBirth.UpdateAllDuties` to ensure auto-ejection does not conflict with vanilla birth flow.
5. **Safety Net** – Patch `Building_GrowthVat.Tick` to prune invalid records if state desynchronizes.

These touch points keep Harmony patches focused on the component responsible for vat lifecycle while providing redundancy via building-level ejection handlers.

