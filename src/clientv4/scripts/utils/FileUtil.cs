using System.IO;
using System.Linq;
using System.Text;
using Godot;

namespace game.scripts.utils;

public static class FileUtil {
    public static string RemoveBom(byte[] fileBytes) {
        switch (fileBytes.Length) {
            // 检测BOM头
            case >= 3 when fileBytes[0] == 0xEF && fileBytes[1] == 0xBB && fileBytes[2] == 0xBF:
                // UTF-8 BOM
                fileBytes = fileBytes.Skip(3).ToArray();
                break;
            case >= 2 when fileBytes[0] == 0xFE && fileBytes[1] == 0xFF:
                // UTF-16 Big-Endian BOM
                fileBytes = fileBytes.Skip(2).ToArray();
                break;
            case >= 2: {
                if (fileBytes[0] == 0xFF && fileBytes[1] == 0xFE)
                {
                    // UTF-16 Little-Endian BOM
                    fileBytes = fileBytes.Skip(2).ToArray();
                }

                break;
            }
            default: {
                if (fileBytes.Length >= 4) {
                    switch (fileBytes[0]) {
                        case 0x00 when fileBytes[1] == 0x00 && fileBytes[2] == 0xFE && fileBytes[3] == 0xFF:
                        // UTF-32 Little-Endian BOM
                        case 0xFF when fileBytes[1] == 0xFE && fileBytes[2] == 0x00 && fileBytes[3] == 0x00:
                            // UTF-32 Big-Endian BOM
                            fileBytes = fileBytes.Skip(4).ToArray();
                            break;
                    }
                }

                break;
            }
        }

        // 将剩余字节转换为字符串
        return Encoding.UTF8.GetString(fileBytes);
    }

    public static void TryCreateUserDataLink(string userDataPath) {
        var basePath = OS.HasFeature("editor") ? "res://" : OS.GetExecutablePath().GetBaseDir();
        var userDataDir = Path.Combine(basePath, userDataPath);
        if (!DirAccess.DirExistsAbsolute(userDataDir)) {
            var absolutePath = Path.GetFullPath(userDataDir);
            if (!DirAccess.DirExistsAbsolute(absolutePath)) {
                var dirAccess = DirAccess.Open(basePath);
                dirAccess.MakeDirRecursive(userDataPath);
            }
        }
        var absoluteUserDataDir = OS.GetUserDataDir();
        if (DirAccess.DirExistsAbsolute(absoluteUserDataDir)) {
            try {
                Directory.Delete(absoluteUserDataDir, true);
            } catch (IOException e) {
                GD.PrintErr("Failed to delete old user data link: ", e.Message);
            }
        }

        var localUserDataPath = DirAccess.Open(userDataDir);
        localUserDataPath.CreateLink(localUserDataPath.GetCurrentDir(), absoluteUserDataDir);
    }
}