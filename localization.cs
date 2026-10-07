using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace chomik;

public static class localization
{
    private static readonly Dictionary<string, string[]> table = new()
    {
        ["write"] = new[] { "написать...", "write..." },
        ["screenshot"] = new[] { "заскриншотить экран", "take a screenshot" },
        ["exit"] = new[] { "выйти", "exit" },
        ["donate"] = new[] { "отправить мне донат", "send me a donate" },
        ["settings"] = new[] { "настройки", "settings" },
        ["music"] = new[] { "слушать музыку", "listen to music" },
        ["eat"] = new[] { "по настоящему съедать файлы", "really eat files" },
        ["trash"] = new[] { "в корзину", "to recycle bin" },
        ["perm"] = new[] { "удалять насовсем", "delete permanently" },
        ["whitelist"] = new[] { "белый список приложений:", "app whitelist:" },
        ["language"] = new[] { "язык:", "language:" },
        ["ok"] = new[] { "ок", "ok" },
        ["cancel"] = new[] { "отмена", "cancel" },
        ["pick_app"] = new[] { "выберите приложение:", "choose an app:" },
        ["pick"] = new[] { "выбрать", "select" },
        ["write_title"] = new[] { "что написать?", "what to write?" },
        ["no_files"] = new[] { "папка files/ не найдена\nили anims.txt пустой", "files/ folder not found\nor anims.txt is empty" }
    };

    public static string lang { get; private set; } = detect();

    [DllImport("kernel32.dll")]
    private static extern ushort GetUserDefaultUILanguage();
    public static string t(string key) => table[key][lang == "ru" ? 0 : 1];
    public static void use(string value) => lang = value == "ru" ? "ru" : "en";
    private static string detect()
    {
        if (!OperatingSystem.IsWindows()) return plat.lang();
        return (GetUserDefaultUILanguage() & 0x3ff) == 0x19 ? "ru" : "en";
    }
}
