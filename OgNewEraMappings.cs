using System;
using System.Collections.Generic;
using System.Linq;

namespace OGDirectImport
{
    internal static class OgNewEraMappings
    {
        public static readonly Dictionary<int, string> MonumentIdNames = new Dictionary<int, string>
        {
            { 0, "None" },
            { 1, "Small Bent Pyramid" },
            { 2, "Medium Bent Pyramid" },
            { 3, "Small Mudbrick Pyramid" },
            { 4, "Medium Mudbrick Pyramid" },
            { 5, "Large Mudbrick Pyramid" },
            { 6, "Mudbrick Pyramid Complex" },
            { 7, "Grand Mudbrick Pyramid Complex" },
            { 8, "Small Stepped Pyramid" },
            { 9, "Medium Stepped Pyramid" },
            { 10, "Large Stepped Pyramid" },
            { 11, "Stepped Pyramid Complex" },
            { 12, "Grand Stepped Pyramid Complex" },
            { 13, "Small Pyramid" },
            { 14, "Medium Pyramid" },
            { 15, "Large Pyramid" },
            { 16, "Pyramid Complex" },
            { 17, "Grand Pyramid Complex" },
            { 18, "Small Mastaba" },
            { 19, "Medium Mastaba" },
            { 20, "Large Mastaba" },
            { 21, "Sphinx" },
            { 22, "Small Obelisk" },
            { 23, "Large Obelisk" },
            { 24, "Sun Temple" },
            { 25, "Mausoleum A" },
            { 26, "Mausoleum B" },
            { 27, "Mausoleum C" },
            { 28, "Pharos Lighthouse" },
            { 29, "Alexandria's Library" },
            { 30, "Caesareum" },
            { 31, "Colossi" },
            { 32, "Temple of Luxor" },
            { 33, "Small Royal Burial Tomb" },
            { 34, "Medium Royal Burial Tomb" },
            { 35, "Large Royal Burial Tomb" },
            { 36, "Grand Royal Burial Tomb" },
            { 37, "Abu Simbel" },
        };

