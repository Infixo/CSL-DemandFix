﻿using System.Reflection;
using UnityEngine;
using ColossalFramework.Plugins;
using HarmonyLib;

namespace DemandFix
{
    public static class DemandFixPatcher
    {
        private const string HarmonyId = "Infixo.DemandFix";
        private static bool patched = false;

        public static void PatchAll()
        {
            if (patched) { Debug.Log("PatchAll: already patched!"); return; }
            //Harmony.DEBUG = true;
            var harmony = new Harmony(HarmonyId);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            if (Harmony.HasAnyPatches(HarmonyId))
            {
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, $"{HarmonyId} methods patched ok");
                patched = true;
                var myOriginalMethods = harmony.GetPatchedMethods();
                foreach (var method in myOriginalMethods)
                    DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, $"{HarmonyId} ...method {method.Name}");
            }
            else
                Debug.Log("ERROR PatchAll: methods not patched");
            //Harmony.DEBUG = false;
        }

        public static void UnpatchAll()
        {
            if (!patched) { Debug.Log("UnpatchAll: not patched!"); return; }
            //Harmony.DEBUG = true;
            var harmony = new Harmony(HarmonyId);
            harmony.UnpatchAll(HarmonyId);
            patched = false;
            //Harmony.DEBUG = false;
        }
    }

    [HarmonyPatch(typeof(ZoneManager))]
    public static class ZoneManagerPatches
    {
        //private const int EmptyVisitorTreshold = 20; // [percent] when there is less empty then demand is generated
        private const int MaxCustomersDemand = 25; // [percent] when there is less empty then demand is generated
        // patch for method that calculates commercial demand
        [HarmonyPatch("CalculateCommercialDemand")]
        [HarmonyPrefix]
        public static bool CalculateCommercialDemandPrefix(ZoneManager __instance, ref int __result, ref District districtData)
        {
            // original code generated by ILSpy
            // workers and homes component
            int workers = (int)(districtData.m_commercialData.m_finalHomeOrWorkCount - districtData.m_commercialData.m_finalEmptyCount); // commercial workers
            int homes = (int)(districtData.m_residentialData.m_finalHomeOrWorkCount - districtData.m_residentialData.m_finalEmptyCount); // no of populated homes
            int demand = Mathf.Clamp(homes, 0, 50);
            //workers = workers * 10 * 16 / 100; // in a moment * 200, so together *320
            //homes = homes * 20 / 100; // same as /5 and in a moment * 200 => *40
            //demand += Mathf.Clamp((homes * 200 - workers * 200) / Mathf.Max(workers, 100), -50, 50); // original
            //demand += Mathf.Clamp((homes * 40 - workers * 320) / Mathf.Max(workers * 160/100, 100), -50, 50); // homes and workers updated
            //demand += Mathf.Clamp((homes * 25 - workers * 200) / Mathf.Max(workers, 62), -50, 50); // because workers are no longer multiplied by 1.6
            demand += Mathf.Clamp( 25 * (homes - (workers << 3)) / Mathf.Max(workers, 62), -50, 50);
            // customers (vistors) component
            int total = (int)districtData.m_visitorData.m_finalHomeOrWorkCount; // total visit places
            int empty = (int)districtData.m_visitorData.m_finalEmptyCount; // empty visit places
            //demand += Mathf.Clamp((total * 100 - empty * 300) / Mathf.Max(total, 100), -50, 50); // original
            //demand += Mathf.Clamp(  EmptyVisitorTreshold - 100 * empty / Mathf.Max(total, 100), -50, 50); // new formula
            demand += MaxCustomersDemand * (total - (empty<<1)) / Mathf.Max(total, 100); // new formula 2 - doesn't go beyond -50..50
            __instance.m_DemandWrapper.OnCalculateCommercialDemand(ref demand);
            __result = Mathf.Clamp(demand, 0, 100);
            return false; // don't execute the original
        }
    }
}