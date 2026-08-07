using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;

namespace ExpansionMod;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("BREAK ARTS III.exe")]

public class Plugin : BasePlugin
{
	internal static new ManualLogSource Log;

	public static string BUNDLE_ROOT;
	public override void Load()
	{
		Log = BepInEx.Logging.Logger.CreateLogSource("ExpansionMod");

		BUNDLE_ROOT = System.IO.Path.Combine(Paths.PluginPath, "ExpansionMod/");

		var harmony = Harmony.CreateAndPatchAll(typeof(Main));

		// var options = new RegisterTypeOptions
		// {
		// 	Interfaces = new Il2CppInterfaceCollection([typeof(IResourceProvider)])
		// };
		// ClassInjector.RegisterTypeInIl2Cpp(typeof(ModuleDataDefResourceProvider), options);

		// ClassInjector.RegisterTypeInIl2Cpp(typeof(TranslationTable));
		IL2CPPChainloader.AddUnityComponent<ModModuleLoader>();
		IL2CPPChainloader.AddUnityComponent<AsyncHandler>();

		ModBundleParser.ParseModBundles();
		ModuleRegistrator.PopulateModuleList();
		ModuleRegistrator.PopulateThumbnails();
	}
	public static void LogInfo(object data) => Log.LogInfo(data);

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
	public static void GameInitStart()
	{
		Plugin.LogInfo("Initializing game logic...");
		Config.Debug_OutputRawSaveData = true;
		// Config.Debug_ShowAllItems = true;

		ModAssetHandler.AcquireVanillaAssets();
	}
	[HarmonyPostfix]
	[HarmonyPatch(typeof(Scene_GameInit), nameof(Scene_GameInit.Start))]
	public static void GameInitStart_Post()
	{
		ModuleRegistrator.PopulateLocalization();
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(MachineDesignerMain), nameof(MachineDesignerMain.Start))]
	public static void MachineDesignerMainStart(MachineDesignerMain __instance)
	{
		mechShader = __instance._BM.MyMainMats[0].shader;
	}

	// [HarmonyPostfix]
	// [HarmonyPatch(typeof(AnimationManager), nameof(AnimationManager.Awake))]
	// public static void AnimationManagerUpdate(AnimationManager __instance, ref bool __runOriginal)
	// {
	// 	__instance._CommandStates.Add(AnimationManager.AllAnimations.Idle, true);
	// }

	[HarmonyPrefix]
	[HarmonyPatch(typeof(MachineDesignerMain._ModuleLoader_d__86), nameof(MachineDesignerMain._ModuleLoader_d__86.MoveNext))]
	public static void ModuleLoader(MachineDesignerMain._ModuleLoader_d__86 __instance, ref bool __runOriginal, ref bool __result)
	{
		Plugin.LogInfo("ModuleLoader: " + __instance.path);
		if (ModBundleParser.IsValidPrefix(__instance.path))
		{
			if (__instance.__1__state == 1 && __instance._handle_5__2.IsValid() && __instance._handle_5__2.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
			{
				Plugin.LogInfo("HIT");
				GameObject module = __instance._handle_5__2.Result;
				ModModuleLoader.SetupModule(module, __instance.path);
			}
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(DataManager), nameof(DataManager.GetUnlockStatus))]
	public static void GetUnlockStatus(ref string key, ref bool __runOriginal, ref DataManager.UnlockStatus __result)
	{
		// Plugin.LogInfo("GetUnlockStatus: " + key);
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
		Plugin.LogInfo("InstantiateModuleSync: " + name);
		if (ModBundleParser.IsValidPrefix(name))
		{
			ModModuleLoader.SetupModule(__result, name);
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(MachineDesignerUI), nameof(MachineDesignerUI.Install_ShowModuleInfo))]
	public static void Install_ShowModuleInfo(MachineDesignerUI __instance, int sumbNum, int dataNum)
	{
		// Plugin.LogInfo($"Install_ShowModuleInfo: {sumbNum} / {dataNum}");
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(MyUtility), nameof(MyUtility.GetModuleInfo))]
	public static void GetModuleInfo(MyUtility __instance, ref ModuleDataDef data, ref bool IsSmall)
	{
		// Plugin.LogInfo($"GetModuleInfo: {data._Name} / {IsSmall}");
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(UsingController), nameof(UsingController.UpdateEffectsColor))]
	[HarmonyPatch(typeof(UsingController), nameof(UsingController.UpdateVFXPower))]
	[HarmonyPatch(typeof(UsingController), nameof(UsingController.ForceStopCharging))]
	[HarmonyPatch(typeof(UsingController), nameof(UsingController.PlayUseEffects))]
	public static void UpdateEffectsColor(UsingController __instance, ref bool __runOriginal)
	{
		// if (!__instance._VFX) __runOriginal = false;
	}
}