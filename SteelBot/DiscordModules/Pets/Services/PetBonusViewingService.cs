using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Sentry;
using SteelBot.Channels.Pets;
using SteelBot.DataProviders;
using SteelBot.DiscordModules.Pets.Helpers;
using SteelBot.DiscordModules.Pets.Models;
using SteelBot.Helpers.Sentry;
using System;
using System.Collections.Generic;
using User = SteelBot.Database.Models.Users.User;

namespace SteelBot.DiscordModules.Pets.Services;

public class PetBonusViewingService
{
    private readonly IHub _sentry;
    private readonly DataCache _cache;

    public PetBonusViewingService(IHub sentry, DataCache cache)
    {
        _sentry = sentry;
        _cache = cache;
    }

    public void View(PetCommandAction request)
    {
        if (request.Action != PetCommandActionType.ViewBonuses) throw new ArgumentException($"Unexpected action type sent to {nameof(View)}");

        var transaction = _sentry.GetCurrentTransaction();
        ViewPetBonuses(request, transaction);
    }

    private void ViewPetBonuses(PetCommandAction request, ITransaction transaction)
    {
        var userAndPetsSpan = transaction.StartChild("Get User and Pets");
        if (!_cache.Users.TryGetUser(request.Guild.Id, request.Member.Id, out var user)
            || !_cache.Pets.TryGetUsersPets(request.Member.Id, out var pets))
        {
            request.Responder.Respond(PetMessages.GetNoPetsAvailableMessage());
            return;
        }
        userAndPetsSpan.Finish();

        var getPetsSpan = transaction.StartChild("Get Available Pets");
        var availablePets = PetShared.GetAvailablePets(user, pets, out var disabledPets);
        var combinedPets = PetShared.Recombine(availablePets, disabledPets);
        getPetsSpan.Finish();
        if (combinedPets.Count > 0)
        {
            var pages = BuildPages(user, request.Member, combinedPets);
            request.Responder.RespondPaginated(pages);
        }
    }

    private List<Page> BuildPages(User user, DiscordMember member, List<PetWithActivation> allPets)
    {
        int maxCapacity = PetShared.GetPetCapacity(user, allPets.ConvertAll(p=>p.Pet));
        int baseCapacity = PetShared.GetBasePetCapacity(user);
        var pages = PetDisplayHelpers.GetPetBonusesSummary(allPets, member.Username, member.AvatarUrl, baseCapacity, maxCapacity);

        return pages;
    }
}