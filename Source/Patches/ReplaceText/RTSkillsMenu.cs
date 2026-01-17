using HarmonyLib;
using Localyssation.LanguageModule;
using Localyssation.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;

namespace Localyssation.Patches.ReplaceText
{
    internal static partial class RTReplacer
    {
        // skills menu
        [HarmonyPatch(typeof(SkillsMenuCell), nameof(SkillsMenuCell.Cell_OnAwake))]
        [HarmonyPostfix]
        public static void SkillsMenuCell_Cell_OnAwake(SkillsMenuCell __instance)
        {
            RTUtil.RemapAllTextUnderObject(__instance.gameObject, new Dictionary<string, string>()
            {
                { "_text_skillsHeader", "TAB_MENU_CELL_SKILLS_HEADER" },
            });
            RTUtil.RemapChildTextsByPath(__instance.transform, new Dictionary<string, string>()
            {
                { "_backdrop_skillPoints/_text_skillPointsTag", "TAB_MENU_CELL_SKILLS_SKILL_POINT_COUNTER" },
                { "_skillContentGroups/Content_generalSkills/_skillsCell_skillListObject_recall/_text_skillRank", "SKILL_RANK_SOULBOUND" },
            }, (transform, key) =>
            {
                if (key == "TAB_MENU_CELL_SKILLS_SKILL_POINT_COUNTER")
                {
                    var text = transform.GetComponent<Text>();
                    if (text) text.alignment = TextAnchor.MiddleLeft;
                }
            });
        }

        [HarmonyPatch(typeof(SkillsMenuCell), nameof(SkillsMenuCell.Init_ClassTabTooltip))]
        [HarmonyPostfix]
        public static void SkillsMenuCell_Init_ClassTabTooltip(SkillsMenuCell __instance, int _tabValue)
        {
            var pStats = Player._mainPlayer._pStats;
            switch ((SkillTier)(byte)_tabValue)
            {
                case SkillTier.NOVICE:
                    ToolTipManager._current.Apply_GenericToolTip(Localyssation.GetString("TAB_MENU_CELL_SKILLS_CLASS_TAB_TOOLTIP_NOVICE"));
                    break;
                case SkillTier.CLASS:
                    var playerClass = Player._mainPlayer._pStats._class;
                    if (!playerClass) return;

                    var classKey = $"{KeyUtil.GetForAsset(playerClass)}_NAME";
                    if (LanguageManager.CurrentLanguage.ContainsKey(classKey + "_VARIANT_OF"))
                        classKey += "_VARIANT_OF";

                    ToolTipManager._current.Apply_GenericToolTip(string.Format(
                        Localyssation.GetString("TAB_MENU_CELL_SKILLS_CLASS_TAB_TOOLTIP"),
                        Localyssation.GetString(classKey, playerClass._className)));
                    break;
                case SkillTier.SUBCLASS:
                    if (!pStats._class || pStats._syncClassTier <= 0)
                    {
                        return;
                    }
                    ToolTipManager._current.Apply_GenericToolTip(
                        string.Format(
                            Localyssation.GetString("TAB_MENU_CELL_SKILLS_CLASS_TAB_TOOLTIP"),
                            KeyUtil.GetForAsset(pStats._class._playerClassTiers[pStats._syncClassTier - 1]).Name.Localize()
                        )
                    );
                    //pStats._class._playerClassTiers[pStats._syncClassTier - 1]._classTierName + " Skills");
                    break;
            }
        }

        [HarmonyPatch(typeof(SkillsMenuCell), nameof(SkillsMenuCell.Handle_CellUpdate))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> SkillsMenuCell_Handle_CellUpdate_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return RTUtil.SimpleStringReplaceTranspiler(instructions, new[] {
                I18nKeys.TabMenu.PAGER_1_PAGE,
                I18nKeys.TabMenu.PAGER_FORMAT
            });
        }

