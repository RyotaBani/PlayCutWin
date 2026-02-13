using System.Text;
using System.Windows;

namespace PlayCutWin;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Enable legacy encodings (e.g., Shift-JIS) for CSV import in some Japanese environments.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        base.OnStartup(e);
    }
}
