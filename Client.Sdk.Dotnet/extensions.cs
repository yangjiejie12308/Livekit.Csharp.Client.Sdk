using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Sdk.Dotnet
{
    internal static class Extensions
    {
        public static bool IsNullOrEmpty(this string value)
        {
            if (value != null)
            {
                return value.Length == 0;
            }

            return true;
        }
    }
}
