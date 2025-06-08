using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace BoomboxController.Multiplayer
{
    public class MultiplayerManager : BoomboxController
    {
        [HarmonyPatch(typeof(HUDManager), "AddPlayerChatMessageServerRpc")]
        [HarmonyPostfix]
        [ServerRpc(RequireOwnership = false)]
        private static void AddPlayerChatMessageServerRpc_HUDManager(HUDManager __instance, string chatMessage, int playerId)
        {
            if (chatMessage.Length > 50)
            {
                __instance.GetType().GetMethod("AddPlayerChatMessageClientRpc", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[2] { chatMessage, playerId });
            }
        }

        [HarmonyPatch(typeof(HUDManager), "AddPlayerChatMessageClientRpc")]
        [HarmonyPostfix]
        [ClientRpc]
        private static void AddPlayerChatMessageClientRpc_HUDManager(HUDManager __instance, string chatMessage, int playerId)
        {
            if (Plugin.config.radiuscheck.Value)
            {
                if (IsCommand(chatMessage, new string[] { "bhelp", "bplay", "btime", "bvolume", "btrack" }))
                {
                    if (!(Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, __instance.playersManager.allPlayerScripts[playerId].transform.position) < 25f))
                    {
                        __instance.GetType().GetMethod("AddChatMessage", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[2] { chatMessage, __instance.playersManager.allPlayerScripts[playerId].playerUsername });
                    }
                }
            }
        }

        [HarmonyPatch(typeof(HUDManager), "SubmitChat_performed")]
        [HarmonyPrefix]
        private static void SubmitChat_performed_HUDManager(HUDManager __instance, ref InputAction.CallbackContext context)
        {
            if (LoadingMusicBoombox)
            {
                if (IsCommand(__instance.chatTextField.text, new string[] { "bhelp", "bplay", "btime", "bvolume", "btrack" })) __instance.chatTextField.text = String.Empty;
            }
            else
            {
                if (!blockcompatibility)
                {
                    if (IsCommand(__instance.chatTextField.text, new string[] { "bhelp", "bplay", "btime", "bvolume", "btrack" }))
                    {
                        SubmitChat(__instance);
                        return;
                    }
                }
                if (!string.IsNullOrEmpty(__instance.chatTextField.text) && __instance.chatTextField.text.Length < 1000)
                {
                    __instance.AddTextToChatOnServer(__instance.chatTextField.text, (int)__instance.localPlayer.playerClientId);
                }
                for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
                {
                    if (StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled && Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, StartOfRound.Instance.allPlayerScripts[i].transform.position) > 24.4f && (!GameNetworkManager.Instance.localPlayerController.holdingWalkieTalkie || !StartOfRound.Instance.allPlayerScripts[i].holdingWalkieTalkie))
                    {
                        __instance.playerCouldRecieveTextChatAnimator.SetTrigger("ping");
                        break;
                    }
                }
            }
        }

        public static void SubmitChat(HUDManager __instance)
        {
            if (!string.IsNullOrEmpty(__instance.chatTextField.text) && __instance.chatTextField.text.Length < 1000)
            {
                __instance.AddTextToChatOnServer(__instance.chatTextField.text, (int)__instance.localPlayer.playerClientId);
            }
            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                if (Plugin.config.radiuscheck.Value)
                {
                    if (StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled && (!GameNetworkManager.Instance.localPlayerController.holdingWalkieTalkie || !StartOfRound.Instance.allPlayerScripts[i].holdingWalkieTalkie))
                    {
                        __instance.playerCouldRecieveTextChatAnimator.SetTrigger("ping");
                        break;
                    }
                }
                else
                {
                    if (StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled && Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, StartOfRound.Instance.allPlayerScripts[i].transform.position) > 24.4f && (!GameNetworkManager.Instance.localPlayerController.holdingWalkieTalkie || !StartOfRound.Instance.allPlayerScripts[i].holdingWalkieTalkie))
                    {
                        __instance.playerCouldRecieveTextChatAnimator.SetTrigger("ping");
                        break;
                    }
                }
            }
            __instance.localPlayer.isTypingChat = false;
            __instance.chatTextField.text = "";
            EventSystem.current.SetSelectedGameObject(null);
            __instance.PingHUDElement(__instance.Chat);
            __instance.typingIndicator.enabled = false;
        }

        public static bool IsCommand(string text, string[] args)
        {
            foreach (string command in args)
            {
                if (text.Contains(command)) return true;
            }
            return false;
        }

        public static string SelectCommand(string text, string[] args)
        {
            foreach (string command in args)
            {
                if (text.Contains(command)) return command;
            }
            return "None";
        }

        [HarmonyPatch(typeof(HUDManager), "AddChatMessage")]
        [HarmonyPrefix]
        private static bool AddChatMessage_Multiplayer(HUDManager __instance, string chatMessage, string nameOfUserWhoTyped = "")
        {
            if (IsCommand(chatMessage, multi_name))
            {
                switch (SelectCommand(chatMessage, multi_name))
                {
                    case "SyncSong":
                        currectTrack = int.Parse(chatMessage[1].ToString());
                        boomboxItem.boomboxAudio.clip = musicList.ToList()[currectTrack].Value;
                        boomboxItem.boomboxAudio.pitch = 1f;
                        boomboxItem.boomboxAudio.Play();
                        boomboxItem.isPlayingMusic = true;
                        boomboxItem.isBeingUsed = true;
                        startMusics = false;
                        break;
                    case "None":
                        break;
                }
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
