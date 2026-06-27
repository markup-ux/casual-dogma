# GameServerSettings.cs

**File:** `Server/Arrowgene.Ddon.Server/Settings/GameServerSettings.cs`

Add after `JobLevelMax` (optional — only needed if you want per-stage overrides):

```csharp
/// <summary>
/// OPTIONAL per-StageId override for the level-sync system.
///
/// By default, recommended levels are auto-detected from the client's stage data for all dungeon/
/// recommended-level zones (towns and open-world fields are intentionally never synced).
///
/// Entries in this map override the auto-detected value for a specific StageId:
///   * Map a StageId to a level to force that zone's recommended level.
///   * Map a StageId to 0 to DISABLE sync for that zone entirely.
/// </summary>
[DefaultValue("new Dictionary<uint, uint>()")]
public Dictionary<uint, uint> StageRecommendedLevels
{
    set
    {
        SetSetting("StageRecommendedLevels", value);
    }
    get
    {
        return TryGetSetting("StageRecommendedLevels", new Dictionary<uint, uint>());
    }
}
```
