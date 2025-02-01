using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using Sentry.Internal.Extensions;
using TONX.Modules.OptionItems;
using TONX.Modules.OptionItems.Interfaces;
using TONX.Roles.Crewmate;
using UnityEngine;
using UnityEngine.UI;
using YamlDotNet.Serialization;
using static TONX.Translator;
using Object = UnityEngine.Object;

namespace TONX
{
    [HarmonyPatch(typeof(LobbyViewSettingsPane))]
    public static class LobbyViewSettingsPanePatch
    {
        private static List<PassiveButton> tonxSettingsButton = new List<PassiveButton>();
        public static List<CategoryHeaderMasked> CategoryHeaders = new List<CategoryHeaderMasked>();
        private static Vector3 buttonPosition = new(-6f, 1.4f, 0f);
        private static Vector3 buttonSize = new(0.45f, 0.45f, 1f);

        [HarmonyPatch(nameof(LobbyViewSettingsPane.Awake)), HarmonyPostfix]
        public static void AwakePostfix(LobbyViewSettingsPane __instance)
        {
            // 调整原版按钮
            var OverviewTab = GameObject.Find("OverviewTab");
            OverviewTab.transform.localScale = buttonSize;
            OverviewTab.transform.localPosition = buttonPosition + new Vector3(0f, 0.18f, 0f);
            var RolesTab = GameObject.Find("RolesTabs");
            RolesTab.transform.localScale = buttonSize;
            RolesTab.transform.localPosition = buttonPosition + new Vector3(1.6f, 0.18f, 0f);

            // 模组按钮
            tonxSettingsButton = new List<PassiveButton>();
            foreach (var tab in Enum.GetValues(typeof(TabGroup)))
            {
                Vector3 offset_up = new (1.6f * ((int)tab + 2), 0.18f, 0f);
                Vector3 offset_down = new (1.6f * ((int)tab - 2), -0.18f, 0f);
                var SettingsButton = Object.Instantiate(__instance.taskTabButton, __instance.taskTabButton.transform.parent);
                SettingsButton.name = tab.ToString() + " VIEWBUTTON";
                SettingsButton.transform.localPosition = buttonPosition + (((int)tab < 2) ? offset_up : offset_down);
                SettingsButton.transform.localScale = buttonSize;
                SettingsButton.buttonText.DestroyTranslator();
                SettingsButton.buttonText.text = GetString($"TabGroup.{tab}");
                SettingsButton.OnClick.RemoveAllListeners();
                SettingsButton.OnClick.AddListener((Action)(() => 
                {
                    __instance.ChangeTab((StringNames)((int)tab + 3551));
                    SettingsButton.SelectButton(true);
                }));
                SettingsButton.OnMouseOut.RemoveAllListeners();
                SettingsButton.OnMouseOver.RemoveAllListeners();
                tonxSettingsButton.Add(SettingsButton);
            }
        }

        [HarmonyPatch(nameof(LobbyViewSettingsPane.ChangeTab)), HarmonyPostfix]
        public static void ChangeTabPostfix(LobbyViewSettingsPane __instance, StringNames category)
        {
            foreach (var button in tonxSettingsButton)
            {
                button.SelectButton(false);
            }
            if ((int)category < 3551) return;
            __instance.taskTabButton.SelectButton(false);
            CreateOptions(__instance);
        }

        public static void CreateOptions(LobbyViewSettingsPane __instance)
        {
            // 删除原版gameobject
            foreach (var vanillaOption in __instance.settingsInfo)
            {
                Object.Destroy(vanillaOption.gameObject);
            }
            __instance.settingsInfo.Clear();

            // 模组设置
            CategoryHeaders = new List<CategoryHeaderMasked>();
            var template = __instance.infoPanelOrigin;
            foreach (var option in OptionItem.AllOptions)
            {
                if ((int)option.Tab != ((int)__instance.currentTab - 3551)) continue;  
                
                if (option.IsText)
                {
                    var categoryHeader = CreateCategoryHeader(__instance, option);
                    CategoryHeaders.Add(categoryHeader);
                    __instance.settingsInfo.Add(categoryHeader.gameObject);
                    continue;
                }

                var infoPanelOption = Object.Instantiate(template, __instance.settingsContainer);
                infoPanelOption.SetMaskLayer(LobbyViewSettingsPane.MASK_LAYER);
                infoPanelOption.titleText.text = option.Name;
                infoPanelOption.settingText.text = option.GetString();
                infoPanelOption.name = option.Name;
                __instance.settingsInfo.Add(infoPanelOption.gameObject);

                var indent = 0f;
                var parent = option.Parent;
                while (parent != null)
                {
                    indent += 0.15f;
                    parent = parent.Parent;
                }

                infoPanelOption.labelBackground.size += new Vector2(2f - indent * 2, 0f);
                infoPanelOption.labelBackground.transform.localPosition += new Vector3(-1f + indent, 0f, 0f);
                infoPanelOption.titleText.rectTransform.sizeDelta += new Vector2(2f - indent * 2, 0f);
                infoPanelOption.titleText.transform.localPosition += new Vector3(-1f + indent, 0f, 0f);

                option.ViewOptionBehaviour = infoPanelOption;
            }
        }

