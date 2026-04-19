using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using I2.Loc;
using Newtonsoft.Json.Linq;
using SFB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OGDirectImport
{
    [BepInPlugin(ModGuid, ModName, VersionString)]
    public sealed class OGDirectImportPlugin : BaseUnityPlugin
    {
        private const string ModGuid = "PANE.ogdirectimport";
        private const string ModName = "OG Direct Import";
        private const string VersionString = "0.1.0";
        private const string ImportButtonObjectName = "OGDirectImportButton";
        private const string ImportButtonLabel = "Import OG MAP/SAV";
        private const string CreateButtonScenePath = "Canvas/Panel_LevelEditor/Generic_Container/PanelContent/Image/CreateButton";
        private const string LoadButtonScenePath = "Canvas/Panel_LevelEditor/Generic_Container/PanelContent/Image/LoadButton";
        private const string CreateButtonParentScenePath = "Canvas/Panel_LevelEditor/Generic_Container/PanelContent/Image";
        private const float CreateButtonTargetY = -7.5f;
        private const float ImportButtonTargetY = -117.5f;
        private const float LoadButtonTargetY = -62.5f;

        internal static ManualLogSource Log;
        internal static OGDirectImportPlugin Instance;

        private Harmony _harmony;

        internal static string PendingSourcePath;
        internal static string PendingJsonPath;
        internal static JObject PendingRoot;
        internal static string PendingArray = "grid";
        internal static int PendingMapSide;
        internal static bool ApplyPending;
        internal static bool ImportArmed;

        private static ConfigEntry<string> ConfPythonExe = null;
        private static ConfigEntry<bool> ConfImportEvents;
        private static ConfigEntry<bool> ConfDumpExtractedJson;

        private static readonly KeyCode Hotkey = KeyCode.O;
        private static readonly Dictionary<int, string[]> OgEmpireCityNames = new Dictionary<int, string[]>
        {
            { 0, new[] { "Elephantine", "Abu" } },
            { 1, new[] { "Abydos", "Abedju" } },
            { 2, new[] { "Bahariya Oasis", "Bahariya Oasis" } },
            { 3, new[] { "Kuban", "Baki" } },
            { 4, new[] { "Apollinopolis", "Behdet" } },
            { 5, new[] { "Bubastis", "Bubastis" } },
            { 6, new[] { "Buhen", "Buhen" } },
            { 7, new[] { "Byblos", "Byblos" } },
            { 8, new[] { "Dahshur", "Dahshur" } },
            { 9, new[] { "Dakhla Oasis", "Dakhla Oasis" } },
            { 10, new[] { "Abusir", "Djedu" } },
            { 11, new[] { "Dunqul Oasis", "Dunqul Oasis" } },
            { 12, new[] { "Enkomi", "Enkomi" } },
            { 13, new[] { "Farafra Oasis", "Farafra Oasis" } },
            { 14, new[] { "Gaza", "Gaza" } },
            { 15, new[] { "Semna", "Heh" } },
            { 16, new[] { "Herakleopolis", "Henen-nesw" } },
            { 17, new[] { "Kahun", "Hetepsenusret" } },
            { 18, new[] { "Mirgissa", "Iken" } },
            { 19, new[] { "Itjtawy", "Itjtawy" } },
            { 20, new[] { "Dendera", "Iunet" } },
            { 21, new[] { "Jericho", "Jericho" } },
            { 22, new[] { "Coptos", "Kebet" } },
            { 23, new[] { "Kerma", "Kerma" } },
            { 24, new[] { "Kharga Oasis", "Kharga Oasis" } },
            { 25, new[] { "Hermopolis", "Khmun" } },
            { 26, new[] { "Knossos", "Knossos" } },
            { 27, new[] { "Kyrene", "Kyrene" } },
            { 28, new[] { "Meidum", "Meidum" } },
            { 29, new[] { "Memphis", "Men-nefer" } },
            { 30, new[] { "Beni Hasan", "Menat Khufu" } },
            { 31, new[] { "Mycenae", "Mycenae" } },
            { 32, new[] { "Hierakonpolis", "Nekhen" } },
            { 33, new[] { "Naqada", "Nubt" } },
            { 34, new[] { "Heliopolis", "On" } },
            { 35, new[] { "Buto", "Perwadjyt" } },
            { 36, new[] { "Punt", "Pwenet" } },
            { 37, new[] { "Qanta", "Qadesh" } },
            { 38, new[] { "Giza", "Rostja" } },
            { 39, new[] { "Avaris", "Rowarty" } },
            { 40, new[] { "Saqqara", "Saqqara" } },
            { 41, new[] { "Lykopolis", "Sauty" } },
            { 42, new[] { "Mersa Gawasis", "Sawu" } },
            { 43, new[] { "Selima Oasis", "Selima Oasis" } },
            { 44, new[] { "Serabit Khadim", "Serabit Khadim" } },
            { 45, new[] { "Sai", "Shaat" } },
            { 46, new[] { "Sharuhen", "Sharuhen" } },
            { 47, new[] { "Thinis", "Thinis" } },
            { 48, new[] { "Timna", "Timna" } },
            { 49, new[] { "Toshka", "Toshka" } },
            { 50, new[] { "Tyre", "Tyre" } },
            { 51, new[] { "Thebes", "Waset" } },
            { 52, new[] { "Pelusium", "Migdol" } },
            { 53, new[] { "Alexandria", "Alexandria" } },
            { 54, new[] { "Sumur", "Sumur" } },
            { 55, new[] { "Deir el-Medina", "Deir el-Medina" } },
            { 56, new[] { "Abu Simbel", "Abu Simbel" } },
            { 57, new[] { "Actium", "Actium" } },
            { 58, new[] { "Rome", "Rome" } },
            { 59, new[] { "Tanis", "Tanis" } },
            { 60, new[] { "Pi-Yer", "Pi-Yer" } },
            { 61, new[] { "Siwi Oasis", "Siwi Oasis" } },
            { 62, new[] { "Maritis", "Maritis" } },
            { 63, new[] { "Piramesse", "Piramesse" } },
            { 64, new[] { "Athens", "Athens" } },
            { 65, new[] { "Cleoantonopolis", "Cleoantonopolis" } },
        };

        private static readonly Dictionary<Good, BuildingType[]> NewEraLinkedGoodsToBuildings = new Dictionary<Good, BuildingType[]>
        {
            { Good.Barley, new[] { BuildingType.FarmBarley, BuildingType.Brewery } },
            { Good.Beer, new[] { BuildingType.Brewery, BuildingType.SenetHouse } },
            { Good.Bricks, new[] { BuildingType.Brickworks } },
            { Good.Chariots, new[] { BuildingType.ChariotMaker, BuildingType.FortCharioteers } },
            { Good.Chickpeas, new[] { BuildingType.FarmChickpeas } },
            { Good.Clay, new[] { BuildingType.ClayPit, BuildingType.Potter, BuildingType.Brickworks } },
            { Good.Copper, new[] { BuildingType.MineCopper, BuildingType.Weaponsmith } },
            { Good.CompositeBows, new[] { BuildingType.BowMaker } },
            { Good.Figs, new[] { BuildingType.FarmFigs } },
            { Good.Fish, new[] { BuildingType.FishingWharf } },
            { Good.Flax, new[] { BuildingType.FarmFlax, BuildingType.Weaver } },
            { Good.GameMeat, new[] { BuildingType.HuntingLodge, BuildingType.Zoo } },
            { Good.Gems, new[] { BuildingType.MineGem, BuildingType.Jeweler } },
            { Good.Gold, new[] { BuildingType.MineGold } },
            { Good.Granite, new[] { BuildingType.QuarryGranite } },
            { Good.Grain, new[] { BuildingType.FarmGrain } },
            { Good.Henna, new[] { BuildingType.FarmHenna, BuildingType.PaintMaker } },
            { Good.Hides, new[] { BuildingType.ShieldMaker } },
            { Good.Jewelry, new[] { BuildingType.Jeweler } },
            { Good.Lamp, new[] { BuildingType.LampMaker } },
            { Good.Lettuce, new[] { BuildingType.FarmLettuce } },
            { Good.Limestone, new[] { BuildingType.QuarryLimestone } },
            { Good.Linen, new[] { BuildingType.Weaver, BuildingType.Mortuary } },
            { Good.Meat, new[] { BuildingType.CattleRanch } },
            { Good.Oil, new[] { BuildingType.LampMaker } },
            { Good.Paint, new[] { BuildingType.PaintMaker } },
            { Good.Papyrus, new[] { BuildingType.PapyrusMaker, BuildingType.ScribalSchool, BuildingType.Library } },
            { Good.Plainstone, new[] { BuildingType.QuarryPlainStone } },
            { Good.Pomegranate, new[] { BuildingType.FarmPomegranates } },
            { Good.Pottery, new[] { BuildingType.Potter, BuildingType.LampMaker } },
            { Good.Reeds, new[] { BuildingType.ReedGatherer, BuildingType.PapyrusMaker } },
            { Good.Sandstone, new[] { BuildingType.QuarrySandstone } },
            { Good.Straw, new[] { BuildingType.FarmGrain, BuildingType.Zoo, BuildingType.Brickworks, BuildingType.CattleRanch } },
            { Good.Shields, new[] { BuildingType.ShieldMaker } },
            { Good.Weapons, new[] { BuildingType.Weaponsmith } },
            { Good.Wood, new[] { BuildingType.WoodCutter, BuildingType.WarshipWharf, BuildingType.TransportWharf, BuildingType.GuildCarpenters, BuildingType.BowMaker } },
        };

        private static readonly Dictionary<BuildingType, Good[]> NewEraLinkedBuildingsToGoods = BuildReverseGoodBuildingLinks();
        private static readonly Dictionary<int, BuildingType[]> OgAllowedBuildingIndexToNewEraBuildings = new Dictionary<int, BuildingType[]>
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
        private static readonly Dictionary<int, string> OgMonumentIdNames = new Dictionary<int, string>
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
        private static readonly Dictionary<int, BuildingType[]> OgAllowedMonumentIdToNewEraBuildings = new Dictionary<int, BuildingType[]>
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

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            ConfImportEvents = Config.Bind("Import", "ImportEvents", true, "Also import OG scenario events into TemplateEvents.");
            ConfDumpExtractedJson = Config.Bind("Debug", "DumpExtractedJson", false, "Dump the internal C# extracted MAP/SAV payload as JSON for debugging.");

            _harmony = new Harmony(ModGuid);
            _harmony.PatchAll(typeof(OGDirectImportPlugin).Assembly);
            Logger.LogInfo($"{ModName} loaded.");
        }

        private void OnDestroy()
        {
            try { _harmony?.UnpatchSelf(); } catch { }
            if (ReferenceEquals(Instance, this))
            {
                Instance = null;
            }
        }

        private void Update()
        {
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (ctrl && shift && Input.GetKeyDown(Hotkey))
            {
                TryPickOgFileAndArmImport();
            }
        }

        private static bool TryEnsureImportButton(MonoBehaviour panel, params string[] sourceHandlerNames)
        {
            if (panel == null)
            {
                return false;
            }

            Transform existing = FindExistingImportButton(panel);
            if (existing != null)
            {
                ApplyMenuButtonLayout();
                return true;
            }

            Button sourceButton = FindSourceCreateButton(panel, sourceHandlerNames);
            if (sourceButton == null)
            {
                return false;
            }

            GameObject clone = Instantiate(sourceButton.gameObject, sourceButton.transform.parent);
            clone.name = ImportButtonObjectName;
            clone.SetActive(true);

            foreach (Localize localize in clone.GetComponentsInChildren<Localize>(true))
            {
                localize.enabled = false;
            }

            foreach (TMP_Text tmpText in clone.GetComponentsInChildren<TMP_Text>(true))
            {
                if (!string.IsNullOrWhiteSpace(tmpText.text))
                {
                    tmpText.text = ImportButtonLabel;
                }
            }

            Button cloneButton = clone.GetComponent<Button>();
            if (cloneButton == null)
            {
                Log?.LogWarning("Cloned OG import button does not contain a Button component.");
                Destroy(clone);
                return false;
            }

            cloneButton.onClick.RemoveAllListeners();
            cloneButton.onClick.AddListener(() =>
            {
                try
                {
                    Instance?.TryPickOgFileAndArmImport();
                }
                catch (Exception ex)
                {
                    Log?.LogError($"OG import button click failed: {ex}");
                }
            });

            int siblingIndex = sourceButton.transform.GetSiblingIndex();
            clone.transform.SetSiblingIndex(Math.Min(siblingIndex + 1, clone.transform.parent.childCount - 1));

            LayoutGroup layoutGroup = clone.transform.parent != null ? clone.transform.parent.GetComponent<LayoutGroup>() : null;
            if (layoutGroup == null)
            {
                RectTransform sourceRect = sourceButton.transform as RectTransform;
                RectTransform cloneRect = clone.transform as RectTransform;
                if (sourceRect != null && cloneRect != null)
                {
                    cloneRect.anchorMin = sourceRect.anchorMin;
                    cloneRect.anchorMax = sourceRect.anchorMax;
                    cloneRect.pivot = sourceRect.pivot;
                    cloneRect.sizeDelta = sourceRect.sizeDelta;
                    cloneRect.anchoredPosition = sourceRect.anchoredPosition + new Vector2(0f, -(sourceRect.rect.height + 12f));
                }
            }

            Log?.LogInfo("Added OG import button to level-editor menu.");
            ApplyMenuButtonLayout();
            return true;
        }

        private static void ApplyMenuButtonLayout()
        {
            SetButtonY(CreateButtonScenePath, CreateButtonTargetY);
            SetButtonY(CreateButtonParentScenePath + "/" + ImportButtonObjectName, ImportButtonTargetY);
            SetButtonY(LoadButtonScenePath, LoadButtonTargetY);
        }

        private static void SetButtonY(string scenePath, float y)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                return;
            }

            GameObject buttonObject = GameObject.Find(scenePath);
            if (buttonObject == null)
            {
                return;
            }

            RectTransform rect = buttonObject.transform as RectTransform;
            if (rect != null)
            {
                Vector3 local = rect.localPosition;
                if (!Mathf.Approximately(local.y, y))
                {
                    rect.localPosition = new Vector3(local.x, y, local.z);
                }

                return;
            }

            Vector3 fallbackLocal = buttonObject.transform.localPosition;
            if (!Mathf.Approximately(fallbackLocal.y, y))
            {
                buttonObject.transform.localPosition = new Vector3(fallbackLocal.x, y, fallbackLocal.z);
            }
        }

        private static Transform FindExistingImportButton(MonoBehaviour panel)
        {
            GameObject globalButton = GameObject.Find(CreateButtonParentScenePath + "/" + ImportButtonObjectName);
            if (globalButton != null)
            {
                return globalButton.transform;
            }

            return FindChildRecursive(panel != null ? panel.transform : null, ImportButtonObjectName);
        }

        private static Button FindSourceCreateButton(MonoBehaviour panel, params string[] sourceHandlerNames)
        {
            GameObject exactButtonObject = GameObject.Find(CreateButtonScenePath);
            if (exactButtonObject != null)
            {
                Button exactButton = exactButtonObject.GetComponent<Button>();
                if (exactButton != null)
                {
                    return exactButton;
                }
            }

            return FindButtonByHandler(panel, sourceHandlerNames);
        }

        private static void ScheduleEnsureImportButton(MonoBehaviour panel, params string[] sourceHandlerNames)
        {
            if (panel == null)
            {
                return;
            }

            if (TryEnsureImportButton(panel, sourceHandlerNames))
            {
                return;
            }

            panel.StartCoroutine(EnsureImportButtonRoutine(panel, sourceHandlerNames));
        }

        private static IEnumerator EnsureImportButtonRoutine(MonoBehaviour panel, params string[] sourceHandlerNames)
        {
            const int maxFrames = 120;
            for (int i = 0; i < maxFrames; i++)
            {
                yield return null;

                if (panel == null)
                {
                    yield break;
                }

                if (TryEnsureImportButton(panel, sourceHandlerNames))
                {
                    yield break;
                }
            }

            try
            {
                LogAvailableButtons(panel);
            }
            catch
            {
            }

            Log?.LogWarning("OG import button was not injected after retries.");
        }

        private static Button FindButtonByHandler(MonoBehaviour panel, params string[] handlerNames)
        {
            if (panel == null)
            {
                return null;
            }

            Button[] buttons = panel.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                if (button == null)
                {
                    continue;
                }

                bool matchedHandler = false;
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                {
                    string methodName = button.onClick.GetPersistentMethodName(i);
                    if (handlerNames != null && handlerNames.Any(name => string.Equals(methodName, name, StringComparison.Ordinal)))
                    {
                        matchedHandler = true;
                        break;
                    }
                }

                if (matchedHandler)
                {
                    return button;
                }
            }

            foreach (Button button in buttons)
            {
                if (button == null)
                {
                    continue;
                }

                string objectName = button.gameObject.name ?? string.Empty;
                if (ContainsAnyToken(objectName, "load", "create", "editor", "map"))
                {
                    return button;
                }
            }

            foreach (Button button in buttons)
            {
                if (button == null)
                {
                    continue;
                }

                TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
                string label = text != null ? text.text : string.Empty;
                if (ContainsAnyToken(label, "load", "create", "mission", "map"))
                {
                    return button;
                }
            }

            TMP_Dropdown dropdown = AccessTools.Field(panel.GetType(), "m_SizeDropdown")?.GetValue(panel) as TMP_Dropdown;
            if (dropdown != null)
            {
                Button nearest = buttons
                    .Where(button => button != null)
                    .OrderBy(button => Math.Abs(button.transform.position.y - dropdown.transform.position.y))
                    .FirstOrDefault();
                if (nearest != null)
                {
                    return nearest;
                }
            }

            return buttons.FirstOrDefault(button => button != null);
        }

        private static bool ContainsAnyToken(string value, params string[] tokens)
        {
            if (string.IsNullOrWhiteSpace(value) || tokens == null || tokens.Length == 0)
            {
                return false;
            }

            foreach (string token in tokens)
            {
                if (!string.IsNullOrWhiteSpace(token) && value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static void LogAvailableButtons(MonoBehaviour panel)
        {
            if (panel == null)
            {
                return;
            }

            Button[] buttons = panel.GetComponentsInChildren<Button>(true);
            if (buttons == null || buttons.Length == 0)
            {
                Log?.LogWarning($"No Button components found under level-editor menu panel. Exact create path found={GameObject.Find(CreateButtonScenePath) != null}.");
                return;
            }

            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null)
                {
                    continue;
                }

                TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
                string label = text != null ? text.text : string.Empty;
                string parentName = button.transform.parent != null ? button.transform.parent.name : "<no-parent>";
                Log?.LogInfo($"Button candidate #{i}: name='{button.gameObject.name}', label='{label}', parent='{parentName}', active={button.gameObject.activeInHierarchy}");
            }

            GameObject exactCreateButton = GameObject.Find(CreateButtonScenePath);
            Log?.LogInfo($"Exact create button path '{CreateButtonScenePath}' found={exactCreateButton != null}.");
        }

        private static Transform FindChildRecursive(Transform parent, string childName)
        {
            if (parent == null || string.IsNullOrWhiteSpace(childName))
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (string.Equals(child.name, childName, StringComparison.Ordinal))
                {
                    return child;
                }

                Transform nested = FindChildRecursive(child, childName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private void TryPickOgFileAndArmImport()
        {
            try
            {
                string[] paths = StandaloneFileBrowser.OpenFilePanel(
                    "Select Pharaoh MAP/SAV",
                    "",
                    new[]
                    {
                        new ExtensionFilter("Pharaoh files", "map", "sav"),
                        new ExtensionFilter("All Files", "*")
                    },
                    false
                );

                string path = (paths != null && paths.Length > 0) ? paths[0] : null;
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    Log?.LogWarning("No MAP/SAV file selected.");
                    return;
                }

                if (!OgBinaryExtractor.TryExtract(path, ConfDumpExtractedJson.Value, out JObject root, out int side, out string gridArrayName, out string dumpedJsonPath, out string error))
                {
                    Log?.LogError($"OG extraction failed: {error}");
                    return;
                }

                PendingSourcePath = path;
                PendingJsonPath = dumpedJsonPath;
                PendingRoot = root;
                PendingMapSide = side;
                PendingArray = gridArrayName;
                ImportArmed = true;
                ApplyPending = true;

                Log?.LogInfo($"Armed OG import: source='{PendingSourcePath}', dumpJson='{PendingJsonPath}', side={PendingMapSide}, array='{PendingArray}'.");

                if (TryApplyNowIfMapEditorAlive())
                {
                    return;
                }

                if (!DirectCreateMapFromOgSize())
                {
                    Log?.LogWarning("Direct create failed. You may need to be in the main menu/editor context where CoreSceneManager exists.");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"TryPickOgFileAndArmImport failed: {ex}");
            }
        }

        private static bool RunExtractor(string sourcePath, out string jsonPath, out string error)
        {
            jsonPath = null;
            error = null;

            try
            {
                string pythonExe = ConfPythonExe.Value?.Trim();
                string extractorScript = EnsureEmbeddedExtractorScript(out string resourceName);
                if (string.IsNullOrWhiteSpace(pythonExe))
                {
                    error = "PythonExe config is empty.";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(extractorScript) || !File.Exists(extractorScript))
                {
                    error = $"Extractor script not found: '{extractorScript}'";
                    return false;
                }

                Log?.LogInfo($"Running embedded extractor with Python='{pythonExe}' Script='{extractorScript}' Resource='{resourceName}'.");

                string tempDir = Path.Combine(Path.GetTempPath(), "OGDirectImport");
                Directory.CreateDirectory(tempDir);
                string outName = Path.GetFileNameWithoutExtension(sourcePath) + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json";
                jsonPath = Path.Combine(tempDir, outName);

                string arguments = $"\"{extractorScript}\" \"{sourcePath}\" --json-out \"{jsonPath}\"";
                var psi = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    if (process == null)
                    {
                        error = "Process.Start returned null.";
                        return false;
                    }

                    string stdout = process.StandardOutput.ReadToEnd();
                    string stderr = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        error = $"Extractor exit code {process.ExitCode}\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}";
                        return false;
                    }
                }

                if (!File.Exists(jsonPath))
                {
                    error = $"Extractor did not produce JSON: '{jsonPath}'";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.ToString();
                return false;
            }
        }

        private static string EnsureEmbeddedExtractorScript(out string resourceName)
        {
            resourceName = null;
            Assembly assembly = Assembly.GetExecutingAssembly();
            string[] resourceNames = assembly.GetManifestResourceNames();
            resourceName = resourceNames.FirstOrDefault(name =>
                string.Equals(name, "OGSaveImport.pharaoh_extract.py", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "OGDirectImport.pharaoh_extract.py", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".pharaoh_extract.py", StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(resourceName))
            {
                throw new FileNotFoundException("Embedded extractor resource pharaoh_extract.py was not found in assembly.");
            }

            string tempDir = Path.Combine(Path.GetTempPath(), "OGDirectImport", "embedded");
            Directory.CreateDirectory(tempDir);

            string assemblyPath = assembly.Location;
            string assemblyStamp = "embedded";
            if (!string.IsNullOrWhiteSpace(assemblyPath) && File.Exists(assemblyPath))
            {
                DateTime lastWriteUtc = File.GetLastWriteTimeUtc(assemblyPath);
                assemblyStamp = lastWriteUtc.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            }

            string targetPath = Path.Combine(tempDir, $"pharaoh_extract_{assemblyStamp}.py");

            using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                {
                    throw new FileNotFoundException($"Embedded extractor resource '{resourceName}' could not be opened.");
                }

                using (var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    resourceStream.CopyTo(fileStream);
                }
            }

            return targetPath;
        }

        private static string ResolveExtractorScriptPath()
        {
            try
            {
                return EnsureEmbeddedExtractorScript(out _);
            }
            catch (Exception ex)
            {
                Log?.LogWarning($"Falling back to legacy extractor path resolution because embedded extraction failed: {ex.Message}");
            }

            try
            {
                string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (!string.IsNullOrWhiteSpace(assemblyDir))
                {
                    return Path.Combine(assemblyDir, "pharaoh_extract.py");
                }
            }
            catch
            {
            }

            return "pharaoh_extract.py";
        }

        private static bool TryReadJsonHeader(string path, out int side, out string gridKey, out string error)
        {
            side = 0;
            gridKey = "grid";
            error = null;

            try
            {
                JObject root = JObject.Parse(File.ReadAllText(path));

                if (root["grid"] is JArray) gridKey = "grid";
                else if (root["tiles"] is JArray) gridKey = "tiles";
                else
                {
                    error = "Neither 'grid' nor 'tiles' array found.";
                    return false;
                }

                JObject map = root["map"] as JObject;
                int width = (int?)map?["width"] ?? 0;
                int height = (int?)map?["height"] ?? 0;
                side = ComputeRequiredCreateSize(root, gridKey, width, height);

                if (side <= 0)
                {
                    error = "Missing or invalid map.width/map.height.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static int ComputeRequiredCreateSize(JObject root, string gridKey, int mapWidth, int mapHeight)
        {
            JArray arr = root?[gridKey] as JArray;
            if (arr == null || arr.Count == 0)
            {
                return Math.Max(mapWidth, mapHeight);
            }

            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;

            foreach (JToken jt in arr)
            {
                int x = (int?)jt["x"] ?? 0;
                int y = (int?)jt["y"] ?? 0;

                if ((y & 1) == 0)
                {
                    x += 1;
                }

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }

            if (minX == int.MaxValue || minY == int.MaxValue)
            {
                return Math.Max(mapWidth, mapHeight);
            }

            int playableWidth = Math.Max(1, maxX - minX + 1);
            int playableHeight = Math.Max(1, maxY - minY + 1);

            int safeBorderX = 1;
            int safeBorderY = 2;
            try
            {
                CellCoord safeBorder = GlobalAccessor.GlobalSettings?.MapSafeBorderSize ?? default;
                if (safeBorder.x > 0) safeBorderX = safeBorder.x;
                if (safeBorder.y > 0) safeBorderY = safeBorder.y;
            }
            catch
            {
            }

            int requiredFromWidth = Math.Max(1, (playableWidth - safeBorderX * 2) * 2);
            int requiredFromHeight = Math.Max(1, playableHeight - safeBorderY * 2);
            int required = Math.Max(requiredFromWidth, requiredFromHeight);

            Log?.LogInfo(
                $"Computed create size from OG grid: rawMap={mapWidth}x{mapHeight}, playable={playableWidth}x{playableHeight}, " +
                $"safeBorder={safeBorderX}x{safeBorderY}, requiredSize={required}.");

            return required;
        }

        private static bool DirectCreateMapFromOgSize()
        {
            try
            {
                if (PendingMapSide <= 0)
                {
                    return false;
                }

                ExecuteDirectCreateMap(PendingMapSide, "direct hotkey flow");
                return true;
            }
            catch (Exception ex)
            {
                Log?.LogWarning($"DirectCreateMapFromOgSize failed: {ex}");
                return false;
            }
        }

        private static bool TryApplyNowIfMapEditorAlive()
        {
            if (!ApplyPending) return false;
            if (SceneManager.GetActiveScene().name != "MapEditor") return false;

            Type mapEditorType = AccessTools.TypeByName("MapEditor");
            if (mapEditorType == null) return false;

            UnityEngine.Object editorObj = UnityEngine.Object.FindObjectOfType(mapEditorType);
            if (editorObj == null) return false;

            try
            {
                Log?.LogInfo("Applying OG import immediately to active MapEditor.");
                ApplyOgJson(editorObj);
                return true;
            }
            catch (Exception ex)
            {
                Log?.LogError($"Immediate apply failed: {ex}");
                return true;
            }
        }

        private static void ApplyOgJson(object mapEditorInstance)
        {
            if (!ApplyPending) return;
            Log?.LogInfo("ApplyOgJson started.");
            ApplyPending = false;
            ImportArmed = false;

            if (PendingRoot == null)
            {
                Log?.LogWarning("PendingRoot missing when trying to apply.");
                return;
            }

            JObject root = PendingRoot;
            PendingRoot = null;
            JArray arr = (root[PendingArray] as JArray) ?? (root["grid"] as JArray) ?? (root["tiles"] as JArray);
            if (arr == null || arr.Count == 0)
            {
                Log?.LogWarning("OG import payload has no grid/tiles.");
                return;
            }

            ApplyTerrainTiles(mapEditorInstance, arr);
            TryApplyScenarioInfo(mapEditorInstance, root);
            TryApplyAvailability(mapEditorInstance, root);
            TryApplyMapFlags(mapEditorInstance, root);
            TryApplyWorldMap(mapEditorInstance, root);

            if (ConfImportEvents.Value)
            {
                TryApplyEvents(mapEditorInstance, root);
            }
        }

        private static void ApplyTerrainTiles(object mapEditorInstance, JArray arr)
        {
            if (!(mapEditorInstance is MapEditor editorInstance))
            {
                Log?.LogWarning("ApplyTerrainTiles expected MapEditor instance.");
                return;
            }

            Type tEditor = editorInstance.GetType();
            LevelMap level = GlobalAccessor.Level ?? (AccessTools.Field(tEditor, "_level")?.GetValue(editorInstance) as LevelMap);
            if (level == null)
            {
                Log?.LogWarning("MapEditor._level is null; cannot apply terrain.");
                return;
            }

            MethodInfo mPaintCell = AccessTools.Method(tEditor, "PaintCell");
            MethodInfo mFloodConn = AccessTools.Method(tEditor, "UpdateFloodplainConnectionsToWater");
            MethodInfo mBrushNeighbourUpdate = AccessTools.Method(tEditor, "BrushZoneNeighbourGatheringUpdate");
            MethodInfo mCliffInvalidPurge = AccessTools.Method(tEditor, "PaintMode_CliffInvalidPurge_Loop");
            MethodInfo mStartAndWaitForPostPaintJob = AccessTools.Method(tEditor, "StartAndWaitForPostPaintJob");

            if (mPaintCell == null)
            {
                Log?.LogWarning("Required map editor methods not found.");
                return;
            }

            FieldInfo fPaintContext = AccessTools.Field(tEditor, "_paintContext");
            if (fPaintContext == null)
            {
                Log?.LogWarning("MapEditor._paintContext not found.");
                return;
            }

            object paintContext = fPaintContext.GetValue(mapEditorInstance);
            if (paintContext == null)
            {
                Log?.LogWarning("MapEditor._paintContext is null.");
                return;
            }

            FieldInfo fBrush = AccessTools.Field(paintContext.GetType(), "Brush");
            if (fBrush == null)
            {
                Log?.LogWarning("PaintContext.Brush field not found.");
                return;
            }

            TerrainType oldBrush = TerrainType.None;
            try { oldBrush = (TerrainType)fBrush.GetValue(paintContext); } catch { }

            Type cellType = mPaintCell.GetParameters()[0].ParameterType;
            Type terrainEnumType = mPaintCell.GetParameters()[1].ParameterType;

            HashSet<Cell> refreshCells = new HashSet<Cell>();
            HashSet<int> refreshGrass = new HashSet<int>();
            HashSet<TerrainType> changedNativeTerrains = new HashSet<TerrainType>();
            HashSet<StaggeredCellCoord> changedCoords = new HashSet<StaggeredCellCoord>();
            HashSet<StaggeredCellCoord> forcedGrassCoords = new HashSet<StaggeredCellCoord>();
            List<Cell> marshCells = new List<Cell>();

            int applied = 0;
            int skipped = 0;

            try
            {
                foreach (JToken jt in arr)
                {
                    int x = (int?)jt["x"] ?? 0;
                    int y = (int?)jt["y"] ?? 0;

                    // Preserve the same correction used by the JSON importer.
                    if ((y & 1) == 0)
                    {
                        x += 1;
                    }

                    OgTerrainImportSpec terrainSpec = DescribeOgTerrain(jt["terrain"]?.Value<string>());
                    string s = terrainSpec.PaintTerrainName;
                    if (string.IsNullOrWhiteSpace(s))
                    {
                        skipped++;
                        continue;
                    }

                    object terrainValue;
                    object brushValue;
                    try
                    {
                        terrainValue = Enum.Parse(terrainEnumType, s, true);
                        brushValue = Enum.Parse(fBrush.FieldType, s, true);
                    }
                    catch
                    {
                        skipped++;
                        continue;
                    }

                    Cell cell = level.GetCell(new StaggeredCellCoord(x, y));
                    if (cell == null)
                    {
                        skipped++;
                        continue;
                    }

                    if (terrainSpec.ForceGrassPlain)
                    {
                        forcedGrassCoords.Add(cell.Coordinates);
                    }

                    TerrainType targetTerrain = (TerrainType)terrainValue;
                    TerrainType prevTerrain = cell.Terrain;
                    if (prevTerrain != targetTerrain)
                    {
                        if (LevelMap.NativeTerrainFilter.Contains(prevTerrain))
                        {
                            changedNativeTerrains.Add(prevTerrain);
                        }
                        if (LevelMap.NativeTerrainFilter.Contains(targetTerrain))
                        {
                            changedNativeTerrains.Add(targetTerrain);
                        }
                    }

                    fBrush.SetValue(paintContext, brushValue);

                    mPaintCell.Invoke(editorInstance, new object[] { cell, terrainValue, refreshCells, refreshGrass });
                    mFloodConn?.Invoke(editorInstance, new object[] { cell, prevTerrain });
                    if (prevTerrain == TerrainType.FloodPlain)
                    {
                        cell.RemoveAdditionalData<FloodplainDistanceData>();
                    }

                    refreshCells.Add(cell);
                    changedCoords.Add(cell.Coordinates);
                    if (targetTerrain == TerrainType.Marsh)
                    {
                        marshCells.Add(cell);
                    }
                    applied++;
                }
            }
            finally
            {
                try { fBrush.SetValue(paintContext, oldBrush); } catch { }
            }

            if (marshCells.Count > 0)
            {
                foreach (Cell marshCell in marshCells)
                {
                    foreach (StaggeredCellCoord neighbourCoord in Isometric.GetRingCoordsFromRange(marshCell.Coordinates, 1))
                    {
                        Cell neighbour = level.GetCell(neighbourCoord);
                        if (neighbour == null || (neighbour.Terrain != TerrainType.Water && neighbour.Terrain != TerrainType.FloodPlain))
                        {
                            continue;
                        }

                        TerrainType previousTerrain = neighbour.Terrain;
                        if (LevelMap.NativeTerrainFilter.Contains(previousTerrain))
                        {
                            changedNativeTerrains.Add(previousTerrain);
                        }
                        changedNativeTerrains.Add(TerrainType.Sand);

                        fBrush.SetValue(paintContext, TerrainType.Marsh);
                        mPaintCell.Invoke(editorInstance, new object[] { neighbour, TerrainType.Sand, refreshCells, refreshGrass });
                        mFloodConn?.Invoke(editorInstance, new object[] { neighbour, previousTerrain });
                        if (previousTerrain == TerrainType.FloodPlain)
                        {
                            neighbour.RemoveAdditionalData<FloodplainDistanceData>();
                        }

                        refreshCells.Add(neighbour);
                        changedCoords.Add(neighbour.Coordinates);
                    }
                }
            }

            try
            {
                int neighbourRange = 1;
                object editorSettings = AccessTools.Field(tEditor, "_editorSettings")?.GetValue(editorInstance);
                if (editorSettings != null)
                {
                    neighbourRange = Math.Max(1, (int)(AccessTools.Field(editorSettings.GetType(), "NeighbourCellUpdateRange")?.GetValue(editorSettings) ?? 1));
                }

                if (mBrushNeighbourUpdate != null && changedCoords.Count > 0)
                {
                    HashSet<StaggeredCellCoord> neighbourCoords = new HashSet<StaggeredCellCoord>(changedCoords);
                    foreach (StaggeredCellCoord coord in changedCoords)
                    {
                        for (int range = 1; range <= neighbourRange + 1; range++)
                        {
                            neighbourCoords.UnionWith(Isometric.GetRingCoordsFromRange(coord, range));
                        }
                    }

                    mBrushNeighbourUpdate.Invoke(editorInstance, new object[] { neighbourCoords, refreshCells });
                }

                mCliffInvalidPurge?.Invoke(editorInstance, new object[] { refreshCells });

                foreach (TerrainType terrainType in changedNativeTerrains)
                {
                    if (LevelMap.NativeTerrainFilter.Contains(terrainType))
                    {
                        GraphicTileInstancing.Instance.ReuploadComputeBufferGPU(level, terrainType);
                    }
                }

                if (refreshGrass.Count > 0)
                {
                    GraphicTileInstancing.Instance.MapEditor_RecomputeGrassPatchBuffer(refreshGrass);
                }

                object mapRenderer = AccessTools.Field(tEditor, "_mapRenderer")?.GetValue(mapEditorInstance);
                MethodInfo mRefreshCellTiles = mapRenderer != null ? AccessTools.Method(mapRenderer.GetType(), "RefreshCellTiles") : null;
                FieldInfo fPostJobWaterCells = AccessTools.Field(tEditor, "_postJobWaterCellsToRefresh");
                if (fPostJobWaterCells?.GetValue(editorInstance) is HashSet<Cell> postJobWaterCells)
                {
                    foreach (Cell cell in refreshCells)
                    {
                        if (cell.Terrain == TerrainType.FloodPlain)
                        {
                            bool surroundedByFloodplain = true;
                            foreach (StaggeredCellCoord squareCoord in Isometric.GetSquareCoordsFromRange(cell.Coordinates, 1))
                            {
                                Cell squareCell = level.GetCell(squareCoord);
                                if (squareCell == null || squareCell.Terrain != TerrainType.FloodPlain)
                                {
                                    surroundedByFloodplain = false;
                                    break;
                                }
                            }

                            if (surroundedByFloodplain)
                            {
                                cell.RemoveAdditionalData<GrassCellData>();
                            }
                        }

                        if (cell.Terrain == TerrainType.Water || cell.Terrain == TerrainType.FloodPlain)
                        {
                            postJobWaterCells.Add(cell);
                        }
                    }
                }

                mRefreshCellTiles?.Invoke(mapRenderer, new object[] { refreshCells });
                AccessTools.Method(tEditor, "ForceRefreshGrass")?.Invoke(mapEditorInstance, Array.Empty<object>());

                if (mStartAndWaitForPostPaintJob != null)
                {
                    Stopwatch wait = Stopwatch.StartNew();
                    while (wait.ElapsedMilliseconds < 5000)
                    {
                        bool pending = false;
                        try
                        {
                            pending = (bool)mStartAndWaitForPostPaintJob.Invoke(editorInstance, Array.Empty<object>());
                        }
                        catch
                        {
                            break;
                        }

                        if (!pending)
                        {
                            break;
                        }

                        System.Threading.Thread.Sleep(5);
                    }
                }

                ApplyForcedGrassPlain(level, forcedGrassCoords);
            }
            catch (Exception ex)
            {
                Log?.LogWarning($"Visual refresh failed (non-fatal): {ex.Message}");
            }

            AccessTools.Method(tEditor, "ComputeCurrentFlood")?.Invoke(mapEditorInstance, Array.Empty<object>());

            Log?.LogInfo($"OG terrain applied. items={arr.Count}, applied={applied}, skipped={skipped}");
        }

        private static string NormalizeOgTerrainName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            string s = raw.Trim();
            if (s.Contains("."))
            {
                s = s.Split('.').Last();
            }

            switch (s.ToLowerInvariant())
            {
                case "deep_water":
                case "shallow_water":
                    return "Water";
                case "floodplain":
                    return "FloodPlain";
                case "marshland":
                    return "Marsh";
                case "meadow":
                case "meadow_grass":
                    return "Meadow";
                case "grass":
                    return "Sand";
                case "road":
                case "road_grass":
                    return "Road";
                case "rock":
                case "rock_grass":
                    return "Rock";
                case "ore_rock":
                case "ore_rock_grass":
                    return "Ore";
                case "tree":
                    return "Tree";
                case "tree_grass":
                    return "Tree";
                case "sand":
                    return "Sand";
                case "dune":
                    return "Dune";
                case "cliff":
                    return "Cliff";
                default:
                    return s;
            }
        }

        private static OgTerrainImportSpec DescribeOgTerrain(string raw)
        {
            string normalized = NormalizeOgTerrainName(raw);
            bool forceGrassPlain = false;

            if (!string.IsNullOrWhiteSpace(raw))
            {
                string s = raw.Trim();
                if (s.Contains("."))
                {
                    s = s.Split('.').Last();
                }

                switch (s.ToLowerInvariant())
                {
                    case "grass":
                    case "tree_grass":
                        forceGrassPlain = true;
                        break;
                }
            }

            return new OgTerrainImportSpec
            {
                PaintTerrainName = normalized,
                ForceGrassPlain = forceGrassPlain
            };
        }

        private static void ApplyForcedGrassPlain(LevelMap level, IEnumerable<StaggeredCellCoord> coords)
        {
            if (level == null || coords == null)
            {
                return;
            }

            int applied = 0;
            foreach (StaggeredCellCoord coord in coords)
            {
                Cell cell = level.GetCell(coord);
                if (cell == null || cell.Terrain != TerrainType.Sand)
                {
                    continue;
                }

                EnvironementCellData environment = cell.GetAdditionalDataOrDefault<EnvironementCellData>();
                if (environment != null && environment.ShouldHideGrass())
                {
                    continue;
                }

                if (cell.ContainsAdditionalData<RoadCellData>())
                {
                    continue;
                }

                GrassCellData grass = cell.GetAdditionalDataOrDefault<GrassCellData>() ?? new GrassCellData(cell);
                grass.Mode = GrassCellData.GrassMode.Plain;
                grass.ComputedPlainLevel = Math.Max(grass.ComputedPlainLevel, 2);
                cell.UpsertAdditionalData(grass);
                applied++;
            }

            if (applied > 0)
            {
                GraphicTileInstancing.Instance.MapEditor_RefreshWholeGrass(level);
                Log?.LogInfo($"Applied forced OG grass plain on {applied} cells.");
            }
        }

        private sealed class OgTerrainImportSpec
        {
            public string PaintTerrainName { get; set; }
            public bool ForceGrassPlain { get; set; }
        }

        private static void TryApplyScenarioInfo(object mapEditorInstance, JObject root)
        {
            try
            {
                JObject info = root["scenario_info"] as JObject;
                if (info == null)
                {
                    Log?.LogInfo("No scenario_info found in extractor JSON.");
                    return;
                }

                object levelObj = AccessTools.Field(mapEditorInstance.GetType(), "_level")?.GetValue(mapEditorInstance);
                if (levelObj == null)
                {
                    Log?.LogWarning("MapEditor._level is null; cannot apply scenario info.");
                    return;
                }

                Type levelType = levelObj.GetType();

                string sourceFile = root["source_file"]?.Value<string>();
                string sourceName = !string.IsNullOrWhiteSpace(sourceFile) ? Path.GetFileNameWithoutExtension(sourceFile) : "OG Import";

                SetMember(levelObj, levelType, "MapName", sourceName);
                if (!string.IsNullOrWhiteSpace(sourceFile))
                {
                    SetMember(levelObj, levelType, "MapPath", sourceFile);
                }

                string subtitle = info["subtitle"]?.Value<string>();
                string briefDescription = info["brief_description"]?.Value<string>();
                if (!string.IsNullOrWhiteSpace(subtitle))
                {
                    SetMember(levelObj, levelType, "CityTagLine", subtitle);
                }
                if (!string.IsNullOrWhiteSpace(sourceName))
                {
                    SetMember(levelObj, levelType, "CityName", sourceName);
                }

                int startYear = ReadInt(info, "start_year");
                if (startYear != 0)
                {
                    SetMember(levelObj, levelType, "StartingYear", startYear);
                    SetMember(levelObj, levelType, "CurrentYear", startYear);
                }

                int initialFunds = ReadInt(info, "initial_funds");
                if (initialFunds != 0)
                {
                    SetMember(levelObj, levelType, "Treasury", initialFunds);
                }

                int rescueLoan = ReadInt(info, "rescue_loan");
                if (rescueLoan != 0)
                {
                    SetMember(levelObj, levelType, "RescueGift", rescueLoan);
                }

                JToken interestToken = info["debt_interest_rate"];
                if (interestToken != null && interestToken.Type != JTokenType.Null)
                {
                    float interest = interestToken.Value<float>();
                    SetMember(levelObj, levelType, "InterestRates", interest / 100f);
                }

                int playerRank = ReadInt(info, "player_rank");
                TrySetEnumMember(levelObj, levelType, "PharaohRank", playerRank);
                TrySetEnumMember(levelObj, levelType, "CurrentSalaryRank", playerRank);

                int currentPharaoh = ReadInt(info, "current_pharaoh");
                TrySetEnumMember(levelObj, levelType, "PharaohName", currentPharaoh);

                int enemyId = ReadInt(info, "enemy_id");
                TrySetEnumMember(levelObj, levelType, "PharaohEnemy", enemyId);

                int herdTypeAnimals = ReadInt(info, "herd_type_animals");
                TrySetEnumMember(levelObj, levelType, "PreyType", herdTypeAnimals);

                int altPredatorType = ReadInt(info, "alt_predator_type");
                TrySetEnumMember(levelObj, levelType, "PredatorType", altPredatorType);

                JObject env = info["env"] as JObject;
                if (env != null)
                {
                    int climate = ReadInt(env, "climate");
                    TrySetEnumMember(levelObj, levelType, "Climate", climate);
                }

                ApplyDeities(levelObj, levelType, info["known_gods"] as JArray);
                ApplyWinConditions(levelObj, levelType, info);
                ApplyBriefing(levelObj, levelType, briefDescription);

                Log?.LogInfo("scenario_info applied.");
            }
            catch (Exception ex)
            {
                Log?.LogWarning($"Scenario info import failed (non-fatal): {ex}");
            }
        }

        private static void ApplyBriefing(object levelObj, Type levelType, string briefDescription)
        {
            if (string.IsNullOrWhiteSpace(briefDescription))
            {
                return;
            }

            // New Era expects a localization key here, but plain text may still be useful in editor/runtime tests.
            SetMember(levelObj, levelType, "BriefingKey", briefDescription);
        }

        private static void TryApplyAvailability(object mapEditorInstance, JObject root)
        {
            try
            {
                object levelObj = AccessTools.Field(mapEditorInstance.GetType(), "_level")?.GetValue(mapEditorInstance);
                if (!(levelObj is LevelMap level))
                {
                    Log?.LogWarning("MapEditor._level is null; cannot import goods/buildings availability.");
                    return;
                }

                HashSet<Good> goods = new HashSet<Good>(level.AvailableGoods);
                CollectAvailableGoods(root, goods);

                HashSet<BuildingType> buildings = new HashSet<BuildingType>(level.AvailableBuildings);
                CollectAvailableBuildings(root, goods, buildings);
                AddSupportingGuildsForAllowedMonuments(level.WinCondition?.RequiredMonuments, buildings);
                CollectGoodsFromEnabledBuildings(buildings, goods);

                ApplyAvailableGoods(level, goods, root["trade_prices"] as JObject);
                ApplyAvailableBuildings(level, buildings);

                Log?.LogInfo($"Availability import applied. Goods={level.AvailableGoods.Count}, buildings={level.AvailableBuildings.Count()}");
            }
            catch (Exception ex)
            {
                Log?.LogWarning($"Availability import failed (non-fatal): {ex}");
            }
        }

        private static void TryApplyMapFlags(object mapEditorInstance, JObject root)
        {
            try
            {
                JObject info = root["scenario_info"] as JObject;
                if (info == null)
                {
                    return;
                }

                object levelObj = AccessTools.Field(mapEditorInstance.GetType(), "_level")?.GetValue(mapEditorInstance);
                if (!(levelObj is LevelMap level))
                {
                    Log?.LogWarning("MapEditor._level is null; cannot import map flags.");
                    return;
                }

                Type editorType = mapEditorInstance.GetType();
                MethodInfo instantiateFlag = AccessTools.Method(editorType, "InstantiateAndStoreFlag");
                object mapRenderer = AccessTools.Field(editorType, "_mapRenderer")?.GetValue(mapEditorInstance);
                MethodInfo refreshCellTile = mapRenderer != null ? AccessTools.Method(mapRenderer.GetType(), "RefreshCellTile") : null;
                FieldInfo flagGameObjectsField = AccessTools.Field(editorType, "_flagGameObjects");
                IDictionary flagGameObjects = flagGameObjectsField?.GetValue(mapEditorInstance) as IDictionary;
                bool instantiateVisuals = !((bool?)(AccessTools.Field(editorType, "_currentlyLoading")?.GetValue(mapEditorInstance)) ?? false);

                MapFlagType[] supportedTypes =
                {
                    MapFlagType.GroundEntry,
                    MapFlagType.GroundExit,
                    MapFlagType.RiverEntry,
                    MapFlagType.RiverExit,
                    MapFlagType.Predator,
                    MapFlagType.Prey,
                    MapFlagType.Fish,
                };

                foreach (MapFlagType flagType in supportedTypes)
                {
                    ClearImportedFlags(level, flagGameObjects, flagType);
                }

                int imported = 0;
                imported += ImportSingleFlag(level, flagGameObjects, instantiateFlag, refreshCellTile, mapRenderer, mapEditorInstance, MapFlagType.GroundEntry, info["entry_point"], instantiateVisuals);
                imported += ImportSingleFlag(level, flagGameObjects, instantiateFlag, refreshCellTile, mapRenderer, mapEditorInstance, MapFlagType.GroundExit, info["exit_point"], instantiateVisuals);
                imported += ImportSingleFlag(level, flagGameObjects, instantiateFlag, refreshCellTile, mapRenderer, mapEditorInstance, MapFlagType.RiverEntry, info["river_entry_point"], instantiateVisuals);
                imported += ImportSingleFlag(level, flagGameObjects, instantiateFlag, refreshCellTile, mapRenderer, mapEditorInstance, MapFlagType.RiverExit, info["river_exit_point"], instantiateVisuals);
                imported += ImportFlagArray(level, flagGameObjects, instantiateFlag, refreshCellTile, mapRenderer, mapEditorInstance, MapFlagType.Predator, info["predator_herd_points"] as JArray, instantiateVisuals);
                imported += ImportFlagArray(level, flagGameObjects, instantiateFlag, refreshCellTile, mapRenderer, mapEditorInstance, MapFlagType.Prey, info["prey_herd_points"] as JArray, instantiateVisuals);
                imported += ImportFlagArray(level, flagGameObjects, instantiateFlag, refreshCellTile, mapRenderer, mapEditorInstance, MapFlagType.Fish, info["fishing_points"] as JArray, instantiateVisuals);

                Log?.LogInfo($"Imported map flags from scenario_info. Count={imported}, instantiateVisuals={instantiateVisuals}");
            }
            catch (Exception ex)
            {
                Log?.LogWarning($"Map flag import failed (non-fatal): {ex}");
            }
        }

        private static void ClearImportedFlags(LevelMap level, IDictionary flagGameObjects, MapFlagType flagType)
        {
            if (level == null)
            {
                return;
            }

            if (!level.FlagCoordinates.TryGetValue(flagType, out List<StaggeredCellCoord> coords) || coords == null || coords.Count == 0)
            {
                return;
            }

            foreach (StaggeredCellCoord coord in coords.ToArray())
            {
                if (flagGameObjects != null && flagGameObjects.Contains(coord))
                {
                    if (flagGameObjects[coord] is GameObject go && go != null)
                    {
                        UnityEngine.Object.Destroy(go);
                    }
                    flagGameObjects.Remove(coord);
                }
            }

            coords.Clear();
        }

        private static int ImportSingleFlag(
            LevelMap level,
            IDictionary flagGameObjects,
            MethodInfo instantiateFlag,
            MethodInfo refreshCellTile,
            object mapRenderer,
            object mapEditorInstance,
            MapFlagType flagType,
            JToken pointToken,
            bool instantiateVisuals)
        {
            if (!TryGetScenarioPointCoord(pointToken, out StaggeredCellCoord coord))
            {
                return 0;
            }

            return AddImportedFlag(level, flagGameObjects, instantiateFlag, refreshCellTile, mapRenderer, mapEditorInstance, flagType, coord, instantiateVisuals) ? 1 : 0;
        }

        private static int ImportFlagArray(
            LevelMap level,
            IDictionary flagGameObjects,
            MethodInfo instantiateFlag,
            MethodInfo refreshCellTile,
            object mapRenderer,
            object mapEditorInstance,
            MapFlagType flagType,
            JArray points,
            bool instantiateVisuals)
        {
            if (points == null || points.Count == 0)
            {
                return 0;
            }

            int count = 0;
            foreach (JToken token in points)
            {
                if (!TryGetScenarioPointCoord(token, out StaggeredCellCoord coord))
                {
                    continue;
                }

                if (AddImportedFlag(level, flagGameObjects, instantiateFlag, refreshCellTile, mapRenderer, mapEditorInstance, flagType, coord, instantiateVisuals))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool AddImportedFlag(
            LevelMap level,
            IDictionary flagGameObjects,
            MethodInfo instantiateFlag,
            MethodInfo refreshCellTile,
            object mapRenderer,
            object mapEditorInstance,
            MapFlagType flagType,
            StaggeredCellCoord coord,
            bool instantiateVisuals)
        {
            if (level == null)
            {
                return false;
            }

            Cell cell = level.GetCell(coord);
            if (cell == null)
            {
                return false;
            }

            if (!level.FlagCoordinates.TryGetValue(flagType, out List<StaggeredCellCoord> coords) || coords == null)
            {
                coords = new List<StaggeredCellCoord>();
                level.FlagCoordinates[flagType] = coords;
            }

            if (coords.Contains(coord))
            {
                return false;
            }

            coords.Add(coord);

            if (flagType == MapFlagType.GroundEntry || flagType == MapFlagType.GroundExit)
            {
                if (!cell.ContainsAdditionalData<RoadCellData>())
                {
                    cell.UpsertAdditionalData(new RoadCellData(cell));
                }
                refreshCellTile?.Invoke(mapRenderer, new object[] { cell });
            }

            if (instantiateVisuals && flagGameObjects != null && !flagGameObjects.Contains(coord))
            {
                instantiateFlag?.Invoke(mapEditorInstance, new object[] { flagType, coord });
            }

            return true;
        }

        private static bool TryGetScenarioPointCoord(JToken token, out StaggeredCellCoord coord)
        {
            coord = default;
            if (!(token is JObject point))
            {
                return false;
            }

            JObject staggered = point["staggered"] as JObject;
            if (staggered != null)
            {
                int? sx = (int?)staggered["x"];
                int? sy = (int?)staggered["y"];
                if (sx.HasValue && sy.HasValue)
                {
                    coord = NormalizeImportedStaggeredCoord(sx.Value, sy.Value);
                    return true;
                }
            }

            int? x = (int?)point["x"];
            int? y = (int?)point["y"];
            if (!x.HasValue || !y.HasValue)
            {
                return false;
            }

            int staggeredX = x.Value;
            int staggeredY = y.Value;
            coord = NormalizeImportedStaggeredCoord(staggeredX, staggeredY);
            return true;
        }

        private static StaggeredCellCoord NormalizeImportedStaggeredCoord(int x, int y)
        {
            // Keep flag/scenario points aligned with the same staggered correction used for terrain cells.
            if ((y & 1) == 0)
            {
                x += 1;
            }

            return new StaggeredCellCoord(x, y);
        }

        private static void CollectAvailableGoods(JObject root, ISet<Good> goods)
        {
            if (root == null || goods == null)
            {
                return;
            }

            JObject allowedGoods = root["allowed_goods"] as JObject;
            bool importedFromAllowedGoods = false;
            foreach (Good good in EnumerateAllowedGoods(allowedGoods))
            {
                importedFromAllowedGoods = true;
                goods.Add(good);
            }

            JObject cityData = root["city_data"] as JObject;

            foreach (Good good in EnumerateGoods(cityData?["active_resource_names"] as JArray, cityData?["active_resource_ids"] as JArray))
            {
                goods.Add(good);
            }

            foreach (JObject foodSlot in (cityData?["food_types_available"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
            {
                Good? good = MapOgGoodToken(foodSlot["resource_name"]) ?? MapOgGoodToken(foodSlot["resource_id"]);
                if (good.HasValue)
                {
                    goods.Add(good.Value);
                }
            }

            foreach (JObject foodSlot in (cityData?["food_types_eaten"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
            {
                Good? good = MapOgGoodToken(foodSlot["resource_name"]) ?? MapOgGoodToken(foodSlot["resource_id"]);
                if (good.HasValue)
                {
                    goods.Add(good.Value);
                }
            }

            foreach (JObject provision in ((root["scenario_info"] as JObject)?["burial_provisions"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
            {
                if (TryMapOgGood(provision["resource_name"]?.Value<string>(), out Good good))
                {
                    goods.Add(good);
                }
            }

            foreach (JObject city in ((root["world_map"] as JObject)?["cities"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
            {
                foreach (Good good in EnumerateCityGoods(city))
                {
                    goods.Add(good);
                }
            }

            foreach (JObject ev in (((root["scenario_events"] as JObject)?["events"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>()))
            {
                foreach (Good eventGood in EnumerateOgEventGoods(ev, ev["item"] as JObject))
                {
                    goods.Add(eventGood);
                }
            }

            if (importedFromAllowedGoods)
            {
                Log?.LogDebug("Merged explicit allowed_goods with legacy good sources.");
            }
        }

        private static IEnumerable<Good> EnumerateAllowedGoods(JObject allowedGoods)
        {
            if (allowedGoods == null)
            {
                yield break;
            }

            HashSet<Good> yielded = new HashSet<Good>();

            foreach (JToken token in allowedGoods["resource_ids"] as JArray ?? new JArray())
            {
                Good? good = MapOgGoodToken(token);
                if (good.HasValue && yielded.Add(good.Value))
                {
                    yield return good.Value;
                }
            }

            foreach (JObject resource in (allowedGoods["resources"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
            {
                Good? good = MapOgGoodToken(resource["resource_id"]);
                if (good.HasValue && yielded.Add(good.Value))
                {
                    yield return good.Value;
                }
            }
        }

        private static IEnumerable<Good> EnumerateCityGoods(JObject city)
        {
            foreach (Good good in EnumerateGoods(city?["sells_resource_names"] as JArray, city?["sells_resource_ids"] as JArray))
            {
                yield return good;
            }

            foreach (Good good in EnumerateGoods(city?["buys_resource_names"] as JArray, city?["buys_resource_ids"] as JArray))
            {
                yield return good;
            }
        }

        private static IEnumerable<Good> EnumerateGoods(JArray names, JArray ids)
        {
            HashSet<Good> yielded = new HashSet<Good>();

            foreach (JToken token in names?.Children() ?? Enumerable.Empty<JToken>())
            {
                Good? good = MapOgGoodToken(token);
                if (good.HasValue && yielded.Add(good.Value))
                {
                    yield return good.Value;
                }
            }

            foreach (JToken token in ids?.Children() ?? Enumerable.Empty<JToken>())
            {
                Good? good = MapOgGoodToken(token);
                if (good.HasValue && yielded.Add(good.Value))
                {
                    yield return good.Value;
                }
            }
        }

        private static void ApplyAvailableGoods(LevelMap level, ISet<Good> goods, JObject tradePrices)
        {
            if (level == null || goods == null)
            {
                return;
            }

            foreach (Good good in goods.OrderBy(g => (int)g))
            {
                if (!level.AvailableGoods.Contains(good))
                {
                    level.AvailableGoods.Add(good);
                }
            }

            level.BuyableGoods.Clear();
            level.BuyableGoods.AddRange(level.AvailableGoods.Where(g => Merchandise.IsFood(g) || Merchandise.IsLuxuryGood(g) || g == Good.Beer || g == Good.Linen || g == Good.Pottery));

            foreach (Good good in level.AvailableGoods)
            {
                if (!level.GoodImportExportPrice.ContainsKey(good) && CommerceManager.DefaultGoodImportExportPrices.TryGetValue(good, out GoodPrice defaultPrice))
                {
                    level.GoodImportExportPrice[good] = new GoodPrice
                    {
                        BuyPrice = defaultPrice.BuyPrice,
                        SellPrice = defaultPrice.SellPrice
                    };
                }
            }

            foreach (JObject priceObj in (tradePrices?["prices"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
            {
                Good? good = MapOgGoodToken(priceObj["resource_name"]) ?? MapOgGoodToken(priceObj["resource_id"]);
                if (!good.HasValue || !level.AvailableGoods.Contains(good.Value))
                {
                    continue;
                }

                level.GoodImportExportPrice[good.Value] = new GoodPrice
                {
                    BuyPrice = ReadInt(priceObj, "buy_price"),
                    SellPrice = ReadInt(priceObj, "sell_price")
                };
            }
        }

        private static void CollectAvailableBuildings(JObject root, ISet<Good> goods, ISet<BuildingType> buildings)
        {
            if (root == null || buildings == null)
            {
                return;
            }

            foreach (BuildingType buildingType in EnumerateAllowedBuildingsFromScenario(root["scenario_info"] as JObject))
            {
                buildings.Add(buildingType);
            }

            List<BuildingType> allowedMonuments = EnumerateAllowedMonumentsFromScenario(root["scenario_info"] as JObject).Distinct().ToList();
            foreach (BuildingType buildingType in allowedMonuments)
            {
                buildings.Add(buildingType);
            }

            AddSupportingGuildsForAllowedMonuments(allowedMonuments, buildings);

            foreach (Good good in goods ?? Enumerable.Empty<Good>())
            {
                foreach (BuildingType buildingType in MapGoodToBuildings(good))
                {
                    buildings.Add(buildingType);
                }
            }

            JObject scenarioInfo = root["scenario_info"] as JObject;
            if (scenarioInfo != null)
            {
                if (((root["world_map"] as JObject)?["cities"] as JArray)?.Count > 0)
                {
                    buildings.Add(BuildingType.Bazaar);
                    buildings.Add(BuildingType.StorageYard);
                    buildings.Add(BuildingType.Granary);
                }

                if (HasSeaTradeRoute(root))
                {
                    buildings.Add(BuildingType.Dock);
                }

                if ((scenarioInfo["fishing_points"] as JArray)?.OfType<JObject>().Any() ?? false)
                {
                    buildings.Add(BuildingType.FishingWharf);
                    buildings.Add(BuildingType.Shipwright);
                }

                if ((scenarioInfo["known_gods"] as JArray) != null)
                {
                    foreach (string god in (scenarioInfo["known_gods"] as JArray).OfType<JObject>().Where(g => g["is_known"]?.Value<bool>() ?? false).Select(g => g["god"]?.Value<string>()))
                    {
                        foreach (BuildingType buildingType in MapGodToBuildings(god))
                        {
                            buildings.Add(buildingType);
                        }
                    }
                }
            }

        }

        private static IEnumerable<BuildingType> EnumerateAllowedBuildingsFromScenario(JObject scenarioInfo)
        {
            JObject allowedBuildings = scenarioInfo?["allowed_buildings"] as JObject;
            if (allowedBuildings == null)
            {
                yield break;
            }

            int playerRank = Math.Max(0, ReadInt(scenarioInfo, "player_rank"));
            HashSet<BuildingType> yielded = new HashSet<BuildingType>();
            foreach (JToken token in (allowedBuildings["enabled_ids"] as JArray)?.Children() ?? Enumerable.Empty<JToken>())
            {
                int ogIndex = token?.Value<int>() ?? -1;
                if (ogIndex == 25)
                {
                    BuildingType palaceTier = ResolveOgPalaceTier(playerRank);
                    if (yielded.Add(palaceTier))
                    {
                        yield return palaceTier;
                    }

                    continue;
                }

                if (ogIndex == 26)
                {
                    BuildingType mansionTier = ResolveOgMansionTier(playerRank);
                    if (yielded.Add(mansionTier))
                    {
                        yield return mansionTier;
                    }

                    continue;
                }

                if (!OgAllowedBuildingIndexToNewEraBuildings.TryGetValue(ogIndex, out BuildingType[] mappedBuildings) || mappedBuildings == null || mappedBuildings.Length == 0)
                {
                    Log?.LogInfo($"No New Era mapping yet for OG allowed-building id {ogIndex}.");
                    continue;
                }

                foreach (BuildingType buildingType in mappedBuildings)
                {
                    if (yielded.Add(buildingType))
                    {
                        yield return buildingType;
                    }
                }
            }
        }

        private static BuildingType ResolveOgPalaceTier(int playerRank)
        {
            // Akhenaten uses scenario_property_player_rank() to enable palace tiers:
            // rank < 6 => village, rank < 8 => town, otherwise city.
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

        private static BuildingType ResolveOgMansionTier(int playerRank)
        {
            // Akhenaten mirrors the same thresholds for mansion tiers:
            // rank < 6 => personal, rank < 8 => family, otherwise dynasty.
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

        private static IEnumerable<BuildingType> EnumerateAllowedMonumentsFromScenario(JObject scenarioInfo)
        {
            JObject monuments = scenarioInfo?["monuments"] as JObject;
            if (monuments == null)
            {
                yield break;
            }

            HashSet<int> emittedMonumentIds = new HashSet<int>();
            HashSet<BuildingType> yielded = new HashSet<BuildingType>();
            foreach (JToken token in (monuments["enabled_ids"] as JArray)?.Children() ?? Enumerable.Empty<JToken>())
            {
                int ogMonumentId = token?.Value<int>() ?? 0;
                if (ogMonumentId == 0 || !emittedMonumentIds.Add(ogMonumentId))
                {
                    continue;
                }

                if (!OgAllowedMonumentIdToNewEraBuildings.TryGetValue(ogMonumentId, out BuildingType[] mappedBuildings) || mappedBuildings == null || mappedBuildings.Length == 0)
                {
                    Log?.LogInfo($"No New Era mapping yet for OG monument id {ogMonumentId} ({GetOgMonumentName(ogMonumentId)}).");
                    continue;
                }

                foreach (BuildingType buildingType in mappedBuildings)
                {
                    if (yielded.Add(buildingType))
                    {
                        yield return buildingType;
                    }
                }
            }
        }

        private static void AddSupportingGuildsForAllowedMonuments(IEnumerable<BuildingType> allowedMonuments, ISet<BuildingType> buildings)
        {
            if (allowedMonuments == null || buildings == null)
            {
                return;
            }

            bool needsBricklayers = false;
            bool needsCarpenters = false;
            bool needsStonemasons = false;
            foreach (BuildingType monumentType in allowedMonuments)
            {
                if (MonumentNeedsBricklayers(monumentType))
                {
                    needsBricklayers = true;
                }

                if (MonumentNeedsCarpenters(monumentType))
                {
                    needsCarpenters = true;
                }

                if (MonumentNeedsStonemasons(monumentType))
                {
                    needsStonemasons = true;
                }
            }

            if (needsBricklayers)
            {
                buildings.Add(BuildingType.GuildBricklayers);
            }

            if (needsCarpenters)
            {
                buildings.Add(BuildingType.GuildCarpenters);
            }

            if (needsStonemasons)
            {
                buildings.Add(BuildingType.GuildStonemasons);
            }
        }

        private static bool MonumentNeedsBricklayers(BuildingType monumentType)
        {
            switch (monumentType)
            {
            case BuildingType.MastabaSmall:
            case BuildingType.MastabaMedium:
            case BuildingType.MastabaLarge:
            case BuildingType.PyramidBrickCoreSmall:
            case BuildingType.PyramidBrickCoreMedium:
            case BuildingType.PyramidBrickCoreLarge:
            case BuildingType.PyramidBrickCoreComplex:
            case BuildingType.PyramidBrickCoreGrandComplex:
                return true;
            default:
                return false;
            }
        }

        private static bool MonumentNeedsCarpenters(BuildingType monumentType)
        {
            switch (monumentType)
            {
            case BuildingType.MastabaSmall:
            case BuildingType.MastabaMedium:
            case BuildingType.MastabaLarge:
            case BuildingType.PyramidBentSmall:
            case BuildingType.PyramidBentMedium:
            case BuildingType.PyramidBrickCoreSmall:
            case BuildingType.PyramidBrickCoreMedium:
            case BuildingType.PyramidBrickCoreLarge:
            case BuildingType.PyramidBrickCoreComplex:
            case BuildingType.PyramidBrickCoreGrandComplex:
            case BuildingType.PyramidSteppedSmall:
            case BuildingType.PyramidSteppedMedium:
            case BuildingType.PyramidSteppedLarge:
            case BuildingType.PyramidSteppedComplex:
            case BuildingType.PyramidSteppedGrandComplex:
            case BuildingType.PyramidTrueSmall:
            case BuildingType.PyramidTrueMedium:
            case BuildingType.PyramidTrueLarge:
            case BuildingType.PyramidTrueComplex:
            case BuildingType.PyramidTrueGrandComplex:
            case BuildingType.MausoleumA:
            case BuildingType.MausoleumB:
            case BuildingType.MausoleumC:
            case BuildingType.RoyalTombSmall:
            case BuildingType.RoyalTombMedium:
            case BuildingType.RoyalTombLarge:
            case BuildingType.RoyalTombGrand:
            case BuildingType.SunTemple:
            case BuildingType.AbuSimbel:
            case BuildingType.PharaohsLighthouse:
            case BuildingType.AlexandriasLibrary:
            case BuildingType.Caesareum:
                return true;
            default:
                return false;
            }
        }

        private static bool MonumentNeedsStonemasons(BuildingType monumentType)
        {
            switch (monumentType)
            {
            case BuildingType.PyramidBentSmall:
            case BuildingType.PyramidBentMedium:
            case BuildingType.PyramidSteppedSmall:
            case BuildingType.PyramidSteppedMedium:
            case BuildingType.PyramidSteppedLarge:
            case BuildingType.PyramidSteppedComplex:
            case BuildingType.PyramidSteppedGrandComplex:
            case BuildingType.PyramidTrueSmall:
            case BuildingType.PyramidTrueMedium:
            case BuildingType.PyramidTrueLarge:
            case BuildingType.PyramidTrueComplex:
            case BuildingType.PyramidTrueGrandComplex:
            case BuildingType.ObeliskSmall:
            case BuildingType.ObeliskLarge:
            case BuildingType.Sphinx:
            case BuildingType.SunTemple:
            case BuildingType.MausoleumA:
            case BuildingType.MausoleumB:
            case BuildingType.MausoleumC:
            case BuildingType.RoyalTombSmall:
            case BuildingType.RoyalTombMedium:
            case BuildingType.RoyalTombLarge:
            case BuildingType.RoyalTombGrand:
            case BuildingType.PharaohsLighthouse:
            case BuildingType.AlexandriasLibrary:
            case BuildingType.Caesareum:
            case BuildingType.AbuSimbel:
                return true;
            default:
                return false;
            }
        }

        private static void CollectGoodsFromEnabledBuildings(IEnumerable<BuildingType> buildings, ISet<Good> goods)
        {
            if (buildings == null || goods == null)
            {
                return;
            }

            foreach (BuildingType buildingType in buildings)
            {
                if (!NewEraLinkedBuildingsToGoods.TryGetValue(buildingType, out Good[] linkedGoods) || linkedGoods == null)
                {
                    continue;
                }

                foreach (Good good in linkedGoods)
                {
                    goods.Add(good);
                }
            }
        }

        private static bool HasSeaTradeRoute(JObject root)
        {
            return ((root["world_map"] as JObject)?["cities"] as JArray)?.OfType<JObject>().Any(city =>
                string.Equals(city["route_type_name"]?.Value<string>(), "sea", StringComparison.OrdinalIgnoreCase)
                || (city["is_sea_trade"]?.Value<bool?>() ?? false)) ?? false;
        }

        private static void ApplyAvailableBuildings(LevelMap level, ISet<BuildingType> desiredBuildings)
        {
            if (level == null || desiredBuildings == null)
            {
                return;
            }

            foreach (BuildingType buildingType in desiredBuildings)
            {
                if (!level.BuildingStates.ContainsKey(buildingType))
                {
                    level.BuildingStates.Add(buildingType, MapBuildingState.Available);
                    continue;
                }

                if (level.BuildingStates[buildingType] == MapBuildingState.Unavailable)
                {
                    level.BuildingStates[buildingType] = MapBuildingState.Available;
                }
            }

            if (desiredBuildings.Contains(BuildingType.FoodWorkCamp) && level.BuildingStates.TryGetValue(BuildingType.MonumentsWorkCamp, out MapBuildingState monumentState) && monumentState == MapBuildingState.Unavailable)
            {
                level.BuildingStates[BuildingType.MonumentsWorkCamp] = MapBuildingState.Available;
            }
        }

        private static Dictionary<BuildingType, Good[]> BuildReverseGoodBuildingLinks()
        {
            Dictionary<BuildingType, HashSet<Good>> reverse = new Dictionary<BuildingType, HashSet<Good>>();

            foreach (KeyValuePair<Good, BuildingType[]> kvp in NewEraLinkedGoodsToBuildings)
            {
                if (kvp.Value == null)
                {
                    continue;
                }

                foreach (BuildingType buildingType in kvp.Value)
                {
                    if (!reverse.TryGetValue(buildingType, out HashSet<Good> goods))
                    {
                        goods = new HashSet<Good>();
                        reverse.Add(buildingType, goods);
                    }

                    goods.Add(kvp.Key);
                }
            }

            return reverse.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.OrderBy(g => (int)g).ToArray());
        }

        private static IEnumerable<BuildingType> MapGoodToBuildings(Good good)
        {
            if (NewEraLinkedGoodsToBuildings.TryGetValue(good, out BuildingType[] linkedBuildings) && linkedBuildings != null)
            {
                foreach (BuildingType buildingType in linkedBuildings)
                {
                    yield return buildingType;
                }
            }
        }

        private static string GetOgMonumentName(int ogMonumentId)
        {
            return OgMonumentIdNames.TryGetValue(ogMonumentId, out string name) ? name : $"Monument {ogMonumentId}";
        }

        private static IEnumerable<BuildingType> MapGodToBuildings(string rawGod)
        {
            string god = rawGod?.Trim().ToLowerInvariant();
            switch (god)
            {
                case "bast":
                    yield return BuildingType.ShrineBast;
                    yield return BuildingType.TempleBast;
                    yield return BuildingType.TempleComplexBast;
                    yield break;
                case "osiris":
                    yield return BuildingType.ShrineOsiris;
                    yield return BuildingType.TempleOsiris;
                    yield return BuildingType.TempleComplexOsiris;
                    yield break;
                case "ptah":
                    yield return BuildingType.ShrinePtah;
                    yield return BuildingType.TemplePtah;
                    yield return BuildingType.TempleComplexPtah;
                    yield break;
                case "ra":
                    yield return BuildingType.ShrineRa;
                    yield return BuildingType.TempleRa;
                    yield return BuildingType.TempleComplexRa;
                    yield break;
                case "seth":
                    yield return BuildingType.ShrineSeth;
                    yield return BuildingType.TempleSeth;
                    yield return BuildingType.TempleComplexSeth;
                    yield break;
            }
        }

        private static void TryApplyWorldMap(object mapEditorInstance, JObject root)
        {
            try
            {
                JObject worldMap = root["world_map"] as JObject;
                JObject allowedCities = root["allowed_cities"] as JObject;
                JArray importedCities = allowedCities?["cities"] as JArray;
                bool usingAllowedCities = importedCities != null && importedCities.Count > 0;
                if ((importedCities == null || importedCities.Count == 0) && worldMap != null)
                {
                    importedCities = worldMap["cities"] as JArray;
                }

                if ((importedCities == null || importedCities.Count == 0) && worldMap?["empire_map_objects"] is JObject empireMapObjects)
                {
                    importedCities = empireMapObjects["cities"] as JArray;
                }

                if (importedCities == null || importedCities.Count == 0)
                {
                    Log?.LogInfo("No allowed city data found in extractor JSON.");
                    return;
                }

                object levelObj = AccessTools.Field(mapEditorInstance.GetType(), "_level")?.GetValue(mapEditorInstance);
                if (!(levelObj is LevelMap level))
                {
                    Log?.LogWarning("MapEditor._level is null; cannot import world map.");
                    return;
                }

                IList cities = AccessTools.Field(mapEditorInstance.GetType(), "_cities")?.GetValue(mapEditorInstance) as IList;
                if (cities == null || cities.Count == 0)
                {
                    Log?.LogWarning("MapEditor._cities is unavailable; cannot import world map.");
                    return;
                }

                PropertyInfo mapCityStatesProperty = AccessTools.Property(levelObj.GetType(), "MapCityStates");
                Dictionary<int, WorldMapCityState> mapCityStates = mapCityStatesProperty?.GetValue(levelObj, null) as Dictionary<int, WorldMapCityState>;
                if (mapCityStates == null)
                {
                    mapCityStates = new Dictionary<int, WorldMapCityState>();
                    if (mapCityStatesProperty != null && mapCityStatesProperty.CanWrite)
                    {
                        mapCityStatesProperty.SetValue(levelObj, mapCityStates, null);
                    }
                }

                Dictionary<int, JObject> routesById = ((worldMap?["empire_map_routes"] as JObject)?["routes"] as JArray)?
                    .OfType<JObject>()
                    .Where(route => route != null)
                    .ToDictionary(route => ReadInt(route, "route_id"), route => route)
                    ?? new Dictionary<int, JObject>();

                ResetWorldMapCities(cities);

                List<NewEraCityCandidate> cityCandidates = BuildNewEraCityCandidates(cities);
                HashSet<int> assignedCityIds = new HashSet<int>();
                int importedCount = 0;

                foreach (JObject importedCity in importedCities.OfType<JObject>())
                {
                    if (importedCity == null)
                    {
                        continue;
                    }

                    int ogCityNameId = ReadInt(importedCity, "city_name_id");
                    int newEraCityId = ResolveNewEraCityIdFromOgCityNameId(ogCityNameId, cityCandidates);
                    if (newEraCityId == ScriptedEvent.s_InvalidId || newEraCityId < 0 || newEraCityId >= cities.Count)
                    {
                        continue;
                    }

                    if (!assignedCityIds.Add(newEraCityId))
                    {
                        Log?.LogWarning($"Skipping duplicate world-map city mapping for OG city {ogCityNameId}: New Era slot {newEraCityId} was already assigned.");
                        continue;
                    }

                    object cityObj = cities[newEraCityId];
                    if (cityObj == null)
                    {
                        continue;
                    }

                    WorldMapCityState state = cityObj.GetType().GetProperty("State")?.GetValue(cityObj, null) as WorldMapCityState;
                    if (state == null)
                    {
                        continue;
                    }

                    JObject routeObj = null;
                    int routeId = ReadInt(importedCity, "route_id", "trade_route_id");
                    if (routeId >= 0)
                    {
                        routesById.TryGetValue(routeId, out routeObj);
                    }

                    ApplyImportedCityState(level, cityCandidates, importedCity, routeObj, newEraCityId, state);

                    mapCityStates[newEraCityId] = state;
                    AccessTools.Method(cityObj.GetType(), "UpdateDisplay", new[] { typeof(bool) })?.Invoke(cityObj, new object[] { true });
                    importedCount++;
                }

                EnsureUniqueWorldMapStatuses(mapCityStates);
                RebuildMapCityStates(level, cities, mapCityStates);

                foreach (object cityObj in cities)
                {
                    if (cityObj == null)
                    {
                        continue;
                    }

                    AccessTools.Method(cityObj.GetType(), "UpdateDisplay", new[] { typeof(bool) })?.Invoke(cityObj, new object[] { true });
                }

                Log?.LogInfo($"Imported {importedCount} world-map cities from {(usingAllowedCities ? "OG allowed city data" : "legacy world_map data")}.");
            }
            catch (Exception ex)
            {
                Log?.LogWarning($"World-map import failed (non-fatal): {ex}");
            }
        }

        private static void ResetWorldMapCities(IList cities)
        {
            if (cities == null)
            {
                return;
            }

            for (int i = 0; i < cities.Count; i++)
            {
                object cityObj = cities[i];
                if (cityObj == null)
                {
                    continue;
                }

                WorldMapCityState state = cityObj.GetType().GetProperty("State")?.GetValue(cityObj, null) as WorldMapCityState;
                if (state == null)
                {
                    continue;
                }

                state.CityId = i;
                state.Enabled = false;
                state.Status = CityStatus.ForeignCity;
                state.CanTrade = false;
                state.TradeFromLand = true;
                state.OpenTradeRoutePrice = 0;
                state.TradeMerchandises.Clear();
                state.YearlyTradedVolume?.Clear();
            }
        }

        private static void ApplyImportedCityState(LevelMap level, IList<NewEraCityCandidate> cityCandidates, JObject importedCity, JObject routeObj, int newEraCityId, WorldMapCityState state)
        {
            bool enabled = importedCity["in_use"]?.Value<bool?>() ?? true;
            bool isOpen = ReadBool(importedCity, "is_open", "trade_route_open");
            int cityType = ReadInt(importedCity, "city_type");
            bool isSeaTrade = importedCity["is_sea_trade"]?.Value<bool?>() ?? false;
            int routeType = routeObj == null ? ReadInt(importedCity, "route_type") : ReadInt(routeObj, "route_type");
            int openTradeRoutePrice = Math.Max(0, ReadInt(importedCity, "cost_to_open", "trade_route_cost"));

            state.CityId = newEraCityId;
            state.Enabled = enabled;
            state.Status = MapOgEmpireCityStatus(cityType);
            state.CanTrade = IsOgTradingCityType(cityType) || HasImportedTradeGoods(importedCity);
            state.TradeFromLand = routeType == 2 ? false : !isSeaTrade;
            state.OpenTradeRoutePrice = isOpen ? 0 : openTradeRoutePrice;

            NewEraCityCandidate candidate = cityCandidates?.FirstOrDefault(c => c.CityId == newEraCityId);
            if (candidate != null && !string.IsNullOrWhiteSpace(candidate.Term))
            {
                state.CityTermName = candidate.Term;
            }

            state.TradeMerchandises.Clear();
            state.YearlyTradedVolume?.Clear();
            ApplyImportedTradeGoods(importedCity, state);
            state.PurgeUnusedTradedRessources(level);
        }

        private static void ApplyImportedTradeGoods(JObject importedCity, WorldMapCityState state)
        {
            if (importedCity == null || state == null)
            {
                return;
            }

            AddImportedTradeGoods(importedCity["sells_resource_names"] as JArray, importedCity["sells_resource_ids"] as JArray, TradeMode.CityExport, state);
            AddImportedTradeGoods(importedCity["buys_resource_names"] as JArray, importedCity["buys_resource_ids"] as JArray, TradeMode.CityImport, state);
        }

        private static void AddImportedTradeGoods(JArray names, JArray ids, TradeMode tradeMode, WorldMapCityState state)
        {
            HashSet<Good> seenGoods = new HashSet<Good>();

            if (names != null)
            {
                foreach (JToken token in names)
                {
                    Good? good = MapOgGoodToken(token);
                    if (!good.HasValue || !seenGoods.Add(good.Value))
                    {
                        continue;
                    }

                    state.TradeMerchandises.Add(new TradeMerchandise
                    {
                        Good = good.Value,
                        TradeMode = tradeMode,
                        TradeVolume = TradeVolume.Medium
                    });
                }
            }

            if (ids != null)
            {
                foreach (JToken token in ids)
                {
                    Good? good = MapOgGoodToken(token);
                    if (!good.HasValue || !seenGoods.Add(good.Value))
                    {
                        continue;
                    }

                    state.TradeMerchandises.Add(new TradeMerchandise
                    {
                        Good = good.Value,
                        TradeMode = tradeMode,
                        TradeVolume = TradeVolume.Medium
                    });
                }
            }
        }

        private static bool HasImportedTradeGoods(JObject importedCity)
        {
            return (importedCity?["sells_resource_ids"] as JArray)?.Count > 0
                || (importedCity?["buys_resource_ids"] as JArray)?.Count > 0
                || (importedCity?["sells_resource_names"] as JArray)?.Count > 0
                || (importedCity?["buys_resource_names"] as JArray)?.Count > 0;
        }

        private static int ResolveNewEraCityIdFromOgCityNameId(int ogCityNameId, IList<NewEraCityCandidate> cityCandidates)
        {
            if (ogCityNameId < 0 || cityCandidates == null || cityCandidates.Count == 0)
            {
                return ScriptedEvent.s_InvalidId;
            }

            if (!OgEmpireCityNames.TryGetValue(ogCityNameId, out string[] rawAliases) || rawAliases == null || rawAliases.Length == 0)
            {
                Log?.LogWarning($"No OG city-name mapping known for city id {ogCityNameId}.");
                return ScriptedEvent.s_InvalidId;
            }

            HashSet<string> aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string alias in rawAliases)
            {
                AddNormalizedAlias(aliases, alias);
            }

            if (ogCityNameId == 52)
            {
                AddNormalizedAlias(aliases, "Pelusion");
            }
            else if (ogCityNameId == 61)
            {
                AddNormalizedAlias(aliases, "Siwa Oasis");
            }

            List<NewEraCityCandidate> matches = cityCandidates
                .Where(c => c.Aliases.Overlaps(aliases))
                .ToList();

            if (matches.Count == 1)
            {
                return matches[0].CityId;
            }

            if (matches.Count > 1)
            {
                NewEraCityCandidate preferred = matches
                    .FirstOrDefault(c => aliases.Contains(c.DisplayNameNormalized) || aliases.Contains(c.TermNormalized));
                return (preferred ?? matches[0]).CityId;
            }

            Log?.LogWarning($"Could not map OG city {ogCityNameId} ({string.Join(" / ", rawAliases)}) to any New Era world-map city.");
            return ScriptedEvent.s_InvalidId;
        }

        private static CityStatus MapOgEmpireCityStatus(int cityType)
        {
            switch (cityType)
            {
                case 0:
                    return CityStatus.MyCity;
                case 1:
                case 2:
                    return CityStatus.PharaohCity;
                case 3:
                case 4:
                    return CityStatus.EgyptianCity;
                case 5:
                case 6:
                default:
                    return CityStatus.ForeignCity;
            }
        }

        private static bool IsOgTradingCityType(int cityType)
        {
            switch (cityType)
            {
                case 1:
                case 3:
                case 5:
                    return true;
                default:
                    return false;
            }
        }

        private static void EnsureUniqueWorldMapStatuses(Dictionary<int, WorldMapCityState> mapCityStates)
        {
            if (mapCityStates == null || mapCityStates.Count == 0)
            {
                return;
            }

            bool foundMyCity = false;
            bool foundPharaohCity = false;
            foreach (KeyValuePair<int, WorldMapCityState> kvp in mapCityStates.OrderBy(kvp => kvp.Key).ToList())
            {
                WorldMapCityState state = kvp.Value;
                if (state == null || !state.Enabled)
                {
                    continue;
                }

                if (state.Status == CityStatus.MyCity)
                {
                    if (!foundMyCity)
                    {
                        foundMyCity = true;
                    }
                    else
                    {
                        state.Status = CityStatus.EgyptianCity;
                    }
                }
                else if (state.Status == CityStatus.PharaohCity)
                {
                    if (!foundPharaohCity)
                    {
                        foundPharaohCity = true;
                    }
                    else
                    {
                        state.Status = CityStatus.EgyptianCity;
                    }
                }
            }
        }

        private static void RebuildMapCityStates(LevelMap level, IList cities, Dictionary<int, WorldMapCityState> mapCityStates)
        {
            if (level == null || cities == null || mapCityStates == null)
            {
                return;
            }

            mapCityStates.Clear();
            for (int i = 0; i < cities.Count; i++)
            {
                object cityObj = cities[i];
                if (cityObj == null)
                {
                    continue;
                }

                WorldMapCityState state = cityObj.GetType().GetProperty("State")?.GetValue(cityObj, null) as WorldMapCityState;
                if (state == null || !state.Enabled)
                {
                    continue;
                }

                state.CityId = i;
                state.PurgeUnusedTradedRessources(level);
                mapCityStates[i] = state;
            }
        }

        private static void ApplyDeities(object levelObj, Type levelType, JArray knownGods)
        {
            if (knownGods == null)
            {
                return;
            }

            object localDeitiesObj = GetMember(levelObj, levelType, "LocalDeities");
            if (!(localDeitiesObj is System.Collections.IList localDeities))
            {
                return;
            }

            localDeities.Clear();
            Type deityType = localDeities.GetType().GetGenericArguments().FirstOrDefault();
            if (deityType == null)
            {
                return;
            }

            object firstKnown = null;
            foreach (JObject god in knownGods.OfType<JObject>())
            {
                if (!(god["is_known"]?.Value<bool>() ?? false))
                {
                    continue;
                }

                string raw = god["god"]?.Value<string>();
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                string mapped = MapOgGodName(raw);
                try
                {
                    object deity = Enum.Parse(deityType, mapped, true);
                    localDeities.Add(deity);
                    if (firstKnown == null)
                    {
                        firstKnown = deity;
                    }
                }
                catch
                {
                }
            }

            if (firstKnown != null)
            {
                SetMember(levelObj, levelType, "PatronDeity", firstKnown);
            }
        }

        private static void ApplyWinConditions(object levelObj, Type levelType, JObject info)
        {
            object winConditionObj = GetMember(levelObj, levelType, "WinCondition");
            if (winConditionObj == null)
            {
                return;
            }

            Type winConditionType = winConditionObj.GetType();
            JObject winCriteria = info["win_criteria"] as JObject;
            if (winCriteria != null)
            {
                SetEnabledGoal(winConditionObj, winConditionType, winCriteria, "population", "Population");
                SetEnabledGoal(winConditionObj, winConditionType, winCriteria, "housing_level", "HouseLevel");
                SetEnabledGoal(winConditionObj, winConditionType, winCriteria, "housing_count", "HouseQuantity");
                SetEnabledGoal(winConditionObj, winConditionType, winCriteria, "kingdom", "KingdomRating");
                SetEnabledGoal(winConditionObj, winConditionType, winCriteria, "prosperity", "ProsperityRating");
                SetEnabledGoal(winConditionObj, winConditionType, winCriteria, "culture", "CultureRating");
                SetMember(winConditionObj, winConditionType, "SandboxMode", !(winCriteria.Properties().Any(p => p.Value?["enabled"]?.Value<bool>() ?? false)));
            }

            ApplyRequiredMonuments(levelObj, levelType, winConditionObj, winConditionType, info);

            JArray burial = info["burial_provisions"] as JArray;
            if (burial != null)
            {
                ApplyBurialGoods(winConditionObj, winConditionType, burial);
            }
        }

        private static void ApplyRequiredMonuments(object levelObj, Type levelType, object winConditionObj, Type winConditionType, JObject info)
        {
            object requiredMonumentsObj = GetMember(winConditionObj, winConditionType, "RequiredMonuments");
            if (!(requiredMonumentsObj is System.Collections.IList requiredMonuments))
            {
                return;
            }

            List<BuildingType> importedMonuments = EnumerateRequiredMonuments(info).Distinct().ToList();
            requiredMonuments.Clear();
            foreach (BuildingType monumentType in importedMonuments)
            {
                requiredMonuments.Add(monumentType);
            }

            if (importedMonuments.Count > 0)
            {
                SetMember(levelObj, levelType, "MonumentPreset", ChooseMonumentPreset(importedMonuments));
            }

            RecomputeMonumentWinConditionScores(winConditionObj, winConditionType, importedMonuments);
        }

        private static IEnumerable<BuildingType> EnumerateRequiredMonuments(JObject info)
        {
            JObject monuments = info?["monuments"] as JObject;
            if (monuments == null)
            {
                yield break;
            }

            HashSet<int> seenOgIds = new HashSet<int>();
            HashSet<BuildingType> yielded = new HashSet<BuildingType>();
            foreach (JToken token in (monuments["enabled_ids"] as JArray)?.Children() ?? Enumerable.Empty<JToken>())
            {
                int ogMonumentId = token?.Value<int>() ?? 0;
                if (ogMonumentId == 0 || !seenOgIds.Add(ogMonumentId))
                {
                    continue;
                }

                if (!OgAllowedMonumentIdToNewEraBuildings.TryGetValue(ogMonumentId, out BuildingType[] mappedBuildings) || mappedBuildings == null || mappedBuildings.Length == 0)
                {
                    Log?.LogInfo($"Skipping OG monument win condition id {ogMonumentId} ({GetOgMonumentName(ogMonumentId)}), no New Era counterpart found.");
                    continue;
                }

                foreach (BuildingType buildingType in mappedBuildings)
                {
                    if (yielded.Add(buildingType))
                    {
                        yield return buildingType;
                    }
                }
            }
        }

        private static MonumentPresetType ChooseMonumentPreset(IEnumerable<BuildingType> monuments)
        {
            HashSet<BuildingType> set = new HashSet<BuildingType>(monuments ?? Enumerable.Empty<BuildingType>());
            if (set.Contains(BuildingType.PharaohsLighthouse) || set.Contains(BuildingType.AlexandriasLibrary) || set.Contains(BuildingType.Caesareum))
            {
                return MonumentPresetType.Alexandria;
            }

            if (set.Contains(BuildingType.AbuSimbel))
            {
                return MonumentPresetType.AbuSimbel;
            }

            if (set.Contains(BuildingType.RoyalTombSmall) || set.Contains(BuildingType.RoyalTombMedium) || set.Contains(BuildingType.RoyalTombLarge) || set.Contains(BuildingType.RoyalTombGrand))
            {
                return MonumentPresetType.ValleyOfTheKings;
            }

            return MonumentPresetType.Pyramids;
        }

        private static void RecomputeMonumentWinConditionScores(object winConditionObj, Type winConditionType, IEnumerable<BuildingType> monuments)
        {
            int totalRating = 0;
            Type monumentComponentType = AccessTools.TypeByName("Monument");
            Dictionary<BuildingType, GameObject> buildingPrefabs = GlobalAccessor.GlobalSettings?.GameplaySettings?.BuildingPrefabs;
            if (buildingPrefabs != null && monumentComponentType != null)
            {
                foreach (BuildingType monumentType in monuments ?? Enumerable.Empty<BuildingType>())
                {
                    if (!buildingPrefabs.TryGetValue(monumentType, out GameObject prefab) || prefab == null)
                    {
                        continue;
                    }

                    Component monumentComponent = prefab.GetComponent(monumentComponentType);
                    object ratingScoreObj = monumentComponent != null ? GetMember(monumentComponent, monumentComponentType, "RatingScore") : null;
                    if (ratingScoreObj is int ratingScore)
                    {
                        totalRating += ratingScore;
                    }
                }
            }

            SetMember(winConditionObj, winConditionType, "MonumentsRating", Mathf.Clamp(totalRating, 0, 100));
            SetMember(winConditionObj, winConditionType, "MonumentScoreRatio", totalRating <= 0 ? 1f : Mathf.Min(100f / (float)totalRating, 1f));
        }

        private static void ApplyBurialGoods(object winConditionObj, Type winConditionType, JArray burial)
        {
            object burialGoodsObj = GetMember(winConditionObj, winConditionType, "BurialGoods");
            if (!(burialGoodsObj is System.Collections.IList burialGoods))
            {
                return;
            }

            burialGoods.Clear();

            int importedCount = 0;
            foreach (JObject provision in burial?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
            {
                Good? mappedGood = MapOgGoodToken(provision["resource_name"]) ?? MapOgGoodToken(provision["resource_id"]);
                if (!mappedGood.HasValue || mappedGood.Value == Good.Gold)
                {
                    continue;
                }

                int required = ReadInt(provision, "required");
                if (required <= 0)
                {
                    continue;
                }

                int dispatched = Math.Max(0, ReadInt(provision, "dispatched"));
                burialGoods.Add(new BurialGood
                {
                    Good = mappedGood.Value,
                    Quantity = required,
                    WasSentToTomb = dispatched >= required
                });

                importedCount++;
                if (importedCount >= MonumentPresets.MaxBurialGoods)
                {
                    Log?.LogWarning($"Burial goods truncated to {MonumentPresets.MaxBurialGoods} entries because the New Era editor UI does not support more slots.");
                    break;
                }
            }
        }

        private static void SetEnabledGoal(object target, Type targetType, JObject criteria, string criteriaName, string memberName)
        {
            JObject section = criteria[criteriaName] as JObject;
            if (section == null)
            {
                return;
            }

            bool enabled = section["enabled"]?.Value<bool>() ?? false;
            int goal = section["goal"]?.Value<int>() ?? 0;
            SetMember(target, targetType, memberName, enabled ? goal : 0);
        }

        private static string MapOgGodName(string raw)
        {
            switch (raw.Trim().ToLowerInvariant())
            {
                case "seth":
                case "set":
                    return "Seth";
                case "osiris":
                    return "Osiris";
                case "ra":
                    return "Ra";
                case "ptah":
                    return "Ptah";
                case "bast":
                case "bastet":
                    return "Bast";
                default:
                    return raw;
            }
        }

        private static void TrySetEnumMember(object target, Type targetType, string memberName, int rawValue)
        {
            if (rawValue < 0)
            {
                return;
            }

            FieldInfo field = FindField(targetType, memberName);
            if (field != null && field.FieldType.IsEnum)
            {
                try
                {
                    object value = Enum.ToObject(field.FieldType, rawValue);
                    field.SetValue(target, value);
                }
                catch
                {
                }
                return;
            }

            PropertyInfo property = FindProperty(targetType, memberName);
            if (property != null && property.CanWrite && property.PropertyType.IsEnum)
            {
                try
                {
                    object value = Enum.ToObject(property.PropertyType, rawValue);
                    property.SetValue(target, value, null);
                }
                catch
                {
                }
            }
        }

        private static void TryApplyEvents(object mapEditorInstance, JObject root)
        {
            try
            {
                JObject scenarioEvents = root["scenario_events"] as JObject;
                JArray events = scenarioEvents?["events"] as JArray;
                if (events == null || events.Count == 0)
                {
                    Log?.LogInfo("No scenario events found in extractor JSON.");
                    return;
                }

                object levelObj = AccessTools.Field(mapEditorInstance.GetType(), "_level")?.GetValue(mapEditorInstance);
                if (levelObj == null)
                {
                    Log?.LogWarning("MapEditor._level is null; cannot import events.");
                    return;
                }

                FieldInfo templateEventsField = AccessTools.Field(levelObj.GetType(), "TemplateEvents");
                if (templateEventsField == null)
                {
                    Log?.LogWarning("LevelMap.TemplateEvents field not found.");
                    return;
                }

                var templateEvents = templateEventsField.GetValue(levelObj) as IList<ScriptedEvent>;
                if (templateEvents == null)
                {
                    Log?.LogWarning("TemplateEvents list unavailable.");
                    return;
                }

                templateEvents.Clear();

                int imported = 0;
                HashSet<int> usedEventIds = new HashSet<int>();
                Dictionary<int, int> ogToImportedEventId = new Dictionary<int, int>();
                List<ImportedOgEvent> importedEvents = new List<ImportedOgEvent>();
                foreach (JObject ev in events.OfType<JObject>())
                {
                    ScriptedEvent scripted = ConvertOgEvent(ev, imported, usedEventIds, out int rawEventId);
                    if (scripted == null)
                    {
                        continue;
                    }

                    templateEvents.Add(scripted);
                    importedEvents.Add(new ImportedOgEvent
                    {
                        Source = ev,
                        Event = scripted
                    });
                    if (rawEventId >= 0 && !ogToImportedEventId.ContainsKey(rawEventId))
                    {
                        ogToImportedEventId.Add(rawEventId, scripted.EventId);
                    }
                    imported++;
                }

                foreach (ImportedOgEvent importedEvent in importedEvents)
                {
                    ApplyOgEventLinks(importedEvent.Source, importedEvent.Event, ogToImportedEventId);
                }

                NormalizeImportedEventLinks(templateEvents);
                NormalizeImportedRelatedCities(mapEditorInstance, levelObj, importedEvents);

                FieldInfo instantiatedEventsField = AccessTools.Field(levelObj.GetType(), "InstantiatedEvents");
                if (instantiatedEventsField?.GetValue(levelObj) is IList<ScriptedEvent> instantiatedEvents)
                {
                    instantiatedEvents.Clear();
                }

                // Keep the editor-side cache in sync, but do not depend on event UI.
                FieldInfo fScriptedEvents = AccessTools.Field(mapEditorInstance.GetType(), "_scriptedEvents");
                if (fScriptedEvents != null)
                {
                    fScriptedEvents.SetValue(mapEditorInstance, templateEvents.OrderBy(e => e.EventId).ToList());
                }

                Log?.LogInfo($"OG events imported into LevelMap.TemplateEvents: {imported}");
            }
            catch (Exception ex)
            {
                Log?.LogWarning($"Event import failed (non-fatal): {ex}");
            }
        }

        private static void NormalizeImportedEventLinks(IList<ScriptedEvent> events)
        {
            HashSet<int> validEventIds = new HashSet<int>(events.Select(e => e.EventId));
            foreach (ScriptedEvent ev in events)
            {
                ev.AcceptEventId = SanitizeLinkedEventId(ev.AcceptEventId, validEventIds, ev.EventId, "Accept");
                ev.RefuseEventId = SanitizeLinkedEventId(ev.RefuseEventId, validEventIds, ev.EventId, "Refuse");
                ev.LateEventId = SanitizeLinkedEventId(ev.LateEventId, validEventIds, ev.EventId, "Late");
                ev.VictoryEventId = SanitizeLinkedEventId(ev.VictoryEventId, validEventIds, ev.EventId, "Victory");
                ev.DefeatEventId = SanitizeLinkedEventId(ev.DefeatEventId, validEventIds, ev.EventId, "Defeat");
                ev.ChainEventId = SanitizeLinkedEventId(ev.ChainEventId, validEventIds, ev.EventId, "Chain");
            }
        }

        private static int SanitizeLinkedEventId(int linkedId, ISet<int> validEventIds, int ownerEventId, string linkName)
        {
            if (linkedId == ScriptedEvent.s_InvalidId)
            {
                return linkedId;
            }

            if (validEventIds.Contains(linkedId))
            {
                return linkedId;
            }

            Log?.LogWarning($"Clearing {linkName} link on event {ownerEventId}: target event {linkedId} is not available in imported event set.");
            return ScriptedEvent.s_InvalidId;
        }

        private static void NormalizeImportedRelatedCities(object mapEditorInstance, object levelObj, IList<ImportedOgEvent> importedEvents)
        {
            IList<ScriptedEvent> events = importedEvents.Select(e => e.Event).ToList();
            PropertyInfo mapCityStatesProperty = AccessTools.Property(levelObj.GetType(), "MapCityStates");
            Dictionary<int, WorldMapCityState> mapCityStates = mapCityStatesProperty?.GetValue(levelObj) as Dictionary<int, WorldMapCityState>;
            if (mapCityStates == null)
            {
                mapCityStates = new Dictionary<int, WorldMapCityState>();
                if (mapCityStatesProperty != null && mapCityStatesProperty.CanWrite)
                {
                    mapCityStatesProperty.SetValue(levelObj, mapCityStates, null);
                }
            }

            IList cities = AccessTools.Field(mapEditorInstance.GetType(), "_cities")?.GetValue(mapEditorInstance) as IList;
            List<NewEraCityCandidate> cityCandidates = BuildNewEraCityCandidates(cities);
            foreach (ImportedOgEvent importedEvent in importedEvents)
            {
                importedEvent.Event.RelatedCityId = ResolveNewEraRelatedCityId(importedEvent.Source, importedEvent.Event, cityCandidates);
            }

            HashSet<int> requestedCityIds = new HashSet<int>(events
                .Where(ev => ev.RelatedCityId != ScriptedEvent.s_InvalidId)
                .Select(ev => ev.RelatedCityId));

            if (cities != null)
            {
                foreach (int cityId in requestedCityIds.ToList())
                {
                    if (cityId < 0 || cityId >= cities.Count)
                    {
                        continue;
                    }

                    object cityObj = cities[cityId];
                    if (cityObj == null)
                    {
                        continue;
                    }

                    PropertyInfo stateProperty = AccessTools.Property(cityObj.GetType(), "State");
                    WorldMapCityState state = stateProperty?.GetValue(cityObj, null) as WorldMapCityState;
                    if (state == null)
                    {
                        continue;
                    }

                    if (!state.Enabled)
                    {
                        state.Enabled = true;
                        Log?.LogInfo($"Enabling world-map city {cityId} because it is referenced by an imported event.");
                    }

                    if (state.CityId == ScriptedEvent.s_InvalidId)
                    {
                        state.CityId = cityId;
                    }

                    mapCityStates[cityId] = state;
                    AccessTools.Method(cityObj.GetType(), "UpdateDisplay", new[] { typeof(bool) })?.Invoke(cityObj, new object[] { true });
                }
            }

            HashSet<int> validCityIds = new HashSet<int>(
                mapCityStates
                    .Where(kvp => kvp.Value != null && kvp.Value.Enabled && kvp.Value.Status != CityStatus.MyCity)
                    .Select(kvp => kvp.Value.CityId));

            foreach (ScriptedEvent ev in events)
            {
                if (ev.RelatedCityId == ScriptedEvent.s_InvalidId)
                {
                    continue;
                }

                if (!validCityIds.Contains(ev.RelatedCityId))
                {
                    Log?.LogWarning($"Clearing RelatedCityId on event {ev.EventId}: target city {ev.RelatedCityId} is not available in MapCityStates.");
                    ev.RelatedCityId = ScriptedEvent.s_InvalidId;
                }
            }
        }

        private static int ResolveNewEraRelatedCityId(JObject source, ScriptedEvent scriptedEvent, IList<NewEraCityCandidate> cityCandidates)
        {
            if (source == null || scriptedEvent == null || cityCandidates == null || cityCandidates.Count == 0)
            {
                return ScriptedEvent.s_InvalidId;
            }

            int ogCityNameId = ResolveOgEventCityNameId(source, scriptedEvent);
            if (ogCityNameId < 0)
            {
                return ScriptedEvent.s_InvalidId;
            }

            int mappedCityId = ResolveNewEraCityIdFromOgCityNameId(ogCityNameId, cityCandidates);
            if (mappedCityId != ScriptedEvent.s_InvalidId)
            {
                NewEraCityCandidate matchedCity = cityCandidates.FirstOrDefault(c => c.CityId == mappedCityId);
                if (matchedCity != null)
                {
                    Log?.LogInfo($"Mapped OG event city {ogCityNameId} to New Era city slot {matchedCity.CityId} ({matchedCity.DisplayName}).");
                }
            }

            return mappedCityId;
        }

        private static int ResolveOgEventCityNameId(JObject source, ScriptedEvent scriptedEvent)
        {
            int cityId = ReadInt(source, "city_id");
            int locationCityId = ReadLocationFieldCityId(source, 0);

            switch (scriptedEvent.Type)
            {
                case ScriptedEventType.Request:
                case ScriptedEventType.TroopRequest:
                case ScriptedEventType.Gift:
                case ScriptedEventType.TradeIncrease:
                case ScriptedEventType.TradeDecrease:
                case ScriptedEventType.PriceIncrease:
                case ScriptedEventType.PriceDecrease:
                case ScriptedEventType.CityStatusChange:
                    return locationCityId >= 0 ? locationCityId : cityId;
                default:
                    return cityId >= 0 ? cityId : locationCityId;
            }
        }

        private static int ReadLocationFieldCityId(JObject source, int index)
        {
            JArray fields = source?["location_fields"] as JArray;
            if (fields == null || index < 0 || index >= fields.Count)
            {
                return ScriptedEvent.s_InvalidId;
            }

            int rawValue = fields[index]?.Value<int>() ?? ScriptedEvent.s_InvalidId;
            if (rawValue > 0)
            {
                return rawValue - 1;
            }

            return ScriptedEvent.s_InvalidId;
        }

        private static List<NewEraCityCandidate> BuildNewEraCityCandidates(IList cities)
        {
            List<NewEraCityCandidate> results = new List<NewEraCityCandidate>();
            if (cities == null)
            {
                return results;
            }

            for (int i = 0; i < cities.Count; i++)
            {
                object cityObj = cities[i];
                if (cityObj == null)
                {
                    continue;
                }

                string cityTerm = cityObj.GetType().GetProperty("CityNameTerm")?.GetValue(cityObj, null) as string ?? string.Empty;
                string displayName = SafeTranslateCityTerm(cityTerm);
                HashSet<string> aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                AddNormalizedAlias(aliases, cityTerm);
                AddNormalizedAlias(aliases, displayName);
                AddNormalizedAlias(aliases, ExtractTermLeaf(cityTerm));

                results.Add(new NewEraCityCandidate
                {
                    CityId = i,
                    Term = cityTerm,
                    TermNormalized = NormalizeCityName(cityTerm),
                    DisplayName = string.IsNullOrWhiteSpace(displayName) ? cityTerm : displayName,
                    DisplayNameNormalized = NormalizeCityName(string.IsNullOrWhiteSpace(displayName) ? cityTerm : displayName),
                    Aliases = aliases
                });
            }

            return results;
        }

        private static string SafeTranslateCityTerm(string cityTerm)
        {
            if (string.IsNullOrWhiteSpace(cityTerm))
            {
                return string.Empty;
            }

            try
            {
                string translated = LocalizationManager.GetTranslation(cityTerm, true, 0, true, false, null, null, true);
                if (!string.IsNullOrWhiteSpace(translated))
                {
                    return translated;
                }
            }
            catch
            {
            }

            return ExtractTermLeaf(cityTerm);
        }

        private static string ExtractTermLeaf(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return string.Empty;
            }

            int slashIndex = term.LastIndexOf('/');
            int hashIndex = term.LastIndexOf('#');
            int splitIndex = Math.Max(slashIndex, hashIndex);
            return splitIndex >= 0 && splitIndex + 1 < term.Length ? term.Substring(splitIndex + 1) : term;
        }

        private static void AddNormalizedAlias(ISet<string> aliases, string value)
        {
            string normalized = NormalizeCityName(value);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                aliases.Add(normalized);
            }
        }

        private static string NormalizeCityName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string deconstructed = value.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder(deconstructed.Length);
            foreach (char ch in deconstructed)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(char.ToLowerInvariant(ch));
                }
            }

            return sb.ToString();
        }

        private sealed class ImportedOgEvent
        {
            public JObject Source;
            public ScriptedEvent Event;
        }

        private sealed class NewEraCityCandidate
        {
            public int CityId;
            public string Term;
            public string TermNormalized;
            public string DisplayName;
            public string DisplayNameNormalized;
            public HashSet<string> Aliases;
        }

        private static ScriptedEvent ConvertOgEvent(JObject ev, int fallbackIndex, HashSet<int> usedEventIds, out int rawEventId)
        {
            int ogType = ReadInt(ev, "event_type", "type", "type_id", "id");
            int subtype = ReadInt(ev, "subtype");
            int senderFaction = ReadInt(ev, "sender_faction");
            if (!TryMapOgEventType(ev, ogType, subtype, out ScriptedEventType mappedType))
            {
                rawEventId = -1;
                return null;
            }

            rawEventId = ReadInt(ev, "event_id", "id", "slot_index");
            int eventId = rawEventId;
            if (eventId < 0)
            {
                eventId = fallbackIndex;
            }

            while (usedEventIds.Contains(eventId))
            {
                eventId++;
            }
            usedEventIds.Add(eventId);

            JObject time = ev["time"] as JObject;
            JObject amountData = ev["amount"] as JObject;
            JObject itemData = ev["item"] as JObject;

            int month = ReadInt(time, "month");
            int yearMin = ReadInt(ev, "year_min", "trigger_year_min");
            if (yearMin == 0)
            {
                yearMin = ReadInt(time, "year");
            }
            int yearMax = ReadInt(ev, "year_max", "trigger_year_max");
            if (yearMax == 0)
            {
                yearMax = yearMin;
            }

            int amountValue = ReadInt(amountData, "value");
            int amountMin = ReadInt(amountData, "f_min");
            int amountMax = ReadInt(amountData, "f_max");
            int amountFixed = ReadInt(amountData, "f_fixed");
            if (amountMin <= 0) amountMin = amountValue;
            if (amountMax <= 0) amountMax = amountValue;
            if (amountFixed > 0)
            {
                amountMin = amountFixed;
                amountMax = amountFixed;
            }

            int triggerType = ReadInt(ev, "event_trigger_type");
            int monthsInitial = ReadInt(ev, "months_initial");
            int monthsLeft = ReadInt(ev, "quest_months_left");
            int cityId = ReadInt(ev, "city_id");
            int festivalDeity = ReadInt(ev, "festival_deity");
            int firstReason = ReadFirstValidOgReason(ev["reasons"] as JArray);
            int eventState = ReadInt(ev, "event_state");
            bool isLate = ev["is_overdue"]?.Value<bool>() ?? false;
            bool isActive = ev["is_active"]?.Value<bool>() ?? false;

            if (month <= 0) month = 1;
            if (month > 12) month = 12;
            if (yearMax < yearMin) yearMax = yearMin;

            var scripted = new ScriptedEvent
            {
                EventId = eventId,
                Type = mappedType,
                TriggerMonth = (Month)(month - 1),
                TriggerYearMin = Math.Max(0, yearMin),
                TriggerYearMax = Math.Max(0, yearMax),
                AmountMin = Math.Max(0, amountMin),
                AmountMax = Math.Max(0, amountMax),
                Frequency = MapOgEventFrequency(triggerType),
                Origin = MapOgEventOrigin(mappedType, senderFaction, cityId),
                RelatedCityId = cityId >= 0 ? cityId : ScriptedEvent.s_InvalidId,
                IsLate = isLate
            };

            scripted.TriggerYear = scripted.TriggerYearMin;
            scripted.Amount = Math.Max(0, amountValue > 0 ? amountValue : amountMin);

            ApplyOgEventTiming(scripted, mappedType, monthsInitial, monthsLeft, isActive, eventState);
            ApplyOgEventState(scripted, eventState, isLate);

            if (mappedType == ScriptedEventType.Request ||
                mappedType == ScriptedEventType.Gift ||
                mappedType == ScriptedEventType.TroopRequest ||
                mappedType == ScriptedEventType.TradeIncrease ||
                mappedType == ScriptedEventType.TradeDecrease ||
                mappedType == ScriptedEventType.PriceIncrease ||
                mappedType == ScriptedEventType.PriceDecrease)
            {
                List<Good> goods = EnumerateOgEventGoods(ev, itemData).ToList();
                if (goods.Count > 0)
                {
                    scripted.Goods.AddRange(goods);
                    scripted.GoodRequested = goods[0];
                }
            }

            if (mappedType == ScriptedEventType.Invasion || mappedType == ScriptedEventType.KingdomInvasion || mappedType == ScriptedEventType.TroopRequest)
            {
                scripted.MilitaryArmyMin = Math.Max(0, amountMin > 0 ? amountMin : amountValue);
                scripted.MilitaryArmyMax = Math.Max(scripted.MilitaryArmyMin, amountMax > 0 ? amountMax : scripted.MilitaryArmyMin);
                SetMember(scripted, typeof(ScriptedEvent), "MilitaryAmount", Math.Max(0, amountValue > 0 ? amountValue : scripted.MilitaryArmyMin));

                if ((mappedType == ScriptedEventType.Invasion || mappedType == ScriptedEventType.KingdomInvasion) && festivalDeity > 0)
                {
                    scripted.IsMilitaryNaval = true;
                    scripted.MilitaryWarshipMin = festivalDeity;
                    scripted.MilitaryWarshipMax = festivalDeity;
                    SetMember(scripted, typeof(ScriptedEvent), "WarshipAmount", festivalDeity);
                }
            }

            if (festivalDeity >= 0)
            {
                DeityName? deity = MapOgFestivalDeity(ev["festival_deity_name"]?.Value<string>());
                if (deity.HasValue)
                {
                    scripted.FestivalDeity = deity.Value;
                    if (mappedType == ScriptedEventType.Request)
                    {
                        scripted.Reason = EventReason.GodFestival;
                    }
                }
            }

            if (mappedType == ScriptedEventType.CityStatusChange)
            {
                int mappedCityStatus = MapOgCityStatusSubtype(subtype);
                TryAssignEnumIfDefined<CityStatusChangeType>(mappedCityStatus, value => scripted.CityStatusChange = value);
            }
            else if (mappedType == ScriptedEventType.Request)
            {
                int mappedReason = MapOgRequestSubtypeToReason(subtype);
                if (mappedReason >= 0)
                {
                    TryAssignEnumIfDefined<EventReason>(mappedReason, value => scripted.Reason = value);
                }
                else if (firstReason >= 0 && firstReason <= (int)EventReason.Threat)
                {
                    TryAssignEnumIfDefined<EventReason>(firstReason, value => scripted.Reason = value);
                }
            }
            else if (mappedType == ScriptedEventType.TroopRequest)
            {
                int mappedReason = MapOgTroopRequestSubtypeToReason(subtype);
                if (mappedReason < 0 && firstReason >= 0)
                {
                    if (firstReason == 5) mappedReason = (int)EventReason.EgyptianCity;
                    else if (firstReason == 6) mappedReason = (int)EventReason.DistantBattle;
                }
                TryAssignEnumIfDefined<EventReason>(mappedReason, value => scripted.Reason = value);
            }

            if ((mappedType == ScriptedEventType.Invasion || mappedType == ScriptedEventType.KingdomInvasion) && ReadInt(itemData, "value") == 4)
            {
                scripted.IsBedouin = true;
            }

            return scripted;
        }

        private static void ApplyOgEventLinks(JObject ev, ScriptedEvent scripted, IReadOnlyDictionary<int, int> ogToImportedEventId)
        {
            int onCompleted = ResolveLinkedOgEventId(ReadInt(ev, "on_completed_action"), ogToImportedEventId);
            int onRefusal = ResolveLinkedOgEventId(ReadInt(ev, "on_refusal_action"), ogToImportedEventId);
            int onTooLate = ResolveLinkedOgEventId(ReadInt(ev, "on_too_late_action"), ogToImportedEventId);
            int onDefeat = ResolveLinkedOgEventId(ReadInt(ev, "on_defeat_action"), ogToImportedEventId);

            switch (scripted.Type)
            {
                case ScriptedEventType.Request:
                case ScriptedEventType.Gift:
                case ScriptedEventType.TroopRequest:
                    scripted.AcceptEventId = onCompleted;
                    scripted.RefuseEventId = onRefusal;
                    scripted.LateEventId = onTooLate;
                    break;
                case ScriptedEventType.Invasion:
                case ScriptedEventType.KingdomInvasion:
                    scripted.VictoryEventId = onCompleted;
                    scripted.DefeatEventId = onDefeat;
                    break;
                default:
                    scripted.ChainEventId = onCompleted;
                    break;
            }
        }

        private static int ResolveLinkedOgEventId(int rawOgEventId, IReadOnlyDictionary<int, int> ogToImportedEventId)
        {
            if (rawOgEventId < 0)
            {
                return ScriptedEvent.s_InvalidId;
            }

            return ogToImportedEventId.TryGetValue(rawOgEventId, out int mapped)
                ? mapped
                : ScriptedEvent.s_InvalidId;
        }

        private static void ApplyOgEventTiming(ScriptedEvent scripted, ScriptedEventType mappedType, int monthsInitial, int monthsLeft, bool isActive, int eventState)
        {
            int totalMonths = monthsInitial > 0 ? monthsInitial : monthsLeft;
            if (scripted.Frequency == EventFrequency.Triggered)
            {
                scripted.Delay = Math.Max(0, monthsInitial);
            }
            else
            {
                if (mappedType == ScriptedEventType.Invasion || mappedType == ScriptedEventType.KingdomInvasion)
                {
                    scripted.InvasionDelay = Math.Max(0, totalMonths);
                }
                else
                {
                    scripted.WaitingMonthThreshold = Math.Max(0, totalMonths);
                }
            }

            if (scripted.Frequency != EventFrequency.Triggered && totalMonths > 0 && monthsLeft >= 0 && monthsLeft <= totalMonths)
            {
                scripted.MonthsElapsed = totalMonths - monthsLeft;
            }

            if (scripted.Frequency == EventFrequency.Triggered)
            {
                scripted.TriggeredEventToTrigger = isActive || eventState == 1;
            }
        }

        private static void ApplyOgEventState(ScriptedEvent scripted, int eventState, bool isLate)
        {
            switch (eventState)
            {
                case 1:
                case 2:
                case 6:
                    scripted.State = ScriptedEventState.Running;
                    break;
                case 3:
                case 4:
                case 5:
                case 7:
                    scripted.State = ScriptedEventState.Done;
                    break;
                default:
                    scripted.State = ScriptedEventState.Waiting;
                    break;
            }

            switch (eventState)
            {
                case 3:
                    scripted.Resolution = ScriptedEventResolution.Comply;
                    break;
                case 4:
                    scripted.Resolution = ScriptedEventResolution.TooLate;
                    scripted.IsLate = true;
                    break;
                case 5:
                    scripted.Resolution = ScriptedEventResolution.Lost;
                    break;
                default:
                    if (isLate)
                    {
                        scripted.Resolution = ScriptedEventResolution.TooLate;
                    }
                    break;
            }
        }

        private static EventFrequency MapOgEventFrequency(int triggerType)
        {
            switch (triggerType)
            {
                case 1:
                case 16:
                    return EventFrequency.Triggered;
                case 2:
                    return EventFrequency.Recurrent;
                default:
                    return EventFrequency.Once;
            }
        }

        private static int ReadFirstValidOgReason(JArray reasons)
        {
            if (reasons == null)
            {
                return -1;
            }

            foreach (JToken token in reasons)
            {
                if (token == null || token.Type == JTokenType.Null)
                {
                    continue;
                }

                int value = token.Value<int>();
                if (value != 65535 && value >= 0)
                {
                    return value;
                }
            }

            return -1;
        }

        private static Good? MapOgGoodFromEvent(JObject ev, JObject itemData)
        {
            string resourceName = ev["item_resource_name"]?.Value<string>();
            if (string.IsNullOrWhiteSpace(resourceName))
            {
                resourceName = ev["param1_resource_name"]?.Value<string>();
            }

            if (TryMapOgGood(resourceName, out Good good))
            {
                return good;
            }

            int rawGood = ReadInt(itemData, "value");
            string fallbackName = ev["param1_resource_name"]?.Value<string>();
            if (rawGood <= 0 && TryMapOgGood(fallbackName, out good))
            {
                return good;
            }

            return null;
        }

        private static IEnumerable<Good> EnumerateOgEventGoods(JObject ev, JObject itemData)
        {
            if (!OgEventCarriesGoods(ev))
            {
                yield break;
            }

            HashSet<Good> yielded = new HashSet<Good>();

            foreach (JToken token in (ev?["resource_names"] as JArray)?.Children() ?? Enumerable.Empty<JToken>())
            {
                Good? good = MapOgGoodToken(token);
                if (good.HasValue && yielded.Add(good.Value))
                {
                    yield return good.Value;
                }
            }

            foreach (JToken token in (ev?["resource_ids"] as JArray)?.Children() ?? Enumerable.Empty<JToken>())
            {
                Good? good = MapOgGoodToken(token);
                if (good.HasValue && yielded.Add(good.Value))
                {
                    yield return good.Value;
                }
            }

            foreach (JObject slot in (ev?["resource_slots"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
            {
                Good? good = MapOgGoodToken(slot["resource_name"]) ?? MapOgGoodToken(slot["resource_id"]);
                if (good.HasValue && yielded.Add(good.Value))
                {
                    yield return good.Value;
                }
            }

            Good? primaryGood = MapOgGoodFromEvent(ev, itemData);
            if (primaryGood.HasValue && yielded.Add(primaryGood.Value))
            {
                yield return primaryGood.Value;
            }
        }

        private static bool OgEventCarriesGoods(JObject ev)
        {
            int ogType = ReadInt(ev, "event_type", "type", "type_id");
            int subtype = ReadInt(ev, "subtype");
            switch (ogType)
            {
                case 1:
                    return subtype != 1 && subtype != 2;
                case 13:
                case 14:
                case 15:
                case 16:
                case 23:
                    return true;
                default:
                    return false;
            }
        }

        private static Good? MapOgGoodToken(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                return null;
            }

            if (token.Type == JTokenType.Integer)
            {
                return MapOgGoodFromResourceId(token.Value<int>());
            }

            if (token.Type == JTokenType.String && TryMapOgGood(token.Value<string>(), out Good parsed))
            {
                return parsed;
            }

            return null;
        }

        private static Good? MapOgGoodFromResourceId(int resourceId)
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

        private static bool TryMapOgGood(string raw, out Good good)
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
                case "luxury_goods": good = Good.Jewelry; return true;
                case "jewelry": good = Good.Jewelry; return true;
                case "timber": good = Good.Wood; return true;
                case "wood": good = Good.Wood; return true;
                case "plainstone": good = Good.Plainstone; return true;
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
                case "lamps": good = Good.Lamp; return true;
                case "lamp": good = Good.Lamp; return true;
                case "marble": good = Good.WhiteMarble; return true;
                default:
                    return Enum.TryParse(raw, true, out good);
            }
        }

        private static DeityName? MapOgFestivalDeity(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            string mapped = MapOgGodName(raw);
            return Enum.TryParse(mapped, true, out DeityName deity) ? deity : (DeityName?)null;
        }

        private static void TryAssignEnumIfDefined<TEnum>(int rawValue, Action<TEnum> setter) where TEnum : struct
        {
            if (rawValue < 0 || !Enum.IsDefined(typeof(TEnum), rawValue))
            {
                return;
            }

            setter((TEnum)Enum.ToObject(typeof(TEnum), rawValue));
        }

        private static int ReadInt(JObject obj, params string[] names)
        {
            if (obj == null)
            {
                return 0;
            }

            foreach (string name in names)
            {
                JToken token = obj[name];
                if (token == null || token.Type == JTokenType.Null) continue;
                if (token.Type == JTokenType.Integer) return token.Value<int>();
                if (token.Type == JTokenType.String && int.TryParse(token.Value<string>(), out int parsed)) return parsed;
            }
            return 0;
        }

        private static bool ReadBool(JObject obj, params string[] names)
        {
            if (obj == null)
            {
                return false;
            }

            foreach (string name in names)
            {
                JToken token = obj[name];
                if (token == null || token.Type == JTokenType.Null)
                {
                    continue;
                }

                if (token.Type == JTokenType.Boolean)
                {
                    return token.Value<bool>();
                }

                if (token.Type == JTokenType.Integer)
                {
                    return token.Value<int>() != 0;
                }

                if (token.Type == JTokenType.String)
                {
                    string raw = token.Value<string>();
                    if (bool.TryParse(raw, out bool parsedBool))
                    {
                        return parsedBool;
                    }

                    if (int.TryParse(raw, out int parsedInt))
                    {
                        return parsedInt != 0;
                    }
                }
            }

            return false;
        }

        private static bool TryMapOgEventType(JObject ev, int ogType, int subtype, out ScriptedEventType mappedType)
        {
            switch (ogType)
            {
                case 0: mappedType = ScriptedEventType.None; return true;
                case 1:
                    if (subtype == 1 || subtype == 2)
                    {
                        mappedType = ScriptedEventType.TroopRequest;
                        return true;
                    }
                    mappedType = ScriptedEventType.Request;
                    return true;
                case 2: mappedType = ScriptedEventType.Invasion; return true;
                case 3: mappedType = ScriptedEventType.EarthQuake; return true;
                case 6: mappedType = ScriptedEventType.WaterTradeProblem; return true;
                case 7: mappedType = ScriptedEventType.LandTradeProblem; return true;
                case 8: mappedType = ScriptedEventType.WageIncrease; return true;
                case 9: mappedType = ScriptedEventType.WageDecrease; return true;
                case 10: mappedType = ScriptedEventType.ContaminatedWater; return true;
                case 11: mappedType = ScriptedEventType.GoldMineCollapse; return true;
                case 12: mappedType = ScriptedEventType.ClayPitFlood; return true;
                case 13: mappedType = ScriptedEventType.TradeIncrease; return true;
                case 14: mappedType = ScriptedEventType.TradeDecrease; return true;
                case 15: mappedType = ScriptedEventType.PriceIncrease; return true;
                case 16: mappedType = ScriptedEventType.PriceDecrease; return true;
                case 17: mappedType = ScriptedEventType.KingdomRatingIncrease; return true;
                case 18: mappedType = ScriptedEventType.KingdomRatingDecrease; return true;
                case 19: mappedType = ScriptedEventType.CityStatusChange; return true;
                case 21: mappedType = ScriptedEventType.FailedFlood; return true;
                case 22: mappedType = ScriptedEventType.PerfectFlood; return true;
                case 23: mappedType = ScriptedEventType.Gift; return true;
                case 24: mappedType = ScriptedEventType.LocustPlague; return true;
                case 25: mappedType = ScriptedEventType.ToadPlague; return true;
                case 26: mappedType = ScriptedEventType.Hailstorm; return true;
                case 27: mappedType = ScriptedEventType.BloodRiver; return true;
                case 28: mappedType = ScriptedEventType.CrimeWave; return true;
                default:
                    mappedType = ScriptedEventType.None;
                    return false;
            }
        }

        private static EventOrigin MapOgEventOrigin(ScriptedEventType mappedType, int senderFaction, int cityId)
        {
            if (mappedType == ScriptedEventType.Request || mappedType == ScriptedEventType.Gift)
            {
                return senderFaction == 1 ? EventOrigin.Pharaoh : EventOrigin.City;
            }

            return cityId >= 0 ? EventOrigin.City : EventOrigin.Pharaoh;
        }

        private static int MapOgRequestSubtypeToReason(int subtype)
        {
            switch (subtype)
            {
                case 0: return (int)EventReason.General;
                case 3: return (int)EventReason.GodFestival;
                case 4: return (int)EventReason.ConstructionProject;
                case 5: return (int)EventReason.Famine;
                case 6: return (int)EventReason.Threat;
                default: return -1;
            }
        }

        private static int MapOgTroopRequestSubtypeToReason(int subtype)
        {
            switch (subtype)
            {
                case 1: return (int)EventReason.EgyptianCity;
                case 2: return (int)EventReason.DistantBattle;
                default: return -1;
            }
        }

        private static int MapOgCityStatusSubtype(int subtype)
        {
            switch (subtype)
            {
                case 0: return (int)CityStatusChangeType.EgyptianCityToDistantCity;
                case 1: return (int)CityStatusChangeType.DistantCityToEgyptianCity;
                case 2: return (int)CityStatusChangeType.TradeAvailable;
                case 3: return (int)CityStatusChangeType.TradeShutsdown;
                case 4: return (int)CityStatusChangeType.UnderSiege;
                default: return -1;
            }
        }

        private static object CreateCoord(Type coordType, int x, int y)
        {
            try
            {
                ConstructorInfo ctor = coordType.GetConstructor(new[] { typeof(int), typeof(int) });
                if (ctor != null)
                {
                    return ctor.Invoke(new object[] { x, y });
                }

                object obj = Activator.CreateInstance(coordType);
                coordType.GetField("x")?.SetValue(obj, x);
                coordType.GetField("y")?.SetValue(obj, y);
                return obj;
            }
            catch
            {
                return null;
            }
        }

        [HarmonyPatch]
        private static class Patch_MenuLevelEditorPanel_InUnityLoad
        {
            private static MethodBase TargetMethod()
            {
                return AccessTools.Method("MenuLevelEditorPanel:InUnityLoad");
            }

            private static void Postfix(MonoBehaviour __instance)
            {
                try
                {
                    ScheduleEnsureImportButton(__instance, "OnLoadClicked", "OnCreateClicked");
                }
                catch (Exception ex)
                {
                    Log?.LogWarning($"Failed to inject OG import button into level-editor menu: {ex}");
                }
            }
        }

        [HarmonyPatch]
        private static class Patch_MenuLevelEditorPanel_Show
        {
            private static MethodBase TargetMethod()
            {
                return AccessTools.Method("MainMenuPanel:Show");
            }

            private static void Postfix(MonoBehaviour __instance)
            {
                try
                {
                    if (__instance != null && string.Equals(__instance.GetType().Name, "MenuLevelEditorPanel", StringComparison.Ordinal))
                    {
                        ScheduleEnsureImportButton(__instance, "OnLoadClicked", "OnCreateClicked");
                    }
                }
                catch (Exception ex)
                {
                    Log?.LogWarning($"Failed to inject OG import button after panel show: {ex}");
                }
            }
        }

        [HarmonyPatch]
        private static class Patch_MapEditor_Awake
        {
            private static MethodBase TargetMethod()
            {
                return AccessTools.Method("MapEditor:Awake");
            }

            private static void Postfix(MonoBehaviour __instance)
            {
                if (!ApplyPending || PendingRoot == null)
                {
                    return;
                }

                Log?.LogInfo("MapEditor.Awake detected, scheduling OG apply...");
                __instance.StartCoroutine(ApplyNextFrame(__instance));
            }

            private static IEnumerator ApplyNextFrame(MonoBehaviour editor)
            {
                yield return null;

                if (!ApplyPending)
                {
                    yield break;
                }

                try
                {
                    Log?.LogInfo("ApplyNextFrame executing OG import.");
                    ApplyOgJson(editor);
                }
                catch (Exception ex)
                {
                    Log?.LogError($"OG import failed in ApplyNextFrame: {ex}");
                }
            }
        }

        [HarmonyPatch]
        private static class Patch_MenuLevelEditorCreatePanel_OnCreateMap
        {
            private static MethodBase TargetMethod()
            {
                return AccessTools.Method("MenuLevelEditorCreatePanel:OnCreateMap");
            }

            private static bool Prefix()
            {
                if (!ImportArmed || PendingRoot == null || PendingMapSide <= 0)
                {
                    return true;
                }

                try
                {
                    ExecuteDirectCreateMap(PendingMapSide, "create panel override");
                    return false;
                }
                catch (Exception ex)
                {
                    Log?.LogWarning($"Failed to override create-map size: {ex}");
                    return true;
                }
            }
        }

        private static void ExecuteDirectCreateMap(int mapSide, string source)
        {
            try
            {
                TryStopMainMenuMusic();
            }
            catch (Exception ex)
            {
                Log?.LogDebug($"StopMusic skipped during {source}: {ex.Message}");
            }

            GlobalAccessor.CurrentMode = GlobalAccessor.GameMode.Editor;
            GlobalAccessor.LevelToCreateContext = new GlobalAccessor.LevelCreateContext
            {
                Size = (MapSize)mapSide,
                Climate = MapHumidity.Normal
            };

            string sceneName = SceneManager.GetActiveScene().name;
            Log?.LogInfo($"Creating OG map via {source}. side={mapSide}, activeScene='{sceneName}'.");
            CoreSceneManager.Instance.TransitionToScene("MapEditor");
        }

        private static void TryStopMainMenuMusic()
        {
            try
            {
                Type mainMenuType = AccessTools.TypeByName("MainMenuManager");
                object manager = AccessTools.Property(mainMenuType, "Instance")?.GetValue(null, null);
                if (manager == null)
                {
                    return;
                }

                Type titlePanelType = AccessTools.TypeByName("MenuTitlePanel");
                if (titlePanelType == null)
                {
                    return;
                }

                MethodInfo getPanel = mainMenuType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(m => m.Name == "GetPanel" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);
                if (getPanel == null)
                {
                    return;
                }

                object panel = getPanel.MakeGenericMethod(titlePanelType).Invoke(manager, null);
                AccessTools.Method(titlePanelType, "StopMusic")?.Invoke(panel, null);
            }
            catch (Exception ex)
            {
                Log?.LogDebug($"TryStopMainMenuMusic skipped: {ex.Message}");
            }
        }

        private static void SetEditorCreateContext(object chosenMapSize)
        {
            Type gaType = AccessTools.TypeByName("GlobalAccessor");
            if (gaType == null)
            {
                throw new InvalidOperationException("GlobalAccessor type not found.");
            }

            Type gameModeType = AccessTools.TypeByName("GlobalAccessor+GameMode");
            object editorMode = gameModeType != null && gameModeType.IsEnum ? Enum.Parse(gameModeType, "Editor") : null;
            if (editorMode != null)
            {
                SetStaticMember(gaType, "CurrentMode", editorMode);
            }

            FieldInfo levelCtxField = AccessTools.Field(gaType, "LevelToCreateContext");
            if (levelCtxField == null)
            {
                throw new InvalidOperationException("GlobalAccessor.LevelToCreateContext not found.");
            }

            Type ctxType = levelCtxField.FieldType;
            object ctx = Activator.CreateInstance(ctxType);
            SetMember(ctx, ctxType, "Size", chosenMapSize);

            Type humidityType = AccessTools.TypeByName("MapHumidity");
            if (humidityType != null && humidityType.IsEnum)
            {
                object normal = Enum.Parse(humidityType, "Normal");
                SetMember(ctx, ctxType, "Climate", normal);
            }

            levelCtxField.SetValue(null, ctx);
        }

        private static void TryTransitionToMapEditor()
        {
            Type sceneManagerType = AccessTools.TypeByName("CoreSceneManager");
            object manager = AccessTools.Property(sceneManagerType, "Instance")?.GetValue(null, null);
            if (manager == null)
            {
                throw new InvalidOperationException("CoreSceneManager.Instance not found.");
            }

            MethodInfo transition = AccessTools.Method(sceneManagerType, "TransitionToScene", new[] { typeof(string) });
            if (transition == null)
            {
                throw new InvalidOperationException("CoreSceneManager.TransitionToScene(string) not found.");
            }

            transition.Invoke(manager, new object[] { "MapEditor" });
        }

        [HarmonyPatch]
        private static class Patch_MapEditor_Start
        {
            private static MethodBase TargetMethod()
            {
                Type t = AccessTools.TypeByName("MapEditor");
                return t == null ? null : AccessTools.Method(t, "Start");
            }

            private static void Postfix(object __instance)
            {
                if (!ApplyPending)
                {
                    return;
                }

                try
                {
                    Log?.LogInfo("MapEditor.Start detected pending OG import.");
                }
                catch
                {
                }
            }
        }

        [HarmonyPatch]
        private static class Patch_MapEditor_FinalizePostRenderInitializationAsync
        {
            private static MethodBase TargetMethod()
            {
                Type t = AccessTools.TypeByName("MapEditor");
                return t == null ? null : AccessTools.Method(t, "FinalizePostRenderInitializationAsync");
            }

            private static IEnumerator Postfix(IEnumerator __result, object __instance)
            {
                while (__result != null && __result.MoveNext())
                {
                    yield return __result.Current;
                }

                if (!ApplyPending)
                {
                    yield break;
                }

                try
                {
                    Log?.LogInfo("FinalizePostRenderInitializationAsync finished, applying OG import.");
                    ApplyOgJson(__instance);
                }
                catch (Exception ex)
                {
                    Log?.LogError($"Apply after map load failed: {ex}");
                }
            }
        }

        [HarmonyPatch]
        private static class Patch_MapEditor_GetTriggerEvents
        {
            private static MethodBase TargetMethod()
            {
                Type mapEditorType = AccessTools.TypeByName("MapEditor");
                return mapEditorType == null ? null : AccessTools.Method(mapEditorType, "GetTriggerEvents");
            }

            private static void Postfix(object __instance, ref List<ScriptedEvent> __result)
            {
                try
                {
                    IList<ScriptedEvent> scriptedEvents = AccessTools.Field(__instance?.GetType(), "_scriptedEvents")?.GetValue(__instance) as IList<ScriptedEvent>;
                    if (scriptedEvents == null)
                    {
                        return;
                    }

                    __result = scriptedEvents
                        .Where(e => e != null)
                        .OrderBy(e => e.EventId)
                        .ToList();
                }
                catch (Exception ex)
                {
                    Log?.LogWarning($"GetTriggerEvents fallback failed: {ex}");
                }
            }
        }

        [HarmonyPatch]
        private static class Patch_MapEditor_UpdateDropdownFromCityId
        {
            private static MethodBase TargetMethod()
            {
                Type mapEditorType = AccessTools.TypeByName("MapEditor");
                Type dropdownType = AccessTools.TypeByName("TMPro.TMP_Dropdown");
                if (mapEditorType == null || dropdownType == null)
                {
                    return null;
                }

                return AccessTools.Method(mapEditorType, "UpdateDropdownFromCityId", new[] { typeof(int), dropdownType });
            }

            private static bool Prefix(object __instance, int id, object dropdown)
            {
                try
                {
                    if (__instance == null || dropdown == null)
                    {
                        return false;
                    }

                    object levelObj = AccessTools.Field(__instance.GetType(), "_level")?.GetValue(__instance);
                    FieldInfo citiesField = AccessTools.Field(__instance.GetType(), "_cities");
                    IList cities = citiesField?.GetValue(__instance) as IList;
                    Dictionary<int, WorldMapCityState> mapCityStates = AccessTools.Property(levelObj?.GetType(), "MapCityStates")?.GetValue(levelObj, null) as Dictionary<int, WorldMapCityState>;

                    List<KeyValuePair<int, WorldMapCityState>> validCities = mapCityStates == null
                        ? new List<KeyValuePair<int, WorldMapCityState>>()
                        : mapCityStates
                            .Where(kvp => kvp.Value != null && kvp.Value.Enabled && kvp.Value.Status != CityStatus.MyCity)
                            .OrderBy(kvp => kvp.Key)
                            .ToList();

                    List<string> options = new List<string>();
                    foreach (KeyValuePair<int, WorldMapCityState> kvp in validCities)
                    {
                        string cityLabel = kvp.Key.ToString();
                        if (cities != null && kvp.Key >= 0 && kvp.Key < cities.Count)
                        {
                            object city = cities[kvp.Key];
                            string cityNameTerm = city?.GetType().GetProperty("CityNameTerm")?.GetValue(city, null) as string;
                            if (!string.IsNullOrWhiteSpace(cityNameTerm))
                            {
                                cityLabel = cityNameTerm;
                            }
                        }

                        options.Add($"[{kvp.Key}] {cityLabel}");
                    }

                    AccessTools.Method(dropdown.GetType(), "ClearOptions")?.Invoke(dropdown, Array.Empty<object>());
                    AccessTools.Method(dropdown.GetType(), "AddOptions", new[] { typeof(List<string>) })?.Invoke(dropdown, new object[] { options });

                    int selectedIndex = 0;
                    if (id != ScriptedEvent.s_InvalidId)
                    {
                        int foundIndex = validCities.FindIndex(kvp => kvp.Value.CityId == id);
                        if (foundIndex >= 0)
                        {
                            selectedIndex = foundIndex;
                        }
                        else
                        {
                            Log?.LogWarning($"City dropdown fallback for event city id {id}: city is not present in current MapCityStates.");
                        }
                    }

                    SetMember(dropdown, dropdown.GetType(), "value", selectedIndex);
                }
                catch (Exception ex)
                {
                    Log?.LogWarning($"Safe UpdateDropdownFromCityId fallback failed: {ex}");
                }

                return false;
            }
        }

        private static void SetMember(object instance, Type type, string memberName, object value)
        {
            FieldInfo field = FindField(type, memberName);
            if (field != null)
            {
                field.SetValue(instance, value);
                return;
            }

            PropertyInfo property = FindProperty(type, memberName);
            if (property != null && property.CanWrite)
            {
                property.SetValue(instance, value, null);
            }
        }

        private static void SetStaticMember(Type type, string memberName, object value)
        {
            FieldInfo field = FindField(type, memberName);
            if (field != null)
            {
                field.SetValue(null, value);
                return;
            }

            PropertyInfo property = FindProperty(type, memberName);
            if (property != null && property.CanWrite)
            {
                property.SetValue(null, value, null);
            }
        }

        private static object GetMember(object instance, Type type, string memberName)
        {
            FieldInfo field = FindField(type, memberName);
            if (field != null)
            {
                return field.GetValue(instance);
            }

            PropertyInfo property = FindProperty(type, memberName);
            if (property != null && property.CanRead)
            {
                return property.GetValue(instance, null);
            }

            return null;
        }

        private static FieldInfo FindField(Type type, string memberName)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            for (Type current = type; current != null; current = current.BaseType)
            {
                FieldInfo field = current.GetField(memberName, flags);
                if (field != null)
                {
                    return field;
                }
            }

            return null;
        }

        private static PropertyInfo FindProperty(Type type, string memberName)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            for (Type current = type; current != null; current = current.BaseType)
            {
                PropertyInfo property = current.GetProperty(memberName, flags);
                if (property != null)
                {
                    return property;
                }
            }

            return null;
        }
    }
}
