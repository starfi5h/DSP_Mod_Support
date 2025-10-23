#if DEBUG 

using BepInEx;
using HarmonyLib;
using System;
using UnityEngine;

namespace ErrorAnalyzer.Testing
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "aaa.dsp.plugin.ErrorTester";
        public const string NAME = "ErrorTester";
        public const string VERSION = "1.0.0";

        static Harmony harmony;
        static bool enableError = false;

        public void Awake()
        {
            harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(Plugin));
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F4))
            {
                throw new IndexOutOfRangeException();
            }
            if (Input.GetKeyDown(KeyCode.F5))
            {
                enableError = !enableError;
                Logger.LogDebug("Enable error: " + enableError);
            }
        }

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(StationComponent), nameof(StationComponent.DetermineDispatch))]
        public static void ExceptionTest()
        {
            if (enableError)
            {
                throw new IndexOutOfRangeException();
            }
        }

        //[HarmonyPostfix, HarmonyPatch(typeof(GameData), nameof(GameData.Import))]
        public static void Import_Postfix()
        {
            if (enableError)
            {
                ExceptionTest();
            }
        }

        //[HarmonyPostfix, HarmonyPatch(typeof(DigitalSystem), nameof(DigitalSystem.GameTick))]
        public static void DigitalSystemGameTick_Postfix()
        {
            if (enableError)
            {
                //throw new IndexOutOfRangeException();
            }
        }

        //[HarmonyPostfix, HarmonyPatch(typeof(AssemblerComponent), nameof(AssemblerComponent.InternalUpdate))]
        public static void AssemblerComponentInternalUpdate_Postfix()
        {
            if (enableError)
            {
                TriggerIndexOutOfRange();
            }
        }

        //[HarmonyPostfix, HarmonyPatch(typeof(LabComponent), nameof(LabComponent.InternalUpdateResearch))]
        public static void InternalUpdateResearch_Postfix()
        {
            if (enableError)
            {
                throw new IndexOutOfRangeException();
            }
        }

        //[HarmonyFinalizer]
        //[HarmonyPatch(typeof(GameLogic), "OnGameLogicFrame")]
        public static Exception OnGameLogicFrame(Exception __exception)
        {
            if (__exception != null)
            {
                Debug.LogError(__exception);
            }
            return __exception;
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIEscMenu), "OnButton1Click")]
        public static void OnButton1Click_Postfix()
        {
            TriggerNullReference();
        }

        public static void TriggerIndexOutOfRange()
        {
            int[] arr = new int[1];
            arr[2] = 0;
        }

        public static void TriggerNullReference()
        {
            string str = null;
            str.Trim();
        }

        public static void TestLog()
        {
            try
            {
                int a = 0;
                int b = 1 / a;
            }
            catch (Exception ex)
            {
                //Debug.LogException(ex);
                Debug.LogError(ex);
            }
        }
    }
}

#endif