        [HarmonyPatch(typeof(SkillsMenuCell), nameof(SkillsMenuCell.Handle_CellUpdate))]
        [HarmonyPostfix]
        public static void SkillsMenuCell_Handle_CellUpdate(SkillsMenuCell __instance)
        {
            if (!TabMenu._current._isOpen || !Player._mainPlayer) return;

            var pStats = Player._mainPlayer._pStats;
            var txt = __instance._skillsCell_classHeader.text;
            var fontSize = __instance._skillsCell_classHeader.fontSize;
            switch (__instance._currentSkillTab)
            {
                case SkillTier.NOVICE:
                    txt = Localyssation.GetString("TAB_MENU_CELL_SKILLS_CLASS_HEADER_NOVICE", txt, fontSize);
                    break;
                case SkillTier.CLASS:
                    var classKey = $"{KeyUtil.GetForAsset(Player._mainPlayer._pStats._class)}_NAME";
                    if (LanguageManager.CurrentLanguage.ContainsKey(classKey + "_VARIANT_OF"))
                        classKey += "_VARIANT_OF";
                    txt = string.Format(
                        Localyssation.GetString("TAB_MENU_CELL_SKILLS_CLASS_HEADER", fontSize: fontSize),
                        Localyssation.GetString(classKey, Player._mainPlayer._pStats._class._className, fontSize));
                    break;
                case SkillTier.SUBCLASS:
                    if (!pStats._class || pStats._syncClassTier <= 0)
                    {
                        return;
                    }
                    txt = I18nKeys.TabMenu.CELL_SKILLS_CLASS_HEADER_FORMAT.Format(
                        KeyUtil.GetForAsset(pStats._class._playerClassTiers[pStats._syncClassTier - 1]).Name
                        );
                    break;
            }
            __instance._skillsCell_classHeader.text = txt;
        }

        [HarmonyPatch(typeof(SkillListDataEntry), nameof(SkillListDataEntry.Update))]
        [HarmonyPostfix]
        public static void SkillListDataEntry_Handle_SkillData(SkillListDataEntry __instance)
        {
            if (!__instance) return;
            if (!Player._mainPlayer || Player._mainPlayer._bufferingStatus || !__instance._scriptSkill) return;
            ScriptableSkill skill = __instance._scriptSkill;
            __instance._skillNameText.text = Localyssation.GetString(
                $"{KeyUtil.GetForAsset(skill)}_NAME",
                __instance._skillNameText.text,
                (int)__instance._skillNameText.fontSize
            );
            SkillStruct _skillStruct = __instance._skillStruct;

            if (_skillStruct._skillUnlocked)
            {
                if (__instance._skillRankText)
                {
                    // "rank" now is skill type
                    if (skill._skillControlType == SkillControlType.Passive)
                    {
                        __instance._skillRankText.text = Localyssation.GetString(KeyUtil.GetForAsset(skill._skillControlType));
                    }
                    else
                    {
                        __instance._skillRankText.text = Localyssation.GetString(KeyUtil.GetForAsset(skill._skillUtilityType));
                    }

                }
            }
            else
            {
                //__instance._skillRankText.text = $"Point Cost: {__instance._scriptSkill._skillRankParams._skillPointCost}";
                if (__instance._skillRankText)
                {
                    __instance._skillRankText.text = string.Format(
                        Localyssation.GetString(I18nKeys.SkillMenu.SKILL_POINT_COST_FORMAT),
                        skill._skillRankParams._skillPointCost
                    );
                }

            }

        }

        [HarmonyPatch(typeof(SkillToolTip), nameof(SkillToolTip.Apply_SkillStats))]
        [HarmonyPostfix]
        public static void SkillToolTip_Apply_SkillStats(SkillToolTip __instance)
        {
            if (!Player._mainPlayer || !__instance._scriptSkill) return;
            var skill = __instance._scriptSkill;
            var key = KeyUtil.GetForAsset(__instance._scriptSkill);
            __instance._toolTipName.text = Localyssation.GetString($"{key}_NAME", fontSize: __instance._toolTipName.fontSize);


            if (skill._skillControlType != SkillControlType.Passive)
            {
                NonPassiveSkillsTooltip();
            }
            else
            {
                __instance._toolTipSubName.text = Localyssation.GetString(KeyUtil.GetForAsset(SkillControlType.Passive));
            }



            void NonPassiveSkillsTooltip()
            {
                __instance._toolTipSubName.text = Localyssation.GetString(KeyUtil.GetForAsset(skill._skillUtilityType));
                __instance._scaleTypeText.text = Localyssation.GetString(KeyUtil.GetForAsset(skill._skillDamageType));
            }

        }

        [HarmonyPatch(typeof(SkillToolTip), nameof(SkillToolTip.Apply_SkillStats))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> SkillToolTip_Apply_SkillStats_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return RTUtil.SimpleStringReplaceTranspiler(instructions, new[] {
                I18nKeys.SkillMenu.TOOLTIP_MANA_COST,
                I18nKeys.SkillMenu.TOOLTIP_HEALTH_COST,
                I18nKeys.SkillMenu.TOOLTIP_STAMINA_COST,
                I18nKeys.SkillMenu.TOOLTIP_CAST_TIME,
                I18nKeys.SkillMenu.TOOLTIP_ITEM_COST,
                I18nKeys.SkillMenu.TOOLTIP_CAST_TIME_INSTANT,
                I18nKeys.SkillMenu.TOOLTIP_COOLDOWN
            });
        }





