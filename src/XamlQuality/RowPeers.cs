using System.Reflection;

namespace Bennewitz.Ninja.XamlQuality;

/// <summary>Which of Avalonia's row peers an item container creates.</summary>
internal enum RowKind
{
    /// <summary>Not a row whose behaviour was measured.</summary>
    None,

    /// <summary>A list or tab row: Avalonia's <c>ListItemAutomationPeer</c>.</summary>
    Content,

    /// <summary>A tree row: Avalonia's <c>TreeViewItemAutomationPeer</c>.</summary>
    Tree,
}

/// <summary>The row peer a container creates, and whether that peer writes its own name.</summary>
/// <param name="Kind">Which of Avalonia's row peers it is, if either.</param>
/// <param name="Peer">The peer type, when <paramref name="Kind"/> is not <see cref="RowKind.None"/>.</param>
/// <param name="NamesItself">Whether the peer writes its own <c>GetNameCore</c> rather than keeping its base's.</param>
internal readonly record struct RowPeer(RowKind Kind, Type? Peer, bool NamesItself);

/// <summary>Which of Avalonia's row peers an item container creates, read from its compiled code without running it.</summary>
/// <remarks>
/// ⭐ <b>Shared because the rules about generated rows check the same rows.</b> <c>BNXQ1009</c> asks how
/// a row is named and <c>BNXQ1010</c> whether it draws, and each checks only the rows whose behaviour
/// was measured: those whose container creates Avalonia's <c>ListItemAutomationPeer</c> or
/// <c>TreeViewItemAutomationPeer</c>. At Avalonia 12.1.3, by its source, a <c>ListBoxItem</c>, and so a
/// <c>ComboBoxItem</c>, and a <c>TabItem</c> create the first, and a <c>TreeViewItem</c> the second, so
/// another framework's rows are left alone. This began as <c>BNXQ1009</c>'s own reading and moved here
/// unchanged.
/// </remarks>
internal static class RowPeers
{
    private const string Peers = "Avalonia.Automation.Peers.";

    /// <summary>
    /// The row peer <paramref name="container"/> creates, read from the <c>newobj</c> in the
    /// <c>OnCreateAutomationPeer</c> it resolves to.
    /// </summary>
    internal static RowPeer Of(Type container)
    {
        const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        if (container.GetMethod("OnCreateAutomationPeer", Instance, Type.EmptyTypes) is not { } create)
        {
            return new RowPeer(RowKind.None, null, false);
        }

        Type[] peers = [.. CompiledStrings.ConstructedBy(create).Where(IsPeer).Distinct()];
        if (peers.Length != 1)
        {
            return new RowPeer(RowKind.None, null, false);
        }

        Type peer = peers[0];
        (RowKind kind, string reads) = Derives(peer, "ListItemAutomationPeer") ? (RowKind.Content, "ContentControlAutomationPeer")
            : Derives(peer, "TreeViewItemAutomationPeer") ? (RowKind.Tree, "ControlAutomationPeer")
            : (RowKind.None, string.Empty);

        if (kind == RowKind.None)
        {
            return new RowPeer(RowKind.None, null, false);
        }

        return new RowPeer(kind, peer, peer.GetMethod("GetNameCore", Instance, Type.EmptyTypes)?.DeclaringType?.FullName != Peers + reads);
    }

    private static bool IsPeer(Type type) => Derives(type, "AutomationPeer");

    private static bool Derives(Type type, string peer)
    {
        for (Type? link = type; link is not null; link = link.BaseType)
        {
            if (link.FullName == Peers + peer)
            {
                return true;
            }
        }

        return false;
    }
}
