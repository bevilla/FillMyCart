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
            TextMeshProUGUI productAmountText = AccessTools.Field(typeof(SalesItem), "m_ProductAmountText").GetValue(__instance) as TextMeshProUGUI;
            if (productAmountText == null)
            {
                Plugin.GetLogger().LogError("Couldn't access the m_ProductAmountText field of SalesItem instance");
                return;
            }

            GameObject autoFillInput = new GameObject("AutoFillInput");
            autoFillInput.transform.SetParent(productAmountText.transform.parent, false);
            TextMeshProUGUI tmp = autoFillInput.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = productAmountText.fontSize;
            tmp.font = productAmountText.font;
            tmp.color = productAmountText.color;
            tmp.alignment = productAmountText.alignment;

            TMP_InputField inputField = autoFillInput.AddComponent<TMP_InputField>();
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

            RectTransform sourceRect = productAmountText.GetComponent<RectTransform>();
            RectTransform inputRect = autoFillInput.GetComponent<RectTransform>();
            inputRect.anchorMin = sourceRect.anchorMin;
            inputRect.anchorMax = sourceRect.anchorMax;
            inputRect.pivot = sourceRect.pivot;
            inputRect.sizeDelta = sourceRect.sizeDelta;
            inputRect.anchoredPosition = sourceRect.anchoredPosition + new Vector2(0, 30);

            GameObject background = new GameObject("Background");
            background.transform.SetParent(autoFillInput.transform, false);

            Image backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = new Color(0, 0, 0, 0.3f);

            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            inputField.targetGraphic = backgroundImage;

            GameObject icon = new GameObject("AutoFillIcon");
            icon.transform.SetParent(autoFillInput.transform.parent, false);

            Image iconImage = icon.AddComponent<Image>();
            iconImage.sprite = GetIconSprite();
            iconImage.SetNativeSize();

            RectTransform iconRect = icon.GetComponent<RectTransform>();
            iconRect.anchorMin = inputRect.anchorMin;
            iconRect.anchorMax = inputRect.anchorMax;
            iconRect.pivot = inputRect.pivot;
            iconRect.sizeDelta = new Vector2(0.01f, 0.01f);
            iconRect.anchoredPosition = new Vector2(0, 31);
        }
    }
}
