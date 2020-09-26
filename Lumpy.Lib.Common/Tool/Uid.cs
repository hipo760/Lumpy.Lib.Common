using System;

namespace Lumpy.Lib.Common.Tool
{
    public static class Uid
    {
        public static string ShortId(int length)
        {
            var candi = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
            var rng = new Random(Guid.NewGuid().GetHashCode());
            var stringChars = new char[length];

            for (var i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = candi[rng.Next(candi.Length)];
            }
            var finalString = new string(stringChars);
            return finalString;
        }
    }
}
