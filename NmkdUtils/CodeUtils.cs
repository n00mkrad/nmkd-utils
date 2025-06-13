

using System.Diagnostics;

namespace NmkdUtils
{
    public class CodeUtils
    {
        /// <summary> Returns <paramref name="failureCondition"/>. Can optionally run a <paramref name="failureAction"/> or a <paramref name="successAction"/>. </summary>
        public static bool Assert(Func<bool> failureCondition, Action? failureAction = null, Action? successAction = null)
        {
            if (failureCondition())
            {
                failureAction?.Invoke();
                return true;
            }

            successAction?.Invoke();
            return false;
        }

        /// <inheritdoc cref="Assert(Func{bool}, Action, Action)"/>
        public static bool Assert(bool failureCondition, Action? failureAction = null, Action? successAction = null) => Assert(() => failureCondition, failureAction, successAction);
        public static bool Validate(Func<bool> requiredCondition, Action? failureAction = null, Action? successAction = null) => Assert(() => requiredCondition() == false, failureAction, successAction);
        public static bool Validate(bool requiredCondition, Action? failureAction = null, Action? successAction = null) => Assert(() => !requiredCondition, failureAction, successAction);

        // Function to set a variable via ref only if a given condition is met
        public static void SetIf<T>(ref T variable, T value, Func<bool> condition)
        {
            if (condition())
                variable = value;
        }

        public static void SetIfNotNull<T>(ref T variable, object? value)
        {
            if (value != null)
                variable = (T)value;
        }

        /// <summary>
        /// Try running an action, catch any exceptions, optionally log them. <paramref name="logEx"/> true = Always log exceptions, false = Never log exceptions, null = Log if no catchAction is provided. <br/>
        /// Stack trace is not logged by default, but can be enabled with <paramref name="printTrace"/>. <br/>
        /// </summary>
        public static bool Try(Action tryAction, Action<Exception>? catchAction = null, bool? logEx = null, string errNote = "", bool printTrace = false)
        {
            if (tryAction == null)
                return true;

            try
            {
                tryAction();
                return true;
            }
            catch (Exception ex)
            {
                if (logEx == true || (logEx == null && catchAction is null))
                {
                    Logger.Log(ex, errNote, printTrace);
                }

                catchAction?.Invoke(ex);
                return false;
            }
        }

        /// <summary> <inheritdoc cref="Try(Action, Action{Exception}, bool?, string, bool)"/> </summary>
        public static TResult Try<TResult>(Func<TResult> tryAction, Func<Exception, TResult>? catchAction = null, bool? logEx = null, string errNote = "", bool printTrace = false)
        {
            if (tryAction == null)
                return default;

            try
            {
                return tryAction();
            }
            catch (Exception ex)
            {
                if (logEx == true || (logEx == null && catchAction is null))
                {
                    Logger.Log(ex, errNote, printTrace);
                }

                if (catchAction != null)
                    return catchAction(ex);
            }

            return default;
        }

        /// <summary>
        /// Shortcut for a while loop that has a time limit, a sleep interval, and an optional break condition.
        /// </summary>
        public static void LimitedWhile(int timeoutMs, int intervalMs, Func<bool>? breakCondition = null, Action? action = null)
        {
            var sw = Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (breakCondition?.Invoke() == true)
                    break;

                action?.Invoke();
                Thread.Sleep(intervalMs);
            }
        }
    }
}
