using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DiscordModules.Stats.Models;

public record struct XpVelocity(ulong Message, ulong Voice, ulong Muted, ulong Deafened, ulong Streaming, ulong Video, ulong Passive);