using System.Drawing;
using System.Windows.Forms;

namespace MyAwesomeMediaManager.Forms
{
    /// <summary>
    /// FlowLayoutPanel that preserves the current scroll position when child
    /// controls receive focus. This prevents the viewport from jumping back
    /// to the first control when a popup window opens or closes.
    /// </summary>
    public class NoAutoScrollFlowLayoutPanel : FlowLayoutPanel
    {
        protected override Point ScrollToControl(Control activeControl)
        {
            // Keep current scroll offset instead of scrolling the focused control
            return this.DisplayRectangle.Location;
        }
    }
}

