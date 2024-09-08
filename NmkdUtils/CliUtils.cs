
namespace NmkdUtils
{
    public class CliUtils
    {
        public static string ReadLine(string prompt, bool linebreak = false)
        {
            Logger.WaitForEmptyQueue();
            Console.Write(prompt + (linebreak ? Environment.NewLine : " "));
            return $"{Console.ReadLine()}";
        }

    }
}