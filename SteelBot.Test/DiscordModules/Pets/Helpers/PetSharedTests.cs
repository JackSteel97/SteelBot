using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SteelBot.Database.Models.Pets;
using SteelBot.Database.Models.Users;
using SteelBot.DiscordModules.Pets.Enums;
using SteelBot.DiscordModules.Pets.Generation;
using SteelBot.DiscordModules.Pets.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SteelBot.Test.DiscordModules.Pets.Helpers;

public class PetSharedTests
{
    [Fact]
    public void GetCapacity_NoPets()
    {
        var user = GetUser(1);
        var pets = new List<Pet>();
        
        var capacity = PetShared.GetPetCapacity(user, pets);
        capacity.Should().Be(3);
    }
    
    [Theory]
    [InlineData(3, 1, 0, 20)]
    [InlineData(4, 1, 0, 40)]
    [InlineData(16, 60, 10, 80)]
    public void GetLevelRequired(int index, int userLevel, int petBonusCapacity, int expectedLevel)
    {
        var user = GetUser(userLevel);

        var baseCapacity = PetShared.GetBasePetCapacity(user);
        var totalCapacity = baseCapacity + petBonusCapacity;
        var levelRequired = PetShared.GetRequiredLevelForPet(index, baseCapacity, totalCapacity);
        levelRequired.Should().Be(expectedLevel);
    }
    
    [Fact]
    public void GetAvailablePets_NoPets()
    {
        var user = GetUser(1);
        var pets = new List<Pet>();

        var activePets = PetShared.GetAvailablePets(user, pets, out var disabledPets);

        activePets.Should().HaveCount(0);
        disabledPets.Should().HaveCount(0);
    }

    [Fact]
    public void GetAvailablePets_OneBonusSlot()
    {
        var user = GetUser(1);
        var pets = GetPets_OnePet(1);

        var activePets = PetShared.GetAvailablePets(user, pets, out var disabledPets);

        activePets.Should().HaveCount(1);
        disabledPets.Should().HaveCount(0);
    }

    [Fact]
    public void GetAvailablePets_NegativeOneBonusSlot()
    {
        var user = GetUser(1);
        var pets = GetPets_OnePet(-1);

        var activePets = PetShared.GetAvailablePets(user, pets, out var disabledPets);

        activePets.Should().HaveCount(1);
        disabledPets.Should().HaveCount(0);
    }

    [Fact]
    public void GetAvailablePets_NegativeTwoBonusSlot()
    {
        var user = GetUser(1);
        var pets = GetPets_OnePet(-2);

        var activePets = PetShared.GetAvailablePets(user, pets, out var disabledPets);

        activePets.Should().HaveCount(1);
        disabledPets.Should().HaveCount(0);
    }

    [Fact]
    public void GetAvailablePets_LastPetsGiveNegativeSlots()
    {
        var user = GetUser(1);
        var pets = GetPets_LastNegatives();

        var activePets = PetShared.GetAvailablePets(user, pets, out var disabledPets);

        activePets.Should().HaveCount(1);
        disabledPets.Should().HaveCount(2);
    }

    [Fact]
    public void GetAvailablePets()
    {
        var user = GetUser(1);
        var pets = GetPets_NegativesMixedWithPositives();

        var activePets = PetShared.GetAvailablePets(user, pets, out var disabledPets);

        activePets.Should().HaveCount(3);
        disabledPets.Should().HaveCount(2);
    }

    [Fact]
    public void GetAvailablePets_AtCapacityWithLastNegatives()
    {
        var user = GetUser(20);
        var pets = GetPets_AtCapacityWithLastNegatives();

        var activePets = PetShared.GetAvailablePets(user, pets, out var disabledPets);

        activePets.Should().HaveCount(3);
        disabledPets.Should().HaveCount(1);
    }

    [Theory]
    [InlineData(1, 3)]
    [InlineData(10, 3)]
    [InlineData(20, 4)]
    [InlineData(30, 4)]
    [InlineData(40, 5)]
    [InlineData(50, 5)]
    [InlineData(60, 6)]
    [InlineData(80, 7)]
    [InlineData(100, 8)]
    public void GetPetCapacity_NoPets(int level, int expectedCapacity)
    {
        var user = GetUser(level);
        var pets = new List<Pet>();

        int actualCapacity = PetShared.GetPetCapacity(user, pets);

        actualCapacity.Should().Be(expectedCapacity);
    }

    [Theory]
    [InlineData(1, 1, 4)]
    [InlineData(10, 1, 4)]
    [InlineData(20, 1, 5)]
    [InlineData(30, 1, 5)]
    [InlineData(40, 1, 6)]
    [InlineData(50, 1, 6)]
    [InlineData(60, 1, 7)]
    [InlineData(80, 1, 8)]
    [InlineData(100, 1, 9)]
    [InlineData(1, 2, 5)]
    [InlineData(10, 2, 5)]
    [InlineData(20, 2, 6)]
    [InlineData(30, 2, 6)]
    [InlineData(40, 2, 7)]
    [InlineData(50, 2, 7)]
    [InlineData(60, 2, 8)]
    [InlineData(80, 2, 9)]
    [InlineData(100, 50, 58)]
    [InlineData(100, 100, 58)]
    public void GetPetCapacity_OnePet(int level, int bonusPetSlots, int expectedCapacity)
    {
        var user = GetUser(level);
        var pets = GetPets_OnePet(bonusPetSlots);

        int actualCapacity = PetShared.GetPetCapacity(user, pets);

        actualCapacity.Should().Be(expectedCapacity);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(10, 1)]
    [InlineData(20, 2)]
    [InlineData(30, 2)]
    [InlineData(40, 3)]
    [InlineData(50, 3)]
    [InlineData(60, 4)]
    [InlineData(80, 5)]
    [InlineData(100, 6)]
    public void GetPetCapacity_LastNegatives(int level, int expectedCapacity)
    {
        var user = GetUser(level);
        var pets = GetPets_LastNegatives();

        int actualCapacity = PetShared.GetPetCapacity(user, pets);

        actualCapacity.Should().Be(expectedCapacity);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(10, 2)]
    [InlineData(20, 3)]
    [InlineData(30, 3)]
    [InlineData(40, 4)]
    [InlineData(50, 4)]
    [InlineData(60, 5)]
    [InlineData(80, 6)]
    [InlineData(100, 7)]
    public void GetPetCapacityFromAllPets_AtCapacityWithLastNegatives(int level, int expectedCapacity)
    {
        var user = GetUser(level);
        var pets = GetPets_AtCapacityWithLastNegatives();

        int actualCapacity = PetShared.GetPetCapacity(user, pets);

        actualCapacity.Should().Be(expectedCapacity);
    }

    private static User GetUser(int level)
    {
        return new User { CurrentLevel = level };
    }

    private List<Pet> GetPets_OnePet(int slots)
    {
        var factory = GetPetFactory();
        var pet = factory.Generate();
        pet.Bonuses = new List<PetBonus> { GetPetBonus(slots) };

        return new List<Pet> { pet };
    }

    private List<Pet> GetPets_LastNegatives()
    {
        var factory = GetPetFactory();
        var pet1 = factory.Generate();
        pet1.Bonuses = new List<PetBonus> { GetPetBonus(1) };
        var pet2 = factory.Generate();
        pet2.Bonuses = new List<PetBonus> { GetPetBonus(2) };
        var pet3 = factory.Generate();
        pet3.Bonuses = new List<PetBonus> { GetPetBonus(-5) };

        return new List<Pet> { pet1, pet2, pet3 };
    }

    private List<Pet> GetPets_NegativesMixedWithPositives()
    {
        var factory = GetPetFactory();
        var pet1 = factory.Generate();
        pet1.Bonuses = new List<PetBonus> { GetPetBonus(1) };
        var pet2 = factory.Generate();
        pet2.Bonuses = new List<PetBonus> { GetPetBonus(2) };
        var pet3 = factory.Generate();
        pet3.Bonuses = new List<PetBonus> { GetPetBonus(-5) };
        var pet4 = factory.Generate();
        pet4.Bonuses = new List<PetBonus> { GetPetBonus(4) };
        var pet5 = factory.Generate();
        pet5.Bonuses = new List<PetBonus> { GetPetBonus(-2) };

        return new List<Pet> { pet1, pet2, pet3, pet4, pet5 };
    }

    private List<Pet> GetPets_AtCapacityWithLastNegatives()
    {
        var factory = GetPetFactory();
        var pet1 = factory.Generate();
        pet1.Bonuses = new List<PetBonus> { GetPetBonus(0) };
        var pet2 = factory.Generate();
        pet2.Bonuses = new List<PetBonus>();

        var pet3 = factory.Generate();
        pet3.Bonuses = new List<PetBonus> { GetPetBonus(0) };

        var pet4 = factory.Generate();
        pet4.Bonuses = new List<PetBonus> { GetPetBonus(-1) };

        return new List<Pet> { pet1, pet2, pet3, pet4 };
    }

    private static PetBonus GetPetBonus(double value, BonusType type = BonusType.PetSlots)
    {
        return new PetBonus { Value = value, BonusType = type };
    }

    private static PetFactory GetPetFactory()
    {
        var loggerMock = new Mock<ILogger<PetFactory>>();
        return new PetFactory(loggerMock.Object);
    }
}