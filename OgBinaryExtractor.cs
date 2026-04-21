using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace OGDirectImport
{
    internal static class OgBinaryExtractor
    {
        private const uint UncompressedMarker = 0x80000000u;
        private const int GridSize = 228;
        private const int MaxEvents = 150;
        private const int MaxGods = 5;
        private const int MaxPredatorHerdPoints = 4;
        private const int MaxFishPoints = 8;
        private const int MaxInvasionPointsLand = 8;
        private const int MaxInvasionPointsSea = 8;
        private const int MaxPreyHerdPoints = 4;
        private const int MaxDisembarkPoints = 3;
        private const int MaxSubtitle = 64;
        private const int MaxBriefDescription = 522;
        private const int ResourcesMax = 36;
        private const int ResourcesFoodsMax = 9;
        private static readonly char[] Cp1252ExtendedChars =
        {
            '\u20AC', '\u0081', '\u201A', '\u0192', '\u201E', '\u2026', '\u2020', '\u2021',
            '\u02C6', '\u2030', '\u0160', '\u2039', '\u0152', '\u008D', '\u017D', '\u008F',
            '\u0090', '\u2018', '\u2019', '\u201C', '\u201D', '\u2022', '\u2013', '\u2014',
            '\u02DC', '\u2122', '\u0161', '\u203A', '\u0153', '\u009D', '\u017E', '\u0178'
        };

        private static readonly int[] PkCopyOffsetBits =
        {
            2, 4, 4, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6,
            6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
        };

        private static readonly int[] PkCopyOffsetCode =
        {
            0x03, 0x0D, 0x05, 0x19, 0x09, 0x11, 0x01, 0x3E, 0x1E, 0x2E, 0x0E, 0x36, 0x16, 0x26, 0x06, 0x3A,
            0x1A, 0x2A, 0x0A, 0x32, 0x12, 0x22, 0x42, 0x02, 0x7C, 0x3C, 0x5C, 0x1C, 0x6C, 0x2C, 0x4C, 0x0C,
            0x74, 0x34, 0x54, 0x14, 0x64, 0x24, 0x44, 0x04, 0x78, 0x38, 0x58, 0x18, 0x68, 0x28, 0x48, 0x08,
            0xF0, 0x70, 0xB0, 0x30, 0xD0, 0x50, 0x90, 0x10, 0xE0, 0x60, 0xA0, 0x20, 0xC0, 0x40, 0x80, 0x00,
        };

        private static readonly int[] PkCopyLengthBaseBits = { 3, 2, 3, 3, 4, 4, 4, 5, 5, 5, 5, 6, 6, 6, 7, 7 };
        private static readonly int[] PkCopyLengthBaseValue = { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x0A, 0x0E, 0x16, 0x26, 0x46, 0x86, 0x106 };
        private static readonly int[] PkCopyLengthBaseCode = { 0x05, 0x03, 0x01, 0x06, 0x0A, 0x02, 0x0C, 0x14, 0x04, 0x18, 0x08, 0x30, 0x10, 0x20, 0x40, 0x00 };
        private static readonly int[] PkCopyLengthExtraBits = { 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8 };

        private static readonly Dictionary<string, uint> TerrainFlags = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase)
        {
            { "TREE", 0x00000001 }, { "ROCK", 0x00000002 }, { "WATER", 0x00000004 }, { "BUILDING", 0x00000008 },
            { "SHRUB", 0x00000010 }, { "GARDEN", 0x00000020 }, { "ROAD", 0x00000040 }, { "GROUNDWATER", 0x00000080 },
            { "CANAL", 0x00000100 }, { "ELEVATION", 0x00000200 }, { "ACCESS_RAMP", 0x00000400 }, { "MEADOW", 0x00000800 },
            { "RUBBLE", 0x00001000 }, { "WALL", 0x00004000 }, { "FLOODPLAIN", 0x00010000 }, { "FERRY_ROUTE", 0x00020000 },
            { "MARSHLAND", 0x00040000 }, { "OUT_OF_BOUNDS", 0x00080000 }, { "ORE", 0x00100000 }, { "IRRIGATION_RANGE", 0x01000000 },
            { "DUNE", 0x02000000 }, { "DEEPWATER", 0x04000000 }, { "SUBMERGED_ROAD", 0x08000000 }, { "GRASS", 0x10000000 },
            { "SHORE", 0x80000000 },
        };

        private static readonly Dictionary<int, string> ResourceNames = new Dictionary<int, string>
        {
            { 0, "none" }, { 1, "grain" }, { 2, "meat" }, { 3, "lettuce" }, { 4, "chickpeas" }, { 5, "pomegranates" },
            { 6, "figs" }, { 7, "fish" }, { 8, "gamemeat" }, { 9, "straw" }, { 10, "weapons" }, { 11, "clay" },
            { 12, "bricks" }, { 13, "pottery" }, { 14, "barley" }, { 15, "beer" }, { 16, "flax" }, { 17, "linen" },
            { 18, "gems" }, { 19, "luxury_goods" }, { 20, "timber" }, { 21, "gold" }, { 22, "reeds" }, { 23, "papyrus" },
            { 24, "stone" }, { 25, "limestone" }, { 26, "granite" }, { 27, "unused12" }, { 28, "chariots" }, { 29, "copper" },
            { 30, "sandstone" }, { 31, "oil" }, { 32, "henna" }, { 33, "paint" }, { 34, "lamps" }, { 35, "marble" },
            { 36, "deben" }, { 37, "troops" },
        };

        private static readonly Dictionary<int, string> EventTypeNames = new Dictionary<int, string>
        {
            { 0, "none" }, { 1, "request" }, { 2, "invasion" }, { 6, "sea_trade_problem" }, { 7, "land_trade_problem" },
            { 8, "wage_increase" }, { 9, "wage_decrease" }, { 10, "contaminated_water" }, { 11, "gold_mine_collapse" },
            { 12, "clay_pit_flood" }, { 13, "demand_increase" }, { 14, "demand_decrease" }, { 15, "price_increase" },
            { 16, "price_decrease" }, { 17, "reputation_increase" }, { 18, "reputation_decrease" }, { 19, "city_status_change" },
            { 20, "message" }, { 21, "failed_flood" }, { 22, "perfect_flood" }, { 23, "gift_from_pharaoh" }, { 24, "locusts" },
            { 25, "frogs" }, { 26, "hailstorm" }, { 27, "blood_river" }, { 28, "crime_wave" }, { 29, "trade_city_under_siege" },
            { 30, "foreign_army_attack_warning" }, { 31, "distant_battle" }, { 32, "distant_battle_won" },
        };

        private static readonly Dictionary<int, string> EventStateNames = new Dictionary<int, string>
        {
            { 0, "initial" }, { 1, "in_progress" }, { 2, "overdue" }, { 3, "finished" }, { 4, "finished_late" },
            { 5, "failed" }, { 6, "received" }, { 7, "already_fired" },
        };

        private static readonly Dictionary<int, string> EventTriggerNames = new Dictionary<int, string>
        {
            { 0, "once" }, { 1, "triggered" }, { 2, "recurring" }, { 4, "already_fired" }, { 8, "activated_8" },
            { 10, "by_rating" }, { 12, "activated_12" }, { 16, "triggered_by_favour" },
        };

        private static readonly string[] GodNames = { "osiris", "ra", "ptah", "seth", "bast" };

        private static readonly Dictionary<int, string> EmpireCityTypeNames = new Dictionary<int, string>
        {
            { 0, "ours" }, { 1, "pharaoh_trading" }, { 2, "pharaoh" }, { 3, "egyptian_trading" }, { 4, "egyptian" },
            { 5, "foreign_trading" }, { 6, "foreign" },
        };

        private static readonly Dictionary<int, string> EmpireObjectTypeNames = new Dictionary<int, string>
        {
            { 0, "ornament" }, { 1, "city" }, { 2, "text" }, { 3, "battle_icon" }, { 4, "land_trade_route" },
            { 5, "sea_trade_route" }, { 6, "kingdom_army" }, { 7, "enemy_army" }, { 8, "distant_battle_route" },
            { 9, "trader" }, { 10, "trade_route" },
        };

        private static readonly Dictionary<int, string> TradeStatusNames = new Dictionary<int, string>
        {
            { 0, "none" }, { 1, "import" }, { 2, "export" }, { 3, "import_as_needed" }, { 4, "export_surplus" },
        };

        private static readonly Dictionary<int, string> AllowedBuildingNames = new Dictionary<int, string>
        {
            { 0, "allowed_structures_group" }, { 1, "raw_materials_group" }, { 2, "gold_mine" }, { 3, "water_lift" },
            { 4, "irrigation_ditch" }, { 5, "fishing_wharf" }, { 6, "work_camp" }, { 7, "granary" }, { 8, "bazaar" },
            { 9, "storage_yard" }, { 10, "dock" }, { 11, "juggling" }, { 12, "music" }, { 13, "dancing" },
            { 14, "senet_games" }, { 15, "festival_square" }, { 16, "scribal_school" }, { 17, "library" },
            { 18, "water_supply" }, { 19, "dentist" }, { 20, "apothecary" }, { 21, "physician" }, { 22, "mortuary" },
            { 23, "tax_collector" }, { 24, "courthouse" }, { 25, "palace" }, { 26, "mansion" }, { 27, "roadblock" },
            { 28, "bridge" }, { 29, "ferry_landing" }, { 30, "gardens" }, { 31, "plaza" }, { 32, "statues" },
            { 33, "wall" }, { 34, "tower" }, { 35, "gatehouse" }, { 36, "recruiter" }, { 37, "fort_infantry" },
            { 38, "fort_archers" }, { 39, "fort_charioteers" }, { 40, "academy" }, { 41, "weaponsmith" }, { 42, "chariot_maker" },
            { 43, "warship_wharf" }, { 44, "transport_wharf" }, { 45, "zoo" }, { 104, "temple_complex_osiris" },
            { 105, "temple_complex_ra" }, { 106, "temple_complex_ptah" }, { 107, "temple_complex_seth" }, { 108, "temple_complex_bast" },
        };

        private static readonly Dictionary<int, string> MonumentNames = new Dictionary<int, string>
        {
            { 0, "none" },
            { 1, "small_bent_pyramid" },
            { 2, "medium_bent_pyramid" },
            { 3, "small_mudbrick_pyramid" },
            { 4, "medium_mudbrick_pyramid" },
            { 5, "large_mudbrick_pyramid" },
            { 6, "mudbrick_pyramid_complex" },
            { 7, "grand_mudbrick_pyramid_complex" },
            { 8, "small_stepped_pyramid" },
            { 9, "medium_stepped_pyramid" },
            { 10, "large_stepped_pyramid" },
            { 11, "stepped_pyramid_complex" },
            { 12, "grand_stepped_pyramid_complex" },
            { 13, "small_pyramid" },
            { 14, "medium_pyramid" },
            { 15, "large_pyramid" },
            { 16, "pyramid_complex" },
            { 17, "grand_pyramid_complex" },
            { 18, "small_mastaba" },
            { 19, "medium_mastaba" },
            { 20, "large_mastaba" },
            { 21, "sphinx" },
            { 22, "small_obelisk" },
            { 23, "large_obelisk" },
            { 24, "sun_temple" },
            { 25, "mausoleum_a" },
            { 26, "mausoleum_b" },
            { 27, "mausoleum_c" },
            { 28, "pharos_lighthouse" },
            { 29, "alexandrias_library" },
            { 30, "caesareum" },
            { 31, "colossi" },
            { 32, "temple_of_luxor" },
            { 33, "small_royal_burial_tomb" },
            { 34, "medium_royal_burial_tomb" },
            { 35, "large_royal_burial_tomb" },
            { 36, "grand_royal_burial_tomb" },
            { 37, "abu_simbel" },
        };

        public static bool TryExtract(string sourcePath, bool dumpJson, out JObject root, out int mapSide, out string gridKey, out string dumpedJsonPath, out string error)
        {
            root = null;
            mapSide = 0;
            gridKey = "grid";
            dumpedJsonPath = null;
            error = null;

            try
            {
                byte[] raw = File.ReadAllBytes(sourcePath);
                FormatInfo info = DetectFormat(raw);
                int consumed;
                List<ChunkData> chunks = ParseChunks(raw, info.Schema, out consumed);
                root = BuildMapJson(sourcePath, info, chunks);
                mapSide = ComputeRequiredCreateSize(root, "grid", (int?)root["map"]?["width"] ?? 0, (int?)root["map"]?["height"] ?? 0);
                if (mapSide <= 0)
                {
                    error = "Could not determine required map size from extracted data.";
                    return false;
                }

                if (dumpJson)
                {
                    string dumpDir = Path.Combine(Path.GetTempPath(), "OGDirectImport", "dumps");
                    Directory.CreateDirectory(dumpDir);
                    string outName = Path.GetFileNameWithoutExtension(sourcePath) + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".json";
                    dumpedJsonPath = Path.Combine(dumpDir, outName);
                    File.WriteAllText(dumpedJsonPath, root.ToString(Formatting.Indented), Encoding.UTF8);
                }

                OGDirectImportPlugin.Log?.LogInfo($"Internal extractor parsed '{sourcePath}'. format={info.Format}, version={info.Version}, consumed={consumed}, mapSide={mapSide}.");
                return true;
            }
            catch (Exception ex)
            {
                error = ex.ToString();
                return false;
            }
        }

        private static FormatInfo DetectFormat(byte[] raw)
        {
            if (raw == null || raw.Length < 8)
            {
                throw new InvalidDataException("Input file is too small.");
            }

            if (raw[0] == (byte)'M' && raw[1] == (byte)'A' && raw[2] == (byte)'P' && raw[3] == (byte)'S')
            {
                int version = ReadInt32LE(raw, 4);
                return new FormatInfo { Format = "map", Version = version, Schema = BuildMapSchema(version) };
            }

            return new FormatInfo
            {
                Format = "save",
                Version = ReadInt32LE(raw, 4),
                Schema = BuildSaveSchema(ReadInt32LE(raw, 4)),
                MissionIndexRaw = ReadUInt32LE(raw, 0),
                HasMissionIndexRaw = true
            };
        }

        private static List<ChunkSpec> BuildSaveSchema(int version)
        {
            int floodplainSize = version < 147 ? 32 : 36;
            int empireObjectsSize = version < 160 ? 15200 : 19600;
            int junk10aSize = version < 149 ? 11000 : 11200;
            List<ChunkSpec> schema = new List<ChunkSpec>
            {
                new ChunkSpec("scenario_mission_index", 4, false), new ChunkSpec("file_version", 4, false), new ChunkSpec("chunks_schema", 6004, false),
                new ChunkSpec("image_grid", 207936, true), new ChunkSpec("edge_grid", 51984, true), new ChunkSpec("building_grid", 103968, true),
                new ChunkSpec("terrain_grid", 207936, true), new ChunkSpec("aqueduct_grid", 51984, true), new ChunkSpec("figure_grid", 103968, true),
                new ChunkSpec("bitfields_grid", 51984, true), new ChunkSpec("sprite_grid", 51984, true), new ChunkSpec("random_grid", 51984, false),
                new ChunkSpec("desirability_grid", 51984, true), new ChunkSpec("elevation_grid", 51984, true), new ChunkSpec("building_damage_grid", 103968, true),
                new ChunkSpec("aqueduct_backup_grid", 51984, true), new ChunkSpec("sprite_backup_grid", 51984, true), new ChunkSpec("figures", 776000, true),
                new ChunkSpec("route_figures", 2000, true), new ChunkSpec("route_paths", 500000, true), new ChunkSpec("formations", 7200, true),
                new ChunkSpec("formations_info", 12, false), new ChunkSpec("city_data", 37808, true), new ChunkSpec("city_data_extra", 72, false),
                new ChunkSpec("buildings", 1056000, true), new ChunkSpec("city_view_orientation", 4, false), new ChunkSpec("game_time", 20, false),
                new ChunkSpec("building_extra_highest_id_ever", 8, false), new ChunkSpec("random_iv", 8, false), new ChunkSpec("city_view_camera", 8, false),
                new ChunkSpec("city_graph_order", 8, false), new ChunkSpec("empire_map_params", 12, false), new ChunkSpec("empire_cities", 6466, true),
                new ChunkSpec("building_count_industry", 288, false), new ChunkSpec("trade_prices", 288, false), new ChunkSpec("figure_names", 84, false),
                new ChunkSpec("scenario_info", 1592, false), new ChunkSpec("max_year", 4, false), new ChunkSpec("messages", 48000, true),
                new ChunkSpec("message_extra", 182, false), new ChunkSpec("building_burning_list_info", 8, false), new ChunkSpec("figure_sequence", 4, false),
                new ChunkSpec("scenario_carry_settings", 12, false), new ChunkSpec("invasion_warnings", 3232, true), new ChunkSpec("scenario_is_custom", 4, false),
                new ChunkSpec("city_sounds", 8960, false), new ChunkSpec("building_extra_highest_id", 4, false), new ChunkSpec("empire_traders", 8804, false),
                new ChunkSpec("building_list_burning", 1000, true), new ChunkSpec("building_list_small", 1000, true), new ChunkSpec("building_list_large", 8000, true),
                new ChunkSpec("junk7a", 32, false), new ChunkSpec("junk7b", 24, false), new ChunkSpec("building_storages", 39200, false),
                new ChunkSpec("trade_routes_limits", 2880, true), new ChunkSpec("trade_routes_traded", 2880, true), new ChunkSpec("junk8", 50, false),
                new ChunkSpec("scenario_map_name", 65, false), new ChunkSpec("bookmarks", 32, false), new ChunkSpec("junk9a", 12, false),
                new ChunkSpec("junk9b", 396, false), new ChunkSpec("soil_fertility_grid", 51984, false), new ChunkSpec("scenario_events", 18600, false),
                new ChunkSpec("scenario_events_extra", 28, false), new ChunkSpec("junk10a", junk10aSize, false), new ChunkSpec("junk10b", 2200, false),
                new ChunkSpec("junk10c", 16, false), new ChunkSpec("junk10d", 8200, false), new ChunkSpec("junk11", 1280, true),
                new ChunkSpec("empire_map_objects", empireObjectsSize, true), new ChunkSpec("empire_map_routes", 16200, true), new ChunkSpec("vegetation_growth", 51984, false),
                new ChunkSpec("junk14", 20, false), new ChunkSpec("bizarre_ordered_fields_1", 528, false), new ChunkSpec("floodplain_settings", floodplainSize, true),
                new ChunkSpec("GRID03_32BIT", 207936, true), new ChunkSpec("bizarre_ordered_fields_4", 312, false), new ChunkSpec("junk16", 64, false),
                new ChunkSpec("tutorial_flags_struct", 41, false), new ChunkSpec("GRID04_8BIT", 51984, true), new ChunkSpec("junk17", 1, false),
                new ChunkSpec("moisture_grid", 51984, true), new ChunkSpec("bizarre_ordered_fields_2", 240, false), new ChunkSpec("bizarre_ordered_fields_3", 432, false),
                new ChunkSpec("junk18", 8, false),
            };

            if (version >= 160)
            {
                schema.Add(new ChunkSpec("junk19", 20, false));
                schema.Add(new ChunkSpec("bizarre_ordered_fields_5", 648, false));
                schema.Add(new ChunkSpec("bizarre_ordered_fields_6", 648, false));
                schema.Add(new ChunkSpec("bizarre_ordered_fields_7", 360, false));
                schema.Add(new ChunkSpec("bizarre_ordered_fields_8", 1344, false));
                schema.Add(new ChunkSpec("bizarre_ordered_fields_9", 1776, false));
            }

            return schema;
        }

        private static List<ChunkSpec> BuildMapSchema(int version)
        {
            int floodplainSize = version < 147 ? 32 : 36;
            int empireObjectsSize = version < 160 ? 15200 : 19600;
            return new List<ChunkSpec>
            {
                new ChunkSpec("map_signature", 4, false), new ChunkSpec("file_version", 4, false), new ChunkSpec("chunks_schema", 6004, false),
                new ChunkSpec("image_grid", 207936, false), new ChunkSpec("edge_grid", 51984, false), new ChunkSpec("terrain_grid", 207936, false),
                new ChunkSpec("bitfields_grid", 51984, false), new ChunkSpec("random_grid", 51984, false), new ChunkSpec("elevation_grid", 51984, false),
                new ChunkSpec("random_iv", 8, false), new ChunkSpec("city_view_camera", 8, false), new ChunkSpec("scenario_info", 1592, false),
                new ChunkSpec("soil_fertility_grid", 51984, false), new ChunkSpec("scenario_events", 18600, false), new ChunkSpec("scenario_events_extra", 28, false),
                new ChunkSpec("junk11", 1280, true), new ChunkSpec("empire_map_objects", empireObjectsSize, true), new ChunkSpec("empire_map_routes", 16200, true),
                new ChunkSpec("vegetation_growth", 51984, false), new ChunkSpec("floodplain_settings", floodplainSize, true), new ChunkSpec("trade_prices", 288, false),
                new ChunkSpec("moisture_grid", 51984, true),
            };
        }

        private static List<ChunkData> ParseChunks(byte[] raw, List<ChunkSpec> schema, out int consumed)
        {
            int offset = 0;
            List<ChunkData> chunks = new List<ChunkData>();
            for (int i = 0; i < schema.Count; i++)
            {
                ChunkSpec spec = schema[i];
                int chunkOffset = offset;
                uint marker = 0;
                int packedSize = spec.Size;
                byte[] data;

                if (spec.Compressed)
                {
                    marker = ReadUInt32LE(raw, offset);
                    offset += 4;
                    if (marker == UncompressedMarker)
                    {
                        data = Slice(raw, offset, spec.Size);
                        packedSize = spec.Size;
                        offset += spec.Size;
                    }
                    else
                    {
                        data = DecompressPkware(Slice(raw, offset, checked((int)marker)), spec.Size);
                        packedSize = checked((int)marker);
                        offset += packedSize;
                    }
                }
                else
                {
                    data = Slice(raw, offset, spec.Size);
                    offset += spec.Size;
                }

                chunks.Add(new ChunkData
                {
                    Index = i,
                    Name = spec.Name,
                    Offset = chunkOffset,
                    Compressed = spec.Compressed,
                    CompressionMarker = marker,
                    StoredSize = spec.Size,
                    PackedSize = packedSize,
                    Data = data
                });
            }

            consumed = offset;
            return chunks;
        }

        private static JObject BuildMapJson(string sourcePath, FormatInfo info, List<ChunkData> chunks)
        {
            Dictionary<string, byte[]> chunkByName = chunks.ToDictionary(c => c.Name, c => c.Data, StringComparer.OrdinalIgnoreCase);
            byte[] scenarioInfoRaw = GetChunk(chunkByName, "scenario_info");
            byte[] terrainGrid = GetChunk(chunkByName, "terrain_grid");
            byte[] edgeGrid = GetChunk(chunkByName, "edge_grid");
            byte[] scenarioEventsRaw = GetChunk(chunkByName, "scenario_events");
            byte[] scenarioEventsExtraRaw = GetChunk(chunkByName, "scenario_events_extra");
            byte[] tradePricesRaw = GetChunk(chunkByName, "trade_prices");
            byte[] cityDataRaw = GetChunk(chunkByName, "city_data");

            if (scenarioInfoRaw == null || terrainGrid == null)
            {
                throw new InvalidDataException("Missing required chunks: scenario_info or terrain_grid.");
            }

            JObject scenarioInfo = ParseScenarioInfo(scenarioInfoRaw);
            JObject inferred = edgeGrid != null ? InferPlayableFromEdge(edgeGrid) : null;
            JObject mapFromScenario = scenarioInfo["map"] as JObject;
            int mapWidth = (int?)mapFromScenario?["width"] ?? 0;
            int mapHeight = (int?)mapFromScenario?["height"] ?? 0;
            if (mapWidth <= 0 && inferred != null) mapWidth = (int?)inferred["map_width"] ?? GridSize;
            if (mapHeight <= 0 && inferred != null) mapHeight = (int?)inferred["map_height"] ?? GridSize;
            if (mapWidth <= 0) mapWidth = GridSize;
            if (mapHeight <= 0) mapHeight = GridSize;

            TransformMeta transformMeta;
            List<CanvasTile> canvasTiles = BuildCanvasTiles(terrainGrid, mapWidth, mapHeight, out transformMeta);
            JArray staggered = BuildStaggered(canvasTiles);
            AttachStaggeredToPoints(scenarioInfo, transformMeta);

            JObject worldMap = BuildWorldMapData(info.Version, chunkByName);
            JObject cityData = cityDataRaw != null ? ParseCityData(cityDataRaw) : null;
            byte[] scenarioMissionIndexRaw = GetChunk(chunkByName, "scenario_mission_index");

            JObject result = new JObject();
            result["source_file"] = sourcePath;
            result["file_format"] = info.Format;
            result["file_version_u32"] = info.Version;
            if (info.HasMissionIndexRaw)
            {
                result["mission_index_raw"] = info.MissionIndexRaw;
            }
            if (scenarioMissionIndexRaw != null && scenarioMissionIndexRaw.Length >= 4)
            {
                result["scenario_mission_index"] = ReadInt32LE(scenarioMissionIndexRaw, 0);
            }

            result["map"] = new JObject
            {
                ["width"] = mapWidth,
                ["height"] = mapHeight,
                ["start_offset"] = (int?)mapFromScenario?["start_offset"] ?? 0,
                ["border_size"] = (int?)mapFromScenario?["border_size"] ?? 0
            };
            result["grid"] = staggered;
            result["scenario_info"] = scenarioInfo;
            result["scenario_events"] = scenarioEventsRaw != null ? ParseScenarioEvents(scenarioEventsRaw) : null;
            result["scenario_events_extra"] = scenarioEventsExtraRaw != null ? ParseScenarioEventsExtra(scenarioEventsExtraRaw) : null;
            result["trade_prices"] = tradePricesRaw != null ? ParseTradePrices(tradePricesRaw) : null;
            result["city_data"] = cityData;
            result["world_map"] = worldMap;
            result["allowed_cities"] = CollectAllowedCities(worldMap);
            result["allowed_goods"] = CollectAllowedGoods(cityData, worldMap);
            result["map_inferred"] = inferred;
            return result;
        }

        private static JObject ParseScenarioInfo(byte[] data)
        {
            ByteReader r = new ByteReader(data);
            JObject result = new JObject();

            result["raw_size"] = data.Length;
            result["start_year"] = r.ReadInt16();
            r.Skip(2);
            result["empire_id"] = r.ReadInt16();
            result["start_message_shown"] = r.ReadByte();
            r.Skip(3);

            JArray gods = new JArray();
            for (int i = 0; i < MaxGods; i++)
            {
                byte rawStatus = r.ReadByte();
                JObject god = new JObject
                {
                    ["god"] = GodNames[i],
                    ["raw_status"] = rawStatus,
                    ["is_known"] = rawStatus != 0,
                    ["is_patron"] = rawStatus == 2,
                    ["rank"] = rawStatus == 2 ? "patron" : (rawStatus == 1 ? "local" : "unknown")
                };
                AddNewEraDeityField(god, GodNames[i]);
                gods.Add(god);
                r.Skip(1);
            }
            result["known_gods"] = gods;

            r.Skip(10);
            r.Skip(2);
            result["initial_funds"] = r.ReadInt32();
            result["enemy_id"] = r.ReadInt16();
            r.Skip(6);

            JObject map = new JObject();
            map["width"] = r.ReadInt32();
            map["height"] = r.ReadInt32();
            map["start_offset"] = r.ReadInt32();
            map["border_size"] = r.ReadInt32();
            result["map"] = map;
            int mapStartOffset = (int)map["start_offset"];

            result["subtitle"] = DecodeCString(r.ReadBytes(MaxSubtitle));
            result["brief_description"] = DecodeCString(r.ReadBytes(MaxBriefDescription));
            result["image_id"] = r.ReadInt16();
            result["is_open_play"] = r.ReadInt16();
            result["player_rank"] = r.ReadInt16();

            ushort[] predatorX = ReadUInt16Array(r, MaxPredatorHerdPoints);
            ushort[] predatorY = ReadUInt16Array(r, MaxPredatorHerdPoints);
            ushort[] fishX = ReadUInt16Array(r, MaxFishPoints);
            ushort[] fishY = ReadUInt16Array(r, MaxFishPoints);
            result["predator_herd_points"] = BuildPointArray(predatorX, predatorY);
            result["fishing_points"] = BuildPointArray(fishX, fishY);

            result["alt_predator_type"] = r.ReadUInt16();
            result["herd_type_animals"] = new JArray(ReadUInt16Array(r, MaxPredatorHerdPoints).Select(v => (int)v));
            r.Skip(34);

            ushort[] landX = ReadUInt16Array(r, MaxInvasionPointsLand);
            ushort[] seaX = ReadUInt16Array(r, MaxInvasionPointsSea);
            ushort[] landY = ReadUInt16Array(r, MaxInvasionPointsLand);
            ushort[] seaY = ReadUInt16Array(r, MaxInvasionPointsSea);
            result["invasion_points_land"] = BuildPointArray(landX, landY);
            result["invasion_points_sea"] = BuildPointArray(seaX, seaY);
            r.Skip(36);

            JObject winCriteria = new JObject();
            winCriteria["culture"] = new JObject { ["goal"] = r.ReadInt32() };
            winCriteria["prosperity"] = new JObject { ["goal"] = r.ReadInt32() };
            winCriteria["monuments"] = new JObject { ["goal"] = r.ReadInt32() };
            winCriteria["kingdom"] = new JObject { ["goal"] = r.ReadInt32() };
            winCriteria["housing_count"] = new JObject { ["goal"] = r.ReadInt32() };
            winCriteria["housing_level"] = new JObject { ["goal"] = r.ReadInt32() };
            ((JObject)winCriteria["culture"])["enabled"] = r.ReadByte() != 0;
            ((JObject)winCriteria["prosperity"])["enabled"] = r.ReadByte() != 0;
            ((JObject)winCriteria["monuments"])["enabled"] = r.ReadByte() != 0;
            ((JObject)winCriteria["kingdom"])["enabled"] = r.ReadByte() != 0;
            ((JObject)winCriteria["housing_count"])["enabled"] = r.ReadByte() != 0;
            ((JObject)winCriteria["housing_level"])["enabled"] = r.ReadByte() != 0;
            r.Skip(6);
            winCriteria["time_limit"] = new JObject { ["enabled"] = r.ReadInt32() != 0, ["years"] = r.ReadInt32() };
            winCriteria["survival_time"] = new JObject { ["enabled"] = r.ReadInt32() != 0, ["years"] = r.ReadInt32() };
            winCriteria["population"] = new JObject { ["enabled"] = r.ReadInt32() != 0, ["goal"] = r.ReadInt32() };
            result["win_criteria"] = winCriteria;

            result["earthquake_point"] = DecodeGridOffset(r.ReadUInt32(), mapStartOffset);
            result["entry_point"] = DecodePackedXYU32(r.ReadUInt32());
            result["exit_point"] = DecodePackedXYU32(r.ReadUInt32());
            r.Skip(28);
            r.Skip(4);
            result["river_entry_point"] = DecodePackedXYU32(r.ReadUInt32());
            result["river_exit_point"] = DecodePackedXYU32(r.ReadUInt32());
            result["rescue_loan"] = r.ReadInt32();
            result["milestone25_year"] = r.ReadInt32();
            result["milestone50_year"] = r.ReadInt32();
            result["milestone75_year"] = r.ReadInt32();
            r.Skip(10);

            bool hasAnimals = r.ReadByte() != 0;
            bool flotsamEnabled = r.ReadByte() != 0;
            int climate = r.ReadByte();
            result["env"] = new JObject
            {
                ["has_animals"] = hasAnimals,
                ["flotsam_enabled"] = flotsamEnabled,
                ["climate"] = climate,
                ["climate_name"] = GetOgClimateName(climate),
                ["newera_climate"] = MapOgClimateToNewEraName(climate)
            };
            r.Skip(1);
            r.Skip(1);
            r.Skip(1);
            r.Skip(8);
            result["monuments_set"] = r.ReadByte();
            result["player_faction"] = r.ReadByte();
            r.Skip(1);
            r.Skip(1);

            result["prey_herd_points"] = BuildPointArray(ReadInt32Array(r, MaxPreyHerdPoints), ReadInt32Array(r, MaxPreyHerdPoints));
            int reservedOffset = r.Offset;
            short[] reservedValues = ReadInt16Array(r, 114);
            result["reserved_data_114"] = new JObject
            {
                ["relative_offset_in_scenario_info"] = reservedOffset,
                ["count"] = reservedValues.Length,
                ["values"] = new JArray(reservedValues.Select(v => (int)v)),
                ["note"] = "Akhenaten binds this block as reserved_data (114 x int16), not as allowed_buildings."
            };

            JArray enabledIds = new JArray();
            JArray enabledNames = new JArray();
            JArray allowedSlots = new JArray();
            for (int i = 0; i < reservedValues.Length; i++)
            {
                bool enabled = reservedValues[i] != 0;
                string name = AllowedBuildingNames.ContainsKey(i) ? AllowedBuildingNames[i] : "building_" + i.ToString(CultureInfo.InvariantCulture);
                JObject slot = new JObject { ["id"] = i, ["name"] = name, ["enabled"] = enabled, ["raw_value"] = reservedValues[i] };
                IEnumerable<BuildingType> mappedBuildings = OgNewEraMappings.MapAllowedBuildings(new[] { i });
                AddNewEraBuildingFields(slot, mappedBuildings);
                allowedSlots.Add(slot);
                if (enabled)
                {
                    enabledIds.Add(i);
                    enabledNames.Add(name);
                }
            }
            IEnumerable<int> enabledAllowedBuildingIds = enabledIds.OfType<JToken>().Select(token => token.Value<int>()).ToList();
            List<BuildingType> enabledAllowedBuildingTypes = new List<BuildingType>(OgNewEraMappings.MapAllowedBuildings(enabledAllowedBuildingIds.Where(id => id != 25 && id != 26)));
            if (enabledAllowedBuildingIds.Contains(25))
            {
                enabledAllowedBuildingTypes.Add(OgNewEraMappings.ResolvePalaceTier(result["player_rank"]?.Value<int>() ?? 0));
            }
            if (enabledAllowedBuildingIds.Contains(26))
            {
                enabledAllowedBuildingTypes.Add(OgNewEraMappings.ResolveMansionTier(result["player_rank"]?.Value<int>() ?? 0));
            }
            JArray enabledAllowedBuildings = BuildNewEraBuildingArray(enabledAllowedBuildingTypes);
            result["allowed_buildings"] = new JObject
            {
                ["relative_offset_in_scenario_info"] = reservedOffset,
                ["count"] = allowedSlots.Count,
                ["enabled_ids"] = enabledIds,
                ["enabled_names"] = enabledNames,
                ["enabled_newera_buildings"] = enabledAllowedBuildings,
                ["enabled_newera_building_type_ids"] = new JArray(enabledAllowedBuildings.OfType<JObject>().Select(b => b["building_type_id"])),
                ["enabled_newera_building_type_names"] = new JArray(enabledAllowedBuildings.OfType<JObject>().Select(b => b["building_type_name"])),
                ["slots"] = allowedSlots,
                ["note"] = "Candidate interpretation of scenario_info reserved_data_114 as OG allowed-buildings flags (114 x int16)."
            };

            result["disembark_points"] = BuildPointArray(ReadInt32Array(r, MaxDisembarkPoints), ReadInt32Array(r, MaxDisembarkPoints));
            result["debt_interest_rate"] = r.ReadUInt32();
            ushort monumentFirst = r.ReadUInt16();
            ushort monumentSecond = r.ReadUInt16();
            ushort monumentThird = r.ReadUInt16();
            JArray monumentEnabledIds = new JArray();
            JArray monumentEnabledNames = new JArray();
            JArray monumentSlots = new JArray();
            ushort[] monumentValues = { monumentFirst, monumentSecond, monumentThird };
            string[] monumentSlotNames = { "first", "second", "third" };
            for (int i = 0; i < monumentValues.Length; i++)
            {
                int monumentId = monumentValues[i];
                string monumentName = GetMonumentName(monumentId);
                bool enabled = monumentId != 0;
                JObject slot = new JObject
                {
                    ["slot"] = monumentSlotNames[i],
                    ["id"] = monumentId,
                    ["name"] = monumentName,
                    ["enabled"] = enabled
                };
                AddNewEraBuildingFields(slot, OgNewEraMappings.MapAllowedMonuments(new[] { monumentId }));
                monumentSlots.Add(slot);
                if (enabled)
                {
                    monumentEnabledIds.Add(monumentId);
                    monumentEnabledNames.Add(monumentName);
                }
            }
            JArray enabledMonumentBuildings = BuildNewEraBuildingArray(OgNewEraMappings.MapAllowedMonuments(monumentEnabledIds.OfType<JToken>().Select(token => token.Value<int>())));

            result["monuments"] = new JObject
            {
                ["first"] = monumentFirst,
                ["second"] = monumentSecond,
                ["third"] = monumentThird,
                ["enabled_ids"] = monumentEnabledIds,
                ["enabled_names"] = monumentEnabledNames,
                ["enabled_newera_buildings"] = enabledMonumentBuildings,
                ["enabled_newera_building_type_ids"] = new JArray(enabledMonumentBuildings.OfType<JObject>().Select(b => b["building_type_id"])),
                ["enabled_newera_building_type_names"] = new JArray(enabledMonumentBuildings.OfType<JObject>().Select(b => b["building_type_name"])),
                ["slots"] = monumentSlots
            };
            r.Skip(2);

            uint[] required = ReadUInt32Array(r, ResourcesMax);
            uint[] dispatched = ReadUInt32Array(r, ResourcesMax);
            JArray burialProvisions = new JArray();
            for (int i = 0; i < ResourcesMax; i++)
            {
                if (required[i] == 0 && dispatched[i] == 0) continue;
                JObject provision = new JObject
                {
                    ["resource_id"] = i,
                    ["resource_name"] = GetResourceName(i),
                    ["required"] = required[i],
                    ["dispatched"] = dispatched[i]
                };
                AddNewEraGoodFields(provision, i, GetResourceName(i));
                burialProvisions.Add(provision);
            }
            result["burial_provisions"] = burialProvisions;
            result["current_pharaoh"] = r.ReadUInt32();
            result["player_incarnation"] = r.ReadUInt32();
            result["bytes_remaining"] = data.Length - r.Offset;
            return result;
        }

        private static JObject ParseScenarioEvents(byte[] data)
        {
            const int headerSize = 4;
            const int recordSize = 124;
            uint declaredCount = data.Length >= headerSize ? ReadUInt32LE(data, 0) : 0u;
            int recordBytes = Math.Max(0, data.Length - headerSize);
            int availableCount = recordBytes / recordSize;
            int total = Math.Max(0, Math.Min(MaxEvents, Math.Min((int)declaredCount, availableCount)));
            JArray events = new JArray();
            for (int i = 0; i < total; i++)
            {
                events.Add(ParseEventRecord(Slice(data, headerSize + i * recordSize, recordSize), i));
            }
            return new JObject
            {
                ["header_size"] = headerSize,
                ["record_size"] = recordSize,
                ["declared_count"] = declaredCount,
                ["available_count"] = availableCount,
                ["events"] = events
            };
        }

        private static JObject ParseScenarioEventsExtra(byte[] data)
        {
            JArray u32Values = new JArray();
            JArray i32Values = new JArray();
            for (int offset = 0; offset + 4 <= data.Length; offset += 4)
            {
                u32Values.Add(ReadUInt32LE(data, offset));
                i32Values.Add(ReadInt32LE(data, offset));
            }
            return new JObject
            {
                ["raw_size"] = data.Length,
                ["hex"] = BitConverter.ToString(data).Replace("-", string.Empty).ToLowerInvariant(),
                ["u32_le"] = u32Values,
                ["i32_le"] = i32Values
            };
        }

        private static JObject ParseEventRecord(byte[] data, int slotIndex)
        {
            Func<int, byte> u8 = offset => data[offset];
            Func<int, ushort> u16 = offset => (ushort)ReadUInt16LE(data, offset);

            int DecodeU8(int value) { return value == 0xFF ? -1 : value; }
            int DecodeU16(int value) { return value == 0xFFFF ? -1 : value; }
            int? DecodeResource(int value) { return (value == 0 || value == 0xFF) ? (int?)null : value; }
            int ReadAmount(int offset) { int value = DecodeU16(u16(offset)); return value < 0 ? 0 : value; }

            int eventId = DecodeU8(u8(0));
            int eventType = u8(2);
            int triggerType = u8(40);
            int rawMonth = u8(3);
            int month = (triggerType == 1 || triggerType == 16) ? 0 : rawMonth + 1;
            int startYear = DecodeU8(u8(24));
            int endYear = DecodeU8(u8(26));
            int fallbackYear = DecodeU8(u8(22));

            int yearValue;
            int yearMin;
            int yearMax;
            if (triggerType == 16)
            {
                yearMin = 0;
                yearMax = 0;
                yearValue = 0;
            }
            else if (startYear < 0 && endYear < 0)
            {
                yearValue = Math.Max(0, fallbackYear);
                yearMin = yearValue;
                yearMax = yearValue;
            }
            else
            {
                yearMin = Math.Max(0, startYear);
                yearMax = Math.Max(yearMin, endYear >= 0 ? endYear : yearMin);
                yearValue = yearMin;
            }

            JArray resourceSlots = new JArray();
            JArray resourceIds = new JArray();
            JArray resourceNames = new JArray();
            int[] resourceOffsets = { 6, 8, 10 };
            for (int i = 0; i < resourceOffsets.Length; i++)
            {
                int rawResource = u8(resourceOffsets[i]);
                int? resourceId = DecodeResource(rawResource);
                JObject resourceSlot = new JObject
                {
                    ["slot_index"] = i,
                    ["slot_label"] = "resource_" + (i + 1).ToString(CultureInfo.InvariantCulture),
                    ["raw_value"] = rawResource,
                    ["resource_id"] = resourceId ?? -1,
                    ["resource_name"] = resourceId.HasValue ? GetResourceName(resourceId.Value) : null
                };
                AddNewEraGoodFields(resourceSlot, resourceId, resourceId.HasValue ? GetResourceName(resourceId.Value) : null);
                resourceSlots.Add(resourceSlot);
                if (resourceId.HasValue)
                {
                    resourceIds.Add(resourceId.Value);
                    resourceNames.Add(GetResourceName(resourceId.Value));
                }
            }

            int fixedAmount = ReadAmount(14);
            int amountMin = ReadAmount(16);
            int amountMax = ReadAmount(18);
            if (amountMin == 0 && amountMax == 0 && fixedAmount > 0)
            {
                amountMin = fixedAmount;
                amountMax = fixedAmount;
            }

            int cityDefault = DecodeU8(u8(30));
            int cityMinRaw = DecodeU8(u8(32));
            int cityMaxRaw = DecodeU8(u8(34));
            if (cityMinRaw < 0 && cityMaxRaw < 0)
            {
                cityMinRaw = cityDefault;
                cityMaxRaw = cityDefault;
            }

            int cityId = cityDefault > 0 ? cityDefault - 1 : (cityMinRaw > 0 ? cityMinRaw - 1 : -1);
            int festivalDeity = DecodeU8(u8(54));
            int senderFaction = DecodeU8(u8(86));
            int subtype = DecodeU8(u8(96));
            int delayMonths = (eventType == 1 || eventType == 2) ? u8(44) : 0;
            int primaryResourceId = resourceIds.Count > 0 ? resourceIds[0].Value<int>() : -1;
            string primaryResourceName = primaryResourceId > 0 ? GetResourceName(primaryResourceId) : null;

            JObject result = new JObject
            {
                ["slot_index"] = slotIndex,
                ["event_id"] = eventId >= 0 ? eventId : slotIndex,
                ["id"] = eventId >= 0 ? eventId : slotIndex,
                ["event_type"] = eventType,
                ["type"] = eventType,
                ["type_name"] = GetOrFallback(EventTypeNames, eventType, "unknown_" + eventType.ToString(CultureInfo.InvariantCulture)),
                ["subtype"] = subtype,
                ["sender_faction"] = senderFaction,
                ["time"] = new JObject { ["year"] = yearValue, ["month"] = month, ["month_raw"] = rawMonth },
                ["trigger_year_min"] = yearMin,
                ["trigger_year_max"] = yearMax,
                ["year_min"] = yearMin,
                ["year_max"] = yearMax,
                ["item"] = new JObject { ["value"] = primaryResourceId, ["f_fixed"] = fixedAmount, ["f_min"] = amountMin, ["f_max"] = amountMax },
                ["item_resource_name"] = primaryResourceName,
                ["amount"] = new JObject { ["value"] = amountMin > 0 ? amountMin : fixedAmount, ["f_fixed"] = fixedAmount, ["f_min"] = amountMin, ["f_max"] = amountMax },
                ["resource_slots"] = resourceSlots,
                ["resource_ids"] = resourceIds,
                ["resource_names"] = resourceNames,
                ["location_fields"] = new JArray { cityMinRaw > 0 ? cityMinRaw : 0, cityMaxRaw > 0 ? cityMaxRaw : 0, cityDefault > 0 ? cityDefault : 0, 0 },
                ["city_id"] = cityId,
                ["city_id_raw"] = cityDefault,
                ["city_range_min_raw"] = cityMinRaw,
                ["city_range_max_raw"] = cityMaxRaw,
                ["on_completed_action"] = DecodeU8(u8(36)),
                ["on_refusal_action"] = DecodeU8(u8(38)),
                ["on_too_late_action"] = DecodeU8(u8(82)),
                ["on_defeat_action"] = DecodeU8(u8(84)),
                ["event_trigger_type"] = triggerType,
                ["event_trigger_name"] = GetOrFallback(EventTriggerNames, triggerType, "unknown_" + triggerType.ToString(CultureInfo.InvariantCulture)),
                ["delay_months"] = delayMonths,
                ["months_initial"] = delayMonths,
                ["quest_months_left"] = delayMonths,
                ["event_state"] = 0,
                ["event_state_name"] = GetOrFallback(EventStateNames, 0, "unknown_0"),
                ["is_overdue"] = false,
                ["is_active"] = false,
                ["can_comply_dialog_shown"] = false,
                ["festival_deity"] = festivalDeity,
                ["festival_deity_name"] = festivalDeity >= 0 && festivalDeity < GodNames.Length ? GodNames[festivalDeity] : null,
                ["param1"] = primaryResourceId,
                ["param1_resource_name"] = primaryResourceName,
                ["route_fields"] = new JArray(),
                ["image"] = new JObject { ["pack"] = 0, ["id"] = 0, ["offset"] = 0 },
                ["reasons"] = new JArray()
            };
            AddNewEraGoodFields(result, primaryResourceId > 0 ? (int?)primaryResourceId : null, primaryResourceName);
            AddNewEraDeityField(result, result["festival_deity_name"]?.Value<string>());
            result["newera_resource_goods"] = BuildNewEraGoodArray(resourceIds.OfType<JToken>().Select(token => token.Value<int>()));
            AddNewEraGoodFields(result["item"] as JObject, primaryResourceId > 0 ? (int?)primaryResourceId : null, primaryResourceName);
            return result;
        }

        private static JObject InferPlayableFromEdge(byte[] edgeBytes)
        {
            if (edgeBytes.Length != GridSize * GridSize) return null;
            int[] freq = new int[256];
            foreach (byte value in edgeBytes) freq[value]++;
            int outsideValue = 0;
            for (int i = 1; i < freq.Length; i++) if (freq[i] > freq[outsideValue]) outsideValue = i;

            int minX = GridSize;
            int minY = GridSize;
            int maxX = -1;
            int maxY = -1;
            int maxDistH = 0;
            int maxDistV = 0;
            for (int y = 0; y < GridSize; y++)
            {
                int rowOff = y * GridSize;
                for (int x = 0; x < GridSize; x++)
                {
                    if (edgeBytes[rowOff + x] == outsideValue) continue;
                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                    int distH = Math.Abs(x - y);
                    int distV = Math.Abs(y - (GridSize - x) + 1);
                    if (distH > maxDistH) maxDistH = distH;
                    if (distV > maxDistV) maxDistV = distV;
                }
            }

            if (maxX < 0 || maxY < 0) return null;
            JObject bounds = new JObject
            {
                ["min_x"] = minX, ["min_y"] = minY, ["max_x"] = maxX, ["max_y"] = maxY,
                ["width"] = maxX - minX + 1, ["height"] = maxY - minY + 1
            };
            return new JObject
            {
                ["grid_width"] = GridSize,
                ["grid_height"] = GridSize,
                ["outside_value_u8"] = outsideValue,
                ["bounds"] = bounds,
                ["start_offset"] = minY * GridSize + minX,
                ["map_width"] = maxDistH * 2,
                ["map_height"] = maxDistV * 2
            };
        }

        private static List<CanvasTile> BuildCanvasTiles(byte[] terrainGrid, int mapWidth, int mapHeight, out TransformMeta transformMeta)
        {
            int? minRow = null;
            int? minRawCol = null;
            for (int my = 0; my < GridSize; my++)
            {
                for (int mx = 0; mx < GridSize; mx++)
                {
                    if (!InsideMapArea(mx, my, mapWidth, mapHeight, GridSize, 0)) continue;
                    int row = mx + my;
                    int col = mx - my;
                    minRow = !minRow.HasValue ? row : Math.Min(minRow.Value, row);
                    minRawCol = !minRawCol.HasValue ? col : Math.Min(minRawCol.Value, col);
                }
            }

            transformMeta = new TransformMeta { MinRow = minRow ?? 0, MinRawCol = minRawCol ?? 0 };
            List<CanvasTile> tiles = new List<CanvasTile>();
            if (!minRow.HasValue || !minRawCol.HasValue) return tiles;

            for (int my = 0; my < GridSize; my++)
            {
                int rowOff = my * GridSize;
                for (int mx = 0; mx < GridSize; mx++)
                {
                    if (!InsideMapArea(mx, my, mapWidth, mapHeight, GridSize, 0)) continue;
                    uint flags = ReadUInt32LE(terrainGrid, (rowOff + mx) * 4);
                    tiles.Add(new CanvasTile
                    {
                        Row = (mx + my) - minRow.Value,
                        Col = ((mx - my) - minRawCol.Value) / 2,
                        Terrain = TerrainKindFromFlags(flags),
                        Flags = flags
                    });
                }
            }
            return tiles;
        }

        private static bool InsideMapArea(int mx, int my, int mapWidth, int mapHeight, int gridLen, int edgeSize)
        {
            int distH = Math.Abs(mx - my);
            int distV = Math.Abs(my - (gridLen - mx) + 1);
            return distH < (mapWidth / 2.0 + 1 - edgeSize) && distV < (mapHeight / 2.0 + 1 - edgeSize);
        }

        private static JArray BuildStaggered(List<CanvasTile> canvasTiles)
        {
            JArray result = new JArray();
            if (canvasTiles == null || canvasTiles.Count == 0) return result;

            int minX = canvasTiles.Min(tile => tile.Col);
            int minY = canvasTiles.Min(tile => tile.Row);
            foreach (CanvasTile tile in canvasTiles.OrderBy(t => t.Row).ThenBy(t => t.Col))
            {
                JArray flagNames = new JArray(GetTerrainFlagNames(tile.Flags));
                result.Add(new JObject
                {
                    ["x"] = tile.Col - minX,
                    ["y"] = tile.Row - minY,
                    ["terrain"] = tile.Terrain ?? "none",
                    ["terrain_flags"] = (long)tile.Flags,
                    ["terrain_flag_names"] = flagNames
                });
            }
            return result;
        }

        private static IEnumerable<string> GetTerrainFlagNames(uint flags)
        {
            foreach (KeyValuePair<string, uint> kvp in TerrainFlags)
            {
                if ((flags & kvp.Value) != 0)
                {
                    yield return kvp.Key;
                }
            }
        }

        private static void AttachStaggeredToPoints(JObject scenarioInfo, TransformMeta transformMeta)
        {
            JObject map = scenarioInfo["map"] as JObject;
            GridPoint origin = StartOffsetToAbsoluteXY((int?)map?["start_offset"] ?? 0);
            string[] arrayFields = { "predator_herd_points", "fishing_points", "invasion_points_land", "invasion_points_sea", "prey_herd_points", "disembark_points" };
            foreach (string field in arrayFields)
            {
                JArray updated = new JArray();
                foreach (JObject point in (scenarioInfo[field] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
                {
                    JObject enriched = (JObject)point.DeepClone();
                    int x = point["x"]?.Value<int>() ?? 0;
                    int y = point["y"]?.Value<int>() ?? 0;
                    enriched["absolute_grid"] = new JObject { ["x"] = x + origin.X, ["y"] = y + origin.Y };
                    enriched["staggered"] = MapPointToStaggered(point, transformMeta, origin);
                    updated.Add(enriched);
                }
                scenarioInfo[field] = updated;
            }

            string[] singleFields = { "entry_point", "exit_point", "river_entry_point", "river_exit_point" };
            foreach (string field in singleFields)
            {
                JObject point = scenarioInfo[field] as JObject;
                if (point == null) continue;
                int x = point["x"]?.Value<int>() ?? 0;
                int y = point["y"]?.Value<int>() ?? 0;
                point["absolute_grid"] = new JObject { ["x"] = x + origin.X, ["y"] = y + origin.Y };
                point["staggered"] = MapPointToStaggered(point, transformMeta, origin);
            }
        }

        private static JObject MapPointToStaggered(JObject point, TransformMeta transformMeta, GridPoint origin)
        {
            int x = (point["x"]?.Value<int>() ?? 0) + origin.X;
            int y = (point["y"]?.Value<int>() ?? 0) + origin.Y;
            int row = (x + y) - transformMeta.MinRow;
            int rawColNorm = (x - y) - transformMeta.MinRawCol;
            return new JObject { ["x"] = rawColNorm / 2, ["y"] = row };
        }

        private static JObject ParseTradePrices(byte[] data)
        {
            ByteReader r = new ByteReader(data);
            JArray prices = new JArray();
            for (int resourceId = 0; resourceId < ResourcesMax; resourceId++)
            {
                JObject price = new JObject
                {
                    ["resource_id"] = resourceId,
                    ["resource_name"] = GetResourceName(resourceId),
                    ["buy_price"] = r.ReadInt32(),
                    ["sell_price"] = r.ReadInt32()
                };
                AddNewEraGoodFields(price, resourceId, GetResourceName(resourceId));
                prices.Add(price);
            }
            return new JObject { ["count"] = prices.Count, ["prices"] = prices };
        }

        private static string GetOgClimateName(int climate)
        {
            switch (climate)
            {
                case 0: return "central";
                case 1: return "northern";
                case 2: return "desert";
                default: return "unknown";
            }
        }

        private static string MapOgClimateToNewEraName(int climate)
        {
            switch (climate)
            {
                case 0: return "humid";
                case 1: return "normal";
                case 2: return "arid";
                default: return "unknown";
            }
        }

        private static JObject ParseCityData(byte[] data)
        {
            ByteReader r = new ByteReader(data);
            r.Skip(18904);
            r.Skip(8);
            r.Skip(4);
            r.Skip(5 * 4);
            r.Skip(2);
            r.Skip(2);
            r.Skip(6 * 4);
            r.Skip(2400 * 4);
            r.Skip(2 * 4);
            r.Skip(100 * 2);
            r.Skip(20 * 4);
            r.Skip(3 * 4);
            r.Skip(4);
            r.Skip(4);
            r.Skip(4);
            r.Skip(4);
            r.Skip(10 * 4);
            r.Skip(4);
            r.Skip(4);
            r.Skip(4);
            r.Skip(4);
            r.Skip(4);
            r.Skip(2);
            r.Skip(2);
            r.Skip(2);
            r.Skip(2);
            r.Skip(2);
            r.Skip(2);
            r.Skip(2);
            r.Skip(2);
            r.Skip(3 * 4);
            r.Skip(2);
            r.Skip(2);
            r.Skip(18 * 2);

            int entryPoint = r.ReadInt32();
            r.Skip(4);
            int exitPoint = r.ReadInt32();
            r.Skip(4);
            r.Skip(4);
            r.Skip(4);
            r.Skip(4);
            short unknown2828 = r.ReadInt16();
            r.Skip(2);

            ushort[] spaceInStorages = ReadUInt16Array(r, ResourcesMax);
            ushort[] storedInStorages = ReadUInt16Array(r, ResourcesMax);

            byte[] tradeStatus = new byte[ResourcesMax];
            for (int i = 0; i < ResourcesMax; i++)
            {
                tradeStatus[i] = r.ReadByte();
                r.Skip(1);
            }

            ushort[] tradingAmount = ReadUInt16Array(r, ResourcesMax);
            bool[] mothballed = new bool[ResourcesMax];
            for (int i = 0; i < ResourcesMax; i++)
            {
                mothballed[i] = r.ReadByte() != 0;
                r.Skip(1);
            }

            r.Skip(2);
            r.Skip(20);
            short[] resourceUnknown00 = ReadInt16Array(r, ResourcesMax);
            short[] granaryFoodStored = ReadInt16Array(r, ResourcesFoodsMax);
            r.Skip(28);
            ushort[] foodTypesAvailable = ReadUInt16Array(r, ResourcesFoodsMax);
            ushort[] foodTypesEaten = ReadUInt16Array(r, ResourcesFoodsMax);
            r.Skip(216);

            bool[] stockpiled = new bool[ResourcesMax];
            for (int i = 0; i < ResourcesMax; i++)
            {
                stockpiled[i] = r.ReadInt32() != 0;
            }

            JArray resources = new JArray();
            JArray activeIds = new JArray();
            JArray activeNames = new JArray();
            for (int i = 0; i < ResourcesMax; i++)
            {
                int resourceId = i + 1;
                string resourceName = GetResourceName(resourceId);
                JObject resource = new JObject
                {
                    ["resource_id"] = resourceId,
                    ["resource_name"] = resourceName,
                    ["space_in_storages"] = spaceInStorages[i],
                    ["stored_in_storages"] = storedInStorages[i],
                    ["trade_status"] = tradeStatus[i],
                    ["trade_status_name"] = GetOrFallback(TradeStatusNames, tradeStatus[i], "trade_status_" + tradeStatus[i].ToString(CultureInfo.InvariantCulture)),
                    ["trading_amount"] = tradingAmount[i],
                    ["mothballed"] = mothballed[i],
                    ["stockpiled"] = stockpiled[i],
                    ["unknown_00"] = resourceUnknown00[i]
                };
                AddNewEraGoodFields(resource, resourceId, resourceName);
                resources.Add(resource);

                if (tradeStatus[i] != 0 || tradingAmount[i] != 0 || mothballed[i] || stockpiled[i] || storedInStorages[i] != 0)
                {
                    activeIds.Add(resourceId);
                    activeNames.Add(resourceName);
                }
            }

            return new JObject
            {
                ["entry_point_grid_offset"] = entryPoint,
                ["exit_point_grid_offset"] = exitPoint,
                ["unknown_2828"] = unknown2828,
                ["resources"] = resources,
                ["active_resource_ids"] = activeIds,
                ["active_resource_names"] = activeNames,
                ["active_newera_goods"] = BuildNewEraGoodArray(activeIds.OfType<JToken>().Select(token => token.Value<int>())),
                ["food_types_available"] = DecodeFoodSlots(foodTypesAvailable),
                ["food_types_eaten"] = DecodeFoodSlots(foodTypesEaten),
                ["granary_food_stored"] = new JArray(granaryFoodStored.Select(v => (int)v))
            };
        }

        private static JObject ParseEmpireMapObjects(byte[] data, int version)
        {
            int recordSize = version < 160 ? 76 : 98;
            int count = data.Length / recordSize;
            JArray objects = new JArray();
            JArray cities = new JArray();

            for (int index = 0; index < count; index++)
            {
                ByteReader r = new ByteReader(Slice(data, index * recordSize, recordSize));
                int objType = r.ReadByte();
                int inUse = r.ReadByte();
                int animationIndex = r.ReadByte();
                r.Skip(1);
                short posX = r.ReadInt16();
                short posY = r.ReadInt16();
                short width = r.ReadInt16();
                short height = r.ReadInt16();
                short imageId = r.ReadInt16();
                short expandedImageId = r.ReadInt16();
                r.Skip(1);
                int distantBattleTravelMonths = r.ReadByte();
                r.Skip(1);
                int textAlign = r.ReadByte();
                short expandedPosX = r.ReadInt16();
                short expandedPosY = r.ReadInt16();
                int cityType = r.ReadByte();
                int cityNameId = r.ReadByte();
                int tradeRouteId = r.ReadByte();
                bool tradeRouteOpen = r.ReadByte() != 0;
                short tradeRouteCost = r.ReadInt16();
                byte[] sells = r.ReadBytes(14);
                r.Skip(8);
                byte[] buys = r.ReadBytes(8);

                JArray tradeDemand = null;
                int? invasionPathId = null;
                int? invasionYears = null;
                uint? trade40 = null;
                uint? trade25 = null;
                uint? trade15 = null;

                invasionPathId = r.ReadByte();
                invasionYears = r.ReadByte();

                if (version < 160)
                {
                    r.Skip(2);
                    trade40 = r.ReadUInt32();
                    trade25 = r.ReadUInt32();
                    trade15 = r.ReadUInt32();
                }
                else
                {
                    tradeDemand = new JArray(r.ReadBytes(ResourcesMax).Select(v => (int)v));
                }

                JObject obj = new JObject
                {
                    ["object_index"] = index,
                    ["object_type"] = objType,
                    ["object_type_name"] = GetOrFallback(EmpireObjectTypeNames, objType, "object_" + objType.ToString(CultureInfo.InvariantCulture)),
                    ["in_use"] = inUse != 0,
                    ["animation_index"] = animationIndex,
                    ["pos"] = new JObject { ["x"] = posX, ["y"] = posY },
                    ["size"] = new JObject { ["width"] = width, ["height"] = height },
                    ["image_id"] = imageId,
                    ["expanded_image_id"] = expandedImageId,
                    ["distant_battle_travel_months"] = distantBattleTravelMonths,
                    ["text_align"] = textAlign,
                    ["expanded_pos"] = new JObject { ["x"] = expandedPosX, ["y"] = expandedPosY },
                    ["city_type"] = cityType,
                    ["city_type_name"] = GetOrFallback(EmpireCityTypeNames, cityType, "city_type_" + cityType.ToString(CultureInfo.InvariantCulture)),
                    ["city_name_id"] = cityNameId,
                    ["trade_route_id"] = tradeRouteId,
                    ["trade_route_open"] = tradeRouteOpen,
                    ["trade_route_cost"] = tradeRouteCost,
                    ["sells_resource_ids"] = new JArray(sells.Where(v => v != 0).Select(v => (int)v)),
                    ["buys_resource_ids"] = new JArray(buys.Where(v => v != 0).Select(v => (int)v)),
                    ["trade_demand"] = tradeDemand,
                    ["trade40"] = trade40.HasValue ? (JToken)trade40.Value : JValue.CreateNull(),
                    ["trade25"] = trade25.HasValue ? (JToken)trade25.Value : JValue.CreateNull(),
                    ["trade15"] = trade15.HasValue ? (JToken)trade15.Value : JValue.CreateNull(),
                    ["trade40_resource_ids"] = trade40.HasValue ? (JToken)new JArray(ExpandTradeMaskResourceIds(trade40.Value)) : JValue.CreateNull(),
                    ["trade25_resource_ids"] = trade25.HasValue ? (JToken)new JArray(ExpandTradeMaskResourceIds(trade25.Value)) : JValue.CreateNull(),
                    ["trade15_resource_ids"] = trade15.HasValue ? (JToken)new JArray(ExpandTradeMaskResourceIds(trade15.Value)) : JValue.CreateNull(),
                    ["trade_levels"] = BuildTradeLevels(tradeDemand, trade40, trade25, trade15),
                    ["invasion_path_id"] = invasionPathId.HasValue ? (JToken)invasionPathId.Value : JValue.CreateNull(),
                    ["invasion_years"] = invasionYears.HasValue ? (JToken)invasionYears.Value : JValue.CreateNull()
                };
                obj["newera_sells_goods"] = BuildNewEraGoodArray(((JArray)obj["sells_resource_ids"]).OfType<JToken>().Select(token => token.Value<int>()));
                obj["newera_buys_goods"] = BuildNewEraGoodArray(((JArray)obj["buys_resource_ids"]).OfType<JToken>().Select(token => token.Value<int>()));
                objects.Add(obj);

                if (objType == 1 && inUse != 0)
                {
                    JObject city = (JObject)obj.DeepClone();
                    city["sells_resource_names"] = new JArray(((JArray)city["sells_resource_ids"]).Select(token => GetResourceName(token.Value<int>())));
                    city["buys_resource_names"] = new JArray(((JArray)city["buys_resource_ids"]).Select(token => GetResourceName(token.Value<int>())));
                    cities.Add(city);
                }
            }

            return new JObject { ["record_size"] = recordSize, ["count"] = count, ["cities"] = cities, ["objects"] = objects };
        }

        private static IEnumerable<int> ExpandTradeMaskResourceIds(uint mask)
        {
            for (int resourceId = 0; resourceId < 32; resourceId++)
            {
                if ((mask & (1u << resourceId)) != 0u)
                {
                    yield return resourceId;
                }
            }
        }

        private static JArray BuildTradeLevels(JArray tradeDemand, uint? trade40, uint? trade25, uint? trade15)
        {
            List<Tuple<int, int>> levels = new List<Tuple<int, int>>();

            if (tradeDemand != null && tradeDemand.Count > 0)
            {
                for (int resourceId = 0; resourceId < tradeDemand.Count; resourceId++)
                {
                    int demandCode = tradeDemand[resourceId]?.Value<int>() ?? 0;
                    if (demandCode > 0)
                    {
                        levels.Add(Tuple.Create(resourceId, demandCode));
                    }
                }
            }
            else
            {
                if (trade15.HasValue)
                {
                    levels.AddRange(ExpandTradeMaskResourceIds(trade15.Value).Select(resourceId => Tuple.Create(resourceId, 1)));
                }

                if (trade25.HasValue)
                {
                    levels.AddRange(ExpandTradeMaskResourceIds(trade25.Value).Select(resourceId => Tuple.Create(resourceId, 2)));
                }

                if (trade40.HasValue)
                {
                    levels.AddRange(ExpandTradeMaskResourceIds(trade40.Value).Select(resourceId => Tuple.Create(resourceId, 3)));
                }
            }

            return new JArray(levels
                .OrderBy(entry => entry.Item1)
                .Select(entry => new JObject
                {
                    ["resource_id"] = entry.Item1,
                    ["resource_name"] = GetResourceName(entry.Item1),
                    ["demand_code"] = entry.Item2,
                    ["demand_name"] = GetTradeDemandName(entry.Item2)
                }));
        }

        private static string GetTradeDemandName(int demandCode)
        {
            switch (demandCode)
            {
                case 1:
                case 15:
                case 1500:
                    return "low";
                case 2:
                case 25:
                case 2500:
                    return "medium";
                case 3:
                case 40:
                case 4000:
                    return "high";
                default:
                    return "unknown";
            }
        }

        private static JObject ParseEmpireMapRoutes(byte[] data)
        {
            const int recordSize = 324;
            int count = data.Length / recordSize;
            JArray routes = new JArray();
            for (int index = 0; index < count; index++)
            {
                ByteReader r = new ByteReader(Slice(data, index * recordSize, recordSize));
                JArray header = new JArray { r.ReadUInt32(), r.ReadUInt32() };
                JArray points = new JArray();
                for (int i = 0; i < 50; i++)
                {
                    int x = r.ReadUInt16();
                    int y = r.ReadUInt16();
                    bool inUse = r.ReadByte() != 0;
                    r.Skip(1);
                    if (inUse) points.Add(new JObject { ["x"] = x, ["y"] = y });
                }
                int routeType = r.ReadByteAfterUInt32Triplet(out uint length, out uint unk00, out uint unk01);
                int numPoints = r.ReadByte();
                bool inUseFlag = r.ReadByte() != 0;
                int unk03 = r.ReadByte();
                routes.Add(new JObject
                {
                    ["route_id"] = index,
                    ["unk_header"] = header,
                    ["length"] = length,
                    ["unk_00"] = unk00,
                    ["unk_01"] = unk01,
                    ["route_type"] = routeType,
                    ["route_type_name"] = routeType == 2 ? "sea" : (routeType == 1 ? "land" : "type_" + routeType.ToString(CultureInfo.InvariantCulture)),
                    ["num_points"] = numPoints,
                    ["in_use"] = inUseFlag,
                    ["unk_03"] = unk03,
                    ["points"] = points
                });
            }
            return new JObject { ["record_size"] = recordSize, ["count"] = count, ["routes"] = routes };
        }

        private static JObject ParseEmpireCities(byte[] data)
        {
            const int recordSize = 106;
            int count = data.Length / recordSize;
            JArray cities = new JArray();
            for (int index = 0; index < count; index++)
            {
                ByteReader r = new ByteReader(Slice(data, index * recordSize, recordSize));
                bool inUse = r.ReadByte() != 0;
                int maxTraders = r.ReadByte();
                int cityType = r.ReadByte();
                int cityNameId = r.ReadByte();
                int routeId = r.ReadByte();
                bool isOpen = r.ReadByte() != 0;
                bool[] buys = new bool[ResourcesMax];
                bool[] sells = new bool[ResourcesMax];
                for (int i = 0; i < ResourcesMax; i++) buys[i] = r.ReadByte() != 0;
                for (int i = 0; i < ResourcesMax; i++) sells[i] = r.ReadByte() != 0;
                short costToOpen = r.ReadInt16();
                short phUnk01 = r.ReadInt16();
                short traderEntryDelay = r.ReadInt16();
                short phUnk02 = r.ReadInt16();
                short empireObjectId = r.ReadInt16();
                bool isSeaTrade = r.ReadByte() != 0;
                int monthsUnderSiege = r.ReadByte();
                JArray traderFigureIds = new JArray { r.ReadInt16(), r.ReadInt16(), r.ReadInt16() };
                r.Skip(10);

                JArray buyIds = new JArray();
                JArray sellIds = new JArray();
                JArray buyNames = new JArray();
                JArray sellNames = new JArray();
                for (int i = 0; i < ResourcesMax; i++)
                {
                    if (buys[i]) { buyIds.Add(i); buyNames.Add(GetResourceName(i)); }
                    if (sells[i]) { sellIds.Add(i); sellNames.Add(GetResourceName(i)); }
                }

                cities.Add(new JObject
                {
                    ["lookup_id"] = index,
                    ["in_use"] = inUse,
                    ["max_traders"] = maxTraders,
                    ["city_type"] = cityType,
                    ["city_type_name"] = GetOrFallback(EmpireCityTypeNames, cityType, "city_type_" + cityType.ToString(CultureInfo.InvariantCulture)),
                    ["city_name_id"] = cityNameId,
                    ["route_id"] = routeId,
                    ["is_open"] = isOpen,
                    ["buys_resource_ids"] = buyIds,
                    ["sells_resource_ids"] = sellIds,
                    ["buys_resource_names"] = buyNames,
                    ["sells_resource_names"] = sellNames,
                    ["cost_to_open"] = costToOpen,
                    ["ph_unk01"] = phUnk01,
                    ["trader_entry_delay"] = traderEntryDelay,
                    ["ph_unk02"] = phUnk02,
                    ["empire_object_id"] = empireObjectId,
                    ["is_sea_trade"] = isSeaTrade,
                    ["months_under_siege"] = monthsUnderSiege,
                    ["trader_figure_ids"] = traderFigureIds
                });
                JObject city = (JObject)cities[cities.Count - 1];
                city["newera_buys_goods"] = BuildNewEraGoodArray(buyIds.OfType<JToken>().Select(token => token.Value<int>()));
                city["newera_sells_goods"] = BuildNewEraGoodArray(sellIds.OfType<JToken>().Select(token => token.Value<int>()));
            }
            return new JObject { ["record_size"] = recordSize, ["count"] = count, ["cities"] = cities };
        }

        private static JObject BuildWorldMapData(int version, Dictionary<string, byte[]> chunkByName)
        {
            JObject empireMapObjects = GetChunk(chunkByName, "empire_map_objects") != null ? ParseEmpireMapObjects(GetChunk(chunkByName, "empire_map_objects"), version) : null;
            JObject empireMapRoutes = GetChunk(chunkByName, "empire_map_routes") != null ? ParseEmpireMapRoutes(GetChunk(chunkByName, "empire_map_routes")) : null;
            JObject empireCities = GetChunk(chunkByName, "empire_cities") != null ? ParseEmpireCities(GetChunk(chunkByName, "empire_cities")) : null;
            if (empireMapObjects == null && empireMapRoutes == null && empireCities == null) return null;

            Dictionary<int, JObject> routesById = ((empireMapRoutes?["routes"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
                .ToDictionary(route => route["route_id"].Value<int>(), route => route);

            Dictionary<int, JObject> runtimeByName = new Dictionary<int, JObject>();
            Dictionary<int, JObject> runtimeByObject = new Dictionary<int, JObject>();
            foreach (JObject city in (empireCities?["cities"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
            {
                if (!(city["in_use"]?.Value<bool>() ?? false)) continue;
                int cityNameId = city["city_name_id"]?.Value<int>() ?? -1;
                int empireObjectId = city["empire_object_id"]?.Value<int>() ?? -1;
                if (!runtimeByName.ContainsKey(cityNameId)) runtimeByName.Add(cityNameId, city);
                if (empireObjectId >= 0 && !runtimeByObject.ContainsKey(empireObjectId)) runtimeByObject.Add(empireObjectId, city);
            }

            JArray mergedCities = new JArray();
            HashSet<int> matchedLookupIds = new HashSet<int>();
            foreach (JObject mapCity in (empireMapObjects?["cities"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
            {
                JObject runtimeCity = null;
                int objectIndex = mapCity["object_index"]?.Value<int>() ?? -1;
                int cityNameId = mapCity["city_name_id"]?.Value<int>() ?? -1;
                if (!runtimeByObject.TryGetValue(objectIndex, out runtimeCity))
                {
                    runtimeByName.TryGetValue(cityNameId, out runtimeCity);
                }

                JObject merged = (JObject)mapCity.DeepClone();
                merged["runtime_city"] = runtimeCity;
                if (runtimeCity != null)
                {
                    matchedLookupIds.Add(runtimeCity["lookup_id"].Value<int>());
                    merged["route_id"] = runtimeCity["route_id"];
                    merged["is_open"] = runtimeCity["is_open"];
                    merged["cost_to_open"] = runtimeCity["cost_to_open"];
                    merged["is_sea_trade"] = runtimeCity["is_sea_trade"];
                    merged["lookup_id"] = runtimeCity["lookup_id"];
                    merged["empire_object_id"] = runtimeCity["empire_object_id"];
                    if ((runtimeCity["buys_resource_ids"] as JArray)?.Count > 0)
                    {
                        merged["buys_resource_ids"] = runtimeCity["buys_resource_ids"];
                        merged["buys_resource_names"] = runtimeCity["buys_resource_names"];
                    }
                    if ((runtimeCity["sells_resource_ids"] as JArray)?.Count > 0)
                    {
                        merged["sells_resource_ids"] = runtimeCity["sells_resource_ids"];
                        merged["sells_resource_names"] = runtimeCity["sells_resource_names"];
                    }
                }
                else
                {
                    merged["route_id"] = mapCity["trade_route_id"];
                    merged["is_open"] = mapCity["trade_route_open"];
                    merged["cost_to_open"] = mapCity["trade_route_cost"];
                    merged["is_sea_trade"] = false;
                }

                int routeId = merged["route_id"]?.Value<int>() ?? -1;
                JObject route;
                if (routeId >= 0 && routesById.TryGetValue(routeId, out route))
                {
                    merged["route_type"] = route["route_type"];
                    merged["route_type_name"] = route["route_type_name"];
                    merged["route_points"] = route["points"];
                    if (runtimeCity == null) merged["is_sea_trade"] = (route["route_type"]?.Value<int>() ?? 0) == 2;
                }

                mergedCities.Add(merged);
            }

            JArray unmatched = new JArray();
            foreach (JObject city in (empireCities?["cities"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
            {
                if ((city["in_use"]?.Value<bool>() ?? false) && !matchedLookupIds.Contains(city["lookup_id"]?.Value<int>() ?? -1))
                {
                    unmatched.Add(city);
                }
            }

            return new JObject
            {
                ["cities"] = mergedCities,
                ["unmatched_runtime_cities"] = unmatched,
                ["empire_map_objects"] = empireMapObjects,
                ["empire_map_routes"] = empireMapRoutes,
                ["empire_cities"] = empireCities
            };
        }

        private static JObject CollectAllowedCities(JObject worldMap)
        {
            if (worldMap == null) return null;
            JArray cities = new JArray();
            AppendAllowedCitiesFromArray(cities, "world_map", worldMap["cities"] as JArray);
            AppendAllowedCitiesFromArray(cities, "runtime_only", worldMap["unmatched_runtime_cities"] as JArray);
            return new JObject
            {
                ["count"] = cities.Count,
                ["city_name_ids"] = new JArray(cities.OfType<JObject>().Select(c => c["city_name_id"]).Where(v => !IsEmptyMarker(v)).Distinct().OrderBy(v => v.Value<int>())),
                ["route_ids"] = new JArray(cities.OfType<JObject>().Select(c => c["route_id"]).Where(v => !IsEmptyMarker(v)).Distinct().OrderBy(v => v.Value<int>())),
                ["lookup_ids"] = new JArray(cities.OfType<JObject>().Select(c => c["lookup_id"]).Where(v => !IsEmptyMarker(v)).Distinct().OrderBy(v => v.Value<int>())),
                ["empire_object_ids"] = new JArray(cities.OfType<JObject>().Select(c => c["empire_object_id"]).Where(v => !IsEmptyMarker(v)).Distinct().OrderBy(v => v.Value<int>())),
                ["cities"] = cities
            };
        }

        private static void AppendAllowedCitiesFromArray(JArray target, string source, JArray sourceCities)
        {
            foreach (JObject city in sourceCities?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
            {
                int? routeId = city["route_id"]?.Value<int?>() ?? city["trade_route_id"]?.Value<int?>();
                int? routeType = city["route_type"]?.Value<int?>();
                bool? isSeaTrade = city["is_sea_trade"]?.Value<bool?>();
                if (!routeType.HasValue && routeId.HasValue && !IsEmptyMarker(routeId.HasValue ? (JToken)routeId.Value : null))
                {
                    routeType = isSeaTrade.GetValueOrDefault() ? 2 : 1;
                }
                string routeTypeName = city["route_type_name"]?.Value<string>();
                if (string.IsNullOrWhiteSpace(routeTypeName) && routeType.HasValue)
                {
                    routeTypeName = routeType.Value == 2 ? "sea" : (routeType.Value == 1 ? "land" : "type_" + routeType.Value.ToString(CultureInfo.InvariantCulture));
                }

                target.Add(new JObject
                {
                    ["source"] = source,
                    ["city_name_id"] = city["city_name_id"],
                    ["lookup_id"] = city["lookup_id"],
                    ["object_index"] = city["object_index"],
                    ["empire_object_id"] = city["empire_object_id"],
                    ["route_id"] = routeId.HasValue ? (JToken)routeId.Value : JValue.CreateNull(),
                    ["route_type"] = routeType.HasValue ? (JToken)routeType.Value : JValue.CreateNull(),
                    ["route_type_name"] = routeTypeName,
                    ["city_type"] = city["city_type"],
                    ["in_use"] = city["in_use"],
                    ["is_open"] = city["is_open"],
                    ["trade_route_open"] = city["is_open"],
                    ["is_sea_trade"] = isSeaTrade.HasValue ? (JToken)isSeaTrade.Value : JValue.CreateNull(),
                    ["cost_to_open"] = city["cost_to_open"] ?? city["trade_route_cost"],
                    ["trade_route_cost"] = city["cost_to_open"] ?? city["trade_route_cost"],
                    ["buys_resource_ids"] = new JArray(NormalizeResourceIds(city["buys_resource_ids"] as JArray)),
                    ["sells_resource_ids"] = new JArray(NormalizeResourceIds(city["sells_resource_ids"] as JArray))
                });
            }
        }

        private static JObject CollectAllowedGoods(JObject cityData, JObject worldMap)
        {
            List<int> cityDataActiveIds = NormalizeResourceIds(cityData?["active_resource_ids"] as JArray);
            List<int> foodTypesAvailableIds = NormalizeResourceIds(((cityData?["food_types_available"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>()).Select(slot => slot["resource_id"]));
            List<int> foodTypesEatenIds = NormalizeResourceIds(((cityData?["food_types_eaten"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>()).Select(slot => slot["resource_id"]));

            List<JObject> worldMapCityRecords = new List<JObject>();
            worldMapCityRecords.AddRange((worldMap?["cities"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>());
            worldMapCityRecords.AddRange((worldMap?["unmatched_runtime_cities"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>());

            List<int> worldMapBuysIds = NormalizeResourceIds(worldMapCityRecords.SelectMany(city => (city["buys_resource_ids"] as JArray)?.Children() ?? Enumerable.Empty<JToken>()));
            List<int> worldMapSellsIds = NormalizeResourceIds(worldMapCityRecords.SelectMany(city => (city["sells_resource_ids"] as JArray)?.Children() ?? Enumerable.Empty<JToken>()));
            List<int> allIds = cityDataActiveIds.Concat(foodTypesAvailableIds).Concat(foodTypesEatenIds).Concat(worldMapBuysIds).Concat(worldMapSellsIds).Distinct().OrderBy(v => v).ToList();

            JArray resources = new JArray();
            foreach (int resourceId in allIds)
            {
                resources.Add(new JObject
                {
                    ["resource_id"] = resourceId,
                    ["from_city_data_active"] = cityDataActiveIds.Contains(resourceId),
                    ["from_food_types_available"] = foodTypesAvailableIds.Contains(resourceId),
                    ["from_food_types_eaten"] = foodTypesEatenIds.Contains(resourceId),
                    ["from_world_map_buy"] = worldMapBuysIds.Contains(resourceId),
                    ["from_world_map_sell"] = worldMapSellsIds.Contains(resourceId)
                });
            }

            return new JObject
            {
                ["count"] = allIds.Count,
                ["resource_ids"] = new JArray(allIds),
                ["newera_goods"] = BuildNewEraGoodArray(allIds),
                ["from_city_data_active_ids"] = new JArray(cityDataActiveIds),
                ["from_food_types_available_ids"] = new JArray(foodTypesAvailableIds),
                ["from_food_types_eaten_ids"] = new JArray(foodTypesEatenIds),
                ["from_world_map_buys_ids"] = new JArray(worldMapBuysIds),
                ["from_world_map_sells_ids"] = new JArray(worldMapSellsIds),
                ["resources"] = resources
            };
        }

        private static int ComputeRequiredCreateSize(JObject root, string gridKey, int mapWidth, int mapHeight)
        {
            JArray arr = root?[gridKey] as JArray;
            if (arr == null || arr.Count == 0) return Math.Max(mapWidth, mapHeight);

            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;
            foreach (JObject tile in arr.OfType<JObject>())
            {
                int x = tile["x"]?.Value<int>() ?? 0;
                int y = tile["y"]?.Value<int>() ?? 0;
                if ((y & 1) == 0) x += 1;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }

            if (minX == int.MaxValue || minY == int.MaxValue) return Math.Max(mapWidth, mapHeight);

            int playableWidth = Math.Max(1, maxX - minX + 1);
            int playableHeight = Math.Max(1, maxY - minY + 1);
            int safeBorderX = 1;
            int safeBorderY = 2;
            try
            {
                CellCoord safeBorder = GlobalAccessor.GlobalSettings != null ? GlobalAccessor.GlobalSettings.MapSafeBorderSize : default(CellCoord);
                if (safeBorder.x > 0) safeBorderX = safeBorder.x;
                if (safeBorder.y > 0) safeBorderY = safeBorder.y;
            }
            catch { }

            int requiredFromWidth = Math.Max(1, (playableWidth - safeBorderX * 2) * 2);
            int requiredFromHeight = Math.Max(1, playableHeight - safeBorderY * 2);
            return Math.Max(requiredFromWidth, requiredFromHeight);
        }

        private static string TerrainKindFromFlags(uint flags)
        {
            if (flags == 0) return "sand";
            if ((flags & TerrainFlags["OUT_OF_BOUNDS"]) != 0) return "out_of_bounds";
            if ((flags & TerrainFlags["DEEPWATER"]) != 0) return "deep_water";
            if ((flags & TerrainFlags["FLOODPLAIN"]) != 0) return "floodplain";
            if ((flags & TerrainFlags["WATER"]) != 0) return "shallow_water";

            bool hasGrass = (flags & TerrainFlags["GROUNDWATER"]) != 0;
            if ((flags & TerrainFlags["ORE"]) != 0) return hasGrass ? "ore_rock_grass" : "ore_rock";
            if ((flags & TerrainFlags["MEADOW"]) != 0) return hasGrass ? "meadow_grass" : "meadow";
            if ((flags & TerrainFlags["ROAD"]) != 0) return hasGrass ? "road_grass" : "road";
            if ((flags & TerrainFlags["TREE"]) != 0) return hasGrass ? "tree_grass" : "tree";
            if ((flags & TerrainFlags["ROCK"]) != 0) return hasGrass ? "rock_grass" : "rock";
            if ((flags & TerrainFlags["MARSHLAND"]) != 0) return "marshland";
            if ((flags & TerrainFlags["DUNE"]) != 0) return "dune";
            if ((flags & TerrainFlags["SHRUB"]) != 0) return "shrub";
            if ((flags & TerrainFlags["ELEVATION"]) != 0) return "elevation";
            if ((flags & TerrainFlags["SUBMERGED_ROAD"]) != 0) return "road";
            if ((flags & TerrainFlags["CANAL"]) != 0 || (flags & TerrainFlags["IRRIGATION_RANGE"]) != 0) return "canal";
            if ((flags & TerrainFlags["WALL"]) != 0) return "wall";
            if ((flags & TerrainFlags["GARDEN"]) != 0) return "garden";
            if ((flags & TerrainFlags["RUBBLE"]) != 0) return "rubble";
            if ((flags & TerrainFlags["SHORE"]) != 0) return "shore";
            if ((flags & TerrainFlags["BUILDING"]) != 0) return "building";
            if (hasGrass) return "grass";
            return flags.ToString(CultureInfo.InvariantCulture);
        }

        private static JObject BuildPoint(int x, int y)
        {
            if (x == 65535 || y == 65535 || x == -1 || y == -1) return null;
            return new JObject { ["x"] = x, ["y"] = y };
        }

        private static JArray BuildPointArray(ushort[] xs, ushort[] ys)
        {
            JArray result = new JArray();
            for (int i = 0; i < Math.Min(xs.Length, ys.Length); i++)
            {
                JObject point = BuildPoint(xs[i], ys[i]);
                if (point != null) result.Add(point);
            }
            return result;
        }

        private static JArray BuildPointArray(int[] xs, int[] ys)
        {
            JArray result = new JArray();
            for (int i = 0; i < Math.Min(xs.Length, ys.Length); i++)
            {
                JObject point = BuildPoint(xs[i], ys[i]);
                if (point != null) result.Add(point);
            }
            return result;
        }

        private static JObject DecodeGridOffset(uint offset, int startOffset)
        {
            if (offset == 0xFFFFFFFFu) return null;
            int absoluteX = (int)(offset % GridSize);
            int absoluteY = (int)(offset / GridSize);
            int relative = (int)offset - startOffset;
            return new JObject
            {
                ["grid_offset_raw"] = offset,
                ["absolute_grid"] = new JObject { ["x"] = absoluteX, ["y"] = absoluteY },
                ["map"] = new JObject { ["x"] = relative % GridSize, ["y"] = relative / GridSize }
            };
        }

        private static JObject DecodePackedXYU32(uint value)
        {
            if (value == 0xFFFFFFFFu) return null;
            int x = (int)(value & 0xFFFF);
            int y = (int)((value >> 16) & 0xFFFF);
            if (x == 65535 || y == 65535) return null;
            return new JObject { ["x"] = x, ["y"] = y, ["packed_raw_u32"] = value };
        }

        private static GridPoint StartOffsetToAbsoluteXY(int startOffset)
        {
            return new GridPoint { X = startOffset % GridSize, Y = startOffset / GridSize };
        }

        private static JArray DecodeFoodSlots(ushort[] values)
        {
            JArray decoded = new JArray();
            for (int i = 0; i < values.Length; i++)
            {
                JObject entry = new JObject
                {
                    ["slot_index"] = i,
                    ["resource_id"] = values[i],
                    ["resource_name"] = values[i] > 0 ? GetResourceName(values[i]) : "none"
                };
                AddNewEraGoodFields(entry, values[i], values[i] > 0 ? GetResourceName(values[i]) : null);
                decoded.Add(entry);
            }
            return decoded;
        }

        private static List<int> NormalizeResourceIds(IEnumerable<JToken> values)
        {
            if (values == null)
            {
                return new List<int>();
            }

            return values.Where(v => v != null && v.Type != JTokenType.Null)
                .Select(v => v.Value<int>())
                .Where(v => v > 0 && v != 65535 && v != int.MaxValue)
                .Distinct()
                .OrderBy(v => v)
                .ToList();
        }

        private static bool IsEmptyMarker(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null) return true;
            int value = token.Value<int>();
            return value == -1 || value == 65535;
        }

        private static string GetResourceName(int resourceId)
        {
            string name;
            return ResourceNames.TryGetValue(resourceId, out name) ? name : "resource_" + resourceId.ToString(CultureInfo.InvariantCulture);
        }

        private static void AddNewEraGoodFields(JObject obj, int? resourceId, string resourceName = null)
        {
            if (obj == null)
            {
                return;
            }

            Good? good = null;
            if (resourceId.HasValue)
            {
                good = OgNewEraMappings.MapGoodFromResourceId(resourceId.Value);
            }

            if (!good.HasValue && !string.IsNullOrWhiteSpace(resourceName) && OgNewEraMappings.TryMapGood(resourceName, out Good parsed))
            {
                good = parsed;
            }

            obj["newera_good_id"] = good.HasValue ? (int)good.Value : -1;
            obj["newera_good_name"] = good.HasValue ? good.Value.ToString() : null;
        }

        private static JArray BuildNewEraGoodArray(IEnumerable<int> resourceIds)
        {
            JArray result = new JArray();
            HashSet<Good> seen = new HashSet<Good>();
            foreach (int resourceId in resourceIds ?? Enumerable.Empty<int>())
            {
                Good? good = OgNewEraMappings.MapGoodFromResourceId(resourceId);
                if (!good.HasValue || !seen.Add(good.Value))
                {
                    continue;
                }

                result.Add(new JObject
                {
                    ["resource_id"] = resourceId,
                    ["newera_good_id"] = (int)good.Value,
                    ["newera_good_name"] = good.Value.ToString()
                });
            }

            return result;
        }

        private static JArray BuildNewEraBuildingArray(IEnumerable<BuildingType> buildingTypes)
        {
            JArray result = new JArray();
            HashSet<BuildingType> seen = new HashSet<BuildingType>();
            foreach (BuildingType buildingType in buildingTypes ?? Enumerable.Empty<BuildingType>())
            {
                if (!seen.Add(buildingType))
                {
                    continue;
                }

                result.Add(new JObject
                {
                    ["building_type_id"] = (int)buildingType,
                    ["building_type_name"] = buildingType.ToString()
                });
            }

            return result;
        }

        private static void AddNewEraBuildingFields(JObject obj, IEnumerable<BuildingType> buildingTypes)
        {
            if (obj == null)
            {
                return;
            }

            JArray buildings = BuildNewEraBuildingArray(buildingTypes);
            obj["newera_buildings"] = buildings;
            obj["newera_building_type_ids"] = new JArray(buildings.OfType<JObject>().Select(b => b["building_type_id"]));
            obj["newera_building_type_names"] = new JArray(buildings.OfType<JObject>().Select(b => b["building_type_name"]));
        }

        private static void AddNewEraDeityField(JObject obj, string rawGodName)
        {
            if (obj == null)
            {
                return;
            }

            string mapped = OgNewEraMappings.MapGodName(rawGodName);
            obj["newera_deity_name"] = string.IsNullOrWhiteSpace(mapped) ? null : mapped;
        }

        private static string GetMonumentName(int monumentId)
        {
            string name;
            return MonumentNames.TryGetValue(monumentId, out name) ? name : "monument_" + monumentId.ToString(CultureInfo.InvariantCulture);
        }

        private static string GetOrFallback(Dictionary<int, string> dict, int key, string fallback)
        {
            string value;
            return dict.TryGetValue(key, out value) ? value : fallback;
        }

        private static byte[] GetChunk(Dictionary<string, byte[]> chunkByName, string name)
        {
            byte[] data;
            return chunkByName.TryGetValue(name, out data) ? data : null;
        }

        private static string DecodeCString(byte[] data)
        {
            int length = Array.IndexOf(data, (byte)0);
            if (length < 0) length = data.Length;
            return DecodeWindows1252(data, length).Trim();
        }

        private static string DecodeWindows1252(byte[] data, int length)
        {
            if (data == null || length <= 0)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                byte value = data[i];
                if (value < 0x80 || value >= 0xA0)
                {
                    sb.Append((char)value);
                    continue;
                }

                sb.Append(Cp1252ExtendedChars[value - 0x80]);
            }

            return sb.ToString();
        }

        private static byte[] Slice(byte[] data, int offset, int length)
        {
            if (offset < 0 || length < 0 || offset + length > data.Length)
            {
                throw new InvalidDataException("Unexpected end of file while slicing binary data.");
            }
            byte[] result = new byte[length];
            Buffer.BlockCopy(data, offset, result, 0, length);
            return result;
        }

        private static short[] ReadInt16Array(ByteReader reader, int count)
        {
            short[] result = new short[count];
            for (int i = 0; i < count; i++) result[i] = reader.ReadInt16();
            return result;
        }

        private static ushort[] ReadUInt16Array(ByteReader reader, int count)
        {
            ushort[] result = new ushort[count];
            for (int i = 0; i < count; i++) result[i] = reader.ReadUInt16();
            return result;
        }

        private static int[] ReadInt32Array(ByteReader reader, int count)
        {
            int[] result = new int[count];
            for (int i = 0; i < count; i++) result[i] = reader.ReadInt32();
            return result;
        }

        private static uint[] ReadUInt32Array(ByteReader reader, int count)
        {
            uint[] result = new uint[count];
            for (int i = 0; i < count; i++) result[i] = reader.ReadUInt32();
            return result;
        }

        private static int ReadUInt16LE(byte[] data, int offset)
        {
            return data[offset] | (data[offset + 1] << 8);
        }

        private static int ReadInt16LE(byte[] data, int offset)
        {
            return (short)ReadUInt16LE(data, offset);
        }

        private static int ReadInt32LE(byte[] data, int offset)
        {
            unchecked
            {
                return data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24);
            }
        }

        private static uint ReadUInt32LE(byte[] data, int offset)
        {
            unchecked
            {
                return (uint)ReadInt32LE(data, offset);
            }
        }

        private static byte[] DecompressPkware(byte[] inputData, int expectedOutputSize)
        {
            return new PkwareExploder(inputData, expectedOutputSize).Explode();
        }

        private sealed class FormatInfo
        {
            public string Format;
            public int Version;
            public List<ChunkSpec> Schema;
            public uint MissionIndexRaw;
            public bool HasMissionIndexRaw;
        }

        private sealed class ChunkSpec
        {
            public ChunkSpec(string name, int size, bool compressed)
            {
                Name = name;
                Size = size;
                Compressed = compressed;
            }

            public string Name;
            public int Size;
            public bool Compressed;
        }

        private sealed class ChunkData
        {
            public int Index;
            public string Name;
            public int Offset;
            public bool Compressed;
            public uint CompressionMarker;
            public int StoredSize;
            public int PackedSize;
            public byte[] Data;
        }

        private sealed class CanvasTile
        {
            public int Row;
            public int Col;
            public string Terrain;
            public uint Flags;
        }

        private sealed class TransformMeta
        {
            public int MinRow;
            public int MinRawCol;
        }

        private sealed class GridPoint
        {
            public int X;
            public int Y;
        }

        private sealed class ByteReader
        {
            private readonly byte[] _data;
            public int Offset { get; private set; }

            public ByteReader(byte[] data)
            {
                _data = data;
            }

            public void Skip(int size) { Offset += size; }
            public byte ReadByte() { return _data[Offset++]; }
            public short ReadInt16() { short v = (short)ReadInt16LE(_data, Offset); Offset += 2; return v; }
            public ushort ReadUInt16() { ushort v = (ushort)ReadUInt16LE(_data, Offset); Offset += 2; return v; }
            public int ReadInt32() { int v = ReadInt32LE(_data, Offset); Offset += 4; return v; }
            public uint ReadUInt32() { uint v = ReadUInt32LE(_data, Offset); Offset += 4; return v; }
            public byte[] ReadBytes(int count)
            {
                byte[] value = Slice(_data, Offset, count);
                Offset += count;
                return value;
            }

            public int ReadByteAfterUInt32Triplet(out uint length, out uint unk00, out uint unk01)
            {
                length = ReadUInt32();
                unk00 = ReadUInt32();
                unk01 = ReadUInt32();
                return ReadByte();
            }
        }

        private sealed class PkwareExploder
        {
            private const int PkEof = 773;
            private const int PkErrorValue = 774;
            private readonly byte[] _input;
            private readonly byte[] _output;
            private readonly byte[] _inputBuffer = new byte[2048];
            private readonly byte[] _outputBuffer = new byte[8708];
            private readonly int[] _copyOffsetJumpTable = ConstructJumpTable(64, PkCopyOffsetBits, PkCopyOffsetCode);
            private readonly int[] _copyLengthJumpTable = ConstructJumpTable(16, PkCopyLengthBaseBits, PkCopyLengthBaseCode);
            private int _inputPtr;
            private int _outputPtr;
            private bool _stop;
            private int _windowSize;
            private int _dictionarySize;
            private int _currentInputByte;
            private int _currentInputBitsAvailable;
            private int _inputBufferPtr = 2048;
            private int _inputBufferEnd;

            public PkwareExploder(byte[] input, int expectedOutputSize)
            {
                _input = input;
                _output = new byte[expectedOutputSize];
            }

            public byte[] Explode()
            {
                byte[] initial = InputFunc(2048);
                if (initial.Length <= 4) throw new InvalidDataException("PKWARE: too few input bytes.");
                Buffer.BlockCopy(initial, 0, _inputBuffer, 0, initial.Length);
                _inputBufferEnd = initial.Length;
                _inputBufferPtr = 3;
                int hasLiteralEncoding = _inputBuffer[0];
                _windowSize = _inputBuffer[1];
                _currentInputByte = _inputBuffer[2];
                _currentInputBitsAvailable = 0;
                if (_windowSize < 4 || _windowSize > 6) throw new InvalidDataException("PKWARE: invalid window size.");
                _dictionarySize = 0xFFFF >> (16 - _windowSize);
                if (hasLiteralEncoding != 0) throw new InvalidDataException("PKWARE: literal encoding unsupported.");

                int result = ExplodeData();
                if (_stop) throw new InvalidDataException("PKWARE: output overflow or corrupt stream.");
                if (result != PkEof) throw new InvalidDataException("PKWARE: decode error (no EOF token).");
                return _output.Take(_outputPtr).ToArray();
            }

            private static int[] ConstructJumpTable(int size, int[] bits, int[] codes)
            {
                int[] jump = new int[256];
                for (int i = size - 1; i >= 0; i--)
                {
                    int bit = bits[i];
                    int code = codes[i];
                    while (true)
                    {
                        jump[code] = i;
                        code += 1 << bit;
                        if (code >= 0x100) break;
                    }
                }
                return jump;
            }

            private byte[] InputFunc(int length)
            {
                if (_stop || _inputPtr >= _input.Length) return new byte[0];
                int end = Math.Min(_inputPtr + length, _input.Length);
                byte[] data = new byte[end - _inputPtr];
                Buffer.BlockCopy(_input, _inputPtr, data, 0, data.Length);
                _inputPtr = end;
                return data;
            }

            private void OutputFunc(byte[] src, int count)
            {
                if (_stop) return;
                int writable = Math.Min(count, _output.Length - _outputPtr);
                if (writable != count) { _stop = true; return; }
                Buffer.BlockCopy(src, 0, _output, _outputPtr, writable);
                _outputPtr += writable;
            }

            private int SetBitsUsed(int numBits)
            {
                if (_currentInputBitsAvailable >= numBits)
                {
                    _currentInputBitsAvailable -= numBits;
                    _currentInputByte = (_currentInputByte >> numBits) & 0xFFFF;
                    return 0;
                }

                _currentInputByte = (_currentInputByte >> _currentInputBitsAvailable) & 0xFFFF;
                if (_inputBufferPtr == _inputBufferEnd)
                {
                    byte[] chunk = InputFunc(2048);
                    if (chunk.Length == 0) return 1;
                    Buffer.BlockCopy(chunk, 0, _inputBuffer, 0, chunk.Length);
                    _inputBufferPtr = 0;
                    _inputBufferEnd = chunk.Length;
                }

                _currentInputByte |= _inputBuffer[_inputBufferPtr] << 8;
                _inputBufferPtr++;
                int shift = numBits - _currentInputBitsAvailable;
                _currentInputByte = (_currentInputByte >> shift) & 0xFFFF;
                _currentInputBitsAvailable += 8 - numBits;
                return 0;
            }

            private int DecodeNextToken()
            {
                if ((_currentInputByte & 1) != 0)
                {
                    if (SetBitsUsed(1) != 0) return PkErrorValue;
                    int index = _copyLengthJumpTable[_currentInputByte & 0xFF];
                    if (SetBitsUsed(PkCopyLengthBaseBits[index]) != 0) return PkErrorValue;
                    int extraBits = PkCopyLengthExtraBits[index];
                    if (extraBits != 0)
                    {
                        int extraValue = _currentInputByte & ((1 << extraBits) - 1);
                        if (SetBitsUsed(extraBits) != 0 && (index + extraValue) != 270) return PkErrorValue;
                        index = PkCopyLengthBaseValue[index] + extraValue;
                    }
                    return index + 256;
                }

                if (SetBitsUsed(1) != 0) return PkErrorValue;
                int result = _currentInputByte & 0xFF;
                if (SetBitsUsed(8) != 0) return PkErrorValue;
                return result;
            }

            private int GetCopyOffset(int copyLength)
            {
                int index = _copyOffsetJumpTable[_currentInputByte & 0xFF];
                if (SetBitsUsed(PkCopyOffsetBits[index]) != 0) return 0;

                int offset;
                if (copyLength == 2)
                {
                    offset = (_currentInputByte & 3) | (index << 2);
                    if (SetBitsUsed(2) != 0) return 0;
                }
                else
                {
                    offset = (_currentInputByte & _dictionarySize) | (index << _windowSize);
                    if (SetBitsUsed(_windowSize) != 0) return 0;
                }
                return offset + 1;
            }

            private int ExplodeData()
            {
                int outputBufferPtr = 4096;
                while (true)
                {
                    int tokenValue = DecodeNextToken();
                    if (tokenValue >= PkErrorValue - 1)
                    {
                        int finalLength = outputBufferPtr - 4096;
                        if (finalLength > 0)
                        {
                            byte[] flush = new byte[finalLength];
                            Buffer.BlockCopy(_outputBuffer, 4096, flush, 0, finalLength);
                            OutputFunc(flush, finalLength);
                        }
                        return tokenValue;
                    }

                    if (tokenValue >= 256)
                    {
                        int length = tokenValue - 254;
                        int offset = GetCopyOffset(length);
                        if (offset == 0) return PkErrorValue;
                        int src = outputBufferPtr - offset;
                        int dst = outputBufferPtr;
                        outputBufferPtr += length;
                        while (length-- > 0) _outputBuffer[dst++] = _outputBuffer[src++];
                    }
                    else
                    {
                        _outputBuffer[outputBufferPtr++] = (byte)(tokenValue & 0xFF);
                    }

                    if (outputBufferPtr >= 8192)
                    {
                        byte[] flush = new byte[4096];
                        Buffer.BlockCopy(_outputBuffer, 4096, flush, 0, 4096);
                        OutputFunc(flush, 4096);
                        int tailLen = outputBufferPtr - 4096;
                        Buffer.BlockCopy(_outputBuffer, 4096, _outputBuffer, 0, tailLen);
                        outputBufferPtr -= 4096;
                        if (_stop) return PkErrorValue;
                    }
                }
            }
        }
    }
}
