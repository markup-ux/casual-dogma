# Official Pawns

Official pawn scripts define server-owned rental pawns. Each `*.csx` file in this
folder returns an `IOfficialPawnScript`; the official pawn module loads these
scripts at startup and shows matching pawns in the riftstone rental list.

The script filename is part of the pawn's stable identity. Rename a file only if
you are comfortable creating a new official pawn identity for the server.

## Minimal Script

```csharp
public class OfficialPawn : IOfficialPawnScript
{
    public override string Name => "Example";
    public override JobId Job => JobId.Warrior;
    public override int MinLevel => 1;
    public override int MaxLevel => 999;
    public override float Quality => PawnQuality.Good;
    public override float RentalCostMultiplier => PawnQuality.ToCostMultiplier(Quality);

    public override CDataEditInfo EditInfo => new CDataEditInfo
    {
        Sex = 1,
        Voice = 1,
        Personality = PawnPersonality.Normal,
        Height = 48600,
        BodyType = 0,
    };

    public override RentalPawnRecord Generate(OfficialPawnContext ctx)
    {
        return ctx.Builder.Build();
    }
}

return new OfficialPawn();
```

Use the `exportappearance` chat command to export a player's current appearance
as a `CDataEditInfo` block, then paste that block into `EditInfo`.

## Script Properties

- `Name`: The display name shown in the pawn list and pawn profile.
- `Job`: The pawn's vocation.
- `EditInfo`: Appearance, voice, personality, and body customization.
- `MinLevel`: Minimum player job level required to see and hire this pawn.
- `MaxLevel`: Maximum player job level that can hire this pawn.
- `PawnLevel`: Optional fixed generated pawn level. `null` scales the pawn to
  the hiring player's active job level.
- `Quality`: The pawn quality tier used by automatic equipment, skill, augment,
  crest, limit break, craft, and cost systems.
- `RentalCostMultiplier`: Extra multiplier applied to the rental cost. Use
  `PawnQuality.ToCostMultiplier(Quality)` to keep cost aligned with quality.
- `AdventureCount`: Optional number of adventures available after hire. `null`
  uses the server default.
- `CraftCount`: Optional number of crafts available after hire. `null` uses the
  server default.
- `IsUnlocked(GameClient client, DdonGameServer server)`: Optional unlock check.
  Return `true` when the player should be able to see, preview, and hire this
  pawn. The default implementation always returns `true`.

Official pawns are server-owned and display `Server` as their Arisen name.

## Unlock Conditions

Override `IsUnlocked` when a pawn should appear only after a player completes
content or reaches a milestone:

```csharp
public override bool IsUnlocked(GameClient client, DdonGameServer server)
{
    return client.Character.HasQuestCompleted((QuestId)12345)
        && client.Character.HasJobOfLevel(JobId.Warrior, 40);
}
```

The method receives the connected `GameClient`, so scripts can inspect the
player's character state. It also receives `DdonGameServer` for advanced checks
that need server managers or database access.

Locked official pawns are hidden from the official pawn list. Direct profile and
hire requests are rejected with the same not-found error used for unavailable
pawns.

## Quality Tiers

Use the constants in `PawnQuality`:

```csharp
PawnQuality.Trash
PawnQuality.Poor
PawnQuality.Normal
PawnQuality.Good
PawnQuality.Superior
PawnQuality.Excellent
PawnQuality.Legendary
```

Quality controls the generated pawn's overall strength. Lower qualities can have
missing gear, fewer crests, weaker skills, fewer augments, and lower craft
levels. Good and higher qualities are intended to look like serious player pawns.
Legendary is the best generated tier.

## Deterministic Generation

`OfficialPawnContext.Rng` is seeded from the official pawn identity, the hiring
character id, and the hiring player's current job level. Use `ctx.Rng` for any
random choices in pawn scripts. The same player hiring the same official pawn at
the same level should get the same generated result, which prevents rerolling by
reconnecting or repeatedly hiring. Leveling up gives that player a new
deterministic result for the new level.

Automatic equipment, crests, limit breaks, skills, augments, and crafting use
separate deterministic streams, so changing one category does not reroll the
others.

If `PawnLevel` is set, automatic generation uses that fixed pawn level and does
not reroll when the hiring player gains levels.

Avoid using `Random.Shared` in official pawn scripts unless you intentionally
want non-deterministic output.

## Automatic Defaults

