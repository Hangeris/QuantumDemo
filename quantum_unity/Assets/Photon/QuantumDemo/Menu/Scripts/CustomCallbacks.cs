using Quantum;
using UnityEngine;

public class CustomCallbacks : QuantumCallbacks
{
    [Header("Reference")]
    [SerializeField] private RuntimePlayer runtimePlayer;
    
    public override void OnGameStart(Quantum.QuantumGame game)
    {
        if (game.Session.IsPaused) 
            return;

        foreach (var localPlayer in game.GetLocalPlayers())
        {
            Debug.Log("CustomCallbacks - sending player: " + localPlayer);
            game.SendPlayerData(localPlayer, runtimePlayer);
        }
    }

    public override void OnGameResync(Quantum.QuantumGame game)
    {
        Debug.Log("Detected Resync. Verified tick: " + game.Frames.Verified.Number);
    }
}