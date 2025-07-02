using BepInEx;
using HarmonyLib;

namespace FillMyCart
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "FillMyCart";
        public const string PLUGIN_NAME = "FillMyCart";
        public const string PLUGIN_VERSION = "1.0.0";

        public static Plugin Instance = null;

        public static BepInEx.Logging.ManualLogSource GetLogger()
        {
            return Instance.Logger;
        }

        void Awake()
        {
            Instance = this;
            new Harmony(PLUGIN_GUID).PatchAll();
        }
    }
}
