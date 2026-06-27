# ClientCommand.cs

**File:** `Server/Arrowgene.Ddon.Cli/Command/ClientCommand.cs`

## Run() — add early exit handler (before other export branches)

```csharp
if (parameter.ArgumentMap.ContainsKey("recommend"))
{
    if (parameter.Arguments.Count < 1)
    {
        Logger.Error("Usage: client <romDir> recommend=<outFile>");
        return CommandResultType.Exit;
    }

    DirectoryInfo romDir = new DirectoryInfo(parameter.Arguments[0]);
    if (!romDir.Exists)
    {
        Logger.Error($"Rom Path Invalid ({romDir.FullName})");
        return CommandResultType.Exit;
    }

    ExportRecommendedLevels(romDir, parameter.ArgumentMap["recommend"]);
    return CommandResultType.Exit;
}
```

## Add ExportRecommendedLevels method

Copy the full `ExportRecommendedLevels` method from this package's upstream `ClientCommand.cs` (reads `base.arc → scr/stage_list` and emits `StageRecommendedLevelTable.cs`).

Usage after building the CLI:

```powershell
dotnet run --project Arrowgene.Ddon.Cli -- client "<romDir>" recommend="Arrowgene.Ddon.Shared/Model/StageRecommendedLevelTable.cs"
```
