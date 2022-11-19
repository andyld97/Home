using System;

namespace Home.API.Helper
{
    public static class GeneralHelper
    {
        public static bool ConvertNullableValue(double? input, out int result)
        {
            result = default;

            if (input == null)
                return false;

            result = Convert.ToInt32(input.Value);
            return true;
        }
    }
}