using System;

namespace dftbsvc
{
    public static class IdGenerator 
    {
        readonly static long BaseTicks = new DateTime(1900, 1, 1).Ticks;

        public static Guid NewId()
        {
            byte[] guidArray = Guid.NewGuid().ToByteArray();

            var now = DateTime.UtcNow;

            byte[] daysArray = BitConverter.GetBytes(new TimeSpan(now.Ticks - BaseTicks).Days);
            byte[] msecsArray = BitConverter.GetBytes((long)(now.TimeOfDay.TotalMilliseconds));

            Array.Reverse(daysArray);
            Array.Reverse(msecsArray);

            Array.Copy(daysArray, daysArray.Length - 2, guidArray, guidArray.Length - 6, 2);
            Array.Copy(msecsArray, msecsArray.Length - 4, guidArray, guidArray.Length - 4, 4);

            return new Guid(guidArray);
        }
    }
}