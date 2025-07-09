using BepInEx.Configuration;
using MyBox;
using System.Collections.Generic;
using UnityEngine;

namespace FillMyCart
{
    internal class ConfigManager
    {
        private static ConfigManager _instance;

        public static ConfigManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ConfigManager();
                }
                return _instance;
            }
        }

        // 1. Settings
        public ConfigEntry<bool> Enable { get; set; }
        public ConfigEntry<bool> EnableAutoFillButton { get; set; }
        public ConfigEntry<bool> EnableMinimumProductAmountTextInput { get; set; }

        // 2. Hotkeys
        public ConfigEntry<KeyboardShortcut> AutoPurchaseHotkey { get; set; }
        public ConfigEntry<KeyboardShortcut> ToggleUIHotkey { get; set; }

        private Dictionary<int, ConfigEntry<int>> m_minimumProductCountEntries = new Dictionary<int, ConfigEntry<int>>();

        public ConfigManager()
        {
            Plugin.Instance.Config.SaveOnConfigSet = true;

            // 1. Settings
            Enable = Plugin.Instance.Config.Bind("1. Settings", "Enable", true, "Enable this mod");
            EnableAutoFillButton = Plugin.Instance.Config.Bind("1. Settings", "EnableAutoFillButton", true, "Enable this mod's \"Auto Fill\" button in the market");
            EnableMinimumProductAmountTextInput = Plugin.Instance.Config.Bind("1. Settings", "EnableMinimumProductAmountTextInput", true, "Enable this mod's text input in the market's product item");

            // 2. Hotkeys
            AutoPurchaseHotkey = Plugin.Instance.Config.Bind("2. Hotkeys", "AutoPurchaseHotkey", new KeyboardShortcut(KeyCode.None), "Automatically fill the cart and purchase");
            ToggleUIHotkey = Plugin.Instance.Config.Bind("2. Hotkeys", "ToggleUIHotkey", new KeyboardShortcut(KeyCode.None), "Key to toggle the UI. No key will always display the UI (default)");
        }

        private void LazyInitMinimumProductCount()
        {
            if (m_minimumProductCountEntries.Count > 0)
                return;

            foreach (var product in Singleton<IDManager>.Instance.Products)
            {
                m_minimumProductCountEntries[product.ID] = Plugin.Instance.Config.Bind("MinimumProductCount", $"Product_{product.ID}", 0, $"{product.ProductName} ({product.ProductBrand})");
            }
        }

        public int GetProductMinimumQuantity(int productId)
        {
            LazyInitMinimumProductCount();
            return m_minimumProductCountEntries[productId].Value;
        }

        public void SetProductMinimumQuantity(int productId, int value)
        {
            LazyInitMinimumProductCount();
            m_minimumProductCountEntries[productId].Value = value;
        }
    }
}
