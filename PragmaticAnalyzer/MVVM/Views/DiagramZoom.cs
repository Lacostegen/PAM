using System.Windows;
using System.Windows.Controls;

namespace PragmaticAnalyzer.MVVM.Views;

internal static class DiagramZoom
{
    internal static void AtPoint(ScrollViewer viewer, UIElement diagram, Point cursor, Action resize)
    {
        // Anchor in the unscaled drawing, not in the scrollable extent or the Viewbox.
        var anchor = viewer.TranslatePoint(cursor, diagram);
        resize();
        viewer.UpdateLayout();
        var displacement = diagram.TranslatePoint(anchor, viewer) - cursor;
        viewer.ScrollToHorizontalOffset(viewer.HorizontalOffset + displacement.X);
        viewer.ScrollToVerticalOffset(viewer.VerticalOffset + displacement.Y);
        viewer.UpdateLayout();
    }
}
