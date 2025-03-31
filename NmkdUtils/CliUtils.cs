

namespace NmkdUtils
{
    public class CliUtils
    {
        public static bool AllowInteraction = true;

        public static string ReadLine(string prompt, bool linebreak = false)
        {
            if(!AllowInteraction)
                return "";

            Logger.WaitForEmptyQueue();
            Console.Write(prompt + (linebreak ? Environment.NewLine : " "));
            Logger.LastLogMsgCon = prompt;
            return $"{Console.ReadLine()}";
        }

        public static bool ReadLineBool(string prompt, bool linebreak = false)
        {
            if (!AllowInteraction)
                return false;

            string input = ReadLine(prompt, linebreak).Low().Trim();
            bool result = input == "y" || input == "true" || input == "1";
            return result;
        }

        public static string ReadLine(string prompt, Action<string> action, bool linebreak = false)
        {
            if (!AllowInteraction)
                return "";

            string input = ReadLine(prompt, linebreak);
            action(input);
            return input;
        }

        public static bool ReadLineBool(string prompt, Action<bool> action, bool linebreak = false)
        {
            if (!AllowInteraction)
                return false;

            bool result = ReadLineBool(prompt, linebreak);
            action(result);
            return result;
        }

        public static void ReplaceLastConsoleLine(string text)
        {
            Console.SetCursorPosition(0, Console.CursorTop - 1); // Move the cursor up one line
            Console.Write(new string(' ', Console.WindowWidth)); // Clear the line
            Console.SetCursorPosition(0, Console.CursorTop); // Move the cursor to the start of the line again
            Console.WriteLine(text); // Write the new text
        }

        public static void ClearConsoleAnsi ()
        {
            Console.Write("\x1b[2J\x1b[H");
        }

    }
}