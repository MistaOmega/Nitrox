﻿using System.Collections.Generic;
using System.Linq;
using NitroxClient.MonoBehaviours.Gui.Input.KeyBindings;
using NitroxClient.MonoBehaviours.Gui.Input.KeyBindings.Actions;
using NitroxClient.Serialization;

namespace NitroxClient.MonoBehaviours.Gui.Input
{
    public class KeyBindingManager
    {
        public List<KeyBinding> KeyboardKeyBindings { get; }

        public KeyBindingManager()
        {
            ClientConfig cfg = ClientConfig.Load(ClientConfigSettings.NitroxRoamingDir);
            KeyboardKeyBindings = new List<KeyBinding>
            {
                // new bindings should not be set to a value equivalent to a pre-existing GameInput.Button enum or another custom binding
                new((int)KeyBindingValues.CHAT, "Chat", GameInput.Device.Keyboard, new DefaultKeyBinding(cfg.OpenChatKeybindPrimary, GameInput.BindingSet.Primary), new ChatKeyBindingAction(), !string.IsNullOrEmpty(cfg.OpenChatKeybindSecondary)),
                new((int)KeyBindingValues.FOCUS_DISCORD, "Focus Discord (Alt +)", GameInput.Device.Keyboard, new DefaultKeyBinding(cfg.FocusDiscordKeybindPrimary, GameInput.BindingSet.Primary), new DiscordFocusBindingAction(),
                    !string.IsNullOrEmpty(cfg.FocusDiscordKeybindSecondary)),
            };
        }

        // Returns highest custom key binding value. If no custom key bindings, returns 0. 
        public int GetHighestKeyBindingValue()
        {
            return KeyboardKeyBindings.Select(keyBinding => (int)keyBinding.Button).DefaultIfEmpty(0).Max();
        }
    }

    /// <summary>
    ///     Refers the keybinding values used for button creation in the options menu, to a more clear form
    /// </summary>
    public enum KeyBindingValues
    {
        CHAT = 45,
        FOCUS_DISCORD = 46
    }
}
