using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace KeyboardAccessibility;

internal static class CombatState
{
    internal delegate void FinishTargetingDelegate(NTargetManager instance, bool cancelled);
    internal static FinishTargetingDelegate? FinishTargeting;

    internal delegate void TryPlayCardDelegate(NMouseCardPlay instance, Creature? target);
    internal static TryPlayCardDelegate? TryPlayCard;

    internal static List<NCreature> Targets { get; set; } = new();

    internal static bool HasMultipleTargets => Targets.Count > 1;

    internal static void InitDelegates()
    {
        FinishTargeting = AccessTools.MethodDelegate<FinishTargetingDelegate>(
            AccessTools.Method(typeof(NTargetManager), "FinishTargeting")
        );
        TryPlayCard = AccessTools.MethodDelegate<TryPlayCardDelegate>(
            AccessTools.Method(typeof(NMouseCardPlay), "TryPlayCard")
        );
    }

    internal static void ClearTargetingLabels()
    {
        foreach (var target in Targets)
        {
            if (GodotObject.IsInstanceValid(target))
                NumberLabels.Remove(target);
        }
        Targets.Clear();
    }
}
