using System;
using System.IO;

namespace PlayCutWin
{
    public sealed class VideoItem
    {
        public string Path { get; }
        public string Name => System.IO.Path.GetFileName(Path);

        public VideoItem(string path)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
        }
    }
}
