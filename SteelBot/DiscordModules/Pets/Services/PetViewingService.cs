using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Sentry;
using SteelBot.Channels.Pets;
using SteelBot.Database.Models.Pets;
using SteelBot.DataProviders;
using SteelBot.DiscordModules.Pets.Helpers;
using SteelBot.DiscordModules.Pets.Models;
using SteelBot.Helpers;
using SteelBot.Helpers.Sentry;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using User = SteelBot.Database.Models.Users.User;

namespace SteelBot.DiscordModules.Pets.Services;

public class PetViewingService
{
    private readonly IHub _sentry;
    private readonly DataCache _cache;

    public PetViewingService(IHub sentry, DataCache cache)
    {
        _sentry = sentry;
        _cache = cache;
    }

    public void View(PetCommandAction request)
    {
        if (request.Action != PetCommandActionType.View) throw new ArgumentException($"Unexpected action type sent to {nameof(View)}");

        var transaction = _sentry.GetCurrentTransaction();
        ViewPets(request, transaction);
    }

    private void ViewPets(PetCommandAction request, ITransaction transaction)
    {
        if (!_cache.Users.TryGetUser(request.Guild.Id, request.Member.Id, out var user)
            || !_cache.Pets.TryGetUsersPets(request.Member.Id, out var pets))
        {
            request.Responder.Respond(PetMessages.GetNoPetsAvailableMessage());
            return;
        }

        var getPetsSpan = transaction.StartChild("Get Available Pets");
        var availablePets = PetShared.GetAvailablePets(user, pets, out var disabledPets);
        var combinedPets = PetShared.Recombine(availablePets, disabledPets);
        getPetsSpan.Finish();
        
        var messageBuilderSpan = transaction.StartChild("Build Message");
        var baseEmbed = PetShared.GetOwnedPetsBaseEmbed(user, pets, disabledPets.Count > 0, request.Member.DisplayName)
            .WithThumbnail(request.Member.AvatarUrl);

        if (combinedPets.Count == 0)
        {
            baseEmbed.WithDescription("You currently own no pets.");
            request.Responder.Respond(new DiscordMessageBuilder().WithEmbed(baseEmbed));
            return;
        }

        var pages = BuildPages(baseEmbed, user, combinedPets);
        messageBuilderSpan.Finish();
        request.Responder.RespondPaginated(pages);
    }

    private List<Page> BuildPages(DiscordEmbedBuilder baseEmbed, User user, List<PetWithActivation> allPets)
    {
        int maxCapacity = PetShared.GetPetCapacity(user, allPets.ConvertAll(p=>p.Pet));
        int baseCapacity = PetShared.GetBasePetCapacity(user);
        var pages = PaginationHelper.GenerateEmbedPages(baseEmbed, allPets, 10, (builder, pet, _) => PetShared.AppendPetDisplayShort(builder, pet.Pet, pet.Active, baseCapacity, maxCapacity));

        return pages;
    }
}