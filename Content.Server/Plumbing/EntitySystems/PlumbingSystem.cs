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

    // I know theres FixedPoint2.Zero but this is just for clarity.
    // Also they're readonly statics which ig are better than FixedPoint2's just statics.
    private static readonly FixedPoint2 Fp2Zero = FixedPoint2.Zero;
    // Why would anyone do this?!
    private static readonly FixedPoint2 Fp2One = FixedPoint2.New(1);


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

    // TODO: Apply Ilya's nuclear subframe-killing solution to this. It's basically totally possible or something right now i don't fucking know.
    // On a top-level this is how it looks like:
    // 2. Go by every pipenet and cache the net's solution's FillFraction for whatever to use
    // 2. Go by every pipenet, and add to it the output of every applied machine
    // 3. Go by every pipenet, and steal from it according to every applied machine
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

        // Also holy shit, can't we just cache heatcapacities somewhere? Dictionaries would be
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
                totalRequested += (float) queuedTransfers[i].MovedSolution.Volume;

            if (totalRequested <= 0)
            {
                Log.Debug("Skipped plumbingnet as no fluid was requested to move.");
                continue;
            }

            // The ratio for pulling fluid from this pipenet, so that we can evenly distribute it across *all* devices pulling from it.

            // That means that, for two of the exact same pump, both pulling an amount of fluid that is exactly as much as in this pipenet,
            //      both will pull half of the pipenet no matter which updates first.
            var volumeFulfillmentRatio = (totalRequested > 0f) ? MathF.Min(1f, (float) net.Solution.Volume / totalRequested) : 0f;

            for (int i = 0; i < c; ++i)
            {
                var transfer = queuedTransfers[i];

                // Distribute the volume that something gets to transfer, depending on how much is currently being transferred out of the pipenet.
                var transferredSolution = transfer.MovedSolution;
                // Just scale it, i dont wanna recalc heatcap if we're going to do it right now
                // FP imprecision bait #1.5
                transferredSolution.ScaleSolutionAndHeatCapacity(volumeFulfillmentRatio);

                // This is good enough,, TODO: make this reagentquantity or something IDFK. So that we can have proper fluid filters etc..
                net.Solution.SplitSolution(transferredSolution.Volume);
                if (transfer.TargetSolution is { } target)
                    target.AddSolution(transferredSolution, _prototypeManager);
            }
        }

        if (_processingStopwatch.Elapsed.TotalMilliseconds >= 3)
            Log.Warning($"Alert! Took {_processingStopwatch.Elapsed.TotalMilliseconds}ms to process {_plumbingNets.Count} plumbingnets.");
    }
}
