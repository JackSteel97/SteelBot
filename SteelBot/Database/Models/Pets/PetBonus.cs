using SteelBot.DiscordModules.Pets.Enums;

namespace SteelBot.Database.Models.Pets
{
    public class PetBonus
    {
        public long RowId { get; set; }
        public long PetId { get; set; }
        public Pet Pet { get; set; }
        public BonusType BonusType { get; set; }
        public double Value { get; set; }
    }
}
