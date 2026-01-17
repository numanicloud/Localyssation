using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using HarmonyLib;
using Localyssation.Util;

namespace Localyssation.Patches.ReplaceText
{
    [HarmonyPatch]
    public class SkillToolTip_Apply_SkillDescriptorInfo
    {
        private static readonly Regex WeaponRequirementPattern = new Regex(@"Requires a (.+)\.");

        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            return AccessTools.GetDeclaredMethods(typeof(SkillToolTip))
                .Where(methodInfo => methodInfo.Name.Contains(
                    $"<{nameof(SkillToolTip.Apply_SkillDescriptorInfo)}>g__Init_TermMacros"))
                .Cast<MethodBase>()
                .FirstOrDefault();
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false,
                    new CodeMatch(OpCodes.Ldfld), // _scriptSkill
					new CodeMatch(OpCodes.Ldfld), // _skillDescription
                    new CodeMatch(OpCodes.Stloc_0))
                .Advance(1)
                .RemoveInstruction()
                .InsertAndAdvance(Transpilers.EmitDelegate<Func<ScriptableSkill, string>>(skill => Localyssation.GetString(KeyUtil.GetForAsset(skill) + "_DESCRIPTION")));

            matcher.Start();
			PatchStringReplace(matcher);

            matcher.Start();
            PatchWeaponRequirement(matcher);

            matcher.Start();
            PatchConditionName(matcher);

            matcher.Start();
            PatchConditionGroupTag(matcher);

            matcher.InstructionEnumeration().LogInstructions("Apply_SkillDescriptorInfo:");

