using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArchEmperorLib;

namespace ArchExpansionMod;

public class MachineDesignerUIPatch
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(MachineDesignerUI), nameof(MachineDesignerUI.Start))]
	private static void MachineDesignerUI_Start(MachineDesignerUI __instance)
	{
		GameObject sampleColorEdit = __instance.Paint_PaletteEditGp.transform.GetChild(0).gameObject;
		Vector3 tempPos = __instance.Paint_PaletteEditGp.transform.GetChild(6).localPosition;
		// Quaternion tempRot = __instance.Paint_PaletteEditGp.transform.GetChild(6).rotation;

		string[] btnNames = ["GLASS", "EXTRA 1", "EXTRA 2", "EXTRA 3"];
		for (int i = 0; i < 4; i++)
		{
			GameObject go = GameObject.Instantiate(original: sampleColorEdit, parent: __instance.Paint_PaletteEditGp.transform);
			go.transform.localPosition = new(tempPos.x, tempPos.y - (45 * (i + 1)), tempPos.z);
			Utils.RescaleUI(go);
			Button newBtn = go.GetComponent<Button>();
			newBtn.onClick.m_PersistentCalls.Clear();
			int temp = 7 + i;
			newBtn.onClick.AddListener((Action)(() => __instance.Paint_OpenMatSettings(temp)));
			go.GetComponentInChildren<TextMeshProUGUI>().text = btnNames[i];
		}

		tempPos = __instance.Paint_PaletteEditGp.transform.GetChild(7).localPosition;
		__instance.Paint_PaletteEditGp.transform.GetChild(7).localPosition = new(tempPos.x, tempPos.y - (45 * 4), tempPos.z);
		tempPos = __instance.Paint_PaletteEditGp.transform.GetChild(8).localPosition;
		__instance.Paint_PaletteEditGp.transform.GetChild(8).localPosition = new(tempPos.x, tempPos.y - (45 * 4), tempPos.z);
	}
}