using System;

namespace Resilient.Net
{
    internal static class Guard
    {
        public static void ThrowIfNull(this object value, string name)
        {
            if (value == null)
            {
                throw new ArgumentNullException(name);
            }
        }

        public static T OrThrow<T>(this T value, string name) where T : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(name);
            }

            return value;
        }

        public static int PositiveOrThrow(this int value, string name)
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(name);
            }

            return value;
        }

        public static TimeSpan PositiveOrThrow(this TimeSpan value, string name)
        {
            ((int)value.TotalMilliseconds).PositiveOrThrow(name);
            return value;
        }
    }
}
