using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SteelBot.Database;
using SteelBot.Database.Models.Pets;
using SteelBot.Services.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelBot.DataProviders.SubProviders
{
    public class PetsProvider
    {
        private readonly ILogger<PetsProvider> Logger;
        private readonly IDbContextFactory<SteelBotContext> DbContextFactory;
        private readonly AppConfigurationService AppConfigurationService;

        private readonly Dictionary<ulong, Dictionary<long, Pet>> PetsByUserId;

        public PetsProvider(ILogger<PetsProvider> logger, IDbContextFactory<SteelBotContext> dbContextFactory, AppConfigurationService appConfigurationService)
        {
            Logger = logger;
            DbContextFactory = dbContextFactory;
            AppConfigurationService = appConfigurationService;

            PetsByUserId = new Dictionary<ulong, Dictionary<long, Pet>>();
            LoadPetsData();
        }

        private void LoadPetsData()
        {
            Logger.LogInformation("Loading data from database: Pets");

            using (SteelBotContext db = DbContextFactory.CreateDbContext())
            {
                var petData = db.Pets.Include(p => p.Attributes).Include(p => p.Bonuses).AsNoTracking().ToList();

                foreach (var pet in petData)
                {
                    AddToCache(pet);
                }
            }
        }

        public bool TryGetUsersPets(ulong userDiscordId, out List<Pet> pets)
        {
            bool userHasPets = PetsByUserId.TryGetValue(userDiscordId, out var indexedPets);
            if (userHasPets)
            {
                pets = indexedPets.Values.ToList();
            }
            else
            {
                pets = new List<Pet>();
            }

            return userHasPets;
        }

        public bool BotKnowsPet(ulong userDiscordId, long petId)
        {
            return PetsByUserId.TryGetValue(userDiscordId, out var pets) && pets.ContainsKey(petId);
        }

        public void AddToCache(Pet pet)
        {
            if (!PetsByUserId.TryGetValue(pet.OwnerDiscordId, out var pets))
            {
                PetsByUserId[pet.OwnerDiscordId] = new Dictionary<long, Pet>
                {
                    { pet.RowId, pet },
                };
            }
            else
            {
                pets.Add(pet.RowId, pet);
            }
        }

        public async Task InsertPet(Pet pet)
        {
            if (!BotKnowsPet(pet.OwnerDiscordId, pet.RowId))
            {
                Logger.LogInformation("Writing a new Pet [{PetName}] for User [{UserId}] to the database", pet.GetName(), pet.OwnerDiscordId);

                int writtenCount;
                using (SteelBotContext db = DbContextFactory.CreateDbContext())
                {
                    db.Pets.Add(pet);
                    writtenCount = await db.SaveChangesAsync();
                }

                if (writtenCount > 0)
                {
                    AddToCache(pet);
                }
                else
                {
                    Logger.LogError("Writing Pet [{PetName}] for User [{UserId}] to the database inserted no entities. The internal cache was not changed.", pet.GetName(), pet.OwnerDiscordId);
                }
            }
        }
    }
}
