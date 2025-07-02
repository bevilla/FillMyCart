using HarmonyLib;
using MyBox;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FillMyCart
{
    [HarmonyPatch(typeof(ProductViewer), "Start")]
    class ProductViewerStartPatch
    {
        public static GameObject autoFillButton = null;

        static void Postfix(ProductViewer __instance)
        {
            TMP_Dropdown productTypeDropdown = AccessTools.Field(typeof(ProductViewer), "m_ProductTypeDropdown").GetValue(__instance) as TMP_Dropdown;
            if (productTypeDropdown == null)
            {
                Plugin.GetLogger().LogError("Couldn't access the m_ProductTypeDropdown field of ProductViewer instance");
                return;
            }

            autoFillButton = new GameObject("AutoFillButton");
            autoFillButton.transform.SetParent(productTypeDropdown.transform.parent, false);

            Image image = autoFillButton.AddComponent<Image>();
            image.color = new Color(0.0f, 0.77f, 0.02f);

            Button button = autoFillButton.AddComponent<Button>();
            button.onClick.AddListener(() => AutoFill());

            RectTransform rect = autoFillButton.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.sizeDelta = new Vector2(60, 16);
            rect.anchoredPosition = new Vector2(-20, -6.4f);

            GameObject textGO = new GameObject("ButtonText");
            textGO.transform.SetParent(autoFillButton.transform, false);

            TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = "Auto Fill";
            text.fontSize = 10;
            text.font = productTypeDropdown.itemText.font;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        private static void AutoFill()
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
    }
}
