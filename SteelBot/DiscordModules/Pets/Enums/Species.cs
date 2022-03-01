using Humanizer;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace SteelBot.DiscordModules.Pets.Enums
{
    public enum Species
    {
        Dog,
        Cat,
        Rabbit,
        Hamster,
        Raccoon,
        GuineaPig,
        Goldfish,
        Spider,
        Armadillo,
        Horse,
        BeardedDragon,
        Tortise,
        Snake,
        Turtle,
        Chicken,
        Budgie,
        Parrot,
        Hare,
        Owl,
        Hedgehog,
        FlyingSquirrel,
        Chinchilla,
        Ferret,
        Frog,
        Pig,
        Cow,
        Sheep,
        Goat,
        Bull,
        Monkey,
        Orangutan,
        Alligator,
        Crocodile,
        Skunk,
        Dragon,
        DikDik,
        Cockroach,
        Fly,
        Sloth,
        Snail,
        Peacock,
        Pheasant,
        Axolotl,
        Panda,
        Salamander,
        Beaver,
        RedPanda,

        [Description("T-Rex")]
        TRex,

        Phoenix,
        Pegasus,
        Unicorn,
        Wyvern,
        Penguin,
        Mosquito,
        Giraffe,
        Meerkat,
        Lemur,
        PrairieDog,
        SeaLion,
        Dolphin,
        Yak,
        Bear,
        ManedWolf,
        Platypus,
        Gorilla,
        Manatee,
        Capybara,
        Pudu,
        Seal,
        Quokka,
        Wombat,
        Otter,
        Octopus,
        BlackPhantomTetra,
        Doge,

        [Description("Yoda-ling")]
        Yodaling,
        Rock,
        Ogre,
        Kraken,
        LochNessMonster,
        Dodo
    }

    public static class SpeciesExtensions
    {
        public static string GetName(this Species species)
        {
            return species.Humanize().Transform(To.TitleCase);
        }

        public static Rarity GetRarity(this Species species)
        {
            return species switch
            {
                Species.Dog => Rarity.Common,
                Species.Cat => Rarity.Common,
                Species.Rabbit => Rarity.Common,
                Species.Hamster => Rarity.Common,
                Species.Raccoon => Rarity.Uncommon,
                Species.GuineaPig => Rarity.Common,
                Species.Goldfish => Rarity.Common,
                Species.Spider => Rarity.Uncommon,
                Species.Armadillo => Rarity.Rare,
                Species.Horse => Rarity.Uncommon,
                Species.BeardedDragon => Rarity.Rare,
                Species.Tortise => Rarity.Rare,
                Species.Snake => Rarity.Uncommon,
                Species.Turtle => Rarity.Rare,
                Species.Chicken => Rarity.Common,
                Species.Budgie => Rarity.Uncommon,
                Species.Parrot => Rarity.Uncommon,
                Species.Hare => Rarity.Rare,
                Species.Owl => Rarity.Rare,
                Species.Hedgehog => Rarity.Rare,
                Species.FlyingSquirrel => Rarity.Epic,
                Species.Chinchilla => Rarity.Rare,
                Species.Ferret => Rarity.Uncommon,
                Species.Frog => Rarity.Uncommon,
                Species.Pig => Rarity.Common,
                Species.Cow => Rarity.Common,
                Species.Sheep => Rarity.Common,
                Species.Goat => Rarity.Uncommon,
                Species.Bull => Rarity.Rare,
                Species.Monkey => Rarity.Rare,
                Species.Orangutan => Rarity.Epic,
                Species.Alligator => Rarity.Epic,
                Species.Crocodile => Rarity.Epic,
                Species.Skunk => Rarity.Rare,
                Species.Dragon => Rarity.Legendary,
                Species.DikDik => Rarity.Epic,
                Species.Cockroach => Rarity.Common,
                Species.Fly => Rarity.Common,
                Species.Sloth => Rarity.Rare,
                Species.Snail => Rarity.Rare,
                Species.Peacock => Rarity.Rare,
                Species.Pheasant => Rarity.Rare,
                Species.Axolotl => Rarity.Epic,
                Species.Panda => Rarity.Epic,
                Species.Salamander => Rarity.Rare,
                Species.Beaver => Rarity.Epic,
                Species.RedPanda => Rarity.Rare,
                Species.TRex => Rarity.Legendary,
                Species.Phoenix => Rarity.Legendary,
                Species.Pegasus => Rarity.Epic,
                Species.Unicorn => Rarity.Legendary,
                Species.Wyvern => Rarity.Epic,
                Species.Penguin => Rarity.Rare,
                Species.Mosquito => Rarity.Common,
                Species.Giraffe => Rarity.Rare,
                Species.Meerkat => Rarity.Rare,
                Species.Lemur => Rarity.Rare,
                Species.PrairieDog => Rarity.Rare,
                Species.SeaLion => Rarity.Rare,
                Species.Dolphin => Rarity.Epic,
                Species.Yak => Rarity.Uncommon,
                Species.Bear => Rarity.Uncommon,
                Species.ManedWolf => Rarity.Epic,
                Species.Platypus => Rarity.Legendary,
                Species.Gorilla => Rarity.Epic,
                Species.Manatee => Rarity.Epic,
                Species.Capybara => Rarity.Legendary,
                Species.Pudu => Rarity.Epic,
                Species.Seal => Rarity.Epic,
                Species.Quokka => Rarity.Epic,
                Species.Wombat => Rarity.Rare,
                Species.Otter => Rarity.Rare,
                Species.Octopus => Rarity.Rare,
                Species.BlackPhantomTetra => Rarity.Uncommon,
                Species.Doge => Rarity.Mythical,
                Species.Yodaling => Rarity.Mythical,
                Species.Rock => Rarity.Mythical,
                Species.Ogre => Rarity.Epic,
                Species.Kraken => Rarity.Legendary,
                Species.LochNessMonster => Rarity.Mythical,
                Species.Dodo => Rarity.Rare,
                _ => throw new ArgumentOutOfRangeException(nameof(species), $"Species does not have a defined rarity for {species}")
            };
        }

        private static readonly TimeSpan Year = TimeSpan.FromDays(365.25);
        private static readonly TimeSpan Month = TimeSpan.FromDays(30);
        private static readonly TimeSpan Week = TimeSpan.FromDays(7);
        public static TimeSpan GetMaxStartingAge(this Species species)
        {
            return species switch
            {
                Species.Dog => 8 * Year,
                Species.Cat => 8 * Year,
                Species.Rabbit => 2 * Year,
                Species.Hamster => 6 * Month,
                Species.Raccoon => Year,
                Species.GuineaPig => Year,
                Species.Goldfish => 2 * Month,
                Species.Spider => 6 * Month,
                Species.Armadillo => Year,
                Species.Horse => 10 * Year,
                Species.BeardedDragon => 2 * Year,
                Species.Tortise => 30 * Year,
                Species.Snake => 2 * Year,
                Species.Turtle => 10 * Year,
                Species.Chicken => 2 * Year,
                Species.Budgie => 2 * Year,
                Species.Parrot => 10 * Year,
                Species.Hare => Year,
                Species.Owl => 10 * Year,
                Species.Hedgehog => Year,
                Species.FlyingSquirrel => 2 * Year,
                Species.Chinchilla => 3 * Year,
                Species.Ferret => 3 * Year,
                Species.Frog => 2 * Year,
                Species.Pig => 5 * Year,
                Species.Cow => 5 * Year,
                Species.Sheep => 5 * Year,
                Species.Goat => 7 * Year,
                Species.Bull => 5 * Year,
                Species.Monkey => 8 * Year,
                Species.Orangutan => 15 * Year,
                Species.Alligator => 15 * Year,
                Species.Crocodile => 25 * Year,
                Species.Skunk => 3 * Year,
                Species.Dragon => 600 * Year,
                Species.DikDik => 2 * Year,
                Species.Cockroach => 10 * Week,
                Species.Fly => Week,
                Species.Sloth => 10 * Year,
                Species.Snail => 6 * Month,
                Species.Peacock => 5 * Year,
                Species.Pheasant => 6 * Month,
                Species.Axolotl => 5 * Year,
                Species.Panda => 10 * Year,
                Species.Salamander => 10 * Year,
                Species.Beaver => 2 * Year,
                Species.RedPanda => 10 * Year,
                Species.TRex => 15 * Year,
                Species.Phoenix => 1000 * Year,
                Species.Pegasus => 15 * Year,
                Species.Unicorn => 100 * Year,
                Species.Wyvern => 10 * Year,
                Species.Penguin => 5 * Year,
                Species.Mosquito => TimeSpan.FromDays(2),
                Species.Giraffe => 10 * Year,
                Species.Meerkat => 5 * Year,
                Species.Lemur => 8 * Year,
                Species.PrairieDog => Year,
                Species.SeaLion => 10 * Year,
                Species.Dolphin => 15 * Year,
                Species.Yak => 6 * Year,
                Species.Bear => 10 * Year,
                Species.ManedWolf => 3 * Year,
                Species.Platypus => 5 * Year,
                Species.Gorilla => 10 * Year,
                Species.Manatee => 30 * Year,
                Species.Capybara => 4 * Year,
                Species.Pudu => 4 * Year,
                Species.Seal => 15 * Year,
                Species.Quokka => 5 * Year,
                Species.Wombat => 10 * Year,
                Species.Otter => 2 * Year,
                Species.Octopus => 1 * Year,
                Species.BlackPhantomTetra => 2 * Year,
                Species.Doge => 1000 * Year,
                Species.Yodaling => 1500 * Year,
                Species.Rock => 2000 * Year,
                Species.Ogre => 50 * Year,
                Species.Kraken => 2000 * Year,
                Species.LochNessMonster => 150 * Year,
                Species.Dodo => 15 * Year,

                _ => throw new ArgumentOutOfRangeException(nameof(species), $"Species does not have a defined max starting age for {species}")
            };
        }

        public static List<BodyPart> GetBodyParts(this Species species)
        {
            var bodyParts = new List<BodyPart>() { BodyPart.Head, BodyPart.Eyes, BodyPart.Body };

            // Get additional parts.
            switch (species)
            {
                case Species.Dog:
                    bodyParts.Add(BodyPart.Tail);
                    bodyParts.Add(BodyPart.Ears);
                    break;
                case Species.Cat:
                    bodyParts.Add(BodyPart.Tail);
                    bodyParts.Add(BodyPart.Ears);
                    break;
                case Species.Rabbit:
                    bodyParts.Add(BodyPart.Tail);
                    bodyParts.Add(BodyPart.Ears);
                    break;
                case Species.Hamster:
                    bodyParts.Add(BodyPart.Tail);
                    break;
                case Species.Raccoon:
                    bodyParts.Add(BodyPart.Tail);
                    bodyParts.Add(BodyPart.Ears);
                    break;
                case Species.GuineaPig:
                    bodyParts.Add(BodyPart.Ears);
                    break;
                case Species.Goldfish:
                    bodyParts.Add(BodyPart.Fins);
                    break;
                case Species.Armadillo:
                    bodyParts.Add(BodyPart.Tail);
                    break;
                case Species.Horse:
                    bodyParts.Add(BodyPart.Tail);
                    bodyParts.Add(BodyPart.Mane);
                    break;
                case Species.BeardedDragon:
                    bodyParts.Add(BodyPart.Tail);
                    break;
                case Species.Turtle:
                    bodyParts.Add(BodyPart.Flippers);
                    break;
                case Species.Chicken:
                    bodyParts.Add(BodyPart.Wings);
                    break;
                case Species.Budgie:
                    bodyParts.Add(BodyPart.Wings);
                    break;
                case Species.Parrot:
                    bodyParts.Add(BodyPart.Wings);
                    break;
                case Species.Hare:
                    bodyParts.Add(BodyPart.Ears);
                    break;
                case Species.Owl:
                    bodyParts.Add(BodyPart.Wings);
                    break;
                case Species.FlyingSquirrel:
                    bodyParts.Add(BodyPart.Tail);
                    break;
                case Species.Chinchilla:
                    bodyParts.Add(BodyPart.Tail);
                    bodyParts.Add(BodyPart.Ears);
                    break;
                case Species.Ferret:
                    bodyParts.Add(BodyPart.Tail);
                    bodyParts.Add(BodyPart.Ears);
                    break;
                case Species.Pig:
                    bodyParts.Add(BodyPart.Tail);
                    bodyParts.Add(BodyPart.Ears);
                    break;
                case Species.Cow:
                    bodyParts.Add(BodyPart.Tail);
                    bodyParts.Add(BodyPart.Ears);
                    break;
                case Species.Sheep:
                    bodyParts.Add(BodyPart.Ears);
                    break;
                case Species.Goat:
                    bodyParts.Add(BodyPart.Tail);
                    bodyParts.Add(BodyPart.Ears);
                    bodyParts.Add(BodyPart.Horns);
                    break;
                case Species.Bull:
                    bodyParts.Add(BodyPart.Horns);
                    bodyParts.Add(BodyPart.Tail);
                    break;
                case Species.Monkey:
                    bodyParts.Add(BodyPart.Tail);
                    bodyParts.Add(BodyPart.Ears);
                    break;
                case Species.Alligator:
                    bodyParts.Add(BodyPart.Tail);
                    break;
                case Species.Crocodile:
                    bodyParts.Add(BodyPart.Tail);
                    break;
                case Species.Skunk:
                    bodyParts.Add(BodyPart.Tail);
                    break;
                case Species.Dragon:
                    bodyParts.Add(BodyPart.Wings);
                    bodyParts.Add(BodyPart.Tail);
                    bodyParts.Add(BodyPart.Spikes);
                    bodyParts.Add(BodyPart.Teeth);
                    break;
                case Species.DikDik:
                    bodyParts.Add(BodyPart.Horns);
                    bodyParts.Add(BodyPart.Ears);
                    break;
                case Species.Fly:
                    bodyParts.Add(BodyPart.Wings);
                    break;
                case Species.Sloth:
                    bodyParts.Add(BodyPart.Tail);
                    bodyParts.Add(BodyPart.Ears);
                    break;
                case Species.Snail:
                    bodyParts.Add(BodyPart.Shell);
                    break;
                case Species.Peacock:
                    bodyParts.Add(BodyPart.Wings);
                    break;
                case Species.Pheasant:
                    bodyParts.Add(BodyPart.Wings);
                    break;
                case Species.Salamander:
                    bodyParts.Add(BodyPart.Tail);
                    break;
                case Species.Beaver:
                    bodyParts.Add(BodyPart.Tail);
                    break;
                case Species.RedPanda:
                    bodyParts.Add(BodyPart.Tail);
                    bodyParts.Add(BodyPart.Ears);
                    break;
                case Species.TRex:
                    bodyParts.Add(BodyPart.Tail);
                    bodyParts.Add(BodyPart.Teeth);
                    break;
                case Species.Phoenix:
                    bodyParts.Add(BodyPart.Wings);
                    bodyParts.Add(BodyPart.Tail);
                    break;
                case Species.Pegasus:
                    bodyParts.Add(BodyPart.Tail);
                    bodyParts.Add(BodyPart.Wings);
                    break;
                case Species.Unicorn:
                    bodyParts.Add(BodyPart.Tail);
                    bodyParts.Add(BodyPart.Horn);
                    break;
                case Species.Wyvern:
                    bodyParts.Add(BodyPart.Tail);
                    bodyParts.Add(BodyPart.Wings);
                    break;
                case Species.Penguin:
                    bodyParts.Add(BodyPart.Flippers);
                    break;
                case Species.Mosquito:
                    bodyParts.Add(BodyPart.Wings);
                    break;
                case Species.Giraffe:
                    bodyParts.Add(BodyPart.Neck);
                    bodyParts.Add(BodyPart.Tail);
                    break;
                case Species.Meerkat:
                    bodyParts.Add(BodyPart.Tail);
                    break;
                case Species.PrairieDog:
                    bodyParts.Add(BodyPart.Tail);
                    break;
                case Species.SeaLion:
                    bodyParts.Add(BodyPart.Flippers);
                    break;
                case Species.Dolphin:
                    bodyParts.Add(BodyPart.Flippers);
                    break;
                case Species.Yak:
                    bodyParts.Add(BodyPart.Horns);
                    break;
                case Species.Bear:
                    bodyParts.Add(BodyPart.Ears);
                    break;
                case Species.ManedWolf:
                    bodyParts.Add(BodyPart.Ears);
                    break;
                case Species.Platypus:
                    bodyParts.Add(BodyPart.Bill);
                    bodyParts.Add(BodyPart.Feet);
                    bodyParts.Add(BodyPart.Tail);
                    break;
                case Species.Manatee:
                    bodyParts.Add(BodyPart.Flippers);
                    break;
                case Species.Seal:
                    bodyParts.Add(BodyPart.Flippers);
                    break;
                case Species.Quokka:
                    bodyParts.Add(BodyPart.Ears);
                    break;
                case Species.Wombat:
                    bodyParts.Add(BodyPart.Ears);
                    break;
                case Species.Otter:
                    bodyParts.Add(BodyPart.Tail);
                    break;
                case Species.Octopus:
                    bodyParts.Add(BodyPart.Legs);
                    break;
                case Species.BlackPhantomTetra:
                    bodyParts.Add(BodyPart.Fins);
                    break;
                case Species.Doge:
                    bodyParts.Add(BodyPart.Ears);
                    bodyParts.Add(BodyPart.Tail);
                    bodyParts.Add(BodyPart.Legs);
                    break;
                case Species.Yodaling:
                    bodyParts.Add(BodyPart.Ears);
                    break;
                case Species.Ogre:
                    bodyParts.Add(BodyPart.Ears);
                    break;
                case Species.Kraken:
                    bodyParts.Add(BodyPart.Tentacles);
                    break;
                case Species.LochNessMonster:
                    bodyParts.Add(BodyPart.Flippers);
                    break;
                case Species.Dodo:
                    bodyParts.Add(BodyPart.Wings);
                    break;

            }

            return bodyParts;
        }
    }
}
