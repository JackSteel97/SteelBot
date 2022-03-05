using SteelBot.Database.Models.Pets;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.Helpers;
using System.Text;

namespace SteelBot.DiscordModules.Pets.Models
{
    public class BonusTotals
    {
        public double All { get; private set; }
        public double Message { get; private set; }
        public double Voice { get; private set; }
        public double MutedPenalty { get; private set; }
        public double DeafenedPenalty { get; private set; }
        public double Streaming { get; private set; }
        public double Video { get; private set; }
        public double PassiveOffline { get; private set; }

        public void Add(Pet pet)
        {
            foreach (var bonus in pet.Bonuses)
            {
                Add(bonus);
            }

            if (pet.Rarity == Rarity.Legendary)
            {
                AddPassive(pet.CurrentLevel);
            }
            else if (pet.Rarity == Rarity.Mythical)
            {
                AddPassive(pet.CurrentLevel * 2);
            }
        }

        public void Add(PetBonus bonus)
        {
            switch (bonus.BonusType)
            {
                case BonusType.All:
                    All += bonus.Value;
                    break;
                case BonusType.Message:
                    Message += bonus.Value;
                    break;
                case BonusType.Voice:
                    Voice += bonus.Value;
                    break;
                case BonusType.Streaming:
                    Streaming += bonus.Value;
                    break;
                case BonusType.Video:
                    Video += bonus.Value;
                    break;
                case BonusType.MutedPentalty:
                    MutedPenalty += bonus.Value;
                    break;
                case BonusType.DeafenedPenalty:
                    DeafenedPenalty += bonus.Value;
                    break;
            }
        }

        public void AddPassive(double passiveXp)
        {
            PassiveOffline += passiveXp;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            if (All != 0)
            {
                sb.Append(GetEmoji(All)).Append(" - `All XP: ").Append(All.ToString("P2")).AppendLine("`");
            }
            if (Message != 0)
            {
                sb.Append(GetEmoji(Message)).Append(" - `Message XP: ").Append(Message.ToString("P2")).AppendLine("`");
            }
            if (Voice != 0)
            {
                sb.Append(GetEmoji(Voice)).Append(" - `Voice XP: ").Append(Voice.ToString("P2")).AppendLine("`");
            }
            if (MutedPenalty != 0)
            {
                sb.Append(GetEmoji(MutedPenalty, true)).Append(" - `Muted Penalty XP: ").Append(MutedPenalty.ToString("P2")).AppendLine("`");
            }
            if (DeafenedPenalty != 0)
            {
                sb.Append(GetEmoji(DeafenedPenalty, true)).Append(" - `Deafened Penalty XP: ").Append(DeafenedPenalty.ToString("P2")).AppendLine("`");
            }
            if (Streaming != 0)
            {
                sb.Append(GetEmoji(Streaming)).Append(" - `Deafened Penalty XP: ").Append(Streaming.ToString("P2")).AppendLine("`");
            }
            if (Video != 0)
            {
                sb.Append(GetEmoji(Video)).Append(" - `Video XP: ").Append(Video.ToString("P2")).AppendLine("`");
            }
            if (PassiveOffline != 0)
            {
                sb.Append(GetEmoji(PassiveOffline)).Append(" - `Passive Offline XP: ").Append(PassiveOffline).AppendLine("`");
            }

            return sb.ToString();
        }

        private string GetEmoji(double value, bool isNegativeType = false)
        {
            var emoji = EmojiConstants.CustomDiscordEmojis.GreenArrowUp;
            if ((isNegativeType && value > 0) || (!isNegativeType && value <= 0))
            {
                emoji = EmojiConstants.CustomDiscordEmojis.RedArrowDown;
            }
            return emoji;
        }
    }
}
