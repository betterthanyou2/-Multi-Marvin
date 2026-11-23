using UnityEngine;
using UnityEngine.InputSystem;

public abstract class Ability : MonoBehaviour
{
    [Tooltip("Keyboard key that triggers this ability. Override WantsToActivate for mouse buttons, double-taps, etc.")]
    public Key key = Key.None;
    public float cooldown = 1f;

    float nextUseTime;

    public bool CanUse => Time.time >= nextUseTime;

    public virtual bool WantsToActivate() =>
        key != Key.None && Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;

    public void TryUse(Pawn owner)
    {
        if (!CanUse) return;
        nextUseTime = Time.time + cooldown;
        Activate(owner);
    }

    protected abstract void Activate(Pawn owner);
}