// Hirdmandr
// a Valheim mod skeleton using Jotunn
// 
// File:    HMSchedule.cs
// Project: Hirdmandr

namespace Hirdmandr
{
    // Every schedule time-of-day value in the mod, in one place. Each constant is a boundary,
    // named for the window it STARTS; windows are half-open [start, next boundary) via
    // ToDInWindow, so a boundary belongs to exactly one window (no overlaps, no gaps) and
    // start > end wraps the midnight boundary. ToD is EnvMan's smooth day fraction
    // (0 = midnight, 0.5 = noon).
    //
    // To change a job's schedule, edit the boundary here; no other file carries a schedule
    // literal (the routing itself is in HirdmandrNPC.GetStateBySchedule).
    public static class HMSchedule
    {
        // Artisan (role Artisan or unassigned): morn socialize -> work -> evening socialize -> rest.
        public const float ArtisanMornSocialStart = 0.2483f;
        public const float ArtisanWorkdayStart = 0.3000f;
        public const float ArtisanEveningSocialStart = 0.7500f;
        public const float ArtisanRestStart = 0.909f;

        // Himthiki: socialize all day, rest at night.
        public const float HimthikiSocialStart = 0.2483f;
        public const float HimthikiRestStart = 0.909f;

        // Thegn day shift: patrol -> socialize -> rest.
        public const float ThegnDayPatrolStart = 0.2483f;
        public const float ThegnDaySocialStart = 0.7683f;
        public const float ThegnDayRestStart = 0.909f;

        // Thegn night shift: patrol (wraps midnight) -> socialize -> rest.
        public const float ThegnNightPatrolStart = 0.7383f;
        public const float ThegnNightSocialStart = 0.2783f;
        public const float ThegnNightRestStart = 0.4183f;

        // Is ToD inside [start, end)? A boundary belongs to the window that STARTS there, so
        // adjacent windows never overlap or leave a gap. start > end wraps the day boundary
        // (midnight), e.g. a rest window that runs 0.909 -> 0.2483.
        public static bool ToDInWindow(float ToD, float start, float end)
        {
            if (start < end)
            {
                return ToD >= start && ToD < end;
            }
            return ToD >= start || ToD < end;
        }
    }
}
