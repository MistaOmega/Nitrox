using UnityEngine.InputSystem;

namespace NitroxClient.MonoBehaviours.Gui.Input.KeyBindings.Actions;

public class PingKeyBindingAction : KeyBinding
{
    public PingKeyBindingAction() : base("Nitrox_Settings_Keybind_Ping", "<Mouse>/middleButton") { }

    public override void Execute(InputAction.CallbackContext _)
    {
        PlayerPingManager.TryQueuePing();
    }
}