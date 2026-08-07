using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace ExpansionMod;

public static class ModuleRegistrator
{
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

	public static List<string> translationAssetKeys = [];

	public static void PopulateModuleList()
	{
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

	public static void PopulateThumbnails()
	{
		foreach (var category in moduleLists)
		{
			foreach (var item in category)
			{
				var handle = AsyncHandler.LoadAsync<Sprite>("Sumb_" + item._name);//.Substring(7));
				handle.execute = true;
				handle.OnLoadCompleteEvent += () =>
				{
					Sprite sprite = Sprite.Instantiate(handle.handle.Result.Cast<Sprite>());
					ModModuleLoader.thumbnails.Add(sprite);
					// Pooler.ins.thumbnails.Add(item._name, sprite);
				};
				handle.OnLoadFailedEvent += () => throw new Exception("LOAD ERR");
			}
		}
	}

	public static void PopulateLocalization()
	{
		Plugin.LogInfo("Registering module translations...");
		var strDB = LocalizationSettings.Instance.GetStringDatabase();
		var tableChineseSimplified = strDB.GetTable("Shop", LocalizationSettings.AvailableLocales.Locales[0]);
		var tableChineseTraditional = strDB.GetTable("Shop", LocalizationSettings.AvailableLocales.Locales[1]);
		var tableEnglish = strDB.GetTable("Shop", LocalizationSettings.AvailableLocales.Locales[2]);
		var tableJapanese = strDB.GetTable("Shop", LocalizationSettings.AvailableLocales.Locales[3]);

		foreach (var modDef in ModBundleParser.loadedModDefs)
			foreach (var table in modDef.translationTables)
			{
				table.ID = "mod_" + table.ID;
				// Plugin.LogInfo("Injecting translation ID: " + table.ID);
				tableChineseSimplified.AddEntry(table.ID, table.SimplifiedChinese);
				tableChineseTraditional.AddEntry(table.ID, table.TraditionalChinese);
				tableEnglish.AddEntry(table.ID, table.English);
				tableJapanese.AddEntry(table.ID, table.Japanese);
			}
	}
}