using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace FactoryLocator
{
    public class WarningSystemPatch
    {
		public static bool Enable { get; set; } = true;

		const int INDEXUPPERBOND = -20000;
		private static int tmpSigalId = 0;
		private static int tick = 3;

		[HarmonyPostfix]
		[HarmonyPatch(typeof(WarningSystem), nameof(WarningSystem.Import))]
		internal static void Import(WarningSystem __instance)
		{
			for (int i = 1; i < __instance.warningCursor; i++)
			{
				if (__instance.warningPool[i].factoryId < INDEXUPPERBOND)
				{
					// recalculate detailId
					__instance.warningPool[i].detailId = -__instance.warningPool[i].factoryId + INDEXUPPERBOND;
				}
			}
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(UIWarningWindow), nameof(UIWarningWindow.Determine))]
		internal static void Determine(ref bool open)
        {
			open &= Enable;
        }

		[HarmonyPostfix]
		[HarmonyPatch(typeof(UIWarningWindow), nameof(UIWarningWindow._OnClose))]
		internal static void OnClose()
		{
			UIwarningTip.Destory();
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(UIWarningWindow), nameof(UIWarningWindow._OnLateUpdate))]
		internal static void OnLateUpdate(UIWarningWindow __instance)
		{
			if (tmpSigalId != 0)
            {
				__instance.selectedSignalId = tmpSigalId;
				if (--tick <= 0)
				{
					tmpSigalId = 0;
				}
			}

			if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.Mouse1))
			{
				Camera worldCamera = UIRoot.instance.overlayCanvas.worldCamera;
				RectTransform rect = (RectTransform)__instance.itemGroup.transform;
				if (RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, worldCamera))
				{
					for (int i = 0; i < __instance.itemCount; i++)
					{
						if (RectTransformUtility.RectangleContainsScreenPoint(__instance.itemEntries[i].rectTrans, Input.mousePosition, worldCamera))
						{
							UIwarningTip.Create(__instance.itemEntries[i], __instance.selectedSignalId);
							break;
						}
					}
				}
				else
                {
					UIwarningTip.Destory();
				}
			}
		}

		public static void AddWarningData(int signalId, int detailId, List<int> planetIds, List<Vector3> localPos, List<int> detailIds = null)
        {
			WarningSystem warningSystem = GameMain.data.warningSystem;
			
			// Extend capacity if needed
			int newCount = warningSystem.warningTotalCount + localPos.Count;
			int wanringCapacity = warningSystem.warningCapacity;
			while (newCount >= wanringCapacity)
				wanringCapacity *= 2;
			if (wanringCapacity > warningSystem.warningCapacity)
				warningSystem.SetWarningCapacity(wanringCapacity);

			// Insert new warningData in empty solts
			for (int i = 0; i < localPos.Count; i++)
            {
				int warningId = warningSystem.warningCursor;
				if (warningSystem.warningRecycleCursor > 0)
					warningId = warningSystem.warningRecycle[--warningSystem.warningRecycleCursor];
				else
					++warningSystem.warningCursor;
				
				ref WarningData warning = ref warningSystem.warningPool[warningId];
				warning.id = warningId;
				warning.state = 1; // ON
				warning.signalId = signalId; // Config
				warning.detailId = detailIds == null ? detailId : detailIds[i];
				warning.factoryId = INDEXUPPERBOND - warning.detailId; // a negative value so it won't get updated
				warning.astroId = planetIds[i]; // local pos reference plaent
				warning.localPos = localPos[i];
			}

			// Focus on new signalId
			UIWarningWindow window = UIRoot.instance.uiGame.warningWindow;
			window.selectedSignalId = signalId;
			tmpSigalId = signalId;
			tick = 3;

			Log.Debug($"Add {localPos.Count}. Cursor = {warningSystem.warningCursor} RecycleCursor = {warningSystem.warningRecycleCursor} Capacity = {warningSystem.warningCapacity}");
		}

		public static void ClearAll()
		{
			int count = 0;
			WarningSystem warningSystem = GameMain.data.warningSystem;
			for (int i = warningSystem.warningCursor - 1; i > 0; i--)
            {
				ref WarningData warning = ref warningSystem.warningPool[i];
				//Log.Debug($"[{i}] {warning.id} {warning.signalId} - {warning.factoryId}");
				if (warning.factoryId < INDEXUPPERBOND)
                {
					warning.SetEmpty();
					// If it is at the tail, just reduce cursor so no need to recycle
					if (i == warningSystem.warningCursor - 1)
						--warningSystem.warningCursor;
					else
						warningSystem.warningRecycle[warningSystem.warningRecycleCursor++] = i;
					count++;
				}

				// Try to shrink capcatiy
				int capacity = warningSystem.warningCapacity;
				while ((capacity / 2) > warningSystem.warningCursor)
                {
					capacity /= 2;
				}
				if (capacity != warningSystem.warningCapacity)
					warningSystem.SetWarningCapacity(capacity);
			}
			Log.Debug($"Remove {count}. Cursor = {warningSystem.warningCursor} RecycleCursor = {warningSystem.warningRecycleCursor} Capacity = {warningSystem.warningCapacity}");
		}

		public static void Debug()
		{
			Log.Info(UIRoot.instance.uiGame.warningWindow.selectedSignalId);
			WarningSystem warningSystem = GameMain.data.warningSystem;
			for (int i = 1; i < warningSystem.warningCursor; i++)
			{
				ref WarningData warning = ref warningSystem.warningPool[i];
				if (warning.factoryId < INDEXUPPERBOND)
				{
					Log.Debug($"[{i}] {warning.signalId} - {warning.factoryId}:" + warning.localPos);
				}
			}
		}
	}
}