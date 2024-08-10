using UnityEngine;

public class CustomCallbacks : QuantumCallbacks
{
    public override void OnGameStart(Quantum.QuantumGame game)
    {
        if (game.Session.IsPaused) 
            return;

        foreach (var localPlayer in game.GetLocalPlayers())
        {
            Debug.Log("CustomCallbacks - sending player: " + localPlayer);
            game.SendPlayerData(localPlayer, new Quantum.RuntimePlayer { });
        }
    }

    public override void OnGameResync(Quantum.QuantumGame game)
    {
        Debug.Log("Detected Resync. Verified tick: " + game.Frames.Verified.Number);
    }
}