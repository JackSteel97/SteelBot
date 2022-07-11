﻿using DSharpPlus.Entities;
using Sentry;
using SteelBot.DataProviders.SubProviders;

namespace SteelBot.DiscordModules.Pets.Helpers;

public static class PetSpaceHelper
{
    public static bool HasSpaceForAnotherPet(DiscordMember user, UsersProvider usersProvider, PetsProvider petsProvider, ITransaction transaction)
    {
        var spaceSpan = transaction.StartChild(nameof(HasSpaceForAnotherPet));

        (int capacity, int allPetsCount) = GetCapacityAndAllPetsCount(user, usersProvider, petsProvider);
        bool hasSpace = allPetsCount < capacity;

        spaceSpan.Finish();
        return hasSpace;
    }

    public static bool CanReplaceToBefriend(DiscordMember user, UsersProvider usersProvider, PetsProvider petsProvider, ITransaction transaction)
    {
        var canReplaceSpan = transaction.StartChild(nameof(CanReplaceToBefriend));

        (int capacity, int allPetsCount) = GetCapacityAndAllPetsCount(user, usersProvider, petsProvider);
        bool canReplace = allPetsCount < capacity + 1;

        canReplaceSpan.Finish();
        return canReplace;
    }

    private static (int capacity, int allPetsCount) GetCapacityAndAllPetsCount(DiscordMember user, UsersProvider usersProvider, PetsProvider petsProvider)
    {
        int capacity = 0;
        int allPetsCount = 0;
        if (!usersProvider.TryGetUser(user.Guild.Id, user.Id, out var dbUser)) return (capacity, allPetsCount);
        
        petsProvider.TryGetUsersPets(user.Id, out var allPets);
        capacity = PetShared.GetPetCapacity(dbUser, allPets);
        allPetsCount = allPets.Count;

        return (capacity, allPetsCount);
    }
}