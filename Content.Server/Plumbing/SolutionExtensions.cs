using System.Reflection;
using System.Reflection.Emit;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Plumbing.Extensions;

public delegate ref float HeatCapacityExper(Solution solution);

/// <summary>
///     More utility methods for solutions.
/// </summary>
public static class SolutionExtensions
{
    // Exception-farm 2025 if someone changes this
    // This is so fucking heinous
    private static readonly FieldInfo HeatCapacityField = typeof(Solution).GetField("_heatCapacity", BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.NonPublic)!
        ?? throw new InvalidOperationException("Couldn't find private field `_heatCapacity` on type `Solution`, was it renamed to something else?");

    private static readonly DynamicMethod HcfDm = new(
        "ExposeHeatCapacity",
        typeof(float).MakeByRefType(),
        new[] { typeof(Solution) }, // Sol
        typeof(SolutionExtensions).Module,
        true
    );

    private static readonly ILGenerator HcfDmIl = HcfDm.GetILGenerator();
    /// <summary>Exposes a solution's heat capacity in the form of a ref.</summary>
    public static readonly HeatCapacityExper ExpHeatCapacity;

    static SolutionExtensions()
    {
        HcfDmIl.Emit(OpCodes.Ldarg_0); // Sol
        HcfDmIl.Emit(OpCodes.Ldflda, HeatCapacityField); // ref Sol
        HcfDmIl.Emit(OpCodes.Ret); // => ref Sol

        ExpHeatCapacity = HcfDm.CreateDelegate<HeatCapacityExper>();
    }

    /// <summary>
    ///     Equivalent to <see cref="Solution.SplitSolution(FixedPoint2)"/>.
    ///         However, does not mutate the solution being split.
    /// </summary>
    /// <remarks>
    ///     Cheaper performance-wise. Heat-capacity isn't re-calculated,
    ///         but multiplied to basically achieve the same one.
    /// </remarks>
    public static Solution CopySplitSolution(this Solution solution, FixedPoint2 toTake, IPrototypeManager? prototypeManager = null)
    {
        if (toTake <= FixedPoint2.Zero)
            return new Solution();

        if (toTake >= solution.Volume)
            return solution.Clone();

        var originalVolume = solution.Volume;
        var effVol = solution.Volume.Value;

        var newSolution = new Solution(solution.Contents.Count) { Temperature = solution.Temperature };

        var remaining = (long) toTake.Value;
        FixedPoint2 taken = 0;

        for (var i = solution.Contents.Count - 1; i >= 0; --i) // iterate backwards because of remove swap.
        {
            var (reagent, quantity) = solution.Contents[i];

            // This is set up such that integer rounding will tend to take more reagents.
            var split = remaining * quantity.Value / effVol;
            effVol -= quantity.Value;

            if (split <= 0)
            {
                DebugTools.Assert(split == 0, "Negative solution quantity while splitting? Long/int overflow?");
                continue;
            }

            var splitInVolume = FixedPoint2.FromCents((int) split);
            newSolution.Contents.Add(
                new ReagentQuantity(
                    reagent,
                    splitInVolume)
            );

            remaining -= split;
            taken += splitInVolume;
        }

        newSolution.MaxVolume = taken;
        newSolution.Volume = taken;

        DebugTools.Assert(remaining >= 0);
        DebugTools.Assert(remaining == 0 || solution.Volume == FixedPoint2.Zero);

        // FP imprecision bait #1. This is the ratio of the taken solution's volume compared to that of the original solution's volume.
        var takenRatio = 1f - (float) (taken / originalVolume);

        // As said we don't mutate anything so we just do this.
        ExpHeatCapacity(newSolution) = ExpHeatCapacity(solution) * takenRatio;

        return newSolution;
    }

    /// <inheritdoc cref="ScaleSolutionAndHeatCapacity(Solution, FixedPoint2)"/>
    public static void ScaleSolutionAndHeatCapacity(this Solution solution, float scale)
        => ScaleSolutionAndHeatCapacity(solution, FixedPoint2.New(scale));

    /// <summary>
    ///     Scales the amount of solution, however does not validate it.
    ///         Instead, manually scales the heatcapacity according to
    ///         the given <paramref name="scale"/>.
    /// </summary>
    /// <remarks>
    ///     As `_heatCapacity` of <see cref="Solution"/> is private,
    ///         reflection is used in this method to not need to index
    ///         an arbitrary number of <see cref="ReagentPrototype"/>s.
    ///
    ///     However, this means that you must make sure that the solution's
    ///         heat capacity is already properly updated before calling this,
    ///         or dirty and re-calculate heatcapacities for this afterwards.
    /// </remarks>
    /// <param name="scale">The scalar to modify the solution by.</param>
    public static void ScaleSolutionAndHeatCapacity(this Solution solution, FixedPoint2 scale)
    {
        if (scale == 1)
            return;

        if (scale == 0)
        {
            solution.RemoveAllSolution();
            return;
        }

        solution.Volume = FixedPoint2.Zero;
        ref List<ReagentQuantity> solutionContents = ref solution.Contents;

        for (int i = solutionContents.Count - 1; i >= 0; --i)
        {
            var old = solutionContents[i];

            // What the fuck? Why isn't this just `old.Volume`? I won't question it though because SURELY there's a good reason for it.
            var newQuantity = old.Quantity * scale;

            if (newQuantity == FixedPoint2.Zero)
                solutionContents.RemoveSwap(i);
            else
            {
                solutionContents[i] = new ReagentQuantity(old.Reagent, newQuantity);
                solution.Volume += newQuantity;
            }
        }

        // FP imprecision bait #2
        ExpHeatCapacity(solution) *= (float) scale;
    }
}
