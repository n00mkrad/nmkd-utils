

using static NmkdUtils.Logger;

namespace NmkdUtils
{
    public class CliUtils
    {
        public static bool AllowInteraction = true;

        /// <summary> Promtps the user and returns the entered text. </summary>
        public static string ReadLine(string prompt, bool linebreak = false)
        {
            if(!AllowInteraction)
                return "";

            Logger.WaitForEmptyQueue();
            Console.ResetColor();
            Console.Write(prompt + (linebreak ? Environment.NewLine : " "));
            Logger.LastLogMsgCon = prompt;
            return $"{Console.ReadLine()}";
        }

        /// <summary> Promtps the user and returns the entered text and runs an <paramref name="action"/> with the entered text (e.g. to assign the value). </summary>
        public static string ReadLine(string prompt, Action<string> action, bool linebreak = false)
        {
            if (!AllowInteraction)
                return "";

            string input = ReadLine(prompt, linebreak);
            action(input);
            return input;
        }

        /// <summary> Promtps the user to enter a bool. If nothing was entered, returns <paramref name="defaultVal"/>. </summary>
        public static bool ReadBool(string prompt, bool defaultVal = false, bool linebreak = false)
        {
            if (!AllowInteraction)
                return false;

            prompt = $"{prompt}? (Y/N - Default = {(defaultVal ? "Y" : "N")}):";
            string input = ReadLine(prompt, linebreak).Trim();

            if (input.IsEmpty())
                return defaultVal;

            return input.IsOneOf(false, "y", "true", "yes", "1");
        }

        /// <summary> Promtps the user to enter a bool and runs an <paramref name="action"/> with the entered bool (e.g. to assign the value). If nothing was entered, <paramref name="defaultVal"/> is used. </summary>
        public static bool ReadBool(string prompt, Action<bool> action, bool defaultVal = false, bool linebreak = false)
        {
            if (!AllowInteraction)
                return false;

            bool result = ReadBool(prompt, defaultVal, linebreak);
            action(result);
            return result;
        }

        /// <summary> Promtps the user to enter an integer. If nothing was entered, returns <paramref name="defaultVal"/>. </summary>
        public static int? ReadInt(string prompt, int? defaultVal = null, bool linebreak = false)
        {
            if (!AllowInteraction)
                return null;

            prompt = $"{prompt} (Default = {defaultVal}):";
            string input = ReadLine(prompt, linebreak).Trim();

            if (input.IsEmpty())
                return defaultVal;

            return input.GetInt();
        }

        /// <summary> Promtps the user to enter an integer and runs an <paramref name="action"/> with the entered number (e.g. to assign the value). If nothing was entered, <paramref name="defaultVal"/> is used. </summary>
        public static int? ReadInt(string prompt, Action<int?> action, int? defaultVal = null, bool linebreak = false)
        {
            if (!AllowInteraction)
                return null;

            int? result = ReadInt(prompt, defaultVal, linebreak);
            action(result);
            return result;
        }

        /// <summary> Replaces the last line in the console with the given <paramref name="text"/> </summary>
        public static void ReplaceLastConsoleLine(string text)
        {
            Console.SetCursorPosition(0, Console.CursorTop - 1); // Move the cursor up one line
            Console.Write(new string(' ', Console.WindowWidth)); // Clear the line
            Console.SetCursorPosition(0, Console.CursorTop); // Move the cursor to the start of the line again
            Console.WriteLine(text); // Write the new text
        }

        /// <summary> Clears the console using ANSI escape codes. </summary>
        public static void ClearConsoleAnsi ()
        {
            Console.Write("\x1b[2J\x1b[H");
        }

        /// <summary> Shows all available ConsoleColor values in the console with a sample text. </summary>
        public static void PrintConsoleColors()
        {
            Log("Console Colors:");
            foreach (ConsoleColor color in Enum.GetValues(typeof(ConsoleColor)))
            {
                Log(new Entry($"  This is ConsoleColor.{color}! - The Quick Brown Fox Jumps Over The Lazy Dog. Lorem Ipsum.") { CustomColor = color, WriteToFile = false });
            }
            Console.ResetColor();
        }
    }
}