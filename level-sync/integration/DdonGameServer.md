# DdonGameServer.cs

**File:** `Server/Arrowgene.Ddon.GameServer/DdonGameServer.cs`

## Constructor — add after ExpManager initialization

```csharp
LevelSyncManager = new LevelSyncManager(this);
```

## Properties — add alongside other manager properties

```csharp
public LevelSyncManager LevelSyncManager { get; }
```
