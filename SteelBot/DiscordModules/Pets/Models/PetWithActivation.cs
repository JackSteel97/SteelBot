using SteelBot.Database.Models.Pets;

namespace SteelBot.DiscordModules.Pets.Models;

public readonly record struct PetWithActivation(Pet Pet, bool Active);