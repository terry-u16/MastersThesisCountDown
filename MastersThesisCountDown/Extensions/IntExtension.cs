using System;
using Microsoft.SPOT;

namespace MastersThesisCountDown.Extensions
{
    public static class IntExtension
    {
        public static string To2DigitString(this int value)
        {
            return ((value / 10) % 10).ToString() + (value % 10).ToString();
        }

        public static string To4DigitString(this int value)
        {
            return ((value / 1000) % 10).ToString() + ((value / 100) % 10).ToString() + ((value / 10) % 10).ToString() + (value % 10).ToString();
        }

    }
}