        public static readonly Dictionary<int, BuildingType[]> AllowedBuildingIndexToNewEraBuildings = new Dictionary<int, BuildingType[]>
        {
            { 0, new[] { BuildingType.ArchitectPost, BuildingType.Firehouse, BuildingType.PoliceStation } },
            { 2, new[] { BuildingType.MineGold } },
            { 3, new[] { BuildingType.WaterLift } },
            { 4, new[] { BuildingType.IrrigationDitch } },
            { 5, new[] { BuildingType.FishingWharf } },
            { 6, new[] { BuildingType.FoodWorkCamp, BuildingType.MonumentsWorkCamp } },
            { 7, new[] { BuildingType.Granary } },
            { 8, new[] { BuildingType.Bazaar } },
            { 9, new[] { BuildingType.StorageYard } },
            { 10, new[] { BuildingType.Dock } },
            { 11, new[] { BuildingType.Booth, BuildingType.JugglerSchool } },
            { 12, new[] { BuildingType.Bandstand, BuildingType.Conservatory } },
            { 13, new[] { BuildingType.Pavilion, BuildingType.DanceSchool } },
            { 14, new[] { BuildingType.SenetHouse } },
            { 15, new[] { BuildingType.FestivalSquare } },
            { 16, new[] { BuildingType.ScribalSchool } },
            { 17, new[] { BuildingType.Library } },
            { 18, new[] { BuildingType.Well, BuildingType.WaterSupply } },
            { 19, new[] { BuildingType.Dentist } },
            { 20, new[] { BuildingType.Apothecary } },
            { 21, new[] { BuildingType.Physician } },
            { 22, new[] { BuildingType.Mortuary } },
            { 23, new[] { BuildingType.TaxCollector } },
            { 24, new[] { BuildingType.Courthouse } },
            { 25, new[] { BuildingType.PalaceVillage, BuildingType.PalaceTown, BuildingType.PalaceCity } },
            { 26, new[] { BuildingType.MansionPersonal, BuildingType.MansionFamily, BuildingType.MansionDynasty } },
            { 27, new[] { BuildingType.Roadblock } },
            { 28, new[] { BuildingType.Bridge } },
            { 29, new[] { BuildingType.FerryLanding } },
            { 30, new[] { BuildingType.Gardens } },
            { 31, new[] { BuildingType.Plaza } },
            { 32, new[] { BuildingType.StatueSmallV1, BuildingType.StatueSmallV2, BuildingType.StatueSmallV3, BuildingType.StatueSmallV4, BuildingType.StatueMediumV1, BuildingType.StatueMediumV2, BuildingType.StatueMediumV3, BuildingType.StatueMediumV4, BuildingType.StatueLargeV1, BuildingType.StatueLargeV2, BuildingType.StatueLargeV3, BuildingType.StatueLargeV4 } },
            { 33, new[] { BuildingType.Wall } },
            { 34, new[] { BuildingType.Tower } },
            { 35, new[] { BuildingType.GateHouse } },
            { 36, new[] { BuildingType.Recruiter } },
            { 37, new[] { BuildingType.FortInfantry } },
            { 38, new[] { BuildingType.FortArchers } },
            { 39, new[] { BuildingType.FortCharioteers } },
            { 40, new[] { BuildingType.Academy } },
            { 41, new[] { BuildingType.Weaponsmith } },
            { 42, new[] { BuildingType.ChariotMaker } },
            { 43, new[] { BuildingType.WarshipWharf } },
            { 44, new[] { BuildingType.TransportWharf } },
            { 45, new[] { BuildingType.Zoo } },
            { 104, new[] { BuildingType.TempleComplexOsiris } },
            { 105, new[] { BuildingType.TempleComplexRa } },
            { 106, new[] { BuildingType.TempleComplexPtah } },
            { 107, new[] { BuildingType.TempleComplexSeth } },
            { 108, new[] { BuildingType.TempleComplexBast } },
        };

        public static readonly Dictionary<int, BuildingType[]> AllowedMonumentIdToNewEraBuildings = new Dictionary<int, BuildingType[]>
        {
            { 1, new[] { BuildingType.PyramidBentSmall } },
            { 2, new[] { BuildingType.PyramidBentMedium } },
            { 3, new[] { BuildingType.PyramidBrickCoreSmall } },
            { 4, new[] { BuildingType.PyramidBrickCoreMedium } },
            { 5, new[] { BuildingType.PyramidBrickCoreLarge } },
            { 6, new[] { BuildingType.PyramidBrickCoreComplex } },
            { 7, new[] { BuildingType.PyramidBrickCoreGrandComplex } },
            { 8, new[] { BuildingType.PyramidSteppedSmall } },
            { 9, new[] { BuildingType.PyramidSteppedMedium } },
            { 10, new[] { BuildingType.PyramidSteppedLarge } },
            { 11, new[] { BuildingType.PyramidSteppedComplex } },
            { 12, new[] { BuildingType.PyramidSteppedGrandComplex } },
            { 13, new[] { BuildingType.PyramidTrueSmall } },
            { 14, new[] { BuildingType.PyramidTrueMedium } },
            { 15, new[] { BuildingType.PyramidTrueLarge } },
            { 16, new[] { BuildingType.PyramidTrueComplex } },
            { 17, new[] { BuildingType.PyramidTrueGrandComplex } },
            { 18, new[] { BuildingType.MastabaSmall } },
            { 19, new[] { BuildingType.MastabaMedium } },
            { 20, new[] { BuildingType.MastabaLarge } },
            { 21, new[] { BuildingType.Sphinx } },
            { 22, new[] { BuildingType.ObeliskSmall } },
            { 23, new[] { BuildingType.ObeliskLarge } },
            { 24, new[] { BuildingType.SunTemple } },
            { 25, new[] { BuildingType.MausoleumA } },
            { 26, new[] { BuildingType.MausoleumB } },
            { 27, new[] { BuildingType.MausoleumC } },
            { 28, new[] { BuildingType.PharaohsLighthouse } },
            { 29, new[] { BuildingType.AlexandriasLibrary } },
            { 30, new[] { BuildingType.Caesareum } },
            { 33, new[] { BuildingType.RoyalTombSmall } },
            { 34, new[] { BuildingType.RoyalTombMedium } },
            { 35, new[] { BuildingType.RoyalTombLarge } },
            { 36, new[] { BuildingType.RoyalTombGrand } },
            { 37, new[] { BuildingType.AbuSimbel } },
        };

