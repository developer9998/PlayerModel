using HarmonyLib;
using PlayerModel.Behaviours.UI;

namespace PlayerModel.Patches
{
    [HarmonyPatch(typeof(CosmeticWardrobe), "Start")]
    public class CosmeticWardrobePatch
    {
        [HarmonyWrapSafe]
        public static void Postfix(CosmeticWardrobe __instance)
        {
            __instance.gameObject.AddComponent<PortableStand>();
        }
    }
}
