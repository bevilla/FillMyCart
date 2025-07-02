using BepInEx.Configuration;
using MyBox;
using System.Collections.Generic;

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

        private Dictionary<int, ConfigEntry<int>> m_minimumProductCountEntries = new Dictionary<int, ConfigEntry<int>>();

        public ConfigManager()
        {
            foreach (var product in Singleton<IDManager>.Instance.Products)
            {
                m_minimumProductCountEntries[product.ID] = Plugin.Instance.Config.Bind("MinimumProductCount", $"Product_{product.ID}", 0, $"{product.ProductName} ({product.ProductBrand})");
            }
            Plugin.Instance.Config.Save();
            Plugin.Instance.Config.SaveOnConfigSet = true;
        }

        public int GetProductMinimumQuantity(int productId)
        {
            return m_minimumProductCountEntries[productId].Value;
        }

        public void SetProductMinimumQuantity(int productId, int value)
        {
            m_minimumProductCountEntries[productId].Value = value;
        }
    }
}
