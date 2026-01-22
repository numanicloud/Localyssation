// ローカライゼーション用の yaml ファイルの差分を取るツール

if (args.Length != 2)
{
    Console.WriteLine("Usage: Localyssation.Diff.exe <old_yaml_file> <new_yaml_file>");
    return;
}

var oldPath = args[0];
var newPath = args[1];

var oldTrans = LoadTranslations(oldPath);
var newTrans = LoadTranslations(newPath);

// ショートサーキットしないように注意
var anyDiff = DumpModified(newTrans, oldTrans)
    | DumpAdded(newTrans, oldTrans)
    | DumpRemoved(newTrans, oldTrans);

if (!anyDiff)
{
    Console.WriteLine("No differences found.");
}

return;

static Dictionary<string, string> LoadTranslations(string path)
{
    var dict = new Dictionary<string, string>();
    foreach (var rawLine in File.ReadAllLines(path))
    {
        var line = rawLine.Trim();
        if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
            continue;
        var idx = line.IndexOf(':');
        if (idx < 0)
            continue;
        var keyPart = line.Substring(0, idx).Trim();
        var valuePart = line.Substring(idx + 1).Trim();
        var key = Unquote(keyPart);
        var value = Unquote(valuePart);
        dict[key] = value;
    }
    return dict;
}

static string Unquote(string s)
{
    if (s.Length >= 2 && ((s[0] == '"' && s[^1] == '"') || (s[0] == '\'' && s[^1] == '\'')))
        return s.Substring(1, s.Length - 2);
    return s;
}

static void WriteLine(string text, ConsoleColor? color = null)
{
    if (color.HasValue)
    {
        Console.ForegroundColor = color.Value;
    }
    Console.WriteLine(text);
    Console.ResetColor();
}

static void Write(string text, ConsoleColor? color = null)
{
    if (color.HasValue)
    {
        Console.ForegroundColor = color.Value;
    }
    Console.Write(text);
    Console.ResetColor();
}

static bool DumpModified(Dictionary<string, string> newMaps, Dictionary<string, string> oldMaps)
{
    var b = false;

    foreach (var key in newMaps.Keys.Intersect(oldMaps.Keys).OrderBy(k => k))
    {
        if (oldMaps[key] == newMaps[key]) continue;
        b = true;

        WriteLine($"[updated] {key}", ConsoleColor.Yellow);
        Write("     new: ", ConsoleColor.DarkGreen);
        WriteLine($"\"{newMaps[key]}\"");
        Write("     old: ", ConsoleColor.DarkRed);
        WriteLine($"\"{oldMaps[key]}\"");
    }

    return b;
}

static bool DumpAdded(Dictionary<string, string> newMaps, Dictionary<string, string> oldMaps)
{
    var b = false;
    foreach (var key in newMaps.Keys.Except(oldMaps.Keys).OrderBy(k => k))
    {
        b = true;
        Write($"  [added] {key}: ", ConsoleColor.Green);
        WriteLine($"\"{newMaps[key]}\"");
    }
    return b;
}

static bool DumpRemoved(Dictionary<string, string> newMaps, Dictionary<string, string> oldMaps)
{
    var b = false;
    foreach (var key in oldMaps.Keys.Except(newMaps.Keys).OrderBy(k => k))
    {
        b = true;
        Write($"[removed] {key}: ", ConsoleColor.Red);
        WriteLine($"\"{oldMaps[key]}\"");
    }
    return b;
}