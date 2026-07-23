using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ExpansionMod;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("BREAK ARTS III.exe")]

public class Plugin : BasePlugin
{
	internal static new ManualLogSource Log;
	public static ConfigFile config;
	public static UniverseLib.AssetBundle assets;
	public override void Load()
	{
		Log = BepInEx.Logging.Logger.CreateLogSource("ExpansionMod");

		string bundlePath = Path.Combine(Paths.PluginPath, "ExpansionMod", "modelassets");
		if (!File.Exists(bundlePath))
			throw new System.Exception($"Bundle not found at: {bundlePath}");

		assets = UniverseLib.AssetBundle.LoadFromFile(bundlePath) ?? throw new System.Exception("Failed to load assets!");

		var harmony = Harmony.CreateAndPatchAll(typeof(Main));
		harmony.PatchAll(typeof(UI));
	}
	public static void LogInfo(object data) => Log.LogInfo(data);
}

public class Main
{
	public static bool init = false;

	public static Dictionary<string, ModuleDataDef> moduleDefs = [];
	public static List<List<ModuleList>> moduleLists = [
		[], // Armor
		[], // Heat
		[], // Gene
		[], // Detec
		[], // Thr
		[], // Ctrl
		[], // Sp
		[], // Wep
		[], // Joint
		[], // Orna
	];

