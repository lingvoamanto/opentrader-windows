using System;
namespace OpenTrader.Indicators
{
    public static class Python
    {
        public static int[] Ones(double[] a)
        {
            int[] ones = new int[a.Length];
            Array.Fill<int>(ones, 1);
            return ones;
        }

        
    }
}
