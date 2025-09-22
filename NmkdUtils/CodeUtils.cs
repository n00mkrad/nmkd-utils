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
        /// <summary> Assert and log a warning if <paramref name="failureCondition"/> is true. </summary>
        public static bool AssertWarn(bool failureCondition, string msg, Action? failureAction = null, Action? successAction = null)
        {
            bool a = Assert(() => failureCondition, failureAction, successAction);
            Logger.Log(msg, Logger.Level.Warning, condition: () => a);
            return a;
        }
        /// <summary> Assert and log an error if <paramref name="failureCondition"/> is true. </summary>
        public static bool AssertErr(bool failureCondition, string msg, Action? failureAction = null, Action? successAction = null)
        {
            bool a = Assert(() => failureCondition, failureAction, successAction);
            Logger.Log(msg, Logger.Level.Error, condition: () => a);
            return a;
        }
        /// <summary> Inverse <see cref="Assert(Func{bool}, Action, Action)"/> for validation logic. </summary>
        public static bool Validate(Func<bool> requiredCondition, Action? failureAction = null, Action? successAction = null) => Assert(() => requiredCondition() == false, failureAction, successAction);
        /// <summary> <inheritdoc cref="Validate(Func{bool}, Action, Action)"/> </summary>
        public static bool Validate(bool requiredCondition, Action? failureAction = null, Action? successAction = null) => Assert(() => !requiredCondition, failureAction, successAction);

        /// <summary> Set <paramref name="variable"/> to <paramref name="value"/> if <paramref name="condition"/> is true. </summary>
        public static void SetIf<T>(ref T variable, T value, Func<bool> condition)
        {
            if (condition())
                variable = value;
        }

        /// <summary> Set <paramref name="variable"/> to <paramref name="value"/> if <paramref name="value"/> is not null. </summary>
        public static void SetIfNotNull<T>(ref T variable, object? value)
        {
            if (value != null)
                variable = (T)value;
        }

        /// <summary>
        /// Try running an action, catch any exceptions, optionally log them. <paramref name="logEx"/> true = Always log exceptions, false = Never log exceptions, null = Log if no catchAction is provided. <br/>
        /// An error description can be added with <paramref name="errNote"/>. Stack trace logging is disabled unless <paramref name="logTrace"/> is true. <br/>
        /// </summary>
        public static bool Try(Action tryAction, Action<Exception>? catchAction = null, bool? logEx = null, string errNote = "", bool logTrace = false)
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
                    Logger.Log(ex, errNote, logTrace);
                }

                catchAction?.Invoke(ex);
                return false;
            }
        }

        public static void TryNoWait(Action tryAction, Action<Exception>? catchAction = null, bool? logEx = null, string errNote = "", bool logTrace = false) =>
            Task.Run(() => Try(tryAction, catchAction, logEx, errNote, logTrace));

        /// <summary>
        /// <inheritdoc cref="Try(Action, Action{Exception}, bool?, string, bool)"/>
        /// If an exception occurs and <paramref name="catchAction"/> is null, <paramref name="fallback"/> will be returned.
        /// </summary>
        public static TResult Try<TResult>(Func<TResult> tryAction, Func<Exception, TResult>? catchAction = null, bool? logEx = null, string errNote = "", bool logTrace = false, TResult? fallback = default)
        {
            if (tryAction == null)
                return fallback;

            try
            {
                return tryAction();
            }
            catch (Exception ex)
            {
                if (logEx == true || (logEx == null && catchAction is null))
                {
                    Logger.Log(ex, errNote, logTrace);
                }

                if (catchAction != null)
                    return catchAction(ex);
            }

            return fallback;
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

        // Get enum values
        public static List<T> GetEnums<T>() where T : Enum => Enum.GetValues(typeof(T)).Cast<T>().ToList();
    }
}
