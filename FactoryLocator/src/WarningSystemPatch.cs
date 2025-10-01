﻿using HarmonyLib;
using System;
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
			/*
			Log.Debug($"Before shrink: {__instance.warningCursor} {__instance.warningRecycleCursor} {__instance.warningCapacity}");
			int cursor = __instance.warningCursor;
			while (--cursor > 0)
            {
				if (__instance.warningPool[cursor].id != 0)
					break;
			}
			__instance.warningCursor = cursor + 1;
			__instance.warningRecycleCursor = 0;
			for (int i = 1; i <= cursor; i++)
            {
				if (__instance.warningPool[i].id == 0)
					__instance.warningRecycle[__instance.warningRecycleCursor++] = i;
			}
			int capacity = __instance.warningCursor > 64 ? __instance.warningCursor : 64;
			__instance.SetWarningCapacity(__instance.warningCapacity);
			Log.Debug($"After shrink: {__instance.warningCursor} {__instance.warningRecycleCursor} {__instance.warningCapacity}");
			*/

			for (int i = 1; i < __instance.warningCursor; i++)
			{
				if (__instance.warningPool[i].factoryId <= INDEXUPPERBOND)
				{
					// recalculate detailId
					__instance.warningPool[i].detailId1 = -__instance.warningPool[i].factoryId + INDEXUPPERBOND;
				}
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(WarningSystem), nameof(WarningSystem.GameTick))]
		internal static void GameTick()
        {
			Plugin.mainLogic.GameTick();
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
							if (Input.GetKeyDown(KeyCode.Mouse0)) // Left click
							{
								if (VFInput.control) SeekNextSignal(__instance.selectedSignalId, __instance.itemEntries[i].detailId1);
								else UIwarningTip.Create(__instance.itemEntries[i], __instance.selectedSignalId);
							}
							else if (Input.GetKeyDown(KeyCode.Mouse1)) // Right click
							{
								HideGroup(__instance.selectedSignalId, __instance.itemEntries[i].detailId1);
							}
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

		[HarmonyPrefix]
		[HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.RemoveEntityWithComponents))]
		internal static void RemoveWarningByEntity(PlanetFactory __instance, int id)
		{
			int planetId = __instance.planetId;
			var ws = GameMain.data.warningSystem;
			for (int i = 1; i < ws.warningCursor; i++)
            {
				ref var warning = ref ws.warningPool[i];
				if (warning.factoryId <= INDEXUPPERBOND && warning.astroId == planetId)
                {
					if ((warning.localPos -__instance.entityPool[id].pos).sqrMagnitude < 1.0f)
					{
						ws.RemoveWarningData(i);
						Log.Debug($"Remove {i} on {planetId}");
					}
				}
            }
		}

		public static void AddWarningData(int signalId, int detailId, List<int> planetIds, List<Vector3> localPos, List<int> detailIds = null)
        {
			if (planetIds.Count != localPos.Count || (detailIds != null && detailIds.Count != localPos.Count))
			{
				Log.Warn($"Length mismatch! planetIds:{planetIds.Count} pos:{localPos.Count}");
				return;
			}

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
				
				int warningDetailId = detailIds == null ? detailId : detailIds[i];
				if (warningDetailId < 0)
                {
					Log.Warn($"warningDetailId {warningDetailId} < 0");
					return;
                }

				ref WarningData warning = ref warningSystem.warningPool[warningId];
				warning.id = warningId;
				warning.state = 1; // ON
				warning.signalId = signalId; // Config
				warning.detailId1 = warningDetailId;
				warning.factoryId = INDEXUPPERBOND - warning.detailId1; // a negative value so it won't get updated
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
				if (warning.factoryId <= INDEXUPPERBOND)
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
			if (count > 0)
				Log.Debug($"Remove {count}. Cursor = {warningSystem.warningCursor} RecycleCursor = {warningSystem.warningRecycleCursor} Capacity = {warningSystem.warningCapacity}");
		}

		public static void HideGroup(int signalId, int detailId)
        {
			var ws = GameMain.data.warningSystem;
			if (signalId > 0)
			{
				for (int i = 1; i < ws.warningCursor; i++)
				{
					ref var warning = ref ws.warningPool[i];
					if (warning.id == i && warning.state > 0 && warning.signalId == signalId && warning.detailId1 == detailId)
					{
						if (warning.factoryId <= INDEXUPPERBOND) // Only apply to query warning icons
						{
							var factoryId = warning.factoryId;
							warning.SetEmpty();
							warning.factoryId = factoryId; // To be clear by ClearAll()
						}
					}
				}
			}
		}


		static int currentIndex = 0;

		public static void SeekNextSignal(int signalId, int detailId)
        {
			var localPlanetId = GameMain.localPlanet?.id ?? 0;
			var ws = GameMain.data.warningSystem;
			if (signalId <= 0 || detailId <= 0 || ws.warningCursor <= 0 || localPlanetId == 0) return;

			currentIndex %= ws.warningCursor;
			var lastIndex = currentIndex;
			var count = 0;
			do
			{
				currentIndex = (currentIndex + 1) % ws.warningCursor;
				ref var warning = ref ws.warningPool[currentIndex];
				if (warning.signalId == signalId && warning.detailId1 == detailId && warning.id == currentIndex && warning.state > 0)
                {
					if (warning.astroId == localPlanetId)
                    {
						Log.Debug($"SeekNextSignal[{currentIndex}]: ({signalId},{detailId}) {warning.localPos}");
						UIRoot.instance.uiGame.globemap.MoveToViewTargetTwoStep(warning.localPos, 200f);
						return;
					}
				}
				if (count++ > 999) break;
			} while (currentIndex != lastIndex);
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