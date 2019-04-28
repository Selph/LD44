﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

#pragma warning disable 618
public class GameState : NetworkBehaviour
{
    public enum GameMode
    {
        Placement,
        Playing,
        PostGame
    }

    public delegate void GameModeChanged(GameMode mode);
    public event GameModeChanged OnGameModeChanged;

    [SyncVar(hook = "SetGameMode")]
    private GameMode _currentGameMode = GameMode.Placement;

    public GameMode GetGameMode() { return _currentGameMode; }
    public void SetGameMode(GameMode mode)
    {
        if (mode != _currentGameMode)
        {
            _currentGameMode = mode;
            OnGameModeChanged(_currentGameMode);
        }
    }
}
#pragma warning disable 618
