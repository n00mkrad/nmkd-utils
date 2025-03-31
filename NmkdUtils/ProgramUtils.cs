using System.Globalization;

namespace NmkdUtils
{
    public class ProgramUtils
    {
        /// <summary>
        /// Sets the culture for the application. Useful for parsing numbers, dates, etc. Default <paramref name="culture"/> is to English (US).
        /// </summary>
        public static void SetCulture (string culture = "en-US")
        {
            var c = new CultureInfo(culture);
            Thread.CurrentThread.CurrentCulture = c;
            Thread.CurrentThread.CurrentUICulture = c;
            CultureInfo.DefaultThreadCurrentCulture = c;
            CultureInfo.DefaultThreadCurrentUICulture = c;
        }
    }
}