	// INIT
	[HarmonyPostfix]
	[HarmonyPatch(typeof(Scene_GameInit), nameof(Scene_GameInit.Start))]
	public static void GameInitStart(Scene_MainMenu __instance)
	{
		moduleDefs.Add("ExpansionMod_0001", new()
		{
			name = "ExpansionMod_0001",
			_Name = "H_99 Name",
			_Structure = 2,
			_DescriptionId = 8,
			_DescriptionKey = TranslationList.Keys.module_detail,
			_DescriptionPrefix = "e_",
			// _Preview = null
		});
		moduleLists[9].Add(new()
		{
			_MdName = "ExpansionMod_0001",
			_name = "Heat_99",
		});


		for (int i = 0; i < AllItemList.AllModuleList.Count; i++)
		{
			if (moduleLists[i].Count == 0) continue;

			int oldLength = AllItemList.AllModuleList[i].Count;
			int newLength = oldLength + moduleLists[i].Count;
			var newArr = new Il2CppReferenceArray<ModuleList>(newLength);

			for (int j = 0; j < newLength; j++)
			{
				if (j < oldLength)
					newArr[j] = AllItemList.AllModuleList[i][j];
				else
					newArr[j] = moduleLists[i][j - oldLength];
			}
			AllItemList.AllModuleList[i] = newArr;
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(MachineDesignerMain), nameof(MachineDesignerMain.Update))]
	public static void MachineDesignerMainUpdate(MachineDesignerMain __instance)
	{
		if (Keyboard.current.jKey.wasPressedThisFrame)
		{
			var sampleMesh = __instance._MM.transform.GetChild(1).GetChild(0).GetChild(0).GetComponent<SkinnedMeshRenderer>();

			GameObject module = ModLoadModule("ExpansionMod_0001", sampleMesh.material.shader);

			// Vanilla-like processing
			__instance.Install_ControlModule = module;
			__instance.SetInstallPhase(MachineDesignerMain.Install_ControlPhase.Grab);
			__instance.Install_ActivateAllMarkers(ModuleData.ModuleType.Normal);
			module.GetComponent<ModuleData>().Init();
			module.GetComponent<ModuleEffects>().SetBasicData(__instance._BM, __instance._MM);
			module.GetComponent<ModuleEffects>().InitSelectedEffets();
			__instance.Install_SetAllThrusterDirSign(true, null);
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(MachineDesignerMain), nameof(MachineDesignerMain.ModuleLoader))]
	public static void ModuleLoader(MachineDesignerMain __instance, string path)
	{
		// Plugin.LogInfo(path);
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(DataManager), nameof(DataManager.GetUnlockStatus))]
	public static void GetUnlockStatus(ref string key, ref bool __runOriginal, ref DataManager.UnlockStatus __result)
	{
		Plugin.LogInfo("GetUnlockStatus: " + key);
		if (key.Contains("ExpansionMod"))
		{
			Plugin.LogInfo("Custom plugin unlocked");	
			__runOriginal = false;
			__result = DataManager.UnlockStatus.unlocked;
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(Pooler), nameof(Pooler.InstantiateModuleSync))]
	public static void InstantiateModuleSync(ref string name, ref bool __runOriginal, ref GameObject __result)
	{
		// Plugin.LogInfo("InstantiateModuleSync: " + name);
		if (name.Contains("ExpansionMod"))
		{
			__runOriginal = false;
			__result = ModLoadModule(name.Substring(7));
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(MachineDesignerUI), nameof(MachineDesignerUI.GetUnlockedList))]
	public static void GetUnlockedList(int type, int cat, ref Il2CppReferenceArray<ModuleList> __result)
	{
		Plugin.LogInfo("GetUnlockedList: " + type + " " + cat);
		foreach (var item in __result)
		{
			Plugin.LogInfo(item._name);
		}
	}

	// [HarmonyPrefix]
	// [HarmonyPatch(typeof(MachineDesignerMain._ModuleLoader_d__86), nameof(MachineDesignerMain._ModuleLoader_d__86.MoveNext))]
	// public static void ModuleLoader(MachineDesignerMain._ModuleLoader_d__86 __instance)
	// {
	// 	var ins = __instance.__4__this;
	// 	Plugin.LogInfo("Loader: " + __instance.path);
	// }

	[HarmonyPostfix]
	[HarmonyPatch(typeof(MachineDesignerUI), nameof(MachineDesignerUI.InitSumbnails))]
	public static void ExtendDesignerThumbnailButtons(MachineDesignerUI __instance)
	{
		int totalModuleCount = 0;
		foreach (var item in moduleLists) totalModuleCount += item.Count;

		Button sampleBtn = __instance.AllSumbnails[0];

		var newArr = new Il2CppReferenceArray<Button>(__instance.AllSumbnails.Count + totalModuleCount);

		for (int i = 0; i < __instance.AllSumbnails.Count + totalModuleCount; i++)
		{
			if (i < __instance.AllSumbnails.Count)
				newArr[i] = __instance.AllSumbnails[i];
			else
			{
				var newBtn = GameObject.Instantiate(sampleBtn.gameObject);
				newBtn.transform.SetParent(sampleBtn.transform.parent);
				newArr[i] = newBtn.GetComponent<Button>();
			}
		}
	}

	public static GameObject ModLoadModule(string key, Shader shader = null)
	{
		GameObject module = GameObject.Instantiate(Plugin.assets.LoadAsset<GameObject>("Dragonite3.prefab"));

		MeshRenderer moduleMesh = module.transform.GetChild(0).GetComponent<MeshRenderer>();

		ModuleData moduleData = module.AddComponent<ModuleData>();
		moduleData.Data = moduleDefs[key];
		moduleData._FileName = "Module/" + moduleDefs[key].name;
		moduleData.AllRenderer = new([moduleMesh]);

		ModuleEffects moduleEffects = module.AddComponent<ModuleEffects>();
		moduleEffects._MD = moduleData;

		GameObject moduleSelectorBox = module.transform.GetChild(0).GetChild(0).gameObject;
		moduleSelectorBox.layer = 29; // "Module"

		CollisionFitter colFitter = moduleSelectorBox.transform.GetChild(0).gameObject.AddComponent<CollisionFitter>();
		colFitter.targetCollider = moduleSelectorBox.GetComponent<BoxCollider>();

		if (shader) foreach (var mat in moduleMesh.materials) // This didn't seem necessary when I loaded everything from scratch.
		{
			mat.shader = shader;
			mat.SetFloat("_MetallicPow", 0f);
		}

		return module;
	}
}