        /// <summary>
        /// Total rewrite
        /// </summary>
        /// <param name="__instance"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(SkillToolTip), nameof(SkillToolTip.Apply_SkillDescriptorInfo))]
        [HarmonyPrefix]
        public static bool SkillToolTip__Apply_SkillDescriptorInfo__Prefix(SkillToolTip __instance)
        {
            PlayerStats _pStats = Player._mainPlayer._pStats;
            int _skillPower = 0;
            switch (__instance._scriptSkill._skillDamageType)
            {
                case DamageType.Strength:
                    _skillPower = __instance._scriptSkill._skillRankParams._baseSkillPower + (int)((float)_pStats._statStruct._attackPower * GameManager._current._statLogics._powerStatPercent);
                    break;
                case DamageType.Dexterity:
                    _skillPower = __instance._scriptSkill._skillRankParams._baseSkillPower + (int)((float)_pStats._statStruct._dexPower * GameManager._current._statLogics._powerStatPercent);
                    break;
                case DamageType.Mind:
                    _skillPower = __instance._scriptSkill._skillRankParams._baseSkillPower + (int)((float)_pStats._statStruct._magicPower * GameManager._current._statLogics._powerStatPercent);
                    break;
            }

            if (__instance._scriptSkill._skillRankParams._useWeaponPower && (bool)_pStats._player._pCombat._equippedWeapon)
            {
                _skillPower += _pStats._player._pCombat._equippedWeapon.Get_BaseCeilingDamage();
            }

            if (__instance._skillConditionsListing.text != string.Empty)
            {
                __instance._skillConditionsListing.text += "\n";
            }

            Init_TermMacros();
            __instance.Apply_ConditionRankInfo();

            void Init_TermMacros()
            {
                if (__instance._scriptSkill._skillDescription == string.Empty) return;

                __instance._toolTipDescription.gameObject.SetActive(value: true);
                string skillDescription = Localyssation.GetString(KeyUtil.GetForAsset(__instance._scriptSkill) + "_DESCRIPTION");
                ScriptableCondition scriptableCondition = null;
                ConditionSlot skillObjectCondition = __instance._scriptSkill._skillRankParams._skillObjectCondition;
                int bonusPower = skillObjectCondition._conditionPower;
                int bonusDuration = skillObjectCondition._conditionDuration;
                if ((bool)skillObjectCondition._scriptableCondition)
                {
                    scriptableCondition = skillObjectCondition._scriptableCondition;
                }

                skillDescription = skillDescription.Replace("$SKP", $"<color=yellow>{_skillPower}</color>");
                skillDescription = skillDescription.Replace("$DMG", $"<color=yellow>({_skillPower} - {_skillPower + 3})</color>");
                skillDescription = skillDescription.Replace("$COOLDWN", string.Format(
                    Localyssation.GetString(I18nKeys.SkillMenu.TOOLTIP_DESCRIPTOR_COOLDOWN), __instance._scriptSkill._skillRankParams._baseCooldown
                ));
                skillDescription = skillDescription.Replace("$MANACOST", string.Format(
                    //$"<color=yellow>Costs {__instance._scriptSkill._skillRankParams._manaCost} Mana.</color>"
                    Localyssation.GetString(I18nKeys.SkillMenu.TOOLTIP_DESCRIPTOR_MANACOST),
                    __instance._scriptSkill._skillRankParams._manaCost
                ));
                skillDescription = skillDescription.Replace("$HEALTHCOST", string.Format(
                    //$"<color=yellow>Costs {__instance._scriptSkill._skillRankParams._healthCost} Health.</color>"
                    Localyssation.GetString(I18nKeys.SkillMenu.TOOLTIP_HEALTH_COST),
                    __instance._scriptSkill._skillRankParams._healthCost
                ));
                skillDescription = skillDescription.Replace("$STAMINACOST", string.Format(
                    //$"<color=yellow>Costs {__instance._scriptSkill._skillRankParams._staminaCost} Stamina.</color>"
                    Localyssation.GetString(I18nKeys.SkillMenu.TOOLTIP_STAMINA_COST),
                    __instance._scriptSkill._skillRankParams._staminaCost
                ));
                skillDescription = (!(__instance._scriptSkill._skillRankParams._baseCastTime > 0.12f)) ?
                    skillDescription.Replace("$CASTTIME", Localyssation.GetString(I18nKeys.SkillMenu.TOOLTIP_DESCRIPTOR_CAST_TIME_INSTANT)) :
                    skillDescription.Replace("$CASTTIME", string.Format(Localyssation.GetString(I18nKeys.SkillMenu.TOOLTIP_DESCRIPTOR_CAST_TIME), __instance._scriptSkill._skillRankParams._baseCastTime));
                skillDescription = skillDescription.Replace("$RECALLINPUT", "<color=yellow>[" + InputControlManager.current.Convert_KeyCodeName(InputControlManager.current._recall.ToString()) + "]</color>");
                skillDescription = skillDescription.Replace("$CHARGEATKINPUT", "<color=yellow>[" + InputControlManager.current.Convert_KeyCodeName(InputControlManager.current._chargeAttack.ToString()) + "]</color>");
                skillDescription = skillDescription.Replace("$DASHINPUT", "<color=yellow>[" + InputControlManager.current.Convert_KeyCodeName(InputControlManager.current._dash.ToString()) + "]</color>");
                if (__instance._scriptSkill._requireShield)
                {
                    skillDescription += Localyssation.GetString(I18nKeys.SkillMenu.TOOLTIP_REQUIRE_SHIELD);
                }

                if (__instance._scriptSkill._toolTipRequirement != SkillToolTipRequirement.NONE)
                {
                    skillDescription += string.Format(
                        Localyssation.GetString(I18nKeys.SkillMenu.TOOLTIP_REQUIEMENT_FORMAT),
                        Localyssation.GetString(KeyUtil.GetForAsset(__instance._scriptSkill._toolTipRequirement))
                    );
                }

                if ((bool)scriptableCondition && !string.IsNullOrWhiteSpace(scriptableCondition._conditionDescription))
                {
                    string text = scriptableCondition.Generate_ConditionDescriptor(
                        _pStats._statStruct,
                        __instance._scriptSkill._skillDamageType,
						bonusPower,
                        bonusDuration,
                        skillObjectCondition._powerPercent,
                        skillObjectCondition._conditionRepeatRate);
                    string text2 = $"\n\n<color=cyan>{Localyssation.GetString(KeyUtil.GetForAsset(scriptableCondition) + "_NAME")} - ({Localyssation.GetString(KeyUtil.GetForAsset(scriptableCondition._conditionGroup) + "_NAME")})";
                    text2 += skillObjectCondition._chance >= 1f
                        ? ""
                        : string.Format(Localyssation.GetString(I18nKeys.SkillMenu.TOOLTIP_DESCRIPTOR_CONDITION_CHANCE), skillObjectCondition._chance * 100f);
                    text2 += "</color>\n" + text;
                    skillDescription += text2;
                }

                __instance._toolTipDescription.text = skillDescription;
            }
            return false;
        }

        [HarmonyPatch(typeof(ScriptableStatusCondition), nameof(ScriptableStatusCondition.Generate_ConditionDescriptor))]
        [HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> ScriptableStatusCondition__Generate_ConditionDescriptor__Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            Localyssation.logger.LogDebug($"{nameof(ScriptableStatusCondition__Generate_ConditionDescriptor__Transpiler)}");

            var conditionDescriptionPos = AccessTools.Field(typeof(ScriptableCondition),
                nameof(ScriptableCondition._conditionDescription));

            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldfld, conditionDescriptionPos),
                new CodeMatch(OpCodes.Stloc_0));
            matcher.Advance(1);

            Localyssation.logger.LogDebug($"matcher Pos={matcher.Pos}, Opcode={matcher.Opcode}, Remaining={matcher.Remaining}");

			matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate<Func<string, ScriptableStatusCondition, string>>(
                    (src, instance) =>
                    {
                        var description = Localyssation.GetString(KeyUtil.GetForAsset(instance) + "_DESCRIPTION");
                        Localyssation.logger.LogDebug($"Description of {instance.name}");
                        Localyssation.logger.LogDebug($"Replaced condition description: '{src}' -> '{description}'");
                        return description;
					}));

			Localyssation.logger.LogDebug($"matcher Pos={matcher.Pos}, Opcode={matcher.Opcode}, Remaining={matcher.Remaining}");
            matcher.InstructionEnumeration().LogInstructions();

			return matcher.InstructionEnumeration();
        }

		//[HarmonyPatch(typeof(ScriptableStatusCondition), nameof(ScriptableStatusCondition.Generate_ConditionDescriptor))]
        //[HarmonyPrefix]
        public static bool ScriptableStatusCondition__Generate_ConditionDescriptor__Prefix(
            ScriptableStatusCondition __instance,
            StatStruct _parentStatStruct,
            DamageType _attribute,
            int _setPower,
            int _setDuration,
            float _setPercent,
            float _setRepeatRate,
			ref string __result)
        {
            string conditionDescription = Localyssation.GetString(KeyUtil.GetForAsset(__instance) + "_DESCRIPTION");
            Localyssation.logger.LogDebug(__instance.name);
            Localyssation.logger.LogDebug($"result-src: {conditionDescription}");

			int num = __instance.Get_ConditionPower(_parentStatStruct, _attribute, _setPower, _setPercent);
            var stat = __instance._statStruct;
            __result = conditionDescription.Replace("$BASEPOWER", $"<color=yellow>{num}</color>")
                .Replace("$APPLY_HEALTH", AsString(num * __instance._applyHealth))
                .Replace("$STAT_MAXHEALTH", AsString(GetStat(stat._maxHealth)))
                .Replace("$STAT_MAXMANA", AsString(GetStat(stat._maxMana)))
                .Replace("$STAT_MAXSTAMINA", AsString(GetStat(stat._maxStamina)))
                .Replace("$STAT_EXP", AsString(stat._experience))
                .Replace("$STAT_ATTACKPOWER", AsString(GetStat(stat._attackPower)))
                .Replace("$STAT_MAGICPOWER", AsString(GetStat(stat._magicPower)))
                .Replace("$STAT_DEXPOWER", AsString(GetStat(stat._dexPower)))
                .Replace("$STAT_DEFENSE", AsString(GetStat(stat._defense)))
                .Replace("$STAT_MAGICDEFENSE", AsString(GetStat(stat._magicDefense)))
                .Replace("$STAT_CRITRATE", AsFloatString(GetFloatStat(stat._criticalRate)))
                .Replace("$STAT_MAGICCRITRATE", AsFloatString(GetFloatStat(stat._magicCriticalRate)))
                .Replace("$STAT_EVASION", AsFloatString(GetFloatStat(stat._evasion)))
                .Replace("$STAT_RESISTFIRE", AsString(GetStat(stat._fireResist)))
                .Replace("$STAT_RESISTWATER", AsString(GetStat(stat._waterResist)))
                .Replace("$STAT_RESISTNATURE", AsString(GetStat(stat._natureResist)))
                .Replace("$STAT_RESISTEARTH", AsString(GetStat(stat._earthResist)))
                .Replace("$STAT_RESISTHOLY", AsString(GetStat(stat._holyResist)))
                .Replace("$STAT_RESISTSHADOW", AsString(GetStat(stat._shadowResist)))
                .Replace("$ABSORB", AsString(GetStat(__instance._damageAbsorbtionAmount)))
                .Replace("$MOVSPEED", AsFloatString(__instance._movSpeedPercentChange * 100f))
				.Replace("$DURATION", "")
				.Replace("$RATE", string.Format(Localyssation.GetString(I18nKeys.ScriptableStatusCondition.RATE_FORMAT), _setRepeatRate));
            if (_setDuration > 0f)
            {
                var duration =
                    string.Format(Localyssation.GetString(
                        I18nKeys.ScriptableStatusCondition.DURATION_FORMAT),
                        _setDuration);
				__result += $" <color=yellow>{duration}</color>.";
			}

            Localyssation.logger.LogDebug($@"
parent-attack: {_parentStatStruct._attackPower}
parent-magic: {_parentStatStruct._magicPower}
parent-dex: {_parentStatStruct._dexPower}
parent-health: {_parentStatStruct._maxHealth}
attribute: {_attribute}
setPower: {_setPower}
setPercent: {_setPercent}
basePower: {num}
applyHealth: {__instance._applyHealth}
num*applyHealth: {num * __instance._applyHealth}
maxHealth: {stat._maxHealth}
maxMana: {stat._maxMana}
maxStamina: {stat._maxStamina}
experience: {stat._experience}
attackPower: {stat._attackPower}
magicPower: {stat._magicPower}
dexPower: {stat._dexPower}
defense: {stat._defense}
magicDefense: {stat._magicDefense}
criticalRate: {stat._criticalRate}
magicCriticalRate: {stat._magicCriticalRate}
evasion: {stat._evasion}
fireResist: {stat._fireResist}
waterResist: {stat._waterResist}
natureResist: {stat._natureResist}
earthResist: {stat._earthResist}
holyResist: {stat._holyResist}
shadowResist: {stat._shadowResist}
damageAbsorbtionAmount: {__instance._damageAbsorbtionAmount}
moveSpeedPercentChange: {__instance._movSpeedPercentChange}
");
            Localyssation.logger.LogDebug($"result-dst: {__result}");
			return false;

            string AsString(int source) => $"<color=yellow>{Mathf.Abs(source)}</color>";

            string AsFloatString(float source) => $"<color=yellow>{Mathf.Abs(source):N2}%</color>";

            int GetStat(int source)
            {
                var result = __instance.StatCalculate(_parentStatStruct, _attribute, source,
                    _setPower, _setPercent);
                Localyssation.logger.LogDebug($"calc({nameof(_parentStatStruct)}, {_attribute}, {source}, {_setPower}, {_setPercent}) = {result}");
                return result;
            }

            float GetFloatStat(float source)
            {
                var result = __instance.FloatStatCalculate(_parentStatStruct,
                    _attribute,
                    source,
                    _setPower,
                    _setPercent);
                return result * 100f;
            }
        }

        [HarmonyPatch(typeof(SkillToolTip), nameof(SkillToolTip.Apply_ConditionRankInfo))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> SkillToolTip__Apply_ConditionRankInfo__Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);
            matcher.MatchForward(true,
                new CodeMatch(OpCodes.Ldfld,
                    AccessTools.Field(typeof(ScriptableCondition),
                        nameof(ScriptableCondition._conditionName))))
                .Advance(1);

            matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_0),
                Transpilers.EmitDelegate<Func<string, ScriptableCondition, string>>((
                    src,
                    condition) => Localyssation.GetString(KeyUtil.GetForAsset(condition) + "_NAME")));

            matcher.MatchForward(true,
                new CodeMatch(OpCodes.Ldfld,
                    AccessTools.Field(typeof(ScriptableConditionGroup),
                        nameof(ScriptableConditionGroup._conditionGroupTag))))
                .Advance(1);

            matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_0),
                Transpilers.EmitDelegate<Func<string, ScriptableCondition, string>>((
                    src,
                    condition) => Localyssation.GetString(KeyUtil.GetForAsset(condition._conditionGroup) + "_NAME")));

            matcher.InstructionEnumeration().LogInstructions("Apply_ConditionRankInfo");

			return RTUtil.SimpleStringReplaceTranspiler(matcher.InstructionEnumeration(), new[] {
                I18nKeys.SkillMenu.TOOLTIP_DESCRIPTOR_CONDITION_CANCEL_ON_HIT
            });
		}

		//[HarmonyPatch(typeof(SkillToolTip), nameof(SkillToolTip.Apply_ConditionRankInfo))]
        //[HarmonyPrefix]
        public static bool SkillToolTip__Apply_ConditionRankInfo__Prefix(SkillToolTip __instance)
        {
            if (__instance._scriptSkill._skillRankParams._selfConditionOutput == null) return false;

            ScriptableCondition condition = __instance._scriptSkill._skillRankParams._selfConditionOutput._scriptableCondition;
            if (!(bool)condition || !(bool)condition._conditionGroup)
                return false;

            __instance._skillConditionsListing.text += "\n";
            var text = string.Format("<color=cyan>{0} - ({1})</color>\n",
                Localyssation.GetString(KeyUtil.GetForAsset(condition) + "_NAME"),
                Localyssation.GetString(KeyUtil.GetForAsset(condition._conditionGroup) + "_NAME")
            );

            var scriptSkill = __instance._scriptSkill;
            var conditionOutput = scriptSkill._skillRankParams._selfConditionOutput;
            text += condition.Generate_ConditionDescriptor(
                Player._mainPlayer._pStats._statStruct,
                scriptSkill._skillDamageType,
                conditionOutput._conditionPower,
                conditionOutput._conditionDuration,
                conditionOutput._powerPercent,
                conditionOutput._conditionRepeatRate);
            __instance._skillConditionsListing.text += text;

            // 廃止された？
            //if (condition._isStackable)
            //{
            //    __instance._skillConditionsListing.text += Localyssation.GetString(I18nKeys.SkillMenu.TOOLTIP_DESCRIPTOR_CONDITION_IS_STACKABLE);
            //}

            if (condition._cancelOnHit)
            {
                __instance._skillConditionsListing.text += Localyssation.GetString(I18nKeys.SkillMenu.TOOLTIP_DESCRIPTOR_CONDITION_CANCEL_ON_HIT);
            }
            return false;
        }

    }

    //[HarmonyPatch]
    //class SkillToolTip__Apply_SkillDescriptorInfo__Transpiler
    //{
    //    private static readonly TargetInnerMethod __CONDITION = new TargetInnerMethod()
    //    {
    //        InnerMethodName = "Init_TermMacros",
    //        ParentMethodName = nameof(SkillToolTip.Apply_SkillDescriptorInfo),
    //        Type = typeof(SkillToolTip)
    //    };
    //    public static MethodBase TargetMethod() => TranspilerHelper.GenerateTargetMethod(__CONDITION);

    //    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    //    {
    //        Localyssation.logger.LogDebug("Transpiling " + TargetMethod().Name);
    //        instructions.Do((c) => Localyssation.logger.LogDebug(c.ToString()));
    //        var matcher = RTUtil.Wrap(instructions)
    //            .ReplaceStrings(new string[]
    //            {
    //                I18nKeys.SkillMenu.TOOLTIP_DESCRIPTOR_COOLDOWN,
    //                I18nKeys.SkillMenu.TOOLTIP_DESCRIPTOR_MANACOST,
    //                I18nKeys.SkillMenu.TOOLTIP_HEALTH_COST,
    //                I18nKeys.SkillMenu.TOOLTIP_STAMINA_COST,
    //                I18nKeys.SkillMenu.TOOLTIP_DESCRIPTOR_CAST_TIME_INSTANT,
    //                I18nKeys.SkillMenu.TOOLTIP_CAST_TIME,
    //                I18nKeys.SkillMenu.TOOLTIP_REQUIRE_SHIELD,
    //                I18nKeys.SkillMenu.TOOLTIP_REQUIRE_MELEE_WEAPON,
    //                I18nKeys.SkillMenu.TOOLTIP_REQUIRE_RANGED_WEAPON,
    //                I18nKeys.SkillMenu.TOOLTIP_REQUIRE_MAGIC_WEAPON,
    //                I18nKeys.SkillMenu.TOOLTIP_REQUIRE_HEAVY_MELEE_WEAPON,
    //                I18nKeys.SkillMenu.TOOLTIP_REQUIRE_HEAVY_RANGED_WEAPON,
    //                I18nKeys.SkillMenu.TOOLTIP_REQUIRE_HEAVY_MAGIC_WEAPON,
    //            }).Matcher();
    //        Localyssation.logger.LogDebug("Transpiling " + TargetMethod().Name);
    //        instructions.Do((c) => Localyssation.logger.LogDebug(c.ToString()));
    //        // string skillDescription = _scriptSkill._skillDescription;
    //        matcher.MatchForward(false, new[]
    //        {
    //            new CodeMatch(OpCodes.Stloc_0)
    //        })
    //            .RemoveInstructionsWithOffsets(-2, -1)
    //            .Advance(-3)
    //            .InsertAndAdvance(new CodeInstruction[] {
    //                Transpilers.EmitDelegate<Func<SkillToolTip, string>>(instance =>
    //                {
    //                    return KeyUtil.GetForAsset(instance._scriptSkill).Description.Localize();
    //                })
    //            });

    //        Localyssation.logger.LogDebug("Replaced `string skillDescription = _scriptSkill._skillDescription;`");

    //        //string conditionDescriptor = scriptableCondition.Generate_ConditionDescriptor(_pStats._statStruct, bonusPower, bonusDuration);
    //        byte conditionDescriptor = 6;

    //        // str3 = string.Format("\n\n<color=cyan>{0} - ({1}) ({2}% Chance)</color>", (object) scriptableCondition._conditionName, (object) scriptableCondition._conditionGroup._conditionGroupTag, (object) (float) ((double) skillObjectCondition._chance * 100.0)) + "\n" + conditionDescriptor;
    //        matcher
    //            .Start()
    //            .MatchForward(false, new CodeMatch(OpCodes.Switch))
    //            .MatchForward(false, new CodeMatch(OpCodes.Brfalse))
    //            .MatchForward(false, new CodeMatch(OpCodes.Brtrue));

    //        var replaceEnd = matcher.MatchForward(false, new[]
    //        {
    //            new CodeMatch(OpCodes.Call, typeof(string).GetMethod("Format", new System.Type[] { typeof(string), typeof(object), typeof(object), typeof(object) })),
    //        }).Pos;
    //        if (matcher.IsInvalid)
    //        {
    //            Localyssation.logger.LogError("Cannot locate end of if - str3");
    //        }

    //        var replaceStart = matcher.MatchBack(false, new[]
    //        {
    //            new CodeMatch(OpCodes.Ldstr, "\n\n<color=cyan>{0} - ({1}) ({2}% Chance)</color>"),
    //        }).Pos;
    //        Localyssation.logger.LogDebug(matcher.Instruction.ToString());
    //        if (matcher.IsInvalid)
    //        {
    //            Localyssation.logger.LogError("Cannot locate start of if - str3");
    //        }
            
    //        Localyssation.logger.LogDebug(matcher.Instruction.ToString());
    //        matcher.RemoveInstructionsInRange(replaceStart, replaceEnd);
    //        matcher.Insert(new CodeInstruction[]{
    //            new CodeInstruction(OpCodes.Ldloc_1),   // scriptableCondition
    //            new CodeInstruction(OpCodes.Ldloc_2),   // skillObjectCondition
    //            Transpilers.EmitDelegate<Func<ScriptableCondition, ConditionSlot, string>>((scriptableCondition, skillObjectCondition) =>
    //            {
    //                return I18nKeys.SkillMenu.TOOLTIP_DESCRIPTOR_CONDITION_CHANCE.Format(
    //                    KeyUtil.GetForAsset(scriptableCondition).Name.Localize(),
    //                    KeyUtil.GetForAsset(scriptableCondition._conditionGroup).Name.Localize(),
    //                    skillObjectCondition._chance * 100
    //                    );
    //            })
    //        });
    //        Localyssation.logger.LogDebug("After replace:");
    //        matcher.Instructions().GetRange(matcher.Pos, 10).Do(ins => Localyssation.logger.LogDebug(ins));
            
    //        Localyssation.logger.LogDebug("Replaced `str3 = string.Format(\"\\n\\n<color=cyan>{0} - ({1}) ({2}% Chance)</color>\", (object) scriptableCondition._conditionName, ...`");



    //        // str3 = "\n\n<color=cyan>" + scriptableCondition._conditionName + " - (" + scriptableCondition._conditionGroup._conditionGroupTag + ")</color>\n" + conditionDescriptor;
    //        matcher.Start()
    //            .MatchForward(false, new CodeMatch(OpCodes.Ldstr, "\n\n<color=cyan>"));

    //        replaceEnd = matcher.MatchForward(false, new[]
    //        {
    //            new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(string), nameof(string.Concat), new[]{ typeof(string[])}))
    //        }).Pos;
    //        if (matcher.IsInvalid)
    //        {
    //            Localyssation.logger.LogError("Cannot locate end of else - str3");
    //        }

    //        replaceStart = matcher.MatchBack(false, new[]
    //        {
    //            new CodeMatch(OpCodes.Ldc_I4_6),
    //            new CodeMatch(OpCodes.Newarr, typeof(string)),
    //            new CodeMatch(OpCodes.Dup),
    //        }).Pos;
    //        if (matcher.IsInvalid)
    //        {
    //            Localyssation.logger.LogError("Cannot locate start of else - str3");
    //        }

    //        Localyssation.logger.LogDebug("Before replace:");
    //        matcher.Instructions().GetRange(matcher.Pos, 10).Do(ins => Localyssation.logger.LogDebug(ins));
    //        matcher.RemoveInstructionsInRange(replaceStart, replaceEnd);



    //        matcher.InsertAndAdvance(new CodeInstruction[]{
    //            new CodeInstruction(OpCodes.Ldloc_1),
    //            new CodeInstruction(OpCodes.Ldloc_S, conditionDescriptor),
    //            Transpilers.EmitDelegate<Func<ScriptableCondition, string, string>>((_sc, _str) =>{
    //                return "\n\n<color=cyan>"
    //                    + KeyUtil.GetForAsset(_sc).Name.Localize()
    //                    + " - ("
    //                    + KeyUtil.GetForAsset(_sc._conditionGroup).Name.Localize()
    //                    + ")</color>\n"
    //                    + _str;
    //            })
    //        });
    //        Localyssation.logger.LogDebug("Replaced `str3 = \"\\n\\n<color=cyan>\" + scriptableCondition._conditionName + \" - (\" + ...`");

    //        return matcher.Instructions();

    //    }
    //}

}
