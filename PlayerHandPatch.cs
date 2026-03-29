using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace KeyboardAccessibility.Patches;

[HarmonyPatch(typeof(NPlayerHand), "_UnhandledInput")]
internal static class PlayerHandPatch
{
    static bool Prefix(NPlayerHand __instance, InputEvent input)
    {
        if (!CombatState.HasMultipleTargets)
            return true;

        var targetManager = NTargetManager.Instance;
        if (!targetManager.IsInSelection)
            return true;

        for (int i = 0; i < GlobalInputHandler.SelectActions.Length && i < CombatState.Targets.Count; i++)
        {
            if (!input.IsActionPressed(GlobalInputHandler.SelectActions[i]))
                continue;

            var target = CombatState.Targets[i];
            if (GodotObject.IsInstanceValid(target))
            {
                targetManager.OnNodeHovered(target);
                CombatState.FinishTargeting!(targetManager, false);
            }

            __instance.GetViewport()?.SetInputAsHandled();
            return false;
        }

        // Block original — it would cancel targeting and switch cards
        return false;
    }
}
