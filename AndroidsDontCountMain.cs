using System.Reflection;
using HarmonyLib;
using Verse;

namespace AndroidsDontCount
{
    [StaticConstructorOnStartup]
    public static class AndroidsDontCountMain
    {
        public const string HarmonyId = "ertzfiend.androidsdontcount";

        static AndroidsDontCountMain()
        {
            var harmony = new Harmony(HarmonyId);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Message("[Androids Don't Count] Patches initialized.");
        }
    }
}
