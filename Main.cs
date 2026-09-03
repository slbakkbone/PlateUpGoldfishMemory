using HarmonyLib;
using KitchenMods;
using System.Reflection;
using UnityEngine;

namespace KitchenGoldfishMemory
{
    public class Main : IModInitializer
    {
        public const string MOD_GUID = $"IcedMilo.PlateUp.{MOD_NAME}";
        public const string MOD_NAME = "Goldfish Memory";
        public const string MOD_VERSION = "0.2.0";

        Harmony _harmony;

        public Main()
        {
            _harmony = new Harmony(MOD_GUID);

            try
            {
                _harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (System.Exception ex)
            {
                LogError($"Failed to apply patches: {ex}");
            }
        }

        public void PostActivate(KitchenMods.Mod mod)
        {
            LogWarning($"{MOD_GUID} v{MOD_VERSION} in use!");
        }

        public void PreInject()
        {
        }

        public void PostInject()
        {
        }

        #region Logging
        public static void LogInfo(string _log) { Debug.Log($"[{MOD_NAME}] " + _log); }
        public static void LogWarning(string _log) { Debug.LogWarning($"[{MOD_NAME}] " + _log); }
        public static void LogError(string _log) { Debug.LogError($"[{MOD_NAME}] " + _log); }
        public static void LogInfo(object _log) { LogInfo(_log.ToString()); }
        public static void LogWarning(object _log) { LogWarning(_log.ToString()); }
        public static void LogError(object _log) { LogError(_log.ToString()); }
        #endregion
    }
}
