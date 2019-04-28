
using GoogleARCore.Examples.CloudAnchors;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class StarComponent : MonoBehaviour
{
    LocalPlayerController localPlayer;
    Interactable interactable;
    GameState gameState;
    Renderer renderer;

    bool IsLocalStar
    {
        get
        {
            return localPlayer.netId.Value == interactable.GetOwnerNetId().Value;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        localPlayer = GameObject.Find("LocalPlayer").GetComponent<LocalPlayerController>();

        interactable = GetComponent<Interactable>();

        gameState = FindObjectOfType<GameState>();

        var arCoreMesh = transform.Find("ARCoreMesh").gameObject;
        renderer = arCoreMesh.GetComponent<Renderer>();

        gameState.OnGameModeChanged += RefreshState;
        RefreshState(gameState.GetGameMode());

        _ChangeMaterial();
    }

    private void RefreshState(GameState.GameMode mode)
    {
        switch(mode)
        {
            case GameState.GameMode.Placement:
                gameObject.SetActive(IsLocalStar);
                break;
            case GameState.GameMode.Playing:
                gameObject.SetActive(!IsLocalStar);
                break;
            case GameState.GameMode.PostGame:
                gameObject.SetActive(true);
                break;
        }
    }

    private static Dictionary<uint, Material> _sharedMaterialsCache = new Dictionary<uint, Material>();

    private void _ChangeMaterial()
    {
        uint localId = localPlayer.netId.Value;
        uint starOwnerId = interactable.GetOwnerNetId().Value;

        if (localId == starOwnerId)
        {
            _sharedMaterialsCache[starOwnerId] = renderer.sharedMaterial;
            return;
        }

        if (!_sharedMaterialsCache.TryGetValue(starOwnerId, out var material))
        {
            material = new Material(Shader.Find("Legacy Shaders/Diffuse"));

            Color color = new Color(
                ((Mathf.Min(starOwnerId % 3, 1) * (100 * starOwnerId)) % 255) / 255f,
                ((Mathf.Min((starOwnerId + 1) % 3, 1) * (200 * starOwnerId)) % 255) / 255f,
                ((Mathf.Min((starOwnerId + 2) % 3, 1) * (100 * starOwnerId)) % 255) / 255f
                );

            material.color = color;
        }

        _sharedMaterialsCache[starOwnerId] = material;
        renderer.sharedMaterial = material;
    }

    private void OnDestroy()
    {
        gameState.OnGameModeChanged -= RefreshState;
    }
}
