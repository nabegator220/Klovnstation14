using System.Threading.Tasks;
using Content.Server.Plumbing.Components;
using Content.Server.Plumbing.Extensions;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Plumbing.EntitySystems;

public sealed class PlumbingSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;


    private Stopwatch _processingStopwatch = new();

    private HashSet<PlumbingNet> _plumbingNets = new();

    private EntityQuery<PlumbingDeviceComponent> _plumbingDeviceQuery;

    public const float UpdateInterval = 0.5f;
    private float _updateAccumulator;

    public override void Initialize()
    {
        base.Initialize();

        _plumbingDeviceQuery = GetEntityQuery<PlumbingDeviceComponent>();
    }

    public bool AddPlumbingNet(PlumbingNet net)
        => _plumbingNets.Add(net);

    public bool RemovePlumbingNet(PlumbingNet net)
        => _plumbingNets.Remove(net);

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _updateAccumulator += frameTime;

        if (_updateAccumulator < UpdateInterval)
            return;

        var deltaTime = _updateAccumulator;
        _updateAccumulator = 0f;

        Process(deltaTime);
    }

    public void Process(float deltaTime)
    {
        _processingStopwatch.Restart();

        Parallel.ForEach(_plumbingNets, net =>
        {
            // Not like i care.
            net.QueuedInputs.Clear();
            net.QueuedTransfers.Clear();
        });

        var plumbingDeviceEnumerator = EntityQueryEnumerator<PlumbingDeviceComponent>();
        while (plumbingDeviceEnumerator.MoveNext(out var uid, out var plumbingDeviceComponent))
        {
            // I wonder if I can just raise this, not directed to any entity, and it wouldn't be race-condition-ops.
            PlumbingDeviceProcessEvent processEvent = new(deltaTime);
            RaiseLocalEvent(uid, ref processEvent);
        }

        // I AM THE GOD OF HELLFIRE
        // First just handle the thingamajigs that spawn in fluid from nowhere instead of transferring it.

        // Also holy shit, can't we just cache heatcapacities for each reagent somewhere? Dictionaries would be
        // *very* good for this use-case.
        Parallel.ForEach(_plumbingNets, net =>
        {
            var c = net.QueuedInputs.Count;
            for (var i = 0; i < c; ++i)
                net.Solution.AddSolution(net.QueuedInputs[i], _prototypeManager);
        });

        // This handles fluid that this pipenet is losing in whatever way, unless some smartass directly split from the pipenet's solution.
        // !! Also we don't really care if how much the machine is requesting is more than how much can actually be physically pulled. :trollface:
        foreach (var net in _plumbingNets)
        {
            var queuedTransfers = net.QueuedTransfers;
            var c = queuedTransfers.Count;

            float totalRequested = 0f;

            for (int i = 0; i < c; ++i)
                totalRequested += (float)queuedTransfers[i].MovedSolution.Volume;

            if (totalRequested <= 0)
            {
                Log.Debug("Skipped plumbingnet as no fluid was requested to move.");
                continue;
            }

            // The ratio for pulling fluid from this pipenet, so that we can evenly distribute it across *all* devices pulling from it.

            // That means that, for two of the exact same pump, both pulling an amount of fluid that is exactly as much as in this pipenet,
            //      both will pull half of the pipenet no matter which updates first.
            // If there's enough in the pipenet to fulfill both pumps completely, then this value will be 1; both pumps will be able to take as much as necessary.
            var volumeFulfillmentRatio = (totalRequested > 0f) ? MathF.Min(1f, (float)net.Solution.Volume / totalRequested) : 0f;

            for (int i = 0; i < c; ++i)
            {
                var transfer = queuedTransfers[i];

                // Distribute the volume that something gets to transfer, depending on how much is currently being transferred out of the pipenet.
                var transferredSolution = transfer.MovedSolution;
                // Just scale it, i dont wanna recalc heatcap if we're going to do it right now
                // FP imprecision bait #1.5
                transferredSolution.ScaleSolutionAndHeatCapacity(volumeFulfillmentRatio);

                net.Solution.RemoveReagents(transferredSolution.Contents, _prototypeManager);
                if (transfer.TargetSolution is { } target)
                    target.AddSolution(transferredSolution, _prototypeManager);
            }
        }

        if (_processingStopwatch.Elapsed.TotalMilliseconds >= 3)
            Log.Warning($"Alert! Took {_processingStopwatch.Elapsed.TotalMilliseconds}ms to process {_plumbingNets.Count} plumbingnets.");
    }
}
