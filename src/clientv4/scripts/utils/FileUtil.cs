using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using Godot;
using Environment = Godot.Environment;

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
                if (fileBytes[0] == 0xFF && fileBytes[1] == 0xFE) {
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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1416:Validate platform compatibility", Justification = "This method is intended for use in different platform via multiple branch.")]
    public static bool TryCreateUserDataLink(string userDataPath, Node env) {
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

        // 检查符号链接是否已正确建立
        var folderInfo = new DirectoryInfo(absoluteUserDataDir);
        if (Directory.Exists(absoluteUserDataDir) && folderInfo.Attributes.HasFlag(FileAttributes.ReparsePoint)) {
            var linkTarget = Path.GetFullPath(userDataPath);
            if (folderInfo.LinkTarget == linkTarget) {
                return true; // 符号链接已正确建立，直接返回
            }
        }

        // 判断是否为Windows系统
        if (OS.GetName() == "Windows") {
            // 检查是否为管理员模式
            var isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

            if (!isAdmin) {
                // 申请管理员权限并重启自身
                try {
                    var processModule = Process.GetCurrentProcess().MainModule;
                    if (processModule != null) {
                        var processInfo = new ProcessStartInfo {
                            FileName = processModule.FileName,
                            UseShellExecute = true,
                            Verb = "runas"
                        };
                        Process.Start(processInfo);
                    }

                    env.GetTree().Quit();
                } catch (Exception e) {
                    GD.PrintErr("Failed to restart with admin privileges: ", e.Message);
                    return false;
                }
            }
        }

        // 删除旧的符号链接
        if (DirAccess.DirExistsAbsolute(absoluteUserDataDir)) {
            try {
                Directory.Delete(absoluteUserDataDir, true);
            } catch (IOException e) {
                GD.PrintErr("Failed to delete old user data link: ", e.Message);
                return false; // 删除失败，返回false
            }
        }

        // 创建符号链接
        var localUserDataPath = DirAccess.Open(userDataDir);
        localUserDataPath.CreateLink(localUserDataPath.GetCurrentDir(), absoluteUserDataDir);
        return true;
    }

    public static bool IsSubDirectoryOf(this string candidate, string other) {
        return candidate.StartsWith(other);
    }
}