using Content.Shared.Chemistry.Components;

namespace Content.Server.Plumbing;

/// <summary>
///     Event for a plumbing device to queue tranfers of fluids.
/// </summary>
[ByRefEvent]
public readonly record struct PlumbingDeviceProcessEvent(float DeltaTime);

/// <summary>
///     Event that a plumbing device passes to signify how much fluid
///         it wants to move and what solution to transfer it to.
///         Meant for moving a solution to another pipenet, but this
///         is more flexible than that.
///
///     If the target solution is null, just assume that the taken
///         solution is getting nuked and deleted and fuck you.
/// </summary>
/// <remarks>
///     <paramref name="MovedSolution"/> must be an abstract solution,
///         that doesn't belong to anything. Otherwise shit will get
///         !fucked up!.
/// </remarks>
public readonly record struct PlumbingDeviceTransferData(Solution MovedSolution, Solution? TargetSolution);
