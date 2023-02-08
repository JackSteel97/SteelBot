using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using SteelBot.Database;
using SteelBot.Database.Models.Pets;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteelBot.DataProviders.SubProviders;

public class PetsProvider
{
    private readonly IDbContextFactory<SteelBotContext> _dbContextFactory;
    private readonly AsyncReaderWriterLock _lock = new();
    private readonly ILogger<PetsProvider> _logger;
    private readonly Dictionary<ulong, Dictionary<long, Pet>> _petsByUserId;

    public PetsProvider(ILogger<PetsProvider> logger, IDbContextFactory<SteelBotContext> dbContextFactory)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;

        _petsByUserId = new Dictionary<ulong, Dictionary<long, Pet>>();
        LoadPetsData();
    }

    private void LoadPetsData()
    {
        using (_lock.WriterLock())
        {
            _logger.LogInformation("Loading data from database: Pets");

            using (var db = _dbContextFactory.CreateDbContext())
            {
                var petData = db.Pets.Include(p => p.Attributes).Include(p => p.Bonuses).AsNoTracking().ToList();

                foreach (var pet in petData) AddToCache(pet);
            }
        }
    }

    public bool TryGetUsersPets(ulong userDiscordId, out List<Pet> pets)
    {
        bool userHasPets;
        using (_lock.ReaderLock())
        {
            userHasPets = _petsByUserId.TryGetValue(userDiscordId, out var indexedPets);
            pets = userHasPets ? indexedPets.Values.Where(p=>!p.IsDead).ToList() : new List<Pet>();
        }

        return userHasPets && pets.Count > 0;
    }

    public bool TryGetUsersPetsCount(ulong userDiscordId, out int numberOfOwnedPets)
    {
        bool userHasPets;
        using (_lock.ReaderLock())
        {
            userHasPets = TryGetUsersPets(userDiscordId, out var pets);
            numberOfOwnedPets = userHasPets ? pets.Count : 0;
        }

        return userHasPets;
    }

    public bool TryGetPet(ulong userDiscordId, long petId, out Pet pet)
    {
        bool result = false;
        using (_lock.ReaderLock())
        {
            result = TryGetPetCore(userDiscordId, petId, out pet);
        }

        return result;
    }

    public async Task<long> InsertPet(Pet pet)
    {
        long id = 0;
        using (await _lock.WriterLockAsync())
        {
            if (!BotKnowsPet(pet.OwnerDiscordId, pet.RowId))
            {
                _logger.LogInformation("Writing a new Pet [{PetName}] for User [{UserId}] to the database", pet.GetName(), pet.OwnerDiscordId);

                int writtenCount;
                using (var db = _dbContextFactory.CreateDbContext())
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
                    _logger.LogError("Writing Pet [{PetName}] for User [{UserId}] to the database inserted no entities. The internal cache was not changed", pet.GetName(), pet.OwnerDiscordId);
                }
            }
        }

        return id;
    }

    public async Task RemovePet(ulong userDiscordId, long petId)
    {
        using (await _lock.WriterLockAsync())
        {
            if (TryGetPetCore(userDiscordId, petId, out var pet))
            {
                _logger.LogInformation("Deleting a Pet [{PetName}] from User [{UserId}]", pet.GetName(), pet.OwnerDiscordId);

                int writtenCount;
                using (var db = _dbContextFactory.CreateDbContext())
                {
                    db.Pets.Remove(pet);
                    writtenCount = await db.SaveChangesAsync();
                }

                if (writtenCount > 0)
                    RemoveFromCache(pet);
                else
                    _logger.LogError("Deleting Pet [{PetName}] from User [{UserId}] from the database altered no entities. The internal cache was not changed", pet.GetName(), pet.OwnerDiscordId);
            }
        }
    }

    public async Task UpdatePets(ICollection<Pet> pets)
    {
        if (pets?.Count == 0) return;

        using (await _lock.WriterLockAsync())
        {
            int writtenCount;
            using (var db = _dbContextFactory.CreateDbContext())
            {
                // Load all the existing pets first - this is way more performant for some unknown reason loading them one-by-one executes far more queries than required.
                var originalPets = await db.Pets
                    .Include(x => x.Bonuses)
                    .Where(x => pets.Select(y => y.RowId).Contains(x.RowId))
                    .ToDictionaryAsync(x => x.RowId);
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
                foreach (var newPet in pets)
                    UpdateInCache(newPet);
            else
                _logger.LogError("Updating A collection of Pets did not alter any entities. The internal cache was not changed");
        }
    }

    public async Task UpdatePet(Pet newPet)
    {
        using (await _lock.WriterLockAsync())
        {
            int writtenCount;
            using (var db = _dbContextFactory.CreateDbContext())
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
                UpdateInCache(newPet);
            else
                _logger.LogError("Updating Pet [{PetId}] with Owner [{UserId}] did not alter any entities. The internal cache was not changed", newPet.RowId, newPet.OwnerDiscordId);
        }
    }

    private bool TryGetPetCore(ulong userDiscordId, long petId, out Pet pet)
    {
        pet = null;
        return _petsByUserId.TryGetValue(userDiscordId, out var pets) && pets.TryGetValue(petId, out pet) && !pet.IsDead;
    }

    private bool BotKnowsPet(ulong userDiscordId, long petId) => _petsByUserId.TryGetValue(userDiscordId, out var pets) && pets.ContainsKey(petId);

    private void AddToCache(Pet pet)
    {
        if (!_petsByUserId.TryGetValue(pet.OwnerDiscordId, out var pets))
            _petsByUserId[pet.OwnerDiscordId] = new Dictionary<long, Pet> { { pet.RowId, pet } };
        else
            pets.Add(pet.RowId, pet);
    }

    private void RemoveFromCache(Pet pet)
    {
        if (_petsByUserId.TryGetValue(pet.OwnerDiscordId, out var pets)) pets.Remove(pet.RowId);
    }

    private void UpdateInCache(Pet newPet)
    {
        if (_petsByUserId.TryGetValue(newPet.OwnerDiscordId, out var pets)) pets[newPet.RowId] = newPet;
    }
}