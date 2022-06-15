using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using SteelBot.Database;
using SteelBot.Database.Models.Pets;
using SteelBot.Services.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SteelBot.DataProviders.SubProviders
{
    public class PetsProvider
    {
        private readonly ILogger<PetsProvider> Logger;
        private readonly IDbContextFactory<SteelBotContext> DbContextFactory;
        private readonly AppConfigurationService AppConfigurationService;
        private readonly AsyncReaderWriterLock Lock = new AsyncReaderWriterLock();

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
            using (Lock.WriterLock())
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
        }

        public bool TryGetUsersPets(ulong userDiscordId, out List<Pet> pets)
        {
            bool userHasPets;
            using (Lock.ReaderLock())
            {
                userHasPets = PetsByUserId.TryGetValue(userDiscordId, out var indexedPets);
                if (userHasPets)
                {
                    pets = indexedPets.Values.ToList();
                }
                else
                {
                    pets = new List<Pet>();
                }
            }

            return userHasPets && pets.Count > 0;
        }

        public bool TryGetUsersPetsCount(ulong userDiscordId, out int numberOfOwnedPets)
        {
            bool userHasPets;
            using (Lock.ReaderLock())
            {
                userHasPets = PetsByUserId.TryGetValue(userDiscordId, out var pets);
                if (userHasPets)
                {
                    numberOfOwnedPets = pets.Count;
                }
                else
                {
                    numberOfOwnedPets = 0;
                }
            }
            return userHasPets;
        }

        public bool TryGetPet(ulong userDiscordId, long petId, out Pet pet)
        {
            using (Lock.ReaderLock())
            {
                return TryGetPetCore(userDiscordId, petId, out pet);
            }
        }

        public async Task<long> InsertPet(Pet pet)
        {
            long id = 0;
            using (await Lock.WriterLockAsync())
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
                        id = pet.RowId;
                    }
                    else
                    {
                        Logger.LogError("Writing Pet [{PetName}] for User [{UserId}] to the database inserted no entities. The internal cache was not changed.", pet.GetName(), pet.OwnerDiscordId);
                    }
                }
            }
            return id;
        }

        public async Task RemovePet(ulong userDiscordId, long petId)
        {
            using (await Lock.WriterLockAsync())
            {
                if (TryGetPetCore(userDiscordId, petId, out var pet))
                {
                    Logger.LogInformation("Deleting a Pet [{PetName}] from User [{UserId}]", pet.GetName(), pet.OwnerDiscordId);

                    int writtenCount;
                    using (SteelBotContext db = DbContextFactory.CreateDbContext())
                    {
                        db.Pets.Remove(pet);
                        writtenCount = await db.SaveChangesAsync();
                    }

                    if (writtenCount > 0)
                    {
                        RemoveFromCache(pet);
                    }
                    else
                    {
                        Logger.LogError("Deleting Pet [{PetName}] from User [{UserId}] from the database altered no entities. The internal cache was not changed.", pet.GetName(), pet.OwnerDiscordId);
                    }
                }
            }
        }

        public async Task UpdatePets(ICollection<Pet> pets)
        {
            if(pets?.Count == 0)
            {
                return;
            }

            using (await Lock.WriterLockAsync())
            {
                int writtenCount;
                using (SteelBotContext db = DbContextFactory.CreateDbContext())
                {
                    // Load all the existing pets first - this is way more performant for some unknown reason loading them one-by-one executes far more queries than required.
                    var originalPets = await db.Pets
                        .Include(x=>x.Bonuses)
                        .Where(x => pets.Select(y => y.RowId).Contains(x.RowId))
                        .ToDictionaryAsync(x=>x.RowId);
                    foreach (var newPet in pets)
                    {
                        // To prevent EF tracking issue, grab and alter existing value.
                        var original = originalPets[newPet.RowId];
                        db.Entry(original).CurrentValues.SetValues(newPet);

                        // The above doesn't update navigation properties. We must manually update any navigation properties we need to like this.
                        original.Bonuses = newPet.Bonuses;

                        db.Pets.Update(original);
                    }
                    writtenCount = await db.SaveChangesAsync();
                }

                if (writtenCount > 0)
                {
                    foreach (var newPet in pets)
                    {
                        UpdateInCache(newPet);
                    }
                }
                else
                {
                    Logger.LogError("Updating A collection of Pets did not alter any entities. The internal cache was not changed.");
                }
            }
        }

        public async Task UpdatePet(Pet newPet)
        {
            using (await Lock.WriterLockAsync())
            {
                int writtenCount;
                using (SteelBotContext db = DbContextFactory.CreateDbContext())
                {
                    // To prevent EF tracking issue, grab and alter existing value.
                    var original = db.Pets.Include(x => x.Bonuses).First(u => u.RowId == newPet.RowId);
                    db.Entry(original).CurrentValues.SetValues(newPet);

                    // The above doesn't update navigation properties. We must manually update any navigation properties we need to like this.
                    original.Bonuses = newPet.Bonuses;

                    db.Pets.Update(original);
                    writtenCount = await db.SaveChangesAsync();
                }

                if (writtenCount > 0)
                {
                    UpdateInCache(newPet);
                }
                else
                {
                    Logger.LogError("Updating Pet [{PetId}] with Owner [{UserId}] did not alter any entities. The internal cache was not changed.", newPet.RowId, newPet.OwnerDiscordId);
                }
            }
        }

        private bool TryGetPetCore(ulong userDiscordId, long petId, out Pet pet)
        {
            pet = null;
            return PetsByUserId.TryGetValue(userDiscordId, out var pets) && pets.TryGetValue(petId, out pet);
        }

        private bool BotKnowsPet(ulong userDiscordId, long petId)
        {
            return PetsByUserId.TryGetValue(userDiscordId, out var pets) && pets.ContainsKey(petId);
        }

        private void AddToCache(Pet pet)
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

        private void RemoveFromCache(Pet pet)
        {
            if (PetsByUserId.TryGetValue(pet.OwnerDiscordId, out var pets))
            {
                pets.Remove(pet.RowId);
            }
        }

        private void UpdateInCache(Pet newPet)
        {
            if (PetsByUserId.TryGetValue(newPet.OwnerDiscordId, out var pets))
            {
                pets[newPet.RowId] = newPet;
            }
        }
    }
}
