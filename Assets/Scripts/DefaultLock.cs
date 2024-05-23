using HarmonyLib;
using Modding;
using Munitions;
using Munitions.ModularMissiles;
using FleetEditor.MissileEditor;
using Ships;
using Bundles;
using Factions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Ships.Serialization;
using System.Linq;
using UnityEngine;
using Utility;
using UI;
using Game;

namespace DefaultLock
{
    public class FalcataFactionLock : IModEntryPoint
    {
        public void PostLoad()
        {
            Harmony harmony = new Harmony("nebulous.FalcataFactionLock");
            harmony.PatchAll();
        }

        public void PreLoad()
        {
        }
    }

    [HarmonyPatch(typeof(HullComponent), "UseableByFaction")]
    class Patch_HullComponent_UseableByFaction
    {
        public static bool Prefix(ref HullComponent __instance, ref bool __result, ref FactionDescription faction)
        {
            if (faction != null && faction.SaveKey == "Falcata Republic/Falcata Republic")
            {
                List<string> _lockedComponents = new List<string>() { "Stock/Gun Plotting Center", "Stock/Energy Regulator", "Stock/Small Energy Regulator" }; // Add your component keys here

                for (int i = 0; i < _lockedComponents.Count; i++)
                {
                    if (__instance.SaveKey == _lockedComponents[i])
                    {
                        __result = false;
                        return false;
                    }
                }

                __result = true;
                return false;
            }

            return true;
        }
    }
}
