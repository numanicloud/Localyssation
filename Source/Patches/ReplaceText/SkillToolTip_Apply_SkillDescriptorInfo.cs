using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Localyssation.Util;

namespace Localyssation.Patches.ReplaceText
{
    [HarmonyPatch]
    public sealed class SkillToolTip_Apply_SkillDescriptorInfo
    {
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
            Localyssation.LogDebug($"Patching string.Replace");
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
                    Localyssation.LogDebug($"Replace({varName}, %) matched");

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
            Localyssation.LogDebug($"Patching weapon requirement");

            var replacements = new Dictionary<string, Func<string>>()
            {
                { Format("shield"), () => Localyssation.GetString(I18nKeys.SkillMenu.TOOLTIP_REQUIRE_SHIELD) },
                { Format("melee weapon"), () => Localize(SkillToolTipRequirement.MELEE) },
                { Format("heavy melee weapon"), () => Localize(SkillToolTipRequirement.HEAVY_MELEE) },
                { Format("ranged weapon"), () => Localize(SkillToolTipRequirement.RANGED) },
                { Format("heavy ranged weapon"), () => Localize(SkillToolTipRequirement.HEAVY_RANGED) },
                { Format("magic weapon"), () => Localize(SkillToolTipRequirement.MAGIC) },
                { Format("heavy magic weapon"), () => Localize(SkillToolTipRequirement.HEAVY_MAGIC) },
                // ↓ 追加効果確率。WeaponRequirementではないが都合がいいので。
                {
                    "\n\n<color=cyan>{0} - ({1}) ({2}% Chance)</color>",
                    () => Localyssation.GetString(
                        I18nKeys.SkillMenu.TOOLTIP_DESCRIPTOR_CONDITION_CHANCE)
                }
            };
            foreach (var replacement in replacements)
            {
                matcher.Start();
                matcher.MatchForward(true,
                        new CodeMatch(OpCodes.Ldstr, replacement.Key))
                    .Advance(1)
                    .InsertAndAdvance(Transpilers.EmitDelegate<Func<string, string>>(
                        original => replacement.Value.Invoke()));
            }
            return;

			string Format(string weaponTypeName) => $" <color=yellow>Requires a {weaponTypeName}.</color>";
            string Localize(SkillToolTipRequirement requirement)
            {
                return string.Format(
                    Localyssation.GetString(I18nKeys.SkillMenu.TOOLTIP_REQUIEMENT_FORMAT),
                    Localyssation.GetString(KeyUtil.GetForAsset(requirement)));
            }
        }

        private static void PatchConditionName(CodeMatcher matcher)
        {
            Localyssation.LogDebug($"Patching conditionName");
            matcher.MatchForward(true,
                    MemberAccessor<ScriptableCondition>
                        .GetFieldInfo(x => x._conditionName)
                        .LdfldMatch())
                .Repeat(cm =>
                {
                    Localyssation.LogDebug($"conditionName matched");
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
            Localyssation.LogDebug($"Patching conditionGroupTag");
            matcher.MatchForward(true,
					MemberAccessor<ScriptableConditionGroup>
                        .GetFieldInfo(x => x._conditionGroupTag)
                        .LdfldMatch())
                .Repeat(cm =>
                {
                    Localyssation.LogDebug($"conditionGroupTag matched");
                    cm.Advance(1)
                        .InsertAndAdvance(
                            new CodeInstruction(OpCodes.Ldloc_1),
							MemberAccessor<ScriptableCondition>
                                .GetFieldInfo(x => x._conditionGroup)
                                .LdfldInstruction(),
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
