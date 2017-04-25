
using System;

namespace DataService.Utils
{
    public static class ConvertHelper
    {
        public static byte ToByte(this string byeStringValue)
        {
            if (string.IsNullOrEmpty(byeStringValue))
                return 0;

            return Convert.ToByte(byeStringValue);
        }
    }
}
