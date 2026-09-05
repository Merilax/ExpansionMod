using ArchEmperorLib;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace ArchExpansionMod;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("BREAK ARTS III.exe")]
[BepInDependency("ArchEmperorLib", BepInDependency.DependencyFlags.HardDependency)]

public class Plugin : BasePlugin
{
	internal static new ManualLogSource Log;

	public static ConfigEntry<bool> logVerbose;
	public static string BUNDLE_ROOT;
	public static UniverseLib.AssetBundle assets;
	public override void Load()
	{
		Log = BepInEx.Logging.Logger.CreateLogSource("ExpansionMod");

		BUNDLE_ROOT = System.IO.Path.Combine(Paths.PluginPath, "ArchExpansionMod");

		string assetBundlePath = System.IO.Path.Combine(Paths.PluginPath, "ArchExpansionMod", "archexpansion_assets");
		assets = UniverseLib.AssetBundle.LoadFromFile(assetBundlePath);

		logVerbose = Config.Bind("Debug", "logVerbose", false, "Log additional events for debugging purposes.");

		var harmony = Harmony.CreateAndPatchAll(typeof(Main));
		harmony.PatchAll(typeof(MainMenuPatch));

		IL2CPPChainloader.AddUnityComponent<ModModuleLoader>();

		ModBundleParser.ParseModBundles();
		ModuleRegistrator.PopulateModuleList();
		ModuleRegistrator.PopulateThumbnails();

		ModRegistry.Register(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION, RequirementScope.Everyone, VersionStrictness.Exact, () => ModBundleParser.loadedModRecords);
	}
	public static void LogInfo(object data) => Log.LogInfo(data);
	public static void LogDebug(object data) { if (logVerbose.Value == true) Log.LogInfo(data); }
}

public class Main
{
	public static bool init = false;
	public static Shader mechShader;

	private static void OnLogMessage(string condition, string stackTrace, LogType type)
	{
		if (type == LogType.Error || type == LogType.Exception)
		{
			Plugin.Log.LogError(condition + "\n" + stackTrace);
		}
	}

	// INIT
	[HarmonyPrefix]
	[HarmonyPatch(typeof(Scene_GameInit), nameof(Scene_GameInit.Start))]
	public static void GameInitStart_Pre()
	{
		ModAssetHandler.AcquireVanillaAssets();
	}
	[HarmonyPostfix]
	[HarmonyPatch(typeof(Scene_GameInit), nameof(Scene_GameInit.Start))]
	public static void GameInitStart_Post()
	{
		ModuleRegistrator.PopulateLocalization();
		LocalizationSettings.add_SelectedLocaleChanged((Il2CppSystem.Action<UnityEngine.Localization.Locale>)((locale) => ModuleRegistrator.PopulateLocalization()));
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(LeaderBoardsManager), nameof(LeaderBoardsManager.AddScore))]
	public static void LeaderBoardsManager_AddScore(ref bool __runOriginal, ref Il2CppSystem.Threading.Tasks.Task<bool> __result)
	{
		__runOriginal = false;
		__result = new(true); // Dumb but works
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(MachineDesignerMain), nameof(MachineDesignerMain.Start))]
	public static void MachineDesignerMainStart(MachineDesignerMain __instance)
	{
		mechShader = __instance._BM.MyMainMats[0].shader;
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(MachineDesignerMain._ModuleLoader_d__86), nameof(MachineDesignerMain._ModuleLoader_d__86.MoveNext))]
	public static void ModuleLoader(MachineDesignerMain._ModuleLoader_d__86 __instance, ref bool __runOriginal, ref bool __result)
	{
		Plugin.LogDebug("ModuleLoader: " + __instance.path);
		if (ModBundleParser.IsValidPrefix(__instance.path))
		{
			if (__instance.__1__state == 1 && __instance._handle_5__2.IsValid() && __instance._handle_5__2.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
			{
				Plugin.LogDebug("HIT");
				GameObject module = __instance._handle_5__2.Result;
				ModModuleLoader.SetupModule(module, __instance.path);
			}
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(DataManager), nameof(DataManager.GetUnlockStatus))]
	public static void GetUnlockStatus(ref string key, ref bool __runOriginal, ref DataManager.UnlockStatus __result)
	{
		Plugin.LogDebug("GetUnlockStatus: " + key);
		if (ModBundleParser.IsValidPrefix(key))
		{
			__runOriginal = false;
			__result = DataManager.UnlockStatus.unlocked;
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(Pooler), nameof(Pooler.InstantiateModuleSync))]
	public static void InstantiateModuleSync(ref string name, ref bool __runOriginal, ref GameObject __result)
	{
		Plugin.LogDebug("InstantiateModuleSync: " + name);
		if (ModBundleParser.IsValidPrefix(name))
		{
			ModModuleLoader.SetupModule(__result, name);
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(MachineDesignerUI), nameof(MachineDesignerUI.Install_ShowModuleInfo))]
	public static void Install_ShowModuleInfo(MachineDesignerUI __instance, int sumbNum, int dataNum)
	{
		Plugin.LogDebug($"Install_ShowModuleInfo: {sumbNum} / {dataNum}");
	}
	// [HarmonyPrefix]
	// [HarmonyPatch(typeof(MyUtility), nameof(MyUtility.GetModuleInfo))]
	// public static void GetModuleInfo(MyUtility __instance, ref ModuleDataDef data, ref bool IsSmall)
	// {
	// 	Plugin.LogDebug($"GetModuleInfo: {data._Name} / {IsSmall}");
	// }

	[HarmonyPostfix]
	[HarmonyPatch(typeof(MachineDesignerUI), nameof(MachineDesignerUI.Start))]
	public static void MachineDesignerUI_Start(MachineDesignerUI __instance)
	{
		try
		{
			__instance.Install_PageNumber.gameObject.active = true;
			__instance.Install_NextAndPrev[0].gameObject.active = true;
			__instance.Install_NextAndPrev[1].gameObject.active = true;
		}
		catch (System.Exception) { }
	}
}

public class MainMenuPatch
{
	static bool once = false;
	[HarmonyPostfix]
	[HarmonyPatch(typeof(Scene_MainMenu), nameof(Scene_MainMenu.Start))]
	public static void InitializeMain(ref Scene_MainMenu __instance)
	{
		if (once) return; once = true;
		Cursor.lockState = CursorLockMode.Confined;
		ModManager.SetCredits(MyPluginInfo.PLUGIN_GUID, MakeCredits());
	}
	private static CreditBlock MakeCredits()
	{
		CreditBlock creditData = new();
		if (Plugin.assets)
			creditData.AddLogo(Plugin.assets.LoadAsset<Sprite>("ExpansionModLogo"));
		creditData.AddText(CreditBlock.FONT_SIZE.TITLE, "ARCHITECT'S EXPANSION MOD");
		creditData.AddSeparator();
		creditData.AddText(CreditBlock.FONT_SIZE.ROLE, "DEVELOPER");
		creditData.AddText(CreditBlock.FONT_SIZE.NORMAL, "Merilax");
		return creditData;
	}
}