using BepInEx;
using HarmonyLib;
using MyBox;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FillMyCart
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "FillMyCart";
        public const string PLUGIN_NAME = "FillMyCart";
        public const string PLUGIN_VERSION = "1.1.0";

        public static Plugin Instance = null;

        public static BepInEx.Logging.ManualLogSource GetLogger()
        {
            return Instance.Logger;
        }

        private List<GameObject> m_togglableGameObjects = new List<GameObject>();

        void Awake()
        {
            Instance = this;
            if (!ConfigManager.Instance.Enable.Value)
                return;
            new Harmony(PLUGIN_GUID).PatchAll();
        }

        void Update()
        {
            if (!ConfigManager.Instance.Enable.Value)
                return;
            if (ConfigManager.Instance.AutoPurchaseHotkey.Value.MainKey != KeyCode.None && ConfigManager.Instance.AutoPurchaseHotkey.Value.IsDown())
            {
                AutoFill();
                Singleton<CartManager>.Instance.MarketShoppingCart.Purchase(false);
            }

            if (ConfigManager.Instance.ToggleUIHotkey.Value.MainKey != KeyCode.None)
            {
                if (ConfigManager.Instance.ToggleUIHotkey.Value.IsDown())
                {
                    foreach (GameObject go in m_togglableGameObjects)
                    {
                        go.SetActive(!go.activeSelf);
                        go.transform.SetAsLastSibling();
                    }
                }
            }
        }

        public static void AutoFill()
        {
            List<ProductSO> products = Singleton<IDManager>.Instance.Products.ToList();
            products.RemoveAll(product => !Singleton<ProductLicenseManager>.Instance.IsProductLicenseUnlocked(product.ID));

            foreach (var product in products)
            {
                int productInCartIndex = Singleton<CartManager>.Instance.CartData.ProductInCarts.FindIndex(itemQuantity => itemQuantity.FirstItemID == product.ID);
                int productMinimumQuantity = ConfigManager.Instance.GetProductMinimumQuantity(product.ID);
                int productPerBox = product.GridLayoutInBox.productCount;
                int productCount = Singleton<InventoryManager>.Instance.GetInventoryAmount(product.ID) + (productInCartIndex < 0 ? 0 : (Singleton<CartManager>.Instance.CartData.ProductInCarts[productInCartIndex].FirstItemCount * productPerBox));
                int numberOfBoxesToBuy = (int)Mathf.Ceil((productMinimumQuantity - productCount) / (float)productPerBox);

                for (int i = 0; i < numberOfBoxesToBuy; ++i)
                {
                    if (Singleton<CartManager>.Instance.MarketShoppingCart.CartMaxed(willBeAddedMore: true))
                        break;
                    float price = Singleton<PriceManager>.Instance.SellingPrice(product.ID);
                    ItemQuantity itemQuantity = new ItemQuantity(product.ID, price);
                    Singleton<CartManager>.Instance.AddCart(itemQuantity, SalesType.PRODUCT);
                }
            }
        }

        public void AddTogglableGameObject(GameObject go)
        {
            m_togglableGameObjects.Add(go);
            if (ConfigManager.Instance.ToggleUIHotkey.Value.MainKey == KeyCode.None)
                return;
            go.SetActive(false);
        }
    }
}
