using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.VFX;

namespace ExpansionMod;

public class ModModuleLoader : MonoBehaviour
{
	public static ModModuleLoader instance;

	public static List<Sprite> thumbnails = [];
	public static Dictionary<string, GameObject> markers = new()
	{
		{"Thrust", null},
		{"Det", null},
		{"Connector", null},
	};

	public void Awake()
	{
		instance = this;
		DontDestroyOnLoad(this);
	}

	public static IEnumerator ModLoadModule(MachineDesignerMain MDM, string key)
	{
		GameObject module = null;

		var handle = AsyncHandler.LoadAsync<GameObject>(key);
		handle.execute = true;
		handle.OnLoadFailedEvent += () => throw new System.Exception($"Failed to load module {key}");
		handle.OnLoadCompleteEvent += () =>
		{
			module = handle.handle.Result.Cast<GameObject>();
		};

		yield return new WaitUntil((Func<bool>)(() => module != null));

		SetupModule(module, key);

		yield return module;
	}

	public static GameObject ModLoadModuleSync(string key)
	{
		var op = Addressables.LoadAssetAsync<GameObject>(key);
		GameObject module = op.WaitForCompletion();

		SetupModule(module, key);

		op.Release();
		return module;
	}

	public static void SetupModule(GameObject module, string key)
	{
		ModuleData moduleData = module.GetComponent<ModuleData>();
		moduleData._FileName = "Module/" + key;

		BoxCollider moduleSelector = module.GetComponentInChildren<BoxCollider>();
		if (!moduleSelector)
			Plugin.Log.LogError($"Module {key} has no BoxCollider selector");

		if (Main.mechShader)
			foreach (var mats in moduleData.AllRenderer)
			{
				foreach (var mat in mats.materials)
				{
					mat.shader = Main.mechShader;
					mat.SetFloat("_MetallicPow", 0f);
				}
			}

		ReplaceAssets(module, moduleData);
	}

	public static void ReplaceAssets(GameObject module, ModuleData data)
	{
		// Connectors
		foreach (ConnectTarget CT in data.ConnectorsCT)
		{
			SetupConnector(CT);
		}

		// Accelerators
		if (data.Data._ThrusterType == ModuleDataDef.ThrusterTypeList.Acc || data.Data._ThrusterType == ModuleDataDef.ThrusterTypeList.Det)
		{
			SetupThruster(module, data);
		}

		// Emitters
		if (data.Data._Category == (TranslationList.CategoryTranslationKey)1)
		{
			SetupEmitter(module, data);
		}
	}

	public static void SetupConnector(ConnectTarget CT)
	{
		// Plugin.LogInfo(markers["Connector"].gameObject);
		GameObject marker = Instantiate<GameObject>(markers["Connector"].gameObject);
		marker.transform.localPosition = CT._Marker.gameObject.transform.localPosition;
		marker.transform.rotation = CT._Marker.gameObject.transform.rotation;
		marker.transform.SetParent(CT._Marker.gameObject.transform.parent);
		DestroyImmediate(CT._Marker);
		CT._Marker = marker;
	}
	public static void SetupThruster(GameObject module, ModuleData data)
	{
		var thrusterInfos = module.GetComponents<ThrusterInfo>();
		foreach (ThrusterInfo thruster in thrusterInfos)
		{
			if (thruster._Trail)
			{
				// Plugin.LogInfo(ModAssetHandler.vfxAssets[thruster._Trail.visualEffectAsset.name]);
				thruster._Trail.visualEffectAsset = ModAssetHandler.vfxAssets[thruster._Trail.visualEffectAsset.name];
			}

			foreach (VisualEffect vfx in thruster._VFXs)
			{
				// Plugin.LogInfo(ModAssetHandler.vfxAssets[vfx.visualEffectAsset.name]);
				vfx.visualEffectAsset = ModAssetHandler.vfxAssets[vfx.visualEffectAsset.name];
			}

			GameObject SDS = null;
			if (data.Data._ThrusterType == ModuleDataDef.ThrusterTypeList.Acc) SDS = markers["Thrust"];
			if (data.Data._ThrusterType == ModuleDataDef.ThrusterTypeList.Det) SDS = markers["Det"];
			SDS = Instantiate<GameObject>(SDS.gameObject);
			SDS.transform.localPosition = thruster._ThrusterDirSign.gameObject.transform.position;
			SDS.transform.rotation = thruster._ThrusterDirSign.gameObject.transform.rotation;
			SDS.transform.SetParent(thruster._ThrusterDirSign.gameObject.transform.parent);
			DestroyImmediate(thruster._ThrusterDirSign);
			thruster._ThrusterDirSign = SDS;
		}
	}
	public static void SetupEmitter(GameObject module, ModuleData data)
	{
		UsingController UC = module.GetComponent<UsingController>();
		// Plugin.LogInfo(UC._VFX.visualEffectAsset.name);
		UC._VFX.visualEffectAsset = ModAssetHandler.vfxAssets["EmtterUse_Basic"];//UC._VFX.visualEffectAsset.name];
		UC._chargingVFX.visualEffectAsset = ModAssetHandler.vfxAssets["EmitterUse_Charging"];//UC._chargingVFX.visualEffectAsset.name];
		UC._chargingSE.clip = ModAssetHandler.audioClips[UC._chargingSE.clip.name];
	}

	public static Renderer GetMesh(Transform xform)
	{
		for (int i = 0; i < xform.childCount; i++)
		{
			Renderer mesh = xform.GetChild(i).GetComponent<Renderer>();
			if (mesh) return mesh;
		}
		return null;
	}
	public static BoxCollider GetBoxCollider(Transform xform)
	{
		BoxCollider box = xform.GetComponent<BoxCollider>();
		if (!box && xform.childCount > 0)
			return GetBoxCollider(xform.GetChild(0));
		return box; // null or BoxCollider
	}
}