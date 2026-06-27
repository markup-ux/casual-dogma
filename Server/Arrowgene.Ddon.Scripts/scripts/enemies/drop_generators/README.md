# Scriptable Enemy Drop Generator

It's possible to have the server inject additional drops when an enemy is killed by providing `.csx` scripts in this directory. These scripts are hotloadable. Each drop generator should implement the interface `IInstanceEnemyDropGenerator`.

> [!WARNING]
> This script module is called in a transaction so don't call functions which use or generate packets directly.

```c#
public interface IInstanceEnemyDropGenerator
{
    public GameMode GameMode { get; }
    public List<InstancedGatheringItem> Generate(GameClient client, InstancedEnemy enemyKilled);
}
```

## Guidelines

The scripts in this directory should be organized by the game mode the generator is used in.

```
enemies
  drop_generator
    normal
      dungeon_boss.csx
      ...
    bbm
      ...
```

If you need to know the location or stage the enemy is killed on, the `InstancedEnemy` object has a field `enemyKilled.StageLayoutId` which contains the location of the enemy.