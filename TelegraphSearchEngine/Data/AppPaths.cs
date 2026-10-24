using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace TelegraphSearchEngine.Data;

public static class AppPaths
{
    private const string AppFolderName = "TelegraphSearchEngine";

    // per-user app data: %LOCALAPPDATA%\TelegraphSearchEngine on windows,
    // ~/.local/share/TelegraphSearchEngine on linux
    public static string DataDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppFolderName);

    public static string DatabasePath => Path.Combine(DataDirectory, "articles.db");
}