            return matcher.InstructionEnumeration();
        }

        private static void PatchStringReplace(CodeMatcher matcher)
        {
            Localyssation.logger.LogDebug($"Patching string.Replace");
            matcher.MatchForward(true,
                    new CodeMatch(OpCodes.Callvirt,
                        AccessTools.Method(
                            typeof(string),
                            nameof(string.Replace),
                            new[] { typeof(string), typeof(string) })))
                .Repeat(cm =>
                {
                    cm.MatchBack(false,
                        new CodeMatch(OpCodes.Ldstr), // 置換変数名
                        new CodeMatch(OpCodes.Ldstr));  // フォーマット文字列
                    var varName = cm.Operand.ToString();
                    Localyssation.logger.LogDebug($"Replace({varName}, %) matched");

                    cm.Advance(2)
                        .InsertAndAdvance(
                            new CodeInstruction(OpCodes.Ldstr, varName),
                            Transpilers.EmitDelegate<Func<string, string, string>>(AlterSkillDescription));
                });
        }

        private static string AlterSkillDescription(string original, string variableName)
        {
            switch (variableName)
            {
            case "$SKP": return "<color=yellow>{0}</color>";
            case "$DMG": return "<color=yellow>({0} - {1})</color>";
            case "$COOLDWN": return Localyssation.GetString(I18nKeys.SkillMenu.TOOLTIP_DESCRIPTOR_COOLDOWN);
            case "$MANACOST":
                return Localyssation.GetString(I18nKeys.SkillMenu
                    .TOOLTIP_DESCRIPTOR_MANACOST);
            case "$HEALTHCOST":
                return Localyssation.GetString(I18nKeys.SkillMenu
                    .TOOLTIP_HEALTH_COST);
            case "$STAMINACOST":
                return Localyssation.GetString(I18nKeys.SkillMenu
                    .TOOLTIP_STAMINA_COST);
            case "$CASTTIME":
                return Localyssation.GetString(I18nKeys.SkillMenu
                    .TOOLTIP_DESCRIPTOR_CAST_TIME);
            case "$CASTTIME_INSTANT":
                return Localyssation.GetString(I18nKeys.SkillMenu
                    .TOOLTIP_DESCRIPTOR_CAST_TIME_INSTANT);
            default: return original;
            }
        }

		private static void PatchWeaponRequirement(CodeMatcher matcher)
        {
            Localyssation.logger.LogDebug($"Patching weapon requirement");
            matcher.MatchForward(true,
                    new CodeMatch(x => x.opcode == OpCodes.Ldstr && ((string)x.operand).Contains(" <color=yellow>Requires a ")))
                .Repeat(match =>
                {
                    Localyssation.logger.LogDebug($"weapon requirement matched");
                    match.Advance(1)
                        .InsertAndAdvance(Transpilers.EmitDelegate<Func<string, string>>(origin =>
                        {
                            var regexMatch = WeaponRequirementPattern.Match(origin);
                            if (!regexMatch.Success) return origin;

                            var weaponType = regexMatch.Groups[1].Value;
                            Localyssation.logger.LogDebug($"weapon type matched: {weaponType}, in {origin}");
							var requirement = AlterWeaponRequirement(weaponType);
							return string.Format(
                                Localyssation.GetString(I18nKeys.SkillMenu
                                    .TOOLTIP_REQUIEMENT_FORMAT),
                                requirement);
                        }));
                });
        }

        private static string AlterWeaponRequirement(string weaponType)
        {
            switch (weaponType)
            {
            case "shield":
                return Localyssation.GetString(I18nKeys.SkillMenu.TOOLTIP_REQUIRE_SHIELD);
            case "melee weapon":
                return Localyssation.GetString(KeyUtil.GetForAsset(SkillToolTipRequirement.MELEE));
            case "heavy melee weapon":
                return Localyssation.GetString(KeyUtil.GetForAsset(SkillToolTipRequirement.HEAVY_MELEE));
            case "ranged weapon":
                return Localyssation.GetString(KeyUtil.GetForAsset(SkillToolTipRequirement.RANGED));
            case "heavy ranged weapon":
                return Localyssation.GetString(KeyUtil.GetForAsset(SkillToolTipRequirement.HEAVY_RANGED));
            case "magic weapon":
                return Localyssation.GetString(KeyUtil.GetForAsset(SkillToolTipRequirement.MAGIC));
            case "heavy magic weapon":
                return Localyssation.GetString(KeyUtil.GetForAsset(SkillToolTipRequirement.HEAVY_MAGIC));
            default:
                return KeyUtil.GetForAsset(SkillToolTipRequirement.NONE);
            }
        }

		private static void PatchConditionName(CodeMatcher matcher)
        {
            Localyssation.logger.LogDebug($"Patching conditionName");
            matcher.MatchForward(true,
                    new CodeMatch(OpCodes.Ldfld,
                        AccessTools.Field(typeof(ScriptableCondition),
                            nameof(ScriptableCondition._conditionName))))
                .Repeat(cm =>
                {
                    Localyssation.logger.LogDebug($"conditionName matched");
                    cm.Advance(1)
                        .InsertAndAdvance(
                            new CodeInstruction(OpCodes.Ldloc_1),
                            EmitConditionNameLocalization());
                });
		}

        private static CodeInstruction EmitConditionNameLocalization()
        {
            return Transpilers.EmitDelegate<Func<string, ScriptableCondition, string>>(
                (src, condition) => Localyssation.GetString(
                    KeyUtil.GetForAsset(condition) + "_NAME"));
        }

		private static void PatchConditionGroupTag(CodeMatcher matcher)
        {
            Localyssation.logger.LogDebug($"Patching conditionGroupTag");
            matcher.MatchForward(true,
                    new CodeMatch(OpCodes.Ldfld,
                        AccessTools.Field(typeof(ScriptableConditionGroup),
                            nameof(ScriptableConditionGroup._conditionGroupTag))))
                .Repeat(cm =>
                {
                    Localyssation.logger.LogDebug($"conditionGroupTag matched");
                    cm.Advance(1)
                        .InsertAndAdvance(
                            new CodeInstruction(OpCodes.Ldloc_1),
                            new CodeInstruction(OpCodes.Ldfld,
                                AccessTools.Field(typeof(ScriptableCondition),
                                    nameof(ScriptableCondition._conditionGroup))),
                            EmitConditionGroupLocalization());
                });
        }

        private static CodeInstruction EmitConditionGroupLocalization()
        {
            return Transpilers.EmitDelegate<Func<string, ScriptableConditionGroup, string>>(
                (src, group) =>
                    Localyssation.GetString(KeyUtil.GetForAsset(group) + "_NAME"));
        }
    }
}