        public static Good? MapGoodFromResourceId(int resourceId)
        {
            switch (resourceId)
            {
                case 1: return Good.Grain;
                case 2: return Good.Meat;
                case 3: return Good.Lettuce;
                case 4: return Good.Chickpeas;
                case 5: return Good.Pomegranate;
                case 6: return Good.Figs;
                case 7: return Good.Fish;
                case 8: return Good.GameMeat;
                case 9: return Good.Straw;
                case 10: return Good.Weapons;
                case 11: return Good.Clay;
                case 12: return Good.Bricks;
                case 13: return Good.Pottery;
                case 14: return Good.Barley;
                case 15: return Good.Beer;
                case 16: return Good.Flax;
                case 17: return Good.Linen;
                case 18: return Good.Gems;
                case 19: return Good.Jewelry;
                case 20: return Good.Wood;
                case 21: return Good.Gold;
                case 22: return Good.Reeds;
                case 23: return Good.Papyrus;
                case 24: return Good.Plainstone;
                case 25: return Good.Limestone;
                case 26: return Good.Granite;
                case 28: return Good.Chariots;
                case 29: return Good.Copper;
                case 30: return Good.Sandstone;
                case 31: return Good.Oil;
                case 32: return Good.Henna;
                case 33: return Good.Paint;
                case 34: return Good.Lamp;
                case 35: return Good.WhiteMarble;
                default:
                    return null;
            }
        }

        public static bool TryMapGood(string raw, out Good good)
        {
            good = default;
            if (string.IsNullOrWhiteSpace(raw) || raw.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            switch (raw.Trim().ToLowerInvariant())
            {
                case "grain": good = Good.Grain; return true;
                case "meat": good = Good.Meat; return true;
                case "lettuce": good = Good.Lettuce; return true;
                case "chickpeas": good = Good.Chickpeas; return true;
                case "pomegranates": good = Good.Pomegranate; return true;
                case "figs": good = Good.Figs; return true;
                case "fish": good = Good.Fish; return true;
                case "gamemeat": good = Good.GameMeat; return true;
                case "straw": good = Good.Straw; return true;
                case "weapons": good = Good.Weapons; return true;
                case "clay": good = Good.Clay; return true;
                case "bricks": good = Good.Bricks; return true;
                case "pottery": good = Good.Pottery; return true;
                case "barley": good = Good.Barley; return true;
                case "beer": good = Good.Beer; return true;
                case "flax": good = Good.Flax; return true;
                case "linen": good = Good.Linen; return true;
                case "gems": good = Good.Gems; return true;
                case "luxury_goods":
                case "jewelry": good = Good.Jewelry; return true;
                case "timber":
                case "wood": good = Good.Wood; return true;
                case "plainstone":
                case "stone": good = Good.Plainstone; return true;
                case "limestone": good = Good.Limestone; return true;
                case "granite": good = Good.Granite; return true;
                case "reeds": good = Good.Reeds; return true;
                case "papyrus": good = Good.Papyrus; return true;
                case "gold": good = Good.Gold; return true;
                case "chariots": good = Good.Chariots; return true;
                case "copper": good = Good.Copper; return true;
                case "sandstone": good = Good.Sandstone; return true;
                case "oil": good = Good.Oil; return true;
                case "henna": good = Good.Henna; return true;
                case "paint": good = Good.Paint; return true;
                case "lamps":
                case "lamp": good = Good.Lamp; return true;
                case "marble": good = Good.WhiteMarble; return true;
                default:
                    return Enum.TryParse(raw, true, out good);
            }
        }

        public static Good? MapGoodToken(object token)
        {
            if (token == null)
            {
                return null;
            }

            if (token is int i)
            {
                return MapGoodFromResourceId(i);
            }

            if (token is long l)
            {
                return MapGoodFromResourceId((int)l);
            }

            if (token is string s && TryMapGood(s, out Good parsed))
            {
                return parsed;
            }

            return null;
        }

        public static string MapGodName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return raw;
            }

            switch (raw.Trim().ToLowerInvariant())
            {
                case "osiris": return nameof(DeityName.Osiris);
                case "ra": return nameof(DeityName.Ra);
                case "ptah": return nameof(DeityName.Ptah);
                case "seth": return nameof(DeityName.Seth);
                case "bast": return nameof(DeityName.Bast);
                default: return raw.Trim();
            }
        }

        public static IEnumerable<BuildingType> MapGodToBuildings(string rawGod)
        {
            switch (MapGodName(rawGod))
            {
                case nameof(DeityName.Bast):
                    yield return BuildingType.ShrineBast;
                    yield return BuildingType.TempleBast;
                    yield return BuildingType.TempleComplexBast;
                    yield break;
                case nameof(DeityName.Osiris):
                    yield return BuildingType.ShrineOsiris;
                    yield return BuildingType.TempleOsiris;
                    yield return BuildingType.TempleComplexOsiris;
                    yield break;
                case nameof(DeityName.Ptah):
                    yield return BuildingType.ShrinePtah;
                    yield return BuildingType.TemplePtah;
                    yield return BuildingType.TempleComplexPtah;
                    yield break;
                case nameof(DeityName.Ra):
                    yield return BuildingType.ShrineRa;
                    yield return BuildingType.TempleRa;
                    yield return BuildingType.TempleComplexRa;
                    yield break;
                case nameof(DeityName.Seth):
                    yield return BuildingType.ShrineSeth;
                    yield return BuildingType.TempleSeth;
                    yield return BuildingType.TempleComplexSeth;
                    yield break;
            }
        }

        public static BuildingType ResolvePalaceTier(int playerRank)
        {
            if (playerRank < 6)
            {
                return BuildingType.PalaceVillage;
            }

            if (playerRank < 8)
            {
                return BuildingType.PalaceTown;
            }

            return BuildingType.PalaceCity;
        }

        public static BuildingType ResolveMansionTier(int playerRank)
        {
            if (playerRank < 6)
            {
                return BuildingType.MansionPersonal;
            }

            if (playerRank < 8)
            {
                return BuildingType.MansionFamily;
            }

            return BuildingType.MansionDynasty;
        }

        public static string GetMonumentName(int monumentId)
        {
            return MonumentIdNames.TryGetValue(monumentId, out string name) ? name : $"Monument {monumentId}";
        }

        public static IEnumerable<BuildingType> MapAllowedBuildings(IEnumerable<int> ogIds)
        {
            foreach (int ogId in ogIds ?? Enumerable.Empty<int>())
            {
                if (!AllowedBuildingIndexToNewEraBuildings.TryGetValue(ogId, out BuildingType[] mapped) || mapped == null)
                {
                    continue;
                }

                foreach (BuildingType building in mapped)
                {
                    yield return building;
                }
            }
        }

        public static IEnumerable<BuildingType> MapAllowedMonuments(IEnumerable<int> ogIds)
        {
            foreach (int ogId in ogIds ?? Enumerable.Empty<int>())
            {
                if (!AllowedMonumentIdToNewEraBuildings.TryGetValue(ogId, out BuildingType[] mapped) || mapped == null)
                {
                    continue;
                }

                foreach (BuildingType building in mapped)
                {
                    yield return building;
                }
            }
        }
    }
}
