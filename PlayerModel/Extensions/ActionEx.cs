using System;
using System.Linq;
using PlayerModel.Tools;

namespace PlayerModel.Extensions
{
    public static class ActionEx
    {
        public static void SafeInvoke(this Action action)
        {
            foreach (var invocation in (action?.GetInvocationList()).Cast<Action>())
            {
                try
                {
                    invocation?.Method?.Invoke(invocation?.Target, null);
                }
                catch(Exception ex)
                {
                    Logging.Error(ex);
                }
            }
        }

        public static void SafeInvoke<T>(this Action<T> action, params object[] args)
        {
            foreach (var invocation in (action?.GetInvocationList()).Cast<Action<T>>())
            {
                try
                {
                    invocation?.Method?.Invoke(invocation?.Target, args);
                }
                catch (Exception ex)
                {
                    Logging.Error(ex);
                }
            }
        }
    }
}
