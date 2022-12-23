using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using SteelBot.Channels.Pets;
using SteelBot.Database.Models.Users;
using SteelBot.DataProviders;
using SteelBot.DiscordModules.Pets.Helpers;
using SteelBot.DiscordModules.Pets.Models;
using SteelBot.Helpers;
using System;
using System.Collections.Generic;

namespace SteelBot.DiscordModules.Pets.Services;

public class PetViewingService
{
    private readonly DataCache _cache;

    public PetViewingService(DataCache cache)
    {
        _cache = cache;
    }

    public void View(PetCommandAction request)
    {
        if (request.Action != PetCommandActionType.View) throw new ArgumentException($"Unexpected action type sent to {nameof(View)}");

        ViewPets(request);
    }

    private void ViewPets(PetCommandAction request)
    {
        if (!_cache.Users.TryGetUser(request.Guild.Id, request.Target.Id, out var user)
            || !_cache.Pets.TryGetUsersPets(request.Target.Id, out var pets))
        {
            request.Responder.Respond(PetMessages.GetNoPetsAvailableMessage());
            return;
        }

        var availablePets = PetShared.GetAvailablePets(user, pets, out var disabledPets);
        var combinedPets = PetShared.Recombine(availablePets, disabledPets);

        var baseEmbed = PetShared.GetOwnedPetsBaseEmbed(user, pets, disabledPets.Count > 0, request.Target.DisplayName)
            .WithThumbnail(request.Target.AvatarUrl);

        if (combinedPets.Count == 0)
        {
            baseEmbed.WithDescription("You currently own no pets.");
            request.Responder.Respond(new DiscordMessageBuilder().WithEmbed(baseEmbed));
            return;
        }

        var pages = BuildPages(baseEmbed, user, combinedPets);
        request.Responder.RespondPaginated(pages);
    }

    private static List<Page> BuildPages(DiscordEmbedBuilder baseEmbed, User user, List<PetWithActivation> allPets)
    {
        int maxCapacity = PetShared.GetPetCapacity(user, allPets.ConvertAll(p => p.Pet));
        int baseCapacity = PetShared.GetBasePetCapacity(user);
        var pages = PaginationHelper.GenerateEmbedPages(baseEmbed, allPets,
            10,
            (builder, pet, _) => PetShared.AppendPetDisplayShort(builder, pet.Pet, pet.Active, baseCapacity, maxCapacity));

        return pages;
    }
}