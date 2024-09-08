

namespace NmkdUtils
{
    public class CodeUtils
    {
        /// <summary> Checks the failure condition <paramref name="failureCondition"/>. Executes the action <paramref name="failureAction"/> and returns true if the condition is met. </summary>
        public static bool Assert(Func<bool> failureCondition, Action failureAction = null)
        {
            if (failureCondition())
            {
                failureAction?.Invoke();
                return true;
            }

            return false;
        }

        /// <inheritdoc cref="Assert(Func{bool}, Action)"/>
        public static bool Assert(bool failureCondition, Action failureAction = null)
        {
            return Assert(() => failureCondition, failureAction);
        }

    }
}
