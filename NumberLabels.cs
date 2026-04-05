using System.Collections.Generic;
using Godot;

namespace KeyboardAccessibility;

internal static class NumberLabels
{
    internal const string LabelName = "KA_NumberLabel";

    private static readonly HashSet<Node> _labeledNodes = new();

    internal static void AddOrUpdate(
        Node parent,
        int number,
        int fontSize,
        Vector2 size,
        Vector2 position,
        int outlineSize = 5
    )
    {
        var existing = parent.GetNodeOrNull<Label>(LabelName);
        if (existing != null)
        {
            existing.Text = number.ToString();
            return;
        }

        var label = new Label();
        label.Name = LabelName;
        label.Text = number.ToString();
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.CustomMinimumSize = size;
        label.Size = size;
        label.Position = position;

        label.AddThemeColorOverride("font_color", new Color("EFC851"));
        label.AddThemeColorOverride("font_outline_color", new Color("000000"));
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeConstantOverride("outline_size", outlineSize);

        label.MouseFilter = Control.MouseFilterEnum.Ignore;

        parent.AddChild(label);
        _labeledNodes.Add(parent);
    }

    internal static void Remove(Node parent)
    {
        var existing = parent.GetNodeOrNull<Label>(LabelName);
        existing?.QueueFree();
        _labeledNodes.Remove(parent);
    }

    internal static void RemoveAll()
    {
        foreach (var node in _labeledNodes)
        {
            if (GodotObject.IsInstanceValid(node))
                node.GetNodeOrNull<Label>(LabelName)?.QueueFree();
        }
        _labeledNodes.Clear();
    }
}
