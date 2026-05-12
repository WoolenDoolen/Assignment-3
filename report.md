# CMPM 121 Assignment 3 Report

## Architecture Diagram

```text
spells.json
    |
    v
SpellBuilder -----> SpellDefinition
    |                    |
    v                    v
SpellCaster ------> Spell --------> SpellCastProfile
    |                 |                 |
    |                 v                 v
    |            ProjectileManager   ProjectileSpellData
    |
    v
PlayerController ----> classes.json
    |
    v
SpellUIContainer ----> SpellUI

GameManager ----> RewardScreenManager ----> SpellBuilder
     ^                    |
     |                    v
EnemySpawner ------> PlayerController
```

The main flow is data-driven. `SpellBuilder` reads `spells.json`, stores every entry as a `SpellDefinition`, and separates base spells from modifier spells. `SpellCaster` owns the player's mana, spell power, selected slot, and four spell slots. When the player casts, the selected `Spell` builds a temporary `SpellCastProfile`, evaluates RPN expressions with `wave` and `power`, applies modifiers, and asks `ProjectileManager` to spawn the correct projectile behavior.

Wave rewards are coordinated by `GameManager`, `RewardScreenManager`, and `EnemySpawner`. `RewardScreenManager` creates and previews a pending random spell after a wave. The player can select an existing slot or drop a spell, and `EnemySpawner.NextWave` equips the pending reward before the next wave starts.

## Added And Changed Classes

`SpellDefinition`: stores each JSON spell entry and exposes helpers for string fields, nested objects, and RPN integer/float evaluation. Methods: `IsBaseSpell`, `GetString`, `GetObject`, `EvaluateInt`, and `EvaluateFloat`.

`ProjectileSpellData`: stores projectile sprite, trajectory, speed, and lifetime, including JSON loading with fallback values. Methods: `Copy` and `FromJson`.

`SpellCastProfile`: stores the final cast-time values after a base spell and its modifiers have been combined.

`Spell`: now supports JSON-driven base spells and modifiers. It applies damage, mana, cooldown, speed, trajectory, delayed recasts, split volleys, spray projectiles, secondary projectiles, and on-hit projectile bursts. Methods: `GetName`, `GetManaCost`, `GetDamage`, `GetDescription`, `GetCooldown`, `GetIcon`, `GetBaseId`, `GetModifierNames`, `IsReady`, `Cast`, `BuildProfile`, `ApplyModifiers`, `CastVolley`, `CastProjectiles`, `CreateProjectile`, `OnHit`, `CastSecondaryProjectiles`, and `CastOnHitProjectiles`.

`SpellBuilder`: loads `spells.json`, builds the starting Arcane Bolt, builds random reward spells, and can build specific base/modifier combinations for testing. Methods: `Build`, `BuildRandom`, `BuildReward`, `BuildWithModifiers`, `LoadDefinitions`, `LoadSpellData`, and `GetDefinition`.

`SpellCaster`: now supports four spell slots, selected-slot casting, slot replacement, slot dropping, equipped-spell counts, and reward slot selection. Methods: `ManaRegeneration`, `Cast`, `GetCurrentSpell`, `GetSpell`, `GetEquippedSpellCount`, `CanDropSpell`, `GetEquipSlotForNextSpell`, `SelectSpell`, `EquipSpell`, `EquipSpellAt`, and `DropSpell`.

`PlayerController`: evaluates wave-scaled class stats from `classes.json`, preserves health and mana percentages while scaling, exposes spell selection, equipping, and dropping to the UI. Methods: `StartLevel`, `EquipSpell`, `DropSpell`, `SelectSpell`, `EquipSpellAt`, `ApplyWaveStats`, `GetClassAttributes`, `LoadClassConfig`, and `EvaluateClassValue`.

`RewardScreenManager`: previews the generated reward spell, shows damage/mana/cooldown, and tells the player which spell slot will receive or replace the reward. Methods: `Start`, `Update`, `GetPlayer`, `EnsurePendingRewardSpell`, `GetMessage`, `GetRewardButtonText`, `GetRewardDescription`, and `GetRewardSlotDescription`.

`SpellUIContainer`: keeps all four spell slots visible, refreshes each slot, highlights the selected slot, and forwards slot/drop commands. Methods: `ConfigureSlots`, `ShowAllSlots`, `Refresh`, `SelectSlot0`, `SelectSlot1`, `SelectSlot2`, `SelectSlot3`, `SelectSlot`, and `DropSlot`.

`SpellUI`: displays spell icon, mana cost, damage, cooldown fill, selection highlight, and enables the drop button only when dropping is valid. Methods: `Start`, `Setup`, `SetSpell`, `Update`, `RefreshText`, `BindDropButton`, `UpdateDropButton`, and `DropSpell`.

`GameManager`: stores the pending reward spell and reward wave so one reward is generated per completed wave. Methods: `SetPendingSpellReward` and `ClearPendingSpellReward`.

## Spell Descriptions

Base spells:

- `arcane_bolt`: a straight projectile with medium damage.
- `magic_missile`: a low-damage homing projectile.
- `arcane_blast`: a medium projectile that creates secondary bolts on impact.
- `arcane_spray`: a cone of fast, short-lived low-damage projectiles.

Required modifiers:

- `damage_amp`: multiplies damage and mana cost.
- `speed_amp`: multiplies projectile speed.
- `doubler`: casts the modified spell again after a short delay.
- `splitter`: casts the modified spell in two slightly different directions.
- `chaos`: greatly increases damage and changes the projectile trajectory to spiraling.
- `homing`: changes the trajectory to homing, reduces damage, and adds mana cost.

Additional modifiers:

- `mana_stream`: lowers mana cost with a small cooldown penalty.
- `overclocked`: lowers cooldown and increases projectile speed, but raises mana cost.
- `nova_burst`: adds behavior by releasing extra arcane shards from the hit point.

## Contributions

Todd Crandell implemented the JSON-driven spell pipeline, RPN stat evaluation, modifier application, random reward generation, wave-based player stat scaling, and the final reward-slot/drop polish. I learned that the spell system is easier to extend when a cast builds one temporary profile from data instead of putting every modifier into hard-coded branches.

ValerieVATS expanded the spell system from one equipped spell to a four-slot inventory and added the visible spell slot UI with selection and drop affordances. This work showed how important it is to keep UI as a view of gameplay state, while `SpellCaster` remains the source of truth for what the player actually has equipped.
