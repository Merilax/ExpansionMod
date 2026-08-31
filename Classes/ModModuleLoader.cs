using System.Collections.Generic;
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
	public static void Start()
	{
		
	}

	public static void SetupModule(GameObject module, string key)
	{
		ModuleData moduleData = module.GetComponent<ModuleData>();
		Plugin.LogDebug(key);
		Plugin.LogDebug(moduleData._FileName);

		if (key.StartsWith("Module/")) moduleData._FileName = key;
		else moduleData._FileName = "Module/" + key;


		BoxCollider moduleSelector = module.GetComponentInChildren<BoxCollider>();
		if (!moduleSelector)
			Plugin.Log.LogError($"Module {key} has no BoxCollider selector");

		string[] validMaterials = ["M_Main", "M_Sub", "M_Mecha", "M_Accent", "M_Light", "M_Belly", "M_Tube", "M_SubLight", "M_Tentacle"];
		if (Main.mechShader)
			foreach (var mats in moduleData.AllRenderer)
			{
				foreach (var mat in mats.materials)
				{
					foreach (var validMat in validMaterials)
					{
						if (mat.name.Contains(validMat))
						{
							mat.shader = Main.mechShader;
							mat.SetFloat("_MetallicPow", 0f);
						}
					}
				}
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
		newMarker.transform.localPosition = new(0,0,.007f);//CT._Marker.gameObject.transform.localPosition;
		newMarker.transform.localRotation = CT._Marker.gameObject.transform.localRotation;
		if (CT._ConnectType == ConnectTarget.ConnectType.Half) newMarker.transform.localScale = new(.68f, .68f, .68f);
		DestroyImmediate(CT._Marker);
		CT._Marker = newMarker;
	}
	public static void SetupThruster(GameObject module, ModuleData data)
	{
		var thrusterInfos = module.GetComponents<ThrusterInfo>();
		foreach (ThrusterInfo thruster in thrusterInfos)
		{
			if (thruster._Trail)
			{
				Plugin.LogDebug(ModAssetHandler.vfxAssets[thruster._Trail.visualEffectAsset.name]);
				try
				{
					thruster._Trail.visualEffectAsset = ModAssetHandler.vfxAssets[thruster._Trail.visualEffectAsset.name];
				}
				catch (System.Exception) { }
			}

			foreach (VisualEffect vfx in thruster._VFXs)
			{
				Plugin.LogDebug(ModAssetHandler.vfxAssets[vfx.visualEffectAsset.name]);
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