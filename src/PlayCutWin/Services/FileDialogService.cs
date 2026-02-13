using Microsoft.Win32;

namespace PlayCutWin.Services;

public interface IFileDialogService
{
    string? PickVideoFile();
    string? PickCsvToImport();
    string? PickCsvToExport(string defaultFileName);
}

public sealed class FileDialogService : IFileDialogService
{
    public string? PickVideoFile()
    {
        var dlg = new OpenFileDialog
        {
            Title = "動画ファイルを選択",
            Filter = "Video files|*.mp4;*.mov;*.m4v;*.avi;*.mkv|All files|*.*"
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public string? PickCsvToImport()
    {
        var dlg = new OpenFileDialog
        {
            Title = "CSVをインポート",
            Filter = "CSV files|*.csv|All files|*.*"
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public string? PickCsvToExport(string defaultFileName)
    {
        var dlg = new SaveFileDialog
        {
            Title = "CSVを書き出し",
            Filter = "CSV files|*.csv|All files|*.*",
            FileName = defaultFileName
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }
}
