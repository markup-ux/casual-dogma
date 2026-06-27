# StageAreaChangeHandler.cs

**File:** `Server/Arrowgene.Ddon.GameServer/Handler/StageAreaChangeHandler.cs`

Add near the end of the handler, before `return queue;`:

```csharp
// Apply or remove level sync based on the recommended level of the stage being entered.
queue.AddRange(Server.LevelSyncManager.HandleStageChange(client, packet.StageId));
```