The official pawn module seeds the builder with the script's `Quality`. If a
script does not configure a category, `Build()` automatically applies sane
defaults for equipment, custom skills, augments, and crafting before returning
the final `RentalPawnRecord`.

Use these methods only when a category should use a quality different from the
script's default:

```csharp
ctx.Builder
    .WithAutoEquipment(Quality)
    .WithAutoCustomSkills(Quality)
    .WithAutoAbilities(Quality)
    .WithAutoCraft(Quality);
```

- `WithAutoEquipment(Quality)`: Picks level-appropriate job equipment, applies
  quality-scaled enhancement values, fills jewelry with variety, and leaves
  lanterns unenhanced. Bitterblack jewelry is kept at `+0` and can receive
  Bitterblack Maze crest rolls.
- `WithAutoCustomSkills(Quality)`: Fills custom skill slots using the configured
  recommended skills first, then job-appropriate fallbacks. Skill selection and
  skill levels respect `SkillData.json` job level requirements.
- `WithAutoAbilities(Quality)`: Fills up to the visible augment slot limit while
  respecting the pawn's augment point budget.
- `WithAutoCraft(Quality)`: Sets craft rank based on pawn level and quality,
  then distributes the rank-derived craft points across the primary craft stats.
- `WithAutoNormalSkills()`: Learned automatically by the builder constructor,
  but can be called again if you replace job data manually.

Specific manual choices take precedence over automatic generation. For example,
setting one weapon slot still lets automatic equipment fill the remaining
performance slots, and setting one augment still lets automatic augments fill
the remaining slots within the point budget. Manual craft data replaces
automatic craft generation for that pawn.

## Equipment

Assign a specific item with `WithEquipmentForSlot`:

```csharp
WithEquipmentForSlot(
    EquipSlot slot,
    ItemId itemId,
    EquipType type = EquipType.Performance,
    byte plusValue = 0,
    params ItemId[] crestIds)
```

- `slot`: The `EquipSlot` enum value for the equipment slot to fill.
- `itemId`: The item to equip.
- `type`: `EquipType.Performance` for actual stats, or `EquipType.Visual` for
  layered/visual equipment.
- `plusValue`: Enhancement level, usually `0` through `3`.
- `crestIds`: Optional crest item ids mounted on the equipment.

Common `EquipSlot` values:

```csharp
EquipSlot.WepMain
EquipSlot.WepSub
EquipSlot.ArmorHelm
EquipSlot.ArmorBody
EquipSlot.WearBody
EquipSlot.ArmorArm
EquipSlot.ArmorLeg
EquipSlot.WearLeg
EquipSlot.Jewelry1
EquipSlot.Jewelry2
EquipSlot.Jewelry3
EquipSlot.Jewelry4
EquipSlot.Jewelry5
EquipSlot.Lantern
```

```csharp
ctx.Builder.WithEquipmentForSlot(
    EquipSlot.WepMain,
    ItemId.SomeSword,
    plusValue: 3,
    crestIds: [ItemId.CrestOfHerculeanPower0]);
```

You can also pass numeric item ids when an enum value does not exist:

```csharp
ctx.Builder.WithEquipmentForSlot(
    EquipSlot.WepMain,
    123456u,
    plusValue: 1,
    crestIds: [98765u]);
```

Assign dress/visual equipment by passing `EquipType.Visual`. Visual gear is
separate from performance gear, so you can keep the automatic level-scaled stats
while forcing a specific look:

```csharp
ctx.Builder
    .WithEquipmentForSlot(
        EquipSlot.WearBody,
        ItemId.SomeCostumeBody,
        type: EquipType.Visual)
    .WithEquipmentForSlot(
        EquipSlot.WearLeg,
        ItemId.SomeCostumeLegs,
        type: EquipType.Visual);
```

Manual crests reserve those crest slots. The automatic crest pass may fill empty
remaining slots after `Generate` returns. Lanterns are intentionally skipped by
automatic enhancement and crest assignment.

Use `ctx.PlayerLevel` when a pawn should gain a special item only after the
hiring player reaches a certain level:

```csharp
if (ctx.PlayerLevel >= 30)
{
    ctx.Builder.WithEquipmentForSlot(
        EquipSlot.WepMain,
        ItemId.SomeSword,
        plusValue: 3,
        crestIds: [ItemId.CrestOfHerculeanPower0]);
}
```

For explicit limit break stats, use the stat name from `LimitBreak.json`:

```csharp
WithLimitBreak(
    EquipSlot slot,
    string statName,
    EquipType type = EquipType.Performance)
```

- `slot`: The equipped item that receives the limit break.
- `statName`: The limit break stat name from `LimitBreak.json`, such as
  `"Blow Power"` or `"Healing Power"`.
- `type`: Which equipment set to modify.

```csharp
ctx.Builder.WithLimitBreak(EquipSlot.WepMain, "Blow Power");
ctx.Builder.WithLimitBreak(EquipSlot.ArmorBody, "Healing Power");
```

The name overload uses the best roll for that stat. Raw enhance ids are also
available when you need an exact roll:

```csharp
ctx.Builder.WithLimitBreak(EquipSlot.WepMain, enhanceId: 20);
```

Automatic limit break assignment does not replace an explicit limit break added
by the script.

## Skills

Automatic skills are applied by default. Use this when the skill quality should
be different from the script's `Quality`:

```csharp
ctx.Builder.WithAutoCustomSkills(Quality);
```

Use manual skills when a pawn needs a specific build:

```csharp
WithCustomSkill(int slot, CustomSkillId skillId, byte level = 1)
WithCustomSkillAtLevel(int slot, CustomSkillId skillId, int minLevel, byte level = 1)
WithCustomSkillForLevels(
    int slot,
    params (int MinLevel, CustomSkillId SkillId, byte Level)[] tiers)
```

- `slot`: Custom skill slot, numbered `1` through `4` in the order shown in the
  game UI.
- `skillId`: The custom skill to equip.
- `level`: The skill level to assign, usually `1` through `10`.
- `minLevel`: The hiring player's job level required before this override is
  applied.
- `tiers`: A level progression table. The builder picks the row with the highest
  `MinLevel` that is less than or equal to `ctx.PlayerLevel`.

```csharp
ctx.Builder
    .WithCustomSkill(1, CustomSkillId.SparkSlash, level: 10)
    .WithCustomSkillAtLevel(2, CustomSkillId.HeavenThrust, minLevel: 40, level: 8)
    .WithCustomSkillForLevels(3,
    [
        (1,  CustomSkillId.PommelStrike, 3),
        (60, CustomSkillId.GreatWindmill, 10),
    ]);
```

`WithCustomSkillRaw` is available for release ids that are not represented by
`CustomSkillId`.

## Augments

Automatic augments are applied by default. Use this when the augment quality
should be different from the script's `Quality`:

```csharp
ctx.Builder.WithAutoAbilities(Quality);
```

Use manual augments when a pawn needs a specific build:

```csharp
WithAbility(int slot, AbilityId abilityId, byte level = 1)
WithAbilityAtLevel(int slot, AbilityId abilityId, int minLevel, byte level = 1)
WithAbilityForLevels(
    int slot,
    params (int MinLevel, AbilityId AbilityId, byte Level)[] tiers)
```

- `slot`: Augment slot, numbered `1` through `8` in the order shown in the game
  UI.
- `abilityId`: The augment to equip.
- `level`: The augment level to assign, usually `1` through `6`.
- `minLevel`: The hiring player's job level required before this override is
  applied.
- `tiers`: A level progression table. The builder picks the row with the highest
  `MinLevel` that is less than or equal to `ctx.PlayerLevel`.

```csharp
ctx.Builder
    .WithAbility(1, AbilityId.Onslaught, level: 6)
    .WithAbilityAtLevel(2, AbilityId.CombatMomentum, minLevel: 40, level: 6)
    .WithAbilityForLevels(3,
    [
        (1,  AbilityId.DeftFooting, 3),
        (60, AbilityId.BraveEffort, 6),
    ]);
```

The automatic augment picker respects the visible augment slot limit and the
pawn's augment point budget. Manual assignments are trusted, so scripts should
keep their own manual augment builds valid.

## Crafting

Automatic crafting data is applied by default. Use this when craft quality
should be different from the script's `Quality`:

```csharp
ctx.Builder.WithAutoCraft(Quality);
```

Use manual crafting when a pawn should be a specialist:

```csharp
WithCraft(
    uint craftRank,
    uint craftRankLimit = OfficialPawnBuilder.MaxCraftRank,
    uint craftPoint = 0,
    uint craftExp = 0,
    IReadOnlyDictionary<CraftSkillType, uint>? skillLevels = null)

WithCraftSkill(CraftSkillType type, uint level)
WithCraftData(CDataPawnCraftData craftData)
```

