using System;
using System.Collections.Generic;
using System.IO;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.VFX;

namespace ExpansionMod;

public class ModAssetHandler
{
	static readonly string[] categories = { "Armors", "Coolers", "Generators", "Radars", "Thrusters", "Physics", "Specials", "Weapons", "Joints", "Ornaments" };
	public static Dictionary<string, VisualEffectAsset> vfxAssets = new()
	{
		{"BoostEffects_Trail", null},
		{"BoostEffects_Round", null},
		{"BoostEffects_Square", null},
		{"BoostEffects_Detonator", null},
		{"EmtterUse_Basic", null},
		{"EmitterUse_Charging", null},
	};
	public static Dictionary<string, AudioClip> audioClips = new()
	{
		{"BA3SE09_Charging_in_2", null},
	};

	public static void PrepareAssets()
	{
		foreach (var modDef in ModBundleParser.pendingLoadModDefs)
		{
			Plugin.LogInfo($"Initializing mod bundle {modDef.bundleName}...");

			var map = new ResourceLocationMap($"{modDef.bundleName}ContentCatalog", modDef.moduleCount);

			var bundleLoc = new ResourceLocationBase(
				"bundleName",
				Path.Combine(Plugin.BUNDLE_ROOT, modDef.bundleName, modDef.bundleName),
				typeof(AssetBundleProvider).FullName,
				Il2CppType.From(typeof(IAssetBundleResource)));

			map.Add("bundleName", bundleLoc.Cast<IResourceLocation>());

			// var provider = new ModuleTranslationResourceProvider();
			// var providers = Addressables.ResourceManager.ResourceProviders;
			// providers.Cast<Il2CppSystem.Collections.Generic.ICollection<IResourceProvider>>().Add(provider.Cast<IResourceProvider>());

			var deps = new Il2CppReferenceArray<IResourceLocation>([bundleLoc.Cast<IResourceLocation>()]);

			int loadedModules = 0;

			for (int i = 0; i < 10; i++)
			{
				List<string> category;
				switch (i)
				{
					case 0: category = modDef.armors; break;
					case 1: category = modDef.coolers; break;
					case 2: category = modDef.generators; break;
					case 3: category = modDef.radars; break;
					case 4: category = modDef.thrusters; break;
					case 5: category = modDef.physics; break;
					case 6: category = modDef.specials; break;
					case 7: category = modDef.weapons; break;
					case 8: category = modDef.joints; break;
					case 9: category = modDef.ornaments; break;
					default: continue;
				}
				foreach (var itemName in category)
				{
					string moduleRoot = $"Assets/{modDef.bundleName}/Modules/{categories[i]}/";
					try
					{
						Plugin.LogInfo($"Registering module definition {itemName}...");
						if (itemName.Length <= 0) continue;

						ModuleList moduleList = new() { _MdName = $"{modDef.bundleName}_{itemName}_DataDef", _name = $"{modDef.bundleName}_{itemName}" };

						RegisterAsset<GameObject>($"Module/{moduleList._name}", $"{moduleRoot}{itemName}/Module.prefab", typeof(BundledAssetProvider), map, deps);
						RegisterAsset<Sprite>($"Sumb_{moduleList._name}", $"{moduleRoot}{itemName}/Thumb.png", typeof(BundledAssetProvider), map, deps);
						RegisterAsset<ModuleDataDef>($"{moduleList._MdName}", $"{moduleRoot}{itemName}/DataDef.asset", typeof(BundledAssetProvider), map, deps);
						// RegisterAsset<TranslationTable>($"Translation_{moduleList._name}", $"{moduleRoot}{itemName}/Translation.asset", typeof(BundledAssetProvider), map, deps);

						// ModuleDefs.translationAssetKeys.Add($"Translation_{moduleList._name}");
						ModuleRegistrator.moduleLists[i].Add(moduleList);
						loadedModules++;
					}
					catch (Exception ex)
					{
						Plugin.Log.LogError($"An error has ocurred when registering module definition {itemName}:");
						Plugin.Log.LogError(ex);
						continue;
					}
				}
			}
			Addressables.AddResourceLocator(map.Cast<IResourceLocator>());

			ModBundleParser.loadedModDefs.Add(modDef);
			ModBundleParser.registeredPrefixes.Add(modDef.bundleName);

			Plugin.LogInfo($"Mod bundle {modDef.bundleName} initialized successfully with {loadedModules} modules.");
		}
	}

	public static void RegisterAsset<T>(string key, string path, Type provider, ResourceLocationMap map, Il2CppReferenceArray<IResourceLocation> deps = null)
	{
		var assetLoc = new ResourceLocationBase(
			key,
			path, // internal id
			provider.FullName,
			Il2CppType.Of<T>(),
			deps);

		map.Add(key, assetLoc.Cast<IResourceLocation>());
	}

	public static void AcquireVanillaAssets()
	{
		Plugin.LogInfo("Acquiring vanilla assets. This should only take a few seconds.");
		// Note: cloning the marker objects through Instantiate() gets them destroyed afterwards.

		GameObject module;

		// Thrusters
		module = Addressables.LoadAssetAsync<GameObject>("Module/Thr_01").WaitForCompletion();
		ModModuleLoader.markers["Thrust"] = module.GetComponent<ThrusterInfo>()._ThrusterDirSign;
		vfxAssets["BoostEffects_Trail"] = module.GetComponent<ThrusterInfo>()._Trail.visualEffectAsset;
		vfxAssets["BoostEffects_Round"] = module.GetComponent<ThrusterInfo>()._VFXs[0].visualEffectAsset;

		module = Addressables.LoadAssetAsync<GameObject>("Module/Thr_02").WaitForCompletion();
		vfxAssets["BoostEffects_Square"] = module.GetComponent<ThrusterInfo>()._VFXs[0].visualEffectAsset;

		// Detonators
		module = Addressables.LoadAssetAsync<GameObject>("Module/Thr_22").WaitForCompletion();
		ModModuleLoader.markers["Det"] = module.GetComponent<ThrusterInfo>()._ThrusterDirSign;
		vfxAssets["BoostEffects_Detonator"] = module.GetComponent<ThrusterInfo>()._VFXs[0].visualEffectAsset;

		// Emitters
		module = Addressables.LoadAssetAsync<GameObject>("Module/Wep_01").WaitForCompletion();
		vfxAssets["EmtterUse_Basic"] = module.GetComponent<UsingController>()._VFX.visualEffectAsset;
		vfxAssets["EmitterUse_Charging"] = module.GetComponent<UsingController>()._chargingVFX.visualEffectAsset;
		audioClips["BA3SE09_Charging_in_2"] = module.GetComponent<UsingController>()._chargingSE.clip;
		ModModuleLoader.markers["Connector"] = module.GetComponent<ModuleData>().ConnectorsCT[0]._Marker;

		Plugin.LogInfo("Acquired vanilla assets.");
	}
}