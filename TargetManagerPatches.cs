using System;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace KeyboardAccessibility.Patches;

[HarmonyPatch(typeof(NTargetManager))]
internal static class TargetManagerPatches
{
    private static void OnStartTargeting(NTargetManager instance)
    {
        var combatRoom = NCombatRoom.Instance;
        if (combatRoom == null)
            return;

        var targets = combatRoom
            .CreatureNodes.Where(c => GodotObject.IsInstanceValid(c) && instance.AllowedToTargetNode(c))
            .OrderBy(c => c.GlobalPosition.X)
            .ToList();

        if (targets.Count == 1)
        {
            instance.OnNodeHovered(targets[0]);
            CombatState.FinishTargeting!(instance, false);
            return;
        }

        CombatState.Targets = targets;
        for (int i = 0; i < targets.Count && i < 9; i++)
        {
            var creature = targets[i];
            var bottomOfHitbox = creature.GetBottomOfHitbox();
            var localBottom = bottomOfHitbox - creature.GlobalPosition;
            NumberLabels.AddOrUpdate(
                creature,
                i + 1,
                24,
                new Vector2(32, 32),
                new Vector2(localBottom.X - 16, localBottom.Y + 36)
            );
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(
        "StartTargeting",
        new[] { typeof(TargetType), typeof(Vector2), typeof(TargetMode), typeof(Func<bool>), typeof(Func<Node, bool>) }
    )]
    static void StartTargetingVector2Postfix(NTargetManager __instance)
    {
        OnStartTargeting(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(
        "StartTargeting",
        new[] { typeof(TargetType), typeof(Control), typeof(TargetMode), typeof(Func<bool>), typeof(Func<Node, bool>) }
    )]
    static void StartTargetingControlPostfix(NTargetManager __instance)
    {
        OnStartTargeting(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch("FinishTargeting")]
    static void FinishTargetingPostfix()
    {
        CombatState.ClearTargetingLabels();
    }
}