Primary craft skill levels are capped at
`OfficialPawnBuilder.MaxCraftSkillDisplayLevel` by the manual helpers so
generated pawns stay inside the normal crafting formula tables. The client
displays primary craft skills one level higher than their stored value, so
`WithCraft` and `WithCraftSkill` accept the displayed level a script author
expects to see. Non-primary craft skill flags can also be set through
`skillLevels` or `WithCraftSkill`.

> [!NOTE]
> Primary craft skills start at level `1` in the pawn profile UI. Use the level
> you want players to see, not the raw stored value.

```csharp
ctx.Builder
    .WithCraft(
        craftRank: 71,
        craftPoint: 70,
        skillLevels: new Dictionary<CraftSkillType, uint>
        {
            [CraftSkillType.ProductionSpeed] = 20,
            [CraftSkillType.EquipmentEnhancement] = 30,
            [CraftSkillType.EquipmentQuality] = 15,
            [CraftSkillType.CostPerformance] = 5,
        })
    .WithCraftSkill(CraftSkillType.ConsumableQuantity, 1);
```

`WithCraftData` replaces the entire `CDataPawnCraftData` object and is intended
for scripts that need exact packet-level control. It uses raw stored craft skill
values instead of display levels.

## Advanced Overrides

These methods are available for special cases:

```csharp
ctx.Builder.WithJobData(customJobData);
ctx.Builder.WithExtendedParams(customExtendedParams);
ctx.Builder.WithRecommendedCustomSkills(customSkillList);
ctx.Builder.WithRecommendedAbilities(customAbilityList);
ctx.Builder.WithCraftData(customCraftData);
```

Use `WithJobData` and `WithExtendedParams` carefully. They replace core values
such as job level data, jewelry slots, and augment capacity.

## Level Gating

Official pawn generation runs with the hiring player's current job level in
`ctx.PlayerLevel` and the generated pawn level in `ctx.PawnLevel`. Use
`ctx.PlayerLevel` for behavior that should unlock as the player progresses:

```csharp
if (ctx.PlayerLevel >= 50)
{
    ctx.Builder
        .WithCustomSkill(1, CustomSkillId.GreatWindmill, level: 10)
        .WithAbility(1, AbilityId.Onslaught, level: 6);
}
```

For skills and augments, the builder also has helper methods for common level
progression patterns:

```csharp
ctx.Builder
    .WithCustomSkillAtLevel(1, CustomSkillId.FalconKick, minLevel: 50, level: 10)
    .WithAbilityAtLevel(1, AbilityId.Onslaught, minLevel: 60, level: 6);
```

Level-gated manual overrides take precedence over automatic defaults whenever
their level condition is met.

Use `PawnLevel` when the pawn itself should stay at a fixed level:

```csharp
public override int? PawnLevel => 5;
```

`MinLevel` and `MaxLevel` still control which players can hire the pawn. A fixed
level pawn can be visible to high-level players if its visibility range allows
it.

## Global Settings

Most server-wide tuning lives in:

```text
Arrowgene.Ddon.Scripts/scripts/settings/uncategorized/official_pawns.csx
```

That file controls:

- Crest tier level thresholds.
- Weapon, armor, and jewelry crest pools by level range.
- Limit break unlock level and late-game level.
- Preferred weapon and armor limit break stat names by job.
- Recommended custom skills by job.
- Recommended augments by job.

Edit the settings file when you want all official pawns to follow a new balance
rule. Edit an individual pawn script when only that pawn should be special.

## Bitterblack Jewelry

Automatic equipment can assign Bitterblack Maze bracelets and earrings. These
items remain `+0` and use the Bitterblack Maze crest roll logic instead of the
normal crest pools.

Earring percentage rolls come from `IBitterblackEarringMixin` through the
RNG-aware overload:

```csharp
RollBitterBlackMazeEarringPercent(JobId jobId, Random rng)
```

Custom mixins should use the provided `rng` so official pawn results remain
deterministic.

## Practical Notes

- Keep filenames stable once players have used a pawn.
- Keep automatic methods and `RentalCostMultiplier` tied to the same `Quality`.
- Put broad balance choices in `official_pawns.csx`.
- Put hand-authored identity, appearance, and special build choices in the pawn
  script.
- Call `ctx.Builder.Build()` once at the end of `Generate`.
