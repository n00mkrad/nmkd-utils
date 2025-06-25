using System.Text.RegularExpressions;

namespace NmkdUtils.DocumentProcessing;

public class XmlDoc
{
    public static void SimplifyDocXml(IEnumerable<string> args)
    {
        foreach (var file in args.Where(File.Exists))
        {
            SimplifyDocXml(file);
        }
    }

    public static void SimplifyDocXml(string path, string outputPath = "")
    {
        if (!path.Low().EndsWith(".xml"))
            return;

        if (outputPath.IsEmpty())
        {
            outputPath = path + ".txt";
        }

        string text = IoUtils.ReadTextFile(path);
        var lines = text.GetLines();

        List<string> removeLinesContaining = ["assembly>", "members>", "doc>", "xml version="];
        string assembly = "";

        for (int i = 0; i < lines.Count; i++)
        {
            if (i > 0 && lines[i - 1].Trim().StartsWith("<assembly>"))
            {
                assembly = StringUtils.RemoveTags(lines[i], curlyBraceTags: false).Trim();
                lines[i] = $"Assembly: {assembly}";
            }
        }

        text = lines.Join("\n");
        text = text.Replace($"Assembly: {assembly}", $"Assembly: {assembly}\n\n", firstOnly: true);
        text = Regex.Replace(text, @"<member\s+name=""([^""]+)"">", "$1", RegexOptions.Multiline);
        text = Regex.Replace(text, @"</member>", "", RegexOptions.Multiline);
        text = Regex.Replace(text, @"(?ms)^(\s*)<summary>\s*\r?\n(.*?)\r?\n\1</summary>", "$1Summary: $2");
        text = Regex.Replace(text, @"<summary>(.*?)</summary>", "Summary: $1", RegexOptions.Singleline);
        text = Regex.Replace(text, @"<paramref\s+name=""([^""]+)""\s*/?>", "$1", RegexOptions.Multiline);
        text = Regex.Replace(text, @"^([MPFT]?:)[^\.]+\.", "$1", RegexOptions.Multiline);
        text = Regex.Replace(text, @"System\.", "", RegexOptions.None);
        text = text.Replace($"{assembly}.", "");

        text = text.GetLines().Where(l => l != "[[REMOVE]]").Join("\n");

        List<string> outLines = [];

        foreach (var line in text.GetLines())
        {
            if (removeLinesContaining.Any(line.Contains))
                continue;

            string outLine = line.Trim(); // Remove indentation

            outLine = outLine.Replace(" <br/>").Replace("<br/>"); // Remove <br/> tags

            if (outLine.Trim().StartsWith("Summary:"))
            {
                outLine = outLine.TrimStart().Replace("Summary:").TrimStart(); // Remove "Summary:" prefix
            }

            // if (outLine.IsNotEmpty() && !outLine.Trim().StartsWith("Member: ") && !outLine.Trim().StartsWith("Assembly: "))
            // {
            //     outLine = memberIndent + outLine.Trim();
            // }

            outLines.Add(outLine.Replace("<inheritdoc cref=", "Refer to ").Replace("<see cref=").Replace("/>").TrimEnd());
        }

        File.WriteAllText(outputPath, outLines.Join("\n"));
    }
}
