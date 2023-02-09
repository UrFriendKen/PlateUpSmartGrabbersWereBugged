using HarmonyLib;
using Kitchen;

namespace KitchenSmartGrabbersWereBugged.Patches
{
    [HarmonyPatch(typeof(PushItems), "OnUpdate")]
    public static class PushItems_Patch
    {

        public static void Prefix()
        {
            
        }
    }
}
