using Photon.Deterministic;
using Quantum;
using UnityEngine;

public class LocalInput : MonoBehaviour
{
    public const string InputJump = "Jump";
    public const string InputHorizontal = "Horizontal";
    public const string InputVertical = "Vertical";
    
    private void OnEnable()
    {
        QuantumCallback.Subscribe(this, (CallbackPollInput callback) => PollInput(callback));
    }

    public void PollInput(CallbackPollInput callback)
    {
        var input = new Quantum.Input();
        
        input.Jump = UnityEngine.Input.GetButton(InputJump);
        
        var inputDirection = new Vector2(
            UnityEngine.Input.GetAxis(InputHorizontal), 
            UnityEngine.Input.GetAxis(InputVertical));
        
        input.X = (short)(inputDirection.x * 10);
        input.Y = (short)(inputDirection.y * 10);
        
        callback.SetInput(input, DeterministicInputFlags.Repeatable);
    }
}