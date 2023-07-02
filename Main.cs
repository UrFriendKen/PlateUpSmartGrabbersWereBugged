using KitchenMods;
using UnityEngine;

// Namespace should have "Kitchen" in the beginning
namespace KitchenSmartGrabbersWereBugged
{
    public class Main : IModInitializer
    {
        // guid must be unique and is recommended to be in reverse domain name notation
        // mod name that is displayed to the player and listed in the mods menu
        // mod version must follow semver e.g. "1.2.3"
        public const string MOD_GUID = "IcedMilo.PlateUp.SmartGrabbersWereBugged";
        public const string MOD_NAME = "Smart Grabbers Were Bugged";
        public const string MOD_VERSION = "0.1.4";

        public Main()
        {
        }

        public void PostActivate(Mod mod)
        {
            LogWarning($"{MOD_GUID} v{MOD_VERSION} in use!");
        }

        public void PreInject() { }

        public void PostInject() { }

        #region Logging
        // You can remove this, I just prefer a more standardized logging
        public static void LogInfo(string _log) { Debug.Log($"[{MOD_NAME}] " + _log); }
        public static void LogWarning(string _log) { Debug.LogWarning($"[{MOD_NAME}] " + _log); }
        public static void LogError(string _log) { Debug.LogError($"[{MOD_NAME}] " + _log); }
        public static void LogInfo(object _log) { LogInfo(_log.ToString()); }
        public static void LogWarning(object _log) { LogWarning(_log.ToString()); }
        public static void LogError(object _log) { LogError(_log.ToString()); }
        #endregion
    }
}
