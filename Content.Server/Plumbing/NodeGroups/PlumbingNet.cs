using System.Linq;
using System.Runtime.CompilerServices;
using Content.Server.Plumbing.EntitySystems;
using Content.Server.Plumbing.Extensions;
using Content.Server.NodeContainer.NodeGroups;
using Content.Shared.Chemistry.Components;
using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.NodeGroups;
using Robust.Shared.Prototypes;
using Content.Shared.FixedPoint;

namespace Content.Server.Plumbing;

[NodeGroup(NodeGroupID.Plumbing)]
public sealed class PlumbingNet : BaseNodeGroup, INodeGroup
{
    private IPrototypeManager? _prototypeManager;
    private PlumbingSystem? _plumbingSystem;

    /// <summary>
    ///     The solution that this net contains. It's <see cref="Solution.MaxVolume"/>
    ///         will be the capacity of each node it contains, combined.
    ///
    ///     It's reactions are not processed and it should never be validated unless
    ///         necessary, to save performance.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public Solution Solution = new() { CanReact = false };

    // Although literal `Queue`s would be nicer, lists are faster to iterate(?) through and that's the only thing we're doing.
    /// <summary>
    ///     Basically, a list of solutions that will be added, from nowhere, to this net. They should NOT come from another
    ///         pipenet.
    /// </summary>
    public List<Solution> QueuedInputs = new();
    /// <summary>
    ///     Basically, a list of solutions that will be moved from this net and either into another net, or disposed of.
    ///         This is actually a list of <see cref="PlumbingDeviceTransferData"/> that accomplish that.
    /// </summary>
    public List<PlumbingDeviceTransferData> QueuedTransfers = new();

    [ViewVariables(VVAccess.ReadOnly)]
    public float AvailableVolume => Solution.AvailableVolume.Float();

    /// <summary>
    ///     Queue a split of solution from this net to another solution.
    /// </summary>
    // This method exists for if anyone wants to change the implementation for how transfers are queued.
    // ..because otherwise you'd have to change everything that adds an transfer, rather than just this method.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void QueueTransfer(Solution originSolution, Solution? targetSolution)
        => QueuedTransfers.Add(new PlumbingDeviceTransferData(originSolution, targetSolution));

    /// <inheritdoc cref="QueueTransfer(Solution, Solution?)"/>
    // Ditto.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void QueueTransfer(Solution originSolution, PlumbingNet? targetNet)
        => QueuedTransfers.Add(new PlumbingDeviceTransferData(originSolution, targetNet?.Solution));

    // Ditto.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void QueueInput(Solution solution)
        => QueuedInputs.Add(solution);

    public override void Initialize(Node sourceNode, IEntityManager entMan)
    {
        base.Initialize(sourceNode, entMan);

        IoCManager.Resolve(ref _prototypeManager);
        _plumbingSystem = entMan.System<PlumbingSystem>();

        _plumbingSystem.AddPlumbingNet(this);
    }
    public override void LoadNodes(List<Node> groupNodes)
    {
        base.LoadNodes(groupNodes);

        foreach (var node in groupNodes)
        {
            var plumbingNode = (PlumbingNode)node;
            Solution.MaxVolume += plumbingNode.Capacity;
        }
    }

    public override void RemoveNode(Node node)
    {
        base.RemoveNode(node);

        // This should only handle nodes that aren't handled by AfterRemake.
        if (!node.Deleting || node is not PlumbingNode plumbing)
            return;

        Solution.SplitSolution(plumbing.Capacity * Solution.FillFraction);
        Solution.MaxVolume -= plumbing.Capacity;
    }

    public override void AfterRemake(IEnumerable<IGrouping<INodeGroup?, Node>> newGroups)
    {
        _plumbingSystem?.RemovePlumbingNet(this);

        var plumbingNets = new List<PlumbingNet>();
        var totalMaxVolume = FixedPoint2.Zero;

        foreach (var newGroup in newGroups)
        {
            if (newGroup.Key is not PlumbingNet net)
                continue;

            plumbingNets.Add(net);
            totalMaxVolume += net.Solution.MaxVolume;
        }

        var cached = Solution.Clone();
        foreach (var net in plumbingNets)
        {
            var netSolution = net.Solution;
            // Cast to float for better precision. Kinda.
            var allocatedFraction = (float)netSolution.MaxVolume / (float)totalMaxVolume;

            var allocatedSolution = cached.Clone();
            allocatedSolution.ScaleSolutionAndHeatCapacity(allocatedFraction);

            netSolution.AddSolution(allocatedSolution, _prototypeManager);
        }
    }

    public override string GetDebugData()
        => @$"Volume: {(float)Solution.Volume} / {(float)Solution.MaxVolume}";
}
