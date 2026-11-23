using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.VFX;

namespace ArchExpansionMod;

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

	public static void SetupModule(GameObject module, string key)
	{
		ModuleData moduleData = module.GetComponent<ModuleData>();

		if (key.StartsWith("Module/")) moduleData._FileName = key;
		else moduleData._FileName = "Module/" + key;
		Plugin.LogDebug("Key: " + key);

		BoxCollider moduleSelector = module.GetComponentInChildren<BoxCollider>();
		if (!moduleSelector)
			Plugin.Log.LogError($"Module {key} has no BoxCollider selector.");

		string[] replacedShaders = ["M_Main", "M_Sub", "M_Mecha", "M_Accent", "M_Light", "M_Belly", "M_Tube", "M_Tentacle"];
		string[] replacedMaterials = ["M_Glass", "M_Extra1", "M_Extra2", "M_Extra3"];

		foreach (Renderer render in moduleData.AllRenderer)
		{
			Il2CppSystem.Collections.Generic.List<Material> mats = new(render.sharedMaterials.Count);

			for (int i = 0; i < render.sharedMaterials.Count; i++)
			{
				Material mat = render.sharedMaterials[i];
				string matName = mat.name;

				for (int j = 0; j < replacedMaterials.Length; j++)
				{
					if (matName.StartsWith(replacedMaterials[j]))
					{
						mat = ModMachineBuildManager.mainMaterials[j];
						mats.Add(mat);
						break;
					}
				}

				for (int j = 0; j < replacedShaders.Length; j++)
				{
					if (matName.StartsWith(replacedShaders[j]))
					{
						mat.shader = ModMachineBuildManager.mechShader;
						mat.SetFloat("_MetallicPow", 0f);
						mats.Add(mat);
						break;
					}
				}
			}
			render.SetSharedMaterials(mats);
		}

		ReplaceAssets(module, moduleData);
	}

	public static void ReplaceAssets(GameObject module, ModuleData data)
	{
		// Connectors
		foreach (ConnectTarget CT in data.ConnectorsCT)
			SetupConnector(CT);
		// Accelerators
		if (data.Data._ThrusterType == ModuleDataDef.ThrusterTypeList.Acc || data.Data._ThrusterType == ModuleDataDef.ThrusterTypeList.Det)
			SetupThruster(module, data);
		// Emitters
		if (data.Data._Category == (TranslationList.CategoryTranslationKey)1)
			SetupEmitter(module, data);
	}

	public static void SetupConnector(ConnectTarget CT)
	{
		// Plugin.LogDebug(markers["Connector"].gameObject);
		GameObject newMarker = Instantiate<GameObject>(markers["Connector"].gameObject);
		newMarker.transform.SetParent(CT._Marker.gameObject.transform.parent);
		newMarker.transform.localPosition = new(0, 0, .007f);//CT._Marker.gameObject.transform.localPosition;
		newMarker.transform.localRotation = CT._Marker.gameObject.transform.localRotation;
		if (CT._ConnectType == ConnectTarget.ConnectType.Half) newMarker.transform.localScale = new(.68f, .68f, .68f);
		DestroyImmediate(CT._Marker);
		CT._Marker = newMarker;
	}
	public static void SetupThruster(GameObject module, ModuleData data)
	{
		var thrusterInfos = module.GetComponents<ThrusterInfo>();
		if (thrusterInfos.Count == 0)
		{
			Plugin.Log.LogError($"Module {data._FileName} failed to load: No ThrusterInfo component was found.");
			return;
		}
		foreach (ThrusterInfo thruster in thrusterInfos)
		{
			if (thruster._Trail)
			{
				// Plugin.LogDebug(ModAssetHandler.vfxAssets[thruster._Trail.visualEffectAsset.name]);
				try
				{
					thruster._Trail.visualEffectAsset = ModAssetHandler.vfxAssets[thruster._Trail.visualEffectAsset.name];
				}
				catch (System.Exception) { }
			}

			foreach (VisualEffect vfx in thruster._VFXs)
			{
				// Plugin.LogDebug(ModAssetHandler.vfxAssets[vfx.visualEffectAsset.name]);
				try
				{
					vfx.visualEffectAsset = ModAssetHandler.vfxAssets[vfx.visualEffectAsset.name];
				}
				catch (System.Exception) { }
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
		if (UC == null)
		{
			Plugin.Log.LogError($"Module {data._FileName} failed to load: No UsingController component was found.");
			return;
		}
		Plugin.LogDebug(UC._VFX.visualEffectAsset.name);
		try
		{
			UC._VFX.visualEffectAsset = ModAssetHandler.vfxAssets[UC._VFX.visualEffectAsset.name];
		}
		catch (System.Exception) { }
		try
		{
			UC._chargingVFX.visualEffectAsset = ModAssetHandler.vfxAssets[UC._chargingVFX.visualEffectAsset.name];
		}
		catch (System.Exception) { }
		try
		{
			UC._chargingSE.clip = ModAssetHandler.audioClips[UC._chargingSE.clip.name];
		}
		catch (System.Exception) { }
	}

	public static BoxCollider GetBoxCollider(Transform xform)
	{
		BoxCollider box = xform.GetComponent<BoxCollider>();
		if (!box && xform.childCount > 0)
			return GetBoxCollider(xform.GetChild(0));
		return box; // null or BoxCollider
	}
}