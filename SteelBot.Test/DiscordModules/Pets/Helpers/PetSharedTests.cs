﻿using FluentAssertions;
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

namespace SteelBot.Test.DiscordModules.Pets.Helpers
{
    public class PetSharedTests
    {
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
            var user = GetUser(40);
            var pets = GetPets_LastNegatives();

            var activePets = PetShared.GetAvailablePets(user, pets, out var disabledPets);

            activePets.Should().HaveCount(1);
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
        [InlineData(1, 1)]
        [InlineData(10, 1)]
        [InlineData(20, 2)]
        [InlineData(30, 2)]
        [InlineData(40, 3)]
        [InlineData(50, 3)]
        [InlineData(60, 4)]
        [InlineData(80, 5)]
        [InlineData(100, 6)]
        public void GetPetCapacity_NoPets(int level, int expectedCapacity)
        {
            var user = GetUser(level);
            var pets = new List<Pet>();

            var actualCapacity = PetShared.GetPetCapacity(user, pets);

            actualCapacity.Should().Be(expectedCapacity);
        }

        [Theory]
        [InlineData(1, 1, 2)]
        [InlineData(10, 1, 2)]
        [InlineData(20, 1, 3)]
        [InlineData(30, 1, 3)]
        [InlineData(40, 1, 4)]
        [InlineData(50, 1, 4)]
        [InlineData(60, 1, 5)]
        [InlineData(80, 1, 6)]
        [InlineData(100, 1, 7)]
        [InlineData(1, 2, 3)]
        [InlineData(10, 2, 3)]
        [InlineData(20, 2, 4)]
        [InlineData(30, 2, 4)]
        [InlineData(40, 2, 5)]
        [InlineData(50, 2, 5)]
        [InlineData(60, 2, 6)]
        [InlineData(80, 2, 7)]
        [InlineData(100, 50, 56)]
        [InlineData(100, 100, 56)]
        public void GetPetCapacity_OnePet(int level, int bonusPetSlots, int expectedCapacity)
        {
            var user = GetUser(level);
            var pets = GetPets_OnePet(bonusPetSlots);

            var actualCapacity = PetShared.GetPetCapacity(user, pets);

            actualCapacity.Should().Be(expectedCapacity);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(10, 1)]
        [InlineData(20, 1)]
        [InlineData(30, 1)]
        [InlineData(40, 1)]
        [InlineData(50, 1)]
        [InlineData(60, 2)]
        [InlineData(80, 3)]
        [InlineData(100, 4)]
        public void GetPetCapacity_LastNegatives(int level, int expectedCapacity)
        {
            var user = GetUser(level);
            var pets = GetPets_LastNegatives();

            var actualCapacity = PetShared.GetPetCapacity(user, pets);

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

            var actualCapacity = PetShared.GetPetCapacity(user, pets);

            actualCapacity.Should().Be(expectedCapacity);
        }

        private User GetUser(int level)
        {
            return new User
            {
                CurrentLevel = level
            };
        }

        private List<Pet> GetPets_OnePet(int slots)
        {
            var factory = GetPetFactory();
            var pet = factory.Generate();
            pet.Bonuses = new List<PetBonus>
            {
                GetPetBonus(slots)
            };

            return new List<Pet>
            {
                pet
            };
        }

        private List<Pet> GetPets_LastNegatives()
        {
            var factory = GetPetFactory();
            var pet1 = factory.Generate();
            pet1.Bonuses = new List<PetBonus>
            {
                GetPetBonus(1)
            };
            var pet2 = factory.Generate();
            pet2.Bonuses = new List<PetBonus>
            {
                GetPetBonus(2)
            };
            var pet3 = factory.Generate();
            pet3.Bonuses = new List<PetBonus>
            {
                GetPetBonus(-5)
            };

            return new List<Pet>
            {
                pet1, pet2, pet3
            };
        }

        private List<Pet> GetPets_AtCapacityWithLastNegatives()
        {
            var factory = GetPetFactory();
            var pet1 = factory.Generate();
            pet1.Bonuses = new List<PetBonus>
            {
                GetPetBonus(1)
            };
            var pet2 = factory.Generate();
            pet2.Bonuses = new List<PetBonus>();

            var pet3 = factory.Generate();
            pet3.Bonuses = new List<PetBonus>
            {
                GetPetBonus(1)
            };

            var pet4 = factory.Generate();
            pet4.Bonuses = new List<PetBonus>
            {
                GetPetBonus(-1)
            };

            return new List<Pet>
            {
                pet1, pet2, pet3, pet4
            };
        }

        private PetBonus GetPetBonus(double value, BonusType type = BonusType.PetSlots)
        {
            return new PetBonus
            {
                Value = value,
                BonusType = type
            };
        }

        private PetFactory GetPetFactory()
        {
            var loggerMock = new Mock<ILogger<PetFactory>>();
            return new PetFactory(loggerMock.Object);
        }
    }
}