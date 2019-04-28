//-----------------------------------------------------------------------
// <copyright file="LocalPlayerController.cs" company="Google">
//
// Copyright 2018 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------

namespace GoogleARCore.Examples.CloudAnchors
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Networking;

    /// <summary>
    /// Local player controller. Handles the spawning of the networked Game Objects.
    /// </summary>
#pragma warning disable 618
    public class LocalPlayerController : NetworkBehaviour
    {
        /// <summary>
        /// The Star model that will represent networked objects in the scene.
        /// </summary>
        public GameObject StarPrefab;

        /// <summary>
        /// The Anchor model that will represent the anchor in the scene.
        /// </summary>
        public GameObject AnchorPrefab;

        [SyncVar(hook = "_OnStarPlaced")]
        public int StarsToPlace = 6;

        public delegate void StarPlaced(int starsToPlace);
        public event StarPlaced OnStarPlaced;

        /// <summary>
        /// The Unity OnStartLocalPlayer() method.
        /// </summary>
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            // A Name is provided to the Game Object so it can be found by other Scripts, since this
            // is instantiated as a prefab in the scene.
            gameObject.name = "LocalPlayer";
        }

        /// <summary>
        /// Will spawn the origin anchor and host the Cloud Anchor. Must be called by the host.
        /// </summary>
        /// <param name="position">Position of the object to be instantiated.</param>
        /// <param name="rotation">Rotation of the object to be instantiated.</param>
        /// <param name="anchor">The ARCore Anchor to be hosted.</param>
        public void SpawnAnchor(Vector3 position, Quaternion rotation, Component anchor)
        {
            // Instantiate Anchor model at the hit pose.
            var anchorObject = Instantiate(AnchorPrefab, position, rotation);

            // Anchor must be hosted in the device.
            anchorObject.GetComponent<AnchorController>().HostLastPlacedAnchor(anchor);

            // Host can spawn directly without using a Command because the server is running in this
            // instance.
            NetworkServer.Spawn(anchorObject);
        }

        /// <summary>
        /// A command run on the server that will spawn the Star prefab in all clients.
        /// </summary>
        /// <param name="position">Position of the object to be instantiated.</param>
        /// <param name="rotation">Rotation of the object to be instantiated.</param>
        [Command]
        public void CmdSpawnStar(Vector3 position, Quaternion rotation)
        {
            if (_IsInPlacementMode() && StarsToPlace > 0)
            {
                Debug.Log("Server: CmdSpawnStar");
                // Instantiate Star model at the hit pose.
                var starObject = Instantiate(StarPrefab, position, rotation);
                starObject.GetComponent<Interactable>().SetOwnerNetId(netId);

                // Spawn the object in all clients.
                NetworkServer.Spawn(starObject);
                StarsToPlace--;
                Debug.LogFormat("Server: {0} stars left to place for id {1}", StarsToPlace, netId);

                if (OnStarPlaced != null)
                {
                    OnStarPlaced(StarsToPlace);
                }

                _CheckIfAllStarsPlaced();
            }
        }

        [Command]
        public void CmdCollectStar(NetworkInstanceId objectNetId)
        {
            if (_IsInPlayingMode())
            {
                Debug.Log("Server: CmdCollectStar");
                var gameObject = NetworkServer.FindLocalObject(objectNetId);
                if (gameObject == null)
                {
                    Debug.LogError("Could not find GameObject from netId: " + objectNetId);
                    return;
                }

                var interactable = gameObject.GetComponent<Interactable>();
                if (interactable.GetOwnerNetId() == netId)
                {
                    Debug.Log("Cannot collect your star");
                    return;
                }

                var playerController = NetworkServer.FindLocalObject(interactable.GetOwnerNetId());
                if (playerController)
                {
                    var healthComponent = playerController.GetComponent<HealthComponent>();
                    healthComponent.DecrementHealth();

                    if (healthComponent.GetCurrentHealth() == 0)
                    {
                        _CheckIfOnlyOnePlayerLeft();
                    }
                }
                else
                {
                    Debug.LogError("No player controller found for interactable");
                }

                NetworkServer.Destroy(gameObject);
            }
            else
            {
                Debug.Log("Cannot collect stars while not in playing mode");
            }
        }

        private void _OnStarPlaced(int starsLeftToPlace)
        {
            StarsToPlace = starsLeftToPlace;

            if (OnStarPlaced != null)
            {
                OnStarPlaced(StarsToPlace);
            }
        }

        private bool _IsInPlacementMode()
        {
            var gameState = FindObjectOfType<GameState>();
            if (gameState)
            {
                return gameState.GetGameMode() == GameState.GameMode.Placement;
            }
            return false;
        }

        private bool _IsInPlayingMode()
        {
            var gameState = FindObjectOfType<GameState>();
            if (gameState)
            {
                return gameState.GetGameMode() == GameState.GameMode.Playing;
            }
            return false;
        }

        private bool _AreAllStarsPlaced()
        {
            var playerControllers = FindObjectsOfType<LocalPlayerController>();
            foreach (var playerController in playerControllers)
            {
                if (playerController.StarsToPlace > 0)
                {
                    return false;
                }
            }
            return true;
        }

        private void _CheckIfAllStarsPlaced()
        {
            if (_AreAllStarsPlaced())
            {
                var gameState = FindObjectOfType<GameState>();
                if (gameState)
                {
                    Debug.Log("All stars placed!");
                    gameState.SetGameMode(GameState.GameMode.Playing);
                }
                else
                {
                    Debug.LogError("Cannot find GameState");
                }
            }
        }

        private bool _IsOnlyOnePlayerLeftAlive()
        {
            int playerAliveCount = 0;
            var playerControllers = FindObjectsOfType<LocalPlayerController>();
            foreach (var playerController in playerControllers)
            {
                if (playerController.GetComponent<HealthComponent>().GetCurrentHealth() > 0)
                {
                    playerAliveCount++;
                }
            }

            return playerAliveCount == 1;
        }

        private void _CheckIfOnlyOnePlayerLeft()
        {
            if (_IsOnlyOnePlayerLeftAlive())
            {
                var gameState = FindObjectOfType<GameState>();
                if (gameState)
                {
                    gameState.SetGameMode(GameState.GameMode.PostGame);
                }
                else
                {
                    Debug.LogError("Cannot find GameState");
                }
            }
        }
    }
#pragma warning restore 618
}