        private static CategoryHeaderMasked CreateCategoryHeader(LobbyViewSettingsPane __instance, OptionItem option)
        {
            var categoryHeader = Object.Instantiate(__instance.categoryHeaderOrigin, Vector3.zero, Quaternion.identity, __instance.settingsContainer);
            categoryHeader.name = option.Name;
            categoryHeader.Title.text = option.GetName();
            var maskLayer = LobbyViewSettingsPane.MASK_LAYER;
            categoryHeader.Background.material.SetInt(PlayerMaterial.MaskLayer, maskLayer);
            if (categoryHeader.Divider != null)
            {
                categoryHeader.Divider.material.SetInt(PlayerMaterial.MaskLayer, maskLayer);
            }
            categoryHeader.Title.fontMaterial.SetFloat("_StencilComp", 3f);
            categoryHeader.Title.fontMaterial.SetFloat("_Stencil", (float)maskLayer);
            return categoryHeader;
        }

        [HarmonyPatch(nameof(LobbyViewSettingsPane.Update)), HarmonyPostfix]
        public static void UpdatePostfix(LobbyViewSettingsPane __instance)
        {
            if ((int)__instance.currentTab < 3551) return;

            var isOdd = true;
            var offset = 2f;
            var isFirst = true;

            foreach (var option in OptionItem.AllOptions)
            {
                if ((int)option.Tab != ((int)__instance.currentTab - 3551)) continue; 
                if (option.IsText)
                {
                    if (isFirst)
                    {
                        offset += 0.3f;
                        isFirst = false;
                    }
                    foreach (var categoryHeader in CategoryHeaders)
                    {
                        if (option.Name == categoryHeader.name)
                        {
                            UpdateCategoryHeader(categoryHeader, ref offset);
                            continue;
                        }
                    }
                    continue;
                }
                if (isFirst) isFirst = false;
                UpdateOption(ref isOdd, option, ref offset);
            }
            __instance.scrollBar.ContentYBounds.max = (-offset) - 1.5f;
        }

        private static void UpdateCategoryHeader(CategoryHeaderMasked categoryHeader, ref float offset)
        {
            var enabled = true;
            // 检测是否隐藏设置
            enabled = (!Options.HideGameSettings.GetBool() || AmongUsClient.Instance.AmHost) && GameStates.IsModHost;
            categoryHeader.gameObject.SetActive(enabled);
            if (enabled)
            {
                offset -= LobbyViewSettingsPane.HEADER_SPACING_Y;
                categoryHeader.transform.localPosition = new(LobbyViewSettingsPane.HEADER_START_X, offset, -2f);
            }
        }

        private static void UpdateOption(ref bool isOdd, OptionItem option, ref float offset)
        {
            if (option?.ViewOptionBehaviour == null || option.ViewOptionBehaviour.gameObject == null) return;

            var enabled = true;
            var parent = option.Parent;

            // 检测是否隐藏设置
            enabled = !option.IsHiddenOn(Options.CurrentGameMode) && (!Options.HideGameSettings.GetBool() || AmongUsClient.Instance.AmHost) && GameStates.IsModHost;
            var infoPanelOption = option.ViewOptionBehaviour;
            while (parent != null && enabled)
            {
                enabled = parent.GetBool();
                parent = parent.Parent;
            }

            infoPanelOption.gameObject.SetActive(enabled);
            
            if (enabled)
            {
                infoPanelOption.labelBackground.color = option is IRoleOptionItem roleOption ? roleOption.RoleColor : (isOdd ? Color.cyan : Color.white);
                infoPanelOption.titleText.text = option.GetName(option is RoleSpawnChanceOptionItem);
                infoPanelOption.settingText.text = option.GetString();

                offset -= LobbyViewSettingsPane.SPACING_Y;
                if (option.IsHeader)
                {
                    offset -= HeaderSpacingY;
                }
                infoPanelOption.transform.localPosition = new Vector3(
                    LobbyViewSettingsPane.START_POS_X + 2f,
                    offset,
                    -2f);

                isOdd = !isOdd;
            }
        }
        private const float HeaderSpacingY = 0.2f;
    }
}