using Arrowgene.Ddon.GameServer;
using Arrowgene.Ddon.GameServer.GatheringItems;
using Arrowgene.Ddon.Shared.Model;
using System;
using System.Collections.Generic;
using System.Linq;

public class InstanceEnemyManager : InstanceAssetManager<Enemy, InstancedEnemy>
{
    private readonly DdonGameServer _Server;
    private Dictionary<StageLayoutId, ushort> _CurrentSubgroup { get; set; }

    private Dictionary<StageLayoutId, Dictionary<int, InstancedEnemy>> _EnemyData;

    public InstanceEnemyManager(DdonGameServer server) : base()
    {
        _Server = server;
        _CurrentSubgroup  = new Dictionary<StageLayoutId, ushort>();
        _EnemyData = new Dictionary<StageLayoutId, Dictionary<int, InstancedEnemy>>();
    }

    protected override InstancedEnemy InstanceAssets(Enemy original)
    {
        long gameTimeMSec = _Server.WeatherManager.RealTimeToGameTimeMS(DateTimeOffset.UtcNow);

        if (original.SpawnTimeEnd < original.SpawnTimeStart)
        {
            // Morning range is 0 (midnight) to end time, Evening range is start time and onwards
            if (gameTimeMSec <= original.SpawnTimeEnd || gameTimeMSec >= original.SpawnTimeStart)
            {
                return new InstancedEnemy(original);
            }
        }
        else if (gameTimeMSec >= original.SpawnTimeStart && gameTimeMSec <= original.SpawnTimeEnd)
        {
            return new InstancedEnemy(original);
        }
        return null;
    }

    protected override Enemy FetchAssetsFromRepository(StageLayoutId stage, int setId)
    {
        var enemiesInStage = _Server.AssetRepository.EnemySpawnAsset.Enemies.GetValueOrDefault(stage) ?? new List<Enemy>();
        if (enemiesInStage.Count > setId)
        {
            return enemiesInStage[setId];
        }
        else
        {
            return null;
        }
    }

    protected override IEnumerable<Enemy> FetchAssetsFromRepository(StageLayoutId stage)
    {
        return _Server.AssetRepository.EnemySpawnAsset.Enemies.GetValueOrDefault(stage) ?? new List<Enemy>();
    }

    protected override List<InstancedEnemy> InstanceAssets(IEnumerable<Enemy> originals)
    {
        List<InstancedEnemy> filteredEnemyList = new List<InstancedEnemy>();

        // Calculate current game time
        long gameTimeMSec = _Server.WeatherManager.RealTimeToGameTimeMS(DateTimeOffset.UtcNow);

        foreach (Enemy original in originals)
        {
            // If end < start, it spans past midnight and needs special range handling
            if(original.SpawnTimeEnd < original.SpawnTimeStart)
            {
                // Morning range is 0 (midnight) to end time, Evening range is start time and onwards
                if(gameTimeMSec <= original.SpawnTimeEnd || gameTimeMSec >= original.SpawnTimeStart)
                {
                    var enemy = new InstancedEnemy(original);
                    if (enemy.Index == Enemy.INVALID_INDEX)
                    {
                        enemy.Index = (byte)filteredEnemyList.Count;
                    }
                    filteredEnemyList.Add(enemy);
                }
            }
            else if(gameTimeMSec >= original.SpawnTimeStart && gameTimeMSec <= original.SpawnTimeEnd)
            {
                var enemy = new InstancedEnemy(original);
                if (enemy.Index == Enemy.INVALID_INDEX)
                {
                    enemy.Index = (byte)filteredEnemyList.Count;
                }
                filteredEnemyList.Add(enemy);
            }
        }
        return filteredEnemyList;
    }

    public void SetInstanceEnemy(StageLayoutId stageId, byte index, InstancedEnemy enemy)
    {
        lock (_EnemyData)
        {
            if (!_EnemyData.ContainsKey(stageId))
            {
                _EnemyData[stageId] = new Dictionary<int, InstancedEnemy>();
            }
            _EnemyData[stageId][index] = enemy;
        }
    }

    public InstancedEnemy GetInstanceEnemy(StageLayoutId stageId, byte index)
    {
        lock (_EnemyData)
        {
            if (!_EnemyData.ContainsKey(stageId))
            {
                return null;
            }

            if (!_EnemyData[stageId].ContainsKey(index))
            {
                return null;
            }
            return _EnemyData[stageId][index];
        }
    }

    public List<InstancedEnemy> GetInstancedEnemies(StageLayoutId stageId)
    {
        lock (_EnemyData)
        {
            if (!_EnemyData.ContainsKey(stageId))
            {
                return new List<InstancedEnemy>();
            }
            return _EnemyData[stageId].Select(x => x.Value).ToList();
        }
    }

    public bool HasInstanceEnemy(StageLayoutId stageId, byte index)
    {
        lock (_EnemyData)
        {
            if (!_EnemyData.ContainsKey(stageId))
            {
                return false;
            }
            return _EnemyData[stageId].ContainsKey(index);
        }
    }

    public bool HasEnemyGroup(StageLayoutId stageId)
    {
        lock (_EnemyData)
        {
            return _EnemyData.ContainsKey(stageId);
        }
    }

    public void ResetEnemyNode(StageLayoutId stageId)
    {
        lock (_EnemyData)
        {
            if (_EnemyData.ContainsKey(stageId))
            {
                _EnemyData[stageId].Clear();
            }
        }
    }

    /// <summary>
    /// Drops all cached party enemy state and asset instances for every layout row of a stage
    /// (all group ids), not just group 0.
    /// </summary>
    public void ResetAllLayoutsForStage(uint stageId)
    {
        lock (_EnemyData)
        {
            foreach (var key in _EnemyData.Keys.Where(k => k.Id == stageId).ToList())
            {
                _EnemyData.Remove(key);
            }
        }

        RemoveAssetsForStage(stageId);
    }

    public override void Clear()
    {
        base.Clear();
        lock (_EnemyData)
        {
            _EnemyData.Clear();
        }
    }

    public ushort GetSubgroup(StageLayoutId stageId)
    {
        return _CurrentSubgroup.GetValueOrDefault(stageId);
    }
}
