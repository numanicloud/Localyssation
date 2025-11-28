
using HarmonyLib;
using System.Collections.Generic;
using System;
using System.Reflection.Emit;
using Localyssation.Util;
using Mirror;
using UnityEngine;

namespace Localyssation.Patches.ReplaceText
{
    internal static partial class RTReplacer
    {
        [HarmonyPatch(typeof(PatternInstanceManager), nameof(PatternInstanceManager.On_DungeonKeyChange))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> PatternInstanceManager__On_DungeonKeyChange__Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return RTUtil.Wrap(instructions)
                .ReplaceStrings(new[]
                {
                    I18nKeys.ChatMessage.DUNGEON_KEY_DISSIPATES,
                    I18nKeys.ChatMessage.RECIEVE_DUNGEON_KEY
                })
                .Unwrap();
        }

        [HarmonyPatch(typeof(ChatBehaviour), nameof(ChatBehaviour.OnClick_GlobalChannel))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> ChatBehaviour__OnClick_GlobalChannel__Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return RTUtil.Wrap(instructions)
                .ReplaceStrings(new[] {
                    I18nKeys.ChatBehaviour.DISABLE_GLOBAL_CHANNEL_MESSAGE,
                    I18nKeys.ChatBehaviour.ENABLE_GLOBAL_CHANNEL_MESSAGE
                })
                .Unwrap();
        }

        [HarmonyPatch(typeof(ChatBehaviour), nameof(ChatBehaviour.OnClick_PartyChannel))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> ChatBehaviour__OnClick_PartyChannel__Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return RTUtil.Wrap(instructions)
                .ReplaceStrings(new[] {
                    I18nKeys.ChatBehaviour.DISABLE_PARTY_CHANNEL_MESSAGE,
                    I18nKeys.ChatBehaviour.ENABLE_PARTY_CHANNEL_MESSAGE
                })
                .Unwrap();
        }

        [HarmonyPatch(typeof(ChatBehaviour), nameof(ChatBehaviour.On_ChannelSwitch))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> ChatBehaviour__On_ChannelSwitch__Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);
            TranspilerHelper.RemoveMethodCallParamsStackForward(matcher, MessageCallbacks.New_ChatMessage, 7, OpCodes.Call);
            matcher.InsertAndAdvance(new CodeInstruction[] {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_2),
                Transpilers.EmitDelegate<Func<ChatBehaviour, string, string>>((cb, _new) =>
                {
                    return I18nKeys.ChatBehaviour.CHANNEL_SWTICH_MESSAGE_FORMAT.Format(cb._player._nickname, _new);
                })
            });
            return matcher.InstructionEnumeration();
        }

        /// Might conflict with command libs
        [HarmonyPatch(typeof(ChatBehaviour), nameof(ChatBehaviour.Send_ChatMessage))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ChatBehaviour__Send_ChatMessage__Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // テストコード
            var matcher = new CodeMatcher(instructions)
                .MatchForward(false,
                    new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(NetworkClient), nameof(NetworkClient.active))));
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_1),
                Transpilers.EmitDelegate<Func<string, string>>(msg =>
				{
                    Localyssation.logger.LogDebug($"Network client active: {NetworkClient.active}");
					Localyssation.logger.LogDebug($"Is Enter Key Down: {Input.GetKeyDown(KeyCode.Return)}");
					Localyssation.logger.LogDebug($"Chat message sent: {msg}");
                    return msg;
				}),
                new CodeInstruction(OpCodes.Pop));

            var matcher2 = new CodeMatcher(matcher.InstructionEnumeration())
                .MatchForward(false,
                    new CodeMatch(OpCodes.Ldarg_1),
                    new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(string), nameof(string.Empty))))
                .Advance(1);
            matcher2.InsertAndAdvance(
                Transpilers.EmitDelegate<Func<string, string>>(msg =>
                {
                    Localyssation.logger.LogDebug($"Chat message sent (empty check): {msg}");
                    return msg;
                }));

			return RTUtil.Wrap(matcher2.InstructionEnumeration())
                .ReplaceStrings(new[] {
                    I18nKeys.ChatBehaviour.GLOBAL_CHANNEL_DISABLED,
                    I18nKeys.ChatBehaviour.PARTY_CHANNEL_DISABLED,
                    I18nKeys.ChatBehaviour.ROOM_CHANNEL_DISABLED,
                    I18nKeys.ChatBehaviour.ENTER_A_ROOM_HINT
                }).Unwrap();
        }

        [HarmonyPatch(typeof(ChatBehaviour), nameof(ChatBehaviour.UserCode_Rpc_RecieveChatMessage__String__Boolean__ChatChannel))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ChatBehaviour__RecieveChatMessage__String__Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions)
                .MatchForward(false,
                    new CodeMatch(OpCodes.Stloc_1),
                    new CodeMatch(OpCodes.Ldc_I4_0),
                    new CodeMatch(OpCodes.Stloc_2),
                    new CodeMatch(OpCodes.Br));
            matcher.InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_1),
                Transpilers.EmitDelegate<Func<string, string>>(msg =>
                {
                    Localyssation.logger.LogDebug($"Chat message received: {msg}");
                    return msg;
                }),
                new CodeInstruction(OpCodes.Pop));

            return matcher.InstructionEnumeration();
		}

        [HarmonyPatch(typeof(ChatBehaviour), nameof(ChatBehaviour.UserCode_Cmd_SendChatMessage__String__ChatChannel))]
        [HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Cmd_SendChatMessage__String__ChatChannel_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions)
                .MatchForward(false,
                    new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(GameManager), nameof(GameManager._current))),
                    new CodeMatch(OpCodes.Ldarg_1),
					new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(GameManager), nameof(GameManager.ContainsUnicodeCharacter))),
                    new CodeMatch(OpCodes.Brfalse),
                    new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(string), nameof(string.Empty))),
                    new CodeMatch(OpCodes.Starg_S))
                .RemoveInstructions(6);

			matcher.InstructionEnumeration()
				.LogInstructions(nameof(Cmd_SendChatMessage__String__ChatChannel_Transpiler));

			return matcher.InstructionEnumeration();
		}


		[HarmonyPatch(typeof(ItemMenuCell), nameof(ItemMenuCell.PromptCmd_DropItem))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ItemMenuCell__PromptCmd_DropItem__Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);
            TranspilerHelper.RemoveMethodCallParamsStackForward(matcher, MessageCallbacks.New_ChatMessage, 5);
            matcher.InsertAndAdvance(new[]
            {
                new CodeInstruction(OpCodes.Ldloc_0),
                Transpilers.EmitDelegate<Func<ScriptableItem, string>>(item =>
                {
                    var key = KeyUtil.GetForAsset(item._scriptableQuest);
                    return I18nKeys.TabMenu.DROP_ITEM_ABANDON_QUEST_FORMAT.Format(key.Localize());
                })
            });
            return matcher.InstructionEnumeration();
        }

        [HarmonyPatch(typeof(WhoMenuCell), nameof(WhoMenuCell.Init_MutePeer))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> WhoMenuCell__Init_MutePeer__Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);
            TranspilerHelper.RemoveMethodCallParamsStackForward(matcher, MessageCallbacks.New_ChatMessage, 7);
            matcher.InsertAndAdvance(new[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate<Func<WhoMenuCell, string>>(cell =>
                {
                    return I18nKeys.ChatMessage.UNMUTE_PLAYER_FORMAT.Format(cell._selectedDataEntry._player._nickname);
                })
            });
            matcher.Advance(1);
            TranspilerHelper.RemoveMethodCallParamsStackForward(matcher, MessageCallbacks.New_ChatMessage, 7);
            matcher.InsertAndAdvance(new[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate<Func<WhoMenuCell, string>>(cell =>
                {
                    return I18nKeys.ChatMessage.MUTE_PLAYER_FORMAT.Format(cell._selectedDataEntry._player._nickname);
                })
            });
            return matcher.InstructionEnumeration();
        }
    }
}
