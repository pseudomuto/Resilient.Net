using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resilient.Net
{
    internal static class Guard
    {
        public static void EnsureNotNull(this object value, string name)
        {
            if (value == null)
            {
                throw new ArgumentNullException(name);
            }
        }

        public static T ValueOrThrow<T>(this T value, string name)
        {
            if (value == null)
            {
                throw new ArgumentNullException(name);
            }

            return value;
        }

        public static int PositiveValueOrThrow(this int value, string name)
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(name);
            }

            return value;
        }       

        public static TimeSpan PositiveOrThrow(this TimeSpan value, string name)
        {
            ((int)value.TotalMilliseconds).PositiveValueOrThrow(name);
            return value;
        }
    }
}
