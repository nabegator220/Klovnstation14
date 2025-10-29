using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._KS14.DeferredSpawn;

/// <summary>
///     System to defer predicted entity spawns to being done on the next tick.
/// </summary>
public sealed class DeferredSpawnSystem : EntitySystem
{
    private readonly Queue<(EntProtoId, MapCoordinates)> _spawnMapQueue = new();
    private readonly Queue<(EntProtoId, EntityCoordinates)> _spawnAttachedQueue = new();

    private const int MaxSpawnsPerTick = 10;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var spawns = 0;

        while (_spawnMapQueue.Count > 0)
        {
            if (spawns++ > MaxSpawnsPerTick)
                return;

            var (entityProtoId, mapCoordinates) = _spawnMapQueue.Dequeue();
            EntityManager.PredictedSpawn(entityProtoId, mapCoordinates);
        }

        while (_spawnAttachedQueue.Count > 0)
        {
            if (spawns++ > MaxSpawnsPerTick)
                return;

            var (entityProtoId, entityCoordinates) = _spawnAttachedQueue.Dequeue();
            EntityManager.PredictedSpawnAttachedTo(entityProtoId, entityCoordinates);
        }
    }

    public void DeferSpawn(EntProtoId entityProtoId, MapCoordinates coordinates) => _spawnMapQueue.Enqueue((entityProtoId, coordinates));

    public void DeferSpawnAttachedTo(EntProtoId entityProtoId, EntityCoordinates coordinates) => _spawnAttachedQueue.Enqueue((entityProtoId, coordinates));
}
