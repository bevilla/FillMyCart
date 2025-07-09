using HarmonyLib;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FillMyCart
{
    [HarmonyPatch(typeof(SalesItem), "Start")]
    class SalesItemStartPatch
    {
        static Sprite iconSprite = null;

        static Sprite GetIconSprite()
        {
            if (iconSprite != null)
                return iconSprite;

            string resourceName = Assembly.GetExecutingAssembly().GetManifestResourceNames().Single(str => str.EndsWith("AutoFillInputIcon.png"));

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    Plugin.GetLogger().LogError($"Resource not found: {resourceName}");
                    return null;
                }

                byte[] data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);

                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(data);
                iconSprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100.0f
                );
            }

            return iconSprite;
        }

        static void Postfix(SalesItem __instance)
        {
            if (!ConfigManager.Instance.EnableMinimumProductAmountTextInput.Value)
                return;

            TextMeshProUGUI inventoryProductAmountText = AccessTools.Field(typeof(SalesItem), "m_InventoryProductAmountText").GetValue(__instance) as TextMeshProUGUI;
            if (inventoryProductAmountText == null)
            {
                Plugin.GetLogger().LogError("Couldn't access the m_InventoryProductAmountText field of SalesItem instance");
                return;
            }

            GameObject obj = inventoryProductAmountText.transform.parent.gameObject;
            GameObject autoFillInput = Object.Instantiate(obj, obj.transform.parent);
            autoFillInput.GetComponent<RectTransform>().anchoredPosition = new Vector2(49.5053f, 9.3384f);
            autoFillInput.name = "AutoFillInput";

            // Replace TMP_Text with TMP_InputField
            TMP_Text storeAmountText = autoFillInput.GetComponentInChildren<TMP_Text>();
            GameObject textGO = storeAmountText.gameObject;
            Object.DestroyImmediate(storeAmountText); // Remove the old text component

            TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = inventoryProductAmountText.fontSize;
            tmp.font = inventoryProductAmountText.font;
            tmp.color = inventoryProductAmountText.color;
            tmp.alignment = inventoryProductAmountText.alignment;

            TMP_InputField inputField = textGO.AddComponent<TMP_InputField>();
            inputField.textComponent = tmp;
            inputField.text = $"{ConfigManager.Instance.GetProductMinimumQuantity(__instance.ProductID)}";
            inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
            inputField.onValueChanged.AddListener(value =>
            {
                string sanitized = new string(value.Where(char.IsDigit).ToArray());

                if (sanitized.Length == 0)
                {
                    sanitized = "0";
                }
                if (sanitized != value)
                {
                    inputField.SetTextWithoutNotify(sanitized);
                }
                ConfigManager.Instance.SetProductMinimumQuantity(__instance.ProductID, int.Parse(sanitized));
            });

            // Update sprite icon
            textGO.transform.parent.GetComponent<Image>().sprite = GetIconSprite();

            // Setup text input transparent background
            GameObject textInputBackground = new GameObject("Background");
            textInputBackground.transform.SetParent(textGO.transform, false);

            Image textInputBackgroundImage = textInputBackground.AddComponent<Image>();
            textInputBackgroundImage.color = new Color(0, 0, 0, 0.3f);

            RectTransform textInputBackgroundRect = textInputBackground.GetComponent<RectTransform>();
            textInputBackgroundRect.anchorMin = Vector2.zero;
            textInputBackgroundRect.anchorMax = Vector2.one;
            textInputBackgroundRect.offsetMin = Vector2.zero;
            textInputBackgroundRect.offsetMax = Vector2.zero;

            inputField.targetGraphic = textInputBackgroundImage;

            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.offsetMin += Vector2.left * 10.0f;

            // Setup text input opaque background (used to hide MoreDetailedComputerInventory UI)
            GameObject opaqueBackground = new GameObject("OpaqueBackground");
            opaqueBackground.transform.SetParent(autoFillInput.transform.parent.transform, false);

            Image opaqueBackgroundImage = opaqueBackground.AddComponent<Image>();
            opaqueBackgroundImage.color = new Color(28.0f / 255.0f, 56.0f / 255.0f, 70.0f / 255.0f, 1.0f); // match with the original background color

            RectTransform opaqueBackgroundRect = opaqueBackground.GetComponent<RectTransform>();
            opaqueBackgroundRect.anchorMin = new Vector2(0.498f, 0.516f);
            opaqueBackgroundRect.anchorMax = new Vector2(0.788f, 0.856f);
            opaqueBackgroundRect.offsetMin = Vector2.zero;
            opaqueBackgroundRect.offsetMax = Vector2.zero;

            Plugin.Instance.AddTogglableGameObject(opaqueBackground);
            Plugin.Instance.AddTogglableGameObject(autoFillInput);
            autoFillInput.transform.SetAsLastSibling();
        }
    }
}
