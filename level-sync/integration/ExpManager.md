# ExpManager.cs

**File:** `Server/Arrowgene.Ddon.GameServer/Characters/ExpManager.cs`

Both `AddExp` and `AddJp` need a try/finally wrapper so database writes use real stats while the player remains synced in combat.

## AddExp

At the start of the method body (after `PacketQueue packets = new();`):

```csharp
// If the character is level-synced for the current zone, restore their real level/stats for the
// duration of this update so EXP/level-up math and the database write use the true values.
var syncToken = _Server.LevelSyncManager.BeginPersistenceSafeUpdate(characterToAddExpTo);
try
{
```

Wrap the existing method body, then before `return packets;`:

```csharp
}
finally
{
    // Re-apply the synced level/stats and, if a real level-up occurred, correct the client's view.
    _Server.LevelSyncManager.EndPersistenceSafeUpdate(client, characterToAddExpTo, syncToken, packets);
}
```

## AddJp

Same pattern at the start:

```csharp
var syncToken = _Server.LevelSyncManager.BeginPersistenceSafeUpdate(characterToJpExpTo);
try
{
```

And before `return packets;`:

```csharp
}
finally
{
    _Server.LevelSyncManager.EndPersistenceSafeUpdate(client, characterToJpExpTo, syncToken, packets);
}
```
