using System;
using Quantum;
using UnityEngine;

public class PlayerCameraHandler : MonoBehaviour
{
    [SerializeField] private EntityView entityView;

    private void OnEnable()
    {
        entityView.OnEntityInstantiated.AddListener(OnEntityInstantiated);
    }
    private void OnDisable()
    {
        entityView.OnEntityInstantiated.RemoveListener(OnEntityInstantiated);
    }

    private void OnEntityInstantiated(QuantumGame quantumGame)
    {
        Debug.Log($"PlayerHandler.OnEntityInstantiated");

        var game = QuantumRunner.Default.Game;
        var frame = game.Frames.Verified;

        if (!frame.TryGet(entityView.EntityRef, out PlayerLink playerLink))
            return;

        if (!game.PlayerIsLocal(playerLink.Player))
            return;
        
        // In a large project this would be changed with Dependency Injection or a Service Locator pattern
        var virtualCamera = FindObjectOfType<Cinemachine.CinemachineVirtualCamera>();
        if (virtualCamera == null)
        {
            Debug.LogError("PlayerHandler.OnEntityInstantiated No virtual camera found in scene. Aborting player setup.");
            return;
        }

        virtualCamera.m_Follow = transform;
    }
}
