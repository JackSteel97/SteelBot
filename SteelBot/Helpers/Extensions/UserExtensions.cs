using SteelBot.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.Helpers.Extensions
{
    public static class UserExtensions
    {
        public static long GetMutedXp(this User user)
        {
            return -(long)Math.Floor(LevellingMaths.GetDurationXp(user.TimeSpentMuted));
        }

        public static long GetDeafendedXp(this User user)
        {
            return -(long)Math.Floor(LevellingMaths.GetDurationXp(user.TimeSpentDeafened, 0.5));
        }

        public static ulong GetVoiceXp(this User user)
        {
            return (ulong)Math.Floor(LevellingMaths.GetDurationXp(user.TimeSpentInVoice));
        }

        public static ulong GetStreamingXp(this User user)
        {
            return (ulong)Math.Floor(LevellingMaths.GetDurationXp(user.TimeSpentStreaming, 5));
        }

        public static ulong GetVideoXp(this User user)
        {
            return (ulong)Math.Floor(LevellingMaths.GetDurationXp(user.TimeSpentOnVideo, 10));
        }
    }
}