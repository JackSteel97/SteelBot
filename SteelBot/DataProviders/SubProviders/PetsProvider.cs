using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using Sentry;
using SteelBot.Database;
using SteelBot.Database.Models.Pets;
using SteelBot.Helpers.Sentry;
using SteelBot.Services.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SteelBot.DataProviders.SubProviders;

public class PetsProvider
{
    private readonly ILogger<PetsProvider> _logger;
    private readonly IDbContextFactory<SteelBotContext> _dbContextFactory;
    private readonly AsyncReaderWriterLock _lock = new AsyncReaderWriterLock();
    private readonly IHub _sentry;

    private readonly Dictionary<ulong, Dictionary<long, Pet>> _petsByUserId;

    public PetsProvider(ILogger<PetsProvider> logger, IDbContextFactory<SteelBotContext> dbContextFactory, IHub sentry)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _sentry = sentry;

        _petsByUserId = new Dictionary<ulong, Dictionary<long, Pet>>();
        LoadPetsData();
    }

    private void LoadPetsData()
    {
        var transaction = _sentry.StartNewConfiguredTransaction("StartUp", nameof(LoadPetsData));

        using (_lock.WriterLock())
        {
            _logger.LogInformation("Loading data from database: Pets");

            using (var db = _dbContextFactory.CreateDbContext())
            {
                var petData = db.Pets.Include(p => p.Attributes).Include(p => p.Bonuses).AsNoTracking().ToList();

                foreach (var pet in petData)
                {
                    AddToCache(pet);
                }
            }
        }

        transaction.Finish();
    }

    public bool TryGetUsersPets(ulong userDiscordId, out List<Pet> pets)
    {
        var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(TryGetUsersPets));

        bool userHasPets;
        using (_lock.ReaderLock())
        {
            userHasPets = _petsByUserId.TryGetValue(userDiscordId, out var indexedPets);
            pets = userHasPets ? indexedPets.Values.ToList() : new List<Pet>();
        }

        transaction.Finish();

        return userHasPets && pets.Count > 0;
    }

    public bool TryGetUsersPetsCount(ulong userDiscordId, out int numberOfOwnedPets)
    {
        var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(TryGetUsersPetsCount));

        bool userHasPets;
        using (_lock.ReaderLock())
        {
            userHasPets = _petsByUserId.TryGetValue(userDiscordId, out var pets);
            numberOfOwnedPets = userHasPets ? pets.Count : 0;
        }

        transaction.Finish();
        return userHasPets;
    }

    public bool TryGetPet(ulong userDiscordId, long petId, out Pet pet)
    {
        var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(TryGetPet));

        bool result = false;
        using (_lock.ReaderLock())
        {
            result = TryGetPetCore(userDiscordId, petId, out pet);
        }

        transaction.Finish();
        return result;
    }

    public async Task<long> InsertPet(Pet pet)
    {
        var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(InsertPet));

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
                    _logger.LogError("Writing Pet [{PetName}] for User [{UserId}] to the database inserted no entities. The internal cache was not changed.", pet.GetName(), pet.OwnerDiscordId);
                }
            }
        }

        transaction.Finish();
        return id;
    }

    public async Task RemovePet(ulong userDiscordId, long petId)
    {
        var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(RemovePet));

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
                {
                    RemoveFromCache(pet);
                }
                else
                {
                    _logger.LogError("Deleting Pet [{PetName}] from User [{UserId}] from the database altered no entities. The internal cache was not changed.", pet.GetName(), pet.OwnerDiscordId);
                }
            }
        }

        transaction.Finish();
    }

    public async Task UpdatePets(ICollection<Pet> pets)
    {
        var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(UpdatePets), $"{pets.Count} pets");

        if (pets?.Count == 0)
        {
            return;
        }

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
            {
                foreach (var newPet in pets)
                {
                    UpdateInCache(newPet);
                }
            }
            else
            {
                _logger.LogError("Updating A collection of Pets did not alter any entities. The internal cache was not changed.");
            }
        }

        transaction.Finish();
    }

    public async Task UpdatePet(Pet newPet)
    {
        var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(UpdatePet));

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
            {
                UpdateInCache(newPet);
            }
            else
            {
                _logger.LogError("Updating Pet [{PetId}] with Owner [{UserId}] did not alter any entities. The internal cache was not changed.", newPet.RowId, newPet.OwnerDiscordId);
            }
        }

        transaction.Finish();
    }

    private bool TryGetPetCore(ulong userDiscordId, long petId, out Pet pet)
    {
        pet = null;
        return _petsByUserId.TryGetValue(userDiscordId, out var pets) && pets.TryGetValue(petId, out pet);
    }

    private bool BotKnowsPet(ulong userDiscordId, long petId) => _petsByUserId.TryGetValue(userDiscordId, out var pets) && pets.ContainsKey(petId);

    private void AddToCache(Pet pet)
    {
        var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(AddToCache));

        if (!_petsByUserId.TryGetValue(pet.OwnerDiscordId, out var pets))
        {
            _petsByUserId[pet.OwnerDiscordId] = new Dictionary<long, Pet>
            {
                { pet.RowId, pet },
            };
        }
        else
        {
            pets.Add(pet.RowId, pet);
        }

        transaction.Finish();
    }

    private void RemoveFromCache(Pet pet)
    {
        var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(RemoveFromCache));

        if (_petsByUserId.TryGetValue(pet.OwnerDiscordId, out var pets))
        {
            pets.Remove(pet.RowId);
        }

        transaction.Finish();
    }

    private void UpdateInCache(Pet newPet)
    {
        var transaction = _sentry.StartSpanOnCurrentTransaction(nameof(UpdateInCache));

        if (_petsByUserId.TryGetValue(newPet.OwnerDiscordId, out var pets))
        {
            pets[newPet.RowId] = newPet;
        }

        transaction.Finish();
    }
}