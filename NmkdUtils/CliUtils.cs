
namespace NmkdUtils
{
    public class CliUtils
    {
        public static string ReadLine(string prompt, bool linebreak = false)
        {
            Logger.WaitForEmptyQueue();
            Console.Write(prompt + (linebreak ? Environment.NewLine : " "));
            Logger.LastLogMsgCon = prompt;
            return $"{Console.ReadLine()}";
        }

        public static void ReplaceLastConsoleLine(string text)
        {
            Console.SetCursorPosition(0, Console.CursorTop - 1); // Move the cursor up one line
            Console.Write(new string(' ', Console.WindowWidth)); // Clear the line
            Console.SetCursorPosition(0, Console.CursorTop); // Move the cursor to the start of the line again
            Console.WriteLine(text); // Write the new text
        }
    }
}