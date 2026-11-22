using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using HarmonyLib;
using UnityEngine;
using UnityEngine.VFX;
using UniverseLib;
using UniverseLib.Utility;

namespace ArchExpansionMod;

public class ModMachineBuildManager
{
	// public struct Palette()
	// {
	// 	public Color glass = new(.92f, .92f, .92f), extra1 = new(.92f, .92f, .92f), extra2 = new(.92f, .92f, .92f), extra3 = new(.92f, .92f, .92f);
	// }
	public struct UsedBundles()
	{
		public Dictionary<string, int> moduleCounts = [];
	}
	public struct CustomMachineData()
	{
		// public Palette mainPalette = new(), subPalette = new();
		public List<MaterialData> mainMatData = [new(), new(), new(), new()];
		public List<MaterialData> subMatData = [new(), new(), new(), new()];
	}

	public static UsedBundles usedBundles = new();

	public static Shader mechShader;
	public static Material[] mainMaterials = [null, null, null, null];
	public static Material[] subMaterials = [null, null, null, null];

	private static CustomMachineData customMachineData = new();
	private static int currentEditMaterialIndex = -1;

	private static bool setShaderOnce = true;
	public static void SetMechShader(Shader shader)
	{
		if (!setShaderOnce) return;

		mechShader = shader;

		mainMaterials[0] = new(Plugin.assets.LoadAsset<Material>("M_Glass"));
		mainMaterials[1] = new(Plugin.assets.LoadAsset<Material>("M_Extra1"));
		mainMaterials[2] = new(Plugin.assets.LoadAsset<Material>("M_Extra2"));
		mainMaterials[3] = new(Plugin.assets.LoadAsset<Material>("M_Extra3"));

		mainMaterials[1].shader = mechShader;
		mainMaterials[1].SetFloat("_MetallicPow", 0f);
		mainMaterials[2].shader = mechShader;
		mainMaterials[2].SetFloat("_MetallicPow", 0f);
		mainMaterials[3].shader = mechShader;
		mainMaterials[3].SetFloat("_MetallicPow", 0f);

		subMaterials[0] = new(Plugin.assets.LoadAsset<Material>("S_Glass"));
		subMaterials[1] = new(Plugin.assets.LoadAsset<Material>("S_Extra1"));
		subMaterials[2] = new(Plugin.assets.LoadAsset<Material>("S_Extra2"));
		subMaterials[3] = new(Plugin.assets.LoadAsset<Material>("S_Extra3"));

		subMaterials[1].shader = mechShader;
		subMaterials[1].SetFloat("_MetallicPow", 0f);
		subMaterials[2].shader = mechShader;
		subMaterials[2].SetFloat("_MetallicPow", 0f);
		subMaterials[3].shader = mechShader;
		subMaterials[3].SetFloat("_MetallicPow", 0f);

		setShaderOnce = false;
	}

	public static bool ParseModule(string key)
	{
		try
		{
			var modDef = ModAssetHandler.moduleDefLists.First(e => e.moduleName == key);
			if (!usedBundles.moduleCounts.ContainsKey(modDef.bundleName))
				usedBundles.moduleCounts.Add(modDef.bundleName, 0);

			usedBundles.moduleCounts[modDef.bundleName] += 1;
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(BuildMachine._ExecuteBuildMachine_Q_d__47), nameof(BuildMachine._ExecuteBuildMachine_Q_d__47.MoveNext))]
	private static void OnExecuteBuildMachine_Pre1(BuildMachine._ExecuteBuildMachine_Q_d__47 __instance)
	{
		if (__instance.__1__state != 0) return;
		OnLoadMachine(__instance.__4__this.FileName, __instance.__4__this);
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(BuildMachine._ExecuteBuildMachine_Q_d__48), nameof(BuildMachine._ExecuteBuildMachine_Q_d__48.MoveNext))]
	private static void OnExecuteBuildMachine_Pre2(BuildMachine._ExecuteBuildMachine_Q_d__48 __instance)
	{
		if (__instance.__1__state != 0) return;
		OnLoadMachine(__instance.__4__this.FileName, __instance.__4__this);
	}

	private static void OnLoadMachine(string FileName, BuildMachine build = null)
	{
		string slotNum = FileName[11..14];

		if (File.Exists(Path.Combine(Plugin.CUSTOM_MACHINE_PATH, $"Arc{slotNum}", $"Arc{slotNum}.json")))
		{
			string stream = File.ReadAllText(Path.Combine(Plugin.CUSTOM_MACHINE_PATH, $"Arc{slotNum}", $"Arc{slotNum}.json"));
			CustomMachineData data = JsonSerializer.Deserialize<CustomMachineData>(stream, Plugin.jsonSerializerOptions);
			UpdateCustomMachineData(data, build);
		}
		else
		{
			OnSaveMachine(FileName);
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(BuildMachine), nameof(BuildMachine.SaveBuildData))]
	public static void OnSaveMachine(string FileName)
	{
		string slotNum = FileName[11..14];
		string json = JsonSerializer.Serialize(customMachineData, Plugin.jsonSerializerOptions);

		Directory.CreateDirectory(Path.Combine(Plugin.CUSTOM_MACHINE_PATH, $"Arc{slotNum}"));

		File.WriteAllText(Path.Combine(Plugin.CUSTOM_MACHINE_PATH, $"Arc{slotNum}", $"Arc{slotNum}.json"), json);
	}

	private static void UpdateCustomMachineData(CustomMachineData data, BuildMachine build = null)
	{
		customMachineData = data;
		// mainMaterials[0].color = data.mainMatData[0]._color;
		// mainMaterials[1].color = data.mainMatData[1]._color;
		// mainMaterials[2].color = data.mainMatData[2]._color;
		// mainMaterials[3].color = data.mainMatData[3]._color;	
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(MachineDesignerUI), nameof(MachineDesignerUI.Paint_OpenMatSettings))]
	public static void MachineDesignerUI_OpenMatSettings(MachineDesignerUI __instance, int MatNum, ref bool __runOriginal)
	{
		if (MatNum > 6)
		{
			__runOriginal = false;
			__instance.Paint_MatSetting_Sectors[1].active = true; // Metallic
			__instance.Paint_MatSetting_Sectors[2].active = MatNum != 7; // Pearl
			__instance.Paint_MatSetting_Sectors[3].active = MatNum != 7; // Matte
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(MachineDesignerMain), nameof(MachineDesignerMain.Paint_StartMaterialEdit))]
	public static void MachineDesignerMain_StartMaterialEdit(MachineDesignerMain __instance, int num, ref bool __runOriginal)
	{
		currentEditMaterialIndex = num - 7;
		if (num >= 7 && num <= 10)
		{
			__runOriginal = false;
			List<MaterialData> materialDatas = __instance.CurrentPaletteIsMain ? customMachineData.mainMatData : customMachineData.subMatData;
			MaterialData matData = materialDatas[num - 7];
			__instance._UI.Paint_RefreshMaterialSettingUI(matData);
			__instance.CurrentEditMaterial = matData;
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(BuildMachine), nameof(BuildMachine.RefreshMats))]
	private static void BuildMachine_RefreshMats(BuildMachine __instance, bool IsMain, ref bool __runOriginal)
	{

		List<MaterialData> materialDatas = IsMain ? customMachineData.mainMatData : customMachineData.subMatData;
		Material[] materials = IsMain ? mainMaterials : subMaterials;
		for (int i = 0; i < 4; i++)
		{
			__instance.RefreshCompositeMechShader(materials[i], materialDatas[i], materialDatas[i]._color);
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(MachineDesignerUI), nameof(MachineDesignerUI.Paint_RefreshMaterialData))]
	private static void MachineDesignerUI_Paint_RefreshMaterialData(MachineDesignerUI __instance, MaterialData md)
	{
		if (currentEditMaterialIndex < 0) return;
		List<MaterialData> materialDatas = __instance.MDM.CurrentPaletteIsMain ? customMachineData.mainMatData : customMachineData.subMatData;

		CopyMatData(md, materialDatas[currentEditMaterialIndex]);
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(BuildMachine), nameof(BuildMachine.RefreshCompositeMechShader))]
	private static void BuildMachine_RefreshCompositeMechShader(Material m, MaterialData md, ref bool __runOriginal)
	{
		if (m.shader.name != "Shader Graphs/CompositeMechShader")
		{
			__runOriginal = false;
			m.SetVector("_BaseColor", md._color);
			m.SetFloat("_Metallic", md._metallic);
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(BuildMachine), nameof(BuildMachine.SetPalette))]
	private static void SetPalette(Renderer Ren, bool IsSub, ref bool __runOriginal)
	{
		if (Ren.GetActualType() == typeof(VFXRenderer)) return;
		ReassignMaterials(Ren, IsSub);
	}
	[HarmonyPostfix]
	[HarmonyPatch(typeof(BuildMachine), nameof(BuildMachine.Preview_SetMaterial))]
	private static void LoadMaterialForDiorama(Transform root)
	{
		var renders = root.GetComponentsInChildren<Renderer>();
		foreach (var render in renders)
		{
			if (render.GetActualType() == typeof(VFXRenderer)) continue;
			bool IsSub = MyUtility.GetModuleDataInParent(render.transform)._SubPalette;
			ReassignMaterials(render, IsSub);
		}
	}
	private static void ReassignMaterials(Renderer Ren, bool IsSub)
	{
		Material[] myMats = IsSub ? subMaterials : mainMaterials;
		Il2CppSystem.Collections.Generic.List<Material> newMats = new(myMats.Length);
		for (int i = 0; i < Ren.materials.Count; i++)
		{
			bool skip = false;
			Material mat = Ren.materials[i];
			for (int j = 0; j < myMats.Length; j++)
			{
				if (mat.name.Contains(myMats[j].name[2..]))
				{
					newMats.Add(myMats[j]);
					skip = true;
					break;
				}
			}
			if (skip) continue;
			newMats.Add(mat);
		}
		Ren.SetSharedMaterials(newMats);
	}

	private static void CopyMatData(MaterialData source, MaterialData target)
	{
		target._color = source._color;
		target._metallic = source._metallic;
		target._pearl = source._pearl;
		target._mat = source._mat;
		target._dirt = source._dirt;
		target._rust = source._rust;
	}
}

public class ColorConverter : JsonConverter<Color>
{
	public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		string val = reader.GetString();
		string[] arr = val.Split(':');
		try
		{
			return new Color(float.Parse(arr[0]), float.Parse(arr[1]), float.Parse(arr[2]), float.Parse(arr[3]));
		}
		catch (Exception)
		{
			return Color.black;
		}
	}

	public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
	{
		string val = $"{value.r}:{value.g}:{value.b}:{value.a}";
		writer.WriteStringValue(val);
	}
}

public class MaterialDataConverter : JsonConverter<MaterialData>
{
	private struct MaterialDataCopy
	{
		public Color _color;
		public float _mat, _metallic, _pearl;
		public float[] _dirt, _rust;

	}
	public override MaterialData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		MaterialDataCopy wrapper = JsonSerializer.Deserialize<MaterialDataCopy>(reader.GetString(), Plugin.jsonSerializerOptions);
		try
		{
			return new MaterialData()
			{
				_color = wrapper._color,
				_mat = wrapper._mat,
				_metallic = wrapper._metallic,
				_pearl = wrapper._pearl,
				_dirt = wrapper._dirt,
				_rust = wrapper._rust,
			};
		}
		catch (Exception)
		{
			return new MaterialData();
		}
	}

	public override void Write(Utf8JsonWriter writer, MaterialData value, JsonSerializerOptions options)
	{
		MaterialDataCopy wrapper = new()
		{
			_color = value._color,
			_mat = value._mat,
			_metallic = value._metallic,
			_pearl = value._pearl,
			_dirt = value._dirt,
			_rust = value._rust,
		};
		string json = JsonSerializer.Serialize(wrapper, Plugin.jsonSerializerOptions);
		writer.WriteStringValue(json);
	}
}