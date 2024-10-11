

namespace NmkdUtils.Data
{
    public class OsData
    {
        public class WindowMsgs
        {
            public const uint WM_CLOSE = 0x0010; // Close the window
            public const uint WM_SYSCOMMAND = 0x0112; // Window menu command (followed by parameter)
            public const uint SC_MINIMIZE = 0xF020; // Minimize the window
            public const uint SC_MAXIMIZE = 0xF030; // Maximize the window
            public const uint SC_RESTORE = 0xF120;  // Restore the window to its normal position and size
            public const uint SC_CLOSE = 0xF060;  // Close the window
        }
    }
}
