using System;
using System.Collections.Generic;
using HarmonyLib;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FillMyCart
{
    [HarmonyPatch(typeof(SalesItem), "Start")]
    class SalesItemStartPatch
    {
        static readonly IDictionary<string, Sprite> iconSprites = new Dictionary<string, Sprite>();

        static Sprite GetIconSprite(string fileName)
        {
            if (iconSprites.TryGetValue(fileName, out var sprite))
                return sprite;

            var resourceName = Assembly.GetExecutingAssembly().GetManifestResourceNames().Single(str => str.EndsWith(fileName));

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    Plugin.GetLogger().LogError($"Resource not found: {resourceName}");
                    return null;
                }

                var data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);

                var texture = new Texture2D(2, 2);
                texture.LoadImage(data);
                iconSprites[fileName] = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100.0f
                );
            }

            return iconSprites[fileName];
        }

        static void Postfix(SalesItem __instance)
        {
            if (!ConfigManager.Instance.EnableMinimumProductAmountTextInput.Value)
                return;

            var inventoryProductAmountText = AccessTools.Field(typeof(SalesItem), "m_InventoryProductAmountText").GetValue(__instance) as TextMeshProUGUI;
            if (inventoryProductAmountText == null)
            {
                Plugin.GetLogger().LogError("Couldn't access the m_InventoryProductAmountText field of SalesItem instance");
                return;
            }
            
            var template = inventoryProductAmountText.transform.parent.gameObject;

            // minimum
            var minimum = CreateInput(
                template,
                new Vector2(49.5053f, 9.3384f),
                "AutoFillMinimumInput",
                GetIconSprite("AutoFillMinimumInputIcon.png"),
                $"{ConfigManager.Instance.GetProductMinimumQuantity(__instance.ProductID)}",
                sanitized => ConfigManager.Instance.SetProductMinimumQuantity(__instance.ProductID, int.Parse(sanitized))
            );

            // threshold
            var threshold = CreateInput(
                template,
                new Vector2(49.5053f, 9.3384f * 2f + 7f),
                "AutoFillThresholdInput",
                GetIconSprite("AutoFillThresholdInputIcon.png"),
                $"{ConfigManager.Instance.GetProductThresholdQuantity(__instance.ProductID)}",
                sanitized => ConfigManager.Instance.SetProductThresholdQuantity(__instance.ProductID, int.Parse(sanitized))
            );
            
            // Setup text input opaque background (used to hide MoreDetailedComputerInventory UI)
            var opaqueBackground = new GameObject("OpaqueBackground");
            opaqueBackground.transform.SetParent(minimum.transform.parent.transform, false);

            var opaqueBackgroundImage = opaqueBackground.AddComponent<Image>();
            opaqueBackgroundImage.color = new Color(28.0f / 255.0f, 56.0f / 255.0f, 70.0f / 255.0f, 1.0f); // match with the original background color

            var opaqueBackgroundRect = opaqueBackground.GetComponent<RectTransform>();
            opaqueBackgroundRect.anchorMin = new Vector2(0.498f, 0.516f);
            opaqueBackgroundRect.anchorMax = new Vector2(0.788f, 0.856f);
            opaqueBackgroundRect.offsetMin = Vector2.zero;
            opaqueBackgroundRect.offsetMax = Vector2.zero;
            Plugin.Instance.AddTogglableGameObject(opaqueBackground);

            // display inputs
            Plugin.Instance.AddTogglableGameObject(minimum);
            Plugin.Instance.AddTogglableGameObject(threshold);
            minimum.transform.SetAsLastSibling();
            threshold.transform.SetAsLastSibling();
        }
        
        private static GameObject CreateInput(GameObject template, Vector2 anchoredPos, string name,
            Sprite icon, string initialText, Action<string> onChange)
        {
            var clone = Object.Instantiate(template, template.transform.parent);
            var rect = clone.GetComponent<RectTransform>();
            rect.anchoredPosition = anchoredPos;
            clone.name = name;

            // find text object
            var oldTmp = clone.GetComponentInChildren<TMP_Text>();
            var textGO = oldTmp.gameObject;

            // replace TMP_Text by TMP_InputField
            Object.DestroyImmediate(oldTmp);

            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            var refTmp = template.GetComponentInChildren<TextMeshProUGUI>();
            tmp.fontSize = refTmp.fontSize;
            tmp.font = refTmp.font;
            tmp.color = refTmp.color;
            tmp.alignment = refTmp.alignment;
            tmp.enableWordWrapping = false;
            tmp.raycastTarget = true;

            var inputField = textGO.AddComponent<TMP_InputField>();
            inputField.textComponent = tmp;
            inputField.text = initialText;
            inputField.contentType = TMP_InputField.ContentType.IntegerNumber;

            inputField.onValueChanged.AddListener(value =>
            {
                var sanitized = new string(value.Where(char.IsDigit).ToArray());
                if (sanitized.Length == 0) sanitized = "0";
                if (sanitized != value) inputField.SetTextWithoutNotify(sanitized);
                onChange?.Invoke(sanitized);
            });

            // icon
            textGO.transform.parent.GetComponent<Image>().sprite = icon;

            // backgrounnd style
            var bg = new GameObject("Background");
            bg.transform.SetParent(textGO.transform, false);

            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.3f);

            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            inputField.targetGraphic = bgImg;

            var textRect = textGO.GetComponent<RectTransform>();
            textRect.offsetMin += Vector2.left * 10f; // padding

            return clone;
        }
    }
}
