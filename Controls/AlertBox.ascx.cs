using System;
using System.Web.UI;

namespace SmartShelf.Controls
{
    /// <summary>
    /// The single success/failure banner every form in the system uses, so no page has to
    /// invent its own message markup.
    /// </summary>
    public partial class AlertBox : UserControl
    {
        public string CssModifier { get; private set; }

        public void ShowSuccess(string message) { Show(message, "alert-success"); }
        public void ShowError(string message) { Show(message, "alert-error"); }
        public void ShowInfo(string message) { Show(message, "alert-info"); }
        public void ShowWarning(string message) { Show(message, "alert-warning"); }

        public void Hide()
        {
            phAlert.Visible = false;
        }

        private void Show(string message, string cssModifier)
        {
            // Always encoded: messages can carry user-supplied values such as a product name.
            litMessage.Text = Server.HtmlEncode(message ?? string.Empty);
            CssModifier = cssModifier;
            phAlert.Visible = true;
        }
    }
}
