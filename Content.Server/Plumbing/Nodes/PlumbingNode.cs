// SPDX-FileCopyrightText: 2025 LaCumbiaDelCoronavirus
//
// SPDX-License-Identifier: MPL-2.0

using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.NodeContainer;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Server.Plumbing;


[DataDefinition, Virtual]
public partial class PlumbingNode : Node, IRotatableNode
{
    private MapSystem _mapSystem;
    private NodeGroupSystem _nodeGroupSystem;

    /// <summary>
    ///     The directions in which this pipe can connect to other pipes around it.
    /// </summary>
    [DataField("pipeDirection")]
    public PipeDirection OriginalPipeDirection;

    /// <summary>
    ///     The directions that this pipe reaches, accounting for rotation.
    /// </summary>
    public PipeDirection CurrentPipeDirection { get; private set; }

    [DataField("rotationsEnabled")]
    public bool RotationsEnabled { get; set; } = true;

    private HashSet<PlumbingNode>? _alwaysReachable;

    /// <summary>
    ///     The <see cref="PlumbingNet"/> this node is a part of.
    /// </summary>
    [ViewVariables]
    private PlumbingNet? PlumbingNet => (PlumbingNet?)NodeGroup;

    /// <summary>
    ///     The solution of this node's <see cref="PlumbingNet"/>.
    /// </summary>
    [ViewVariables]
    public Solution? NetSolution => PlumbingNet?.Solution;

    [DataField]
    public FixedPoint2 Capacity { get; set; } = 10f;

    public override void Initialize(EntityUid owner, IEntityManager entMan)
    {
        base.Initialize(owner, entMan);
        _mapSystem = entMan.System<MapSystem>();
        _nodeGroupSystem = entMan.System<NodeGroupSystem>();

        CurrentPipeDirection = OriginalPipeDirection.RotatePipeDirection(entMan.GetComponent<TransformComponent>(owner).LocalRotation);
    }

    bool IRotatableNode.RotateNode(in MoveEvent ev)
    {
        if (OriginalPipeDirection == PipeDirection.Fourway)
            return false;

        // update valid pipe direction
        if (!RotationsEnabled)
        {
            if (CurrentPipeDirection == OriginalPipeDirection)
                return false;

            CurrentPipeDirection = OriginalPipeDirection;
            return true;
        }

        var oldDirection = CurrentPipeDirection;
        CurrentPipeDirection = OriginalPipeDirection.RotatePipeDirection(ev.NewRotation);
        return oldDirection != CurrentPipeDirection;
    }

    public override void OnAnchorStateChanged(IEntityManager entityManager, bool anchored)
    {
        if (!anchored)
            return;

        // update valid pipe directions

        if (!RotationsEnabled)
        {
            CurrentPipeDirection = OriginalPipeDirection;
            return;
        }

        var xform = entityManager.GetComponent<TransformComponent>(Owner);
        CurrentPipeDirection = OriginalPipeDirection.RotatePipeDirection(xform.LocalRotation);
    }

    public void AddAlwaysReachable(PlumbingNode node)
    {
        if (node.NodeGroupID != NodeGroupID) return;
        _alwaysReachable ??= new();
        _alwaysReachable.Add(node);

        if (NodeGroup != null)
            _nodeGroupSystem.QueueRemakeGroup((BaseNodeGroup) NodeGroup);
    }

    public void RemoveAlwaysReachable(PlumbingNode node)
    {
        if (_alwaysReachable == null) return;

        _alwaysReachable.Remove(node);

        if (NodeGroup != null)
            _nodeGroupSystem.QueueRemakeGroup((BaseNodeGroup) NodeGroup);
    }

    public override IEnumerable<Node> GetReachableNodes(TransformComponent xform,
        EntityQuery<NodeContainerComponent> nodeQuery,
        EntityQuery<TransformComponent> xformQuery,
        MapGridComponent? grid,
        IEntityManager entMan)
    {
        if (_alwaysReachable != null)
        {
            var remQ = new RemQueue<PlumbingNode>();
            foreach (var pipe in _alwaysReachable)
            {
                if (pipe.Deleting)
                    remQ.Add(pipe);

                yield return pipe;
            }

            foreach (var pipe in remQ)
                _alwaysReachable.Remove(pipe);
        }

        if (!xform.Anchored || grid == null)
            yield break;

        var pos = _mapSystem.TileIndicesFor(grid.Owner, grid, xform.Coordinates);

        for (var i = 0; i < PipeDirectionHelpers.PipeDirections; i++)
        {
            var pipeDir = (PipeDirection) (1 << i);

            if (!CurrentPipeDirection.HasDirection(pipeDir))
                continue;

            foreach (var pipe in LinkableNodesInDirection(pos, pipeDir, grid, nodeQuery))
            {
                yield return pipe;
            }
        }
    }

    /// <summary>
    ///     Gets the pipes that can connect to us from entities on the tile or adjacent in a direction.
    /// </summary>
    private IEnumerable<PlumbingNode> LinkableNodesInDirection(Vector2i pos, PipeDirection pipeDir, MapGridComponent grid,
        EntityQuery<NodeContainerComponent> nodeQuery)
    {
        foreach (var pipe in PipesInDirection(pos, pipeDir, grid, nodeQuery))
        {
            if (pipe.NodeGroupID == NodeGroupID
                && pipe.CurrentPipeDirection.HasDirection(pipeDir.GetOpposite()))
            {
                yield return pipe;
            }
        }
    }

    /// <summary>
    ///     Gets the pipes from entities on the tile adjacent in a direction.
    /// </summary>
    protected IEnumerable<PlumbingNode> PipesInDirection(Vector2i pos, PipeDirection pipeDir, MapGridComponent grid,
        EntityQuery<NodeContainerComponent> nodeQuery)
    {
        var offsetPos = pos.Offset(pipeDir.ToDirection());
        foreach (var entity in _mapSystem.GetAnchoredEntities(grid.Owner, grid, offsetPos))
        {
            if (!nodeQuery.TryGetComponent(entity, out var container))
                continue;

            foreach (var node in container.Nodes.Values)
            {
                if (node is PlumbingNode pipe)
                    yield return pipe;
            }
        }
    }
}
