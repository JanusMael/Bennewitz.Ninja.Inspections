using System.Xml.Linq;

namespace Bennewitz.Ninja.XamlQuality;

/// <summary>What tooltip an element declares, in any spelling markup allows, and what may give it one.</summary>
/// <remarks>
/// <para>
/// ⭐ <b>Every framework's spelling, attribute and property element alike.</b> Avalonia's attached
/// <c>ToolTip.Tip</c>, WPF's <c>ToolTip</c> property and its attached <c>ToolTipService.ToolTip</c>, and
/// MAUI's attached <c>ToolTipProperties.Text</c>. A property element counts when it holds text or an
/// element, such as a tooltip with content of its own or a binding.
/// </para>
/// <para>
/// ⚠ <b>A blank tooltip says nothing.</b> An attribute whose value is empty or whitespace is not a
/// tooltip: it explains nothing to the person who points at the control.
/// </para>
/// </remarks>
internal static class Tooltips
{
    /// <summary>The attached properties, owner and member, as markup writes them.</summary>
    private static readonly string[] Attached = ["ToolTip.Tip", "ToolTipService.ToolTip", "ToolTipProperties.Text"];

    /// <summary>WPF's own property, written alone as an attribute or after the element's type.</summary>
    private const string Plain = "ToolTip";

    /// <summary>What <paramref name="element"/> declares as its tooltip.</summary>
    internal static TooltipDeclaration Of(XElement element)
    {
        bool blank = false;

        foreach (XAttribute attribute in element.Attributes())
        {
            if (!IsTooltip(attribute.Name.LocalName, element.Name.LocalName))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(attribute.Value))
            {
                return TooltipDeclaration.Declared;
            }

            blank = true;
        }

        foreach (XElement child in element.Elements())
        {
            if (IsTooltip(child.Name.LocalName, element.Name.LocalName)
                && child.Name.LocalName.Contains('.', StringComparison.Ordinal)
                && (child.HasElements || !string.IsNullOrWhiteSpace(child.Value)))
            {
                return TooltipDeclaration.Declared;
            }
        }

        return blank ? TooltipDeclaration.Blank : TooltipDeclaration.None;
    }

    /// <summary>Whether a setter's property, as <see cref="ResourceScope.SetterProperty"/> reads it, is a tooltip.</summary>
    internal static bool IsTooltipProperty(string written)
    {
        string name = written.Trim();
        int colon = name.LastIndexOf(':');
        if (colon >= 0)
        {
            name = name[(colon + 1)..];
        }

        return string.Equals(name, Plain, StringComparison.Ordinal)
            || Attached.Any(attached => string.Equals(name, attached, StringComparison.Ordinal))
            || name.EndsWith("." + Plain, StringComparison.Ordinal);
    }

    /// <summary>
    /// A tooltip's attribute or property element on an element named <paramref name="ownerLocalName"/>: an attached
    /// property, WPF's plain <c>ToolTip</c>, or <c>Owner.ToolTip</c> for the element's own type. A prefix is the XML
    /// namespace's, so it is never part of the local name.
    /// </summary>
    private static bool IsTooltip(string localName, string ownerLocalName) =>
        string.Equals(localName, Plain, StringComparison.Ordinal)
        || string.Equals(localName, ownerLocalName + "." + Plain, StringComparison.Ordinal)
        || Attached.Any(attached => string.Equals(localName, attached, StringComparison.Ordinal));
}

/// <summary>What an element declares as its tooltip.</summary>
internal enum TooltipDeclaration
{
    /// <summary>No tooltip in any spelling, or only a property element that sets nothing.</summary>
    None,

    /// <summary>A tooltip attribute whose value is empty or whitespace.</summary>
    Blank,

    /// <summary>A non-blank tooltip, literal, bound or with content of its own, in any spelling.</summary>
    Declared,
}
