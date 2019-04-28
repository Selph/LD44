//-----------------------------------------------------------------------
// <copyright file="CloudAnchorsExampleController.cs" company="Google">
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
    using GoogleARCore;
    using System;
    using UnityEngine;
    using UnityEngine.Networking;

    /// <summary>
    /// Controller for the Cloud Anchors Example. Handles the ARCore lifecycle.
    /// </summary>
    public class CloudAnchorsExampleController : MonoBehaviour
    {
        [Header("GAME JAM")]

        public float maxDistanceFromAnchor = 4;
        public GameObject limitPrefab;

        public HeartsBar heartsBar;

        [Header("ARCore")]

        /// <summary>
        /// The UI Controller.
        /// </summary>
        public NetworkManagerUIController UIController;

        /// <summary>
        /// The root for ARCore-specific GameObjects in the scene.
        /// </summary>
        public GameObject ARCoreRoot;

        /// <summary>
        /// The helper that will calculate the World Origin offset when performing a raycast or
        /// generating planes.
        /// </summary>
        public ARCoreWorldOriginHelper ARCoreWorldOriginHelper;

        [Header("ARKit")]

        /// <summary>
        /// The root for ARKit-specific GameObjects in the scene.
        /// </summary>
        public GameObject ARKitRoot;

        /// <summary>
        /// The first-person camera used to render the AR background texture for ARKit.
        /// </summary>
        public Camera ARKitFirstPersonCamera;

        /// <summary>
        /// A helper object to ARKit functionality.
        /// </summary>
        private ARKitHelper m_ARKit = new ARKitHelper();

        /// <summary>
        /// Indicates whether the Origin of the new World Coordinate System, i.e. the Cloud Anchor,
        /// was placed.
        /// </summary>
        private bool m_IsOriginPlaced = false;

        /// <summary>
        /// Indicates whether the Anchor was already instantiated.
        /// </summary>
        private bool m_AnchorAlreadyInstantiated = false;

        /// <summary>
        /// Indicates whether the Cloud Anchor finished hosting.
        /// </summary>
        private bool m_AnchorFinishedHosting = false;

        /// <summary>
        /// True if the app is in the process of quitting due to an ARCore connection error,
        /// otherwise false.
        /// </summary>
        private bool m_IsQuitting = false;

        /// <summary>
        /// The anchor component that defines the shared world origin.
        /// </summary>
        private Component m_WorldOriginAnchor = null;
        [NonSerialized]
        public Collider m_AnchorCollider;

        /// <summary>
        /// The last pose of the hit point from AR hit test.
        /// </summary>
        private Pose? m_LastHitPose = null;

        /// <summary>
        /// The current cloud anchor mode.
        /// </summary>
        private ApplicationMode m_CurrentMode = ApplicationMode.Ready;

        /// <summary>
        /// The Network Manager.
        /// </summary>
#pragma warning disable 618
        private NetworkManager m_NetworkManager;
#pragma warning restore 618

        private bool m_MatchStarted = false;

        private bool m_AreEventsBound = false;

        /// <summary>
        /// Enumerates modes the example application can be in.
        /// </summary>
        public enum ApplicationMode
        {
            Ready,
            Hosting,
            Resolving,
        }

        /// <summary>
        /// The Unity Start() method.
        /// </summary>
        public void Start()
        {
#pragma warning disable 618
            m_NetworkManager = UIController.GetComponent<NetworkManager>();
#pragma warning restore 618

            // A Name is provided to the Game Object so it can be found by other Scripts
            // instantiated as prefabs in the scene.
            gameObject.name = "CloudAnchorsExampleController";
            ARCoreRoot.SetActive(false);
            ARKitRoot.SetActive(false);
            _ResetStatus();

        }

        /// <summary>
        /// The Unity Update() method.
        /// </summary>
        public void Update()
        {
            _UpdateApplicationLifecycle();

            // If we are neither in hosting nor resolving mode then the update is complete.
            if (m_CurrentMode != ApplicationMode.Hosting &&
                m_CurrentMode != ApplicationMode.Resolving)
            {
                return;
            }

            // If the origin anchor has not been placed yet, then update in resolving mode is
            // complete.
            if (m_CurrentMode == ApplicationMode.Resolving && !m_IsOriginPlaced)
            {
                return;
            }
            
            _BindEvents();

#if UNITY_EDITOR
            if (!Input.GetMouseButtonDown(0))
                return;

            var position = Input.mousePosition;
#else
            // If the player has not touched the screen, we are done with this update.
            Touch touch;
            if (Input.touchCount < 1 || (touch = Input.GetTouch(0)).phase != TouchPhase.Began)
            {
                return;
            }

            var position = touch.position;
#endif

            TrackableHit arcoreHitResult = new TrackableHit();
            m_LastHitPose = null;

            // Raycast against the location the player touched to search for planes.
            if (Application.platform != RuntimePlatform.IPhonePlayer)
            {
                if (ARCoreWorldOriginHelper.Raycast(position.x, position.y,
                        TrackableHitFlags.PlaneWithinPolygon, out arcoreHitResult))
                {
                    m_LastHitPose = arcoreHitResult.Pose;
                }
            }
            else
            {
                Pose hitPose;
                if (m_ARKit.RaycastPlane(
                    ARKitFirstPersonCamera, position.x, position.y, out hitPose))
                {
                    m_LastHitPose = hitPose;
                }
            }

            if(_CanCollectStars() && _TrySelectStart())
            {
                return;
            }

            // If there was an anchor placed, then instantiate the corresponding object.
            if (m_LastHitPose != null)
            {
                // The first touch on the Hosting mode will instantiate the origin anchor. Any
                // subsequent touch will instantiate a star, both in Hosting and Resolving modes.
                if (_CanPlaceStars(m_LastHitPose.Value))
                {
                    _InstantiateStar();
                }
                else if (!m_IsOriginPlaced && m_CurrentMode == ApplicationMode.Hosting)
                {
                    if (Application.platform != RuntimePlatform.IPhonePlayer)
                    {
                        m_WorldOriginAnchor =
                            arcoreHitResult.Trackable.CreateAnchor(arcoreHitResult.Pose);
                    }
                    else
                    {
                        m_WorldOriginAnchor = m_ARKit.CreateAnchor(m_LastHitPose.Value);
                    }

                    SetWorldOrigin(m_WorldOriginAnchor.transform);
                    _InstantiateAnchor();
                    OnAnchorInstantiated(true);
                }
            }
        }

        /// <summary>
        /// Sets the apparent world origin so that the Origin of Unity's World Coordinate System
        /// coincides with the Anchor. This function needs to be called once the Cloud Anchor is
        /// either hosted or resolved.
        /// </summary>
        /// <param name="anchorTransform">Transform of the Cloud Anchor.</param>
        public void SetWorldOrigin(Transform anchorTransform)
        {
            if (m_IsOriginPlaced)
            {
                Debug.LogWarning("The World Origin can be set only once.");
                return;
            }

            m_IsOriginPlaced = true;

            if (Application.platform != RuntimePlatform.IPhonePlayer)
            {
                ARCoreWorldOriginHelper.SetWorldOrigin(anchorTransform);
            }
            else
            {
                m_ARKit.SetWorldOrigin(anchorTransform);
            }
        }

        /// <summary>
        /// Handles user intent to enter a mode where they can place an anchor to host or to exit
        /// this mode if already in it.
        /// </summary>
        public void OnEnterHostingModeClick()
        {
            if (m_CurrentMode == ApplicationMode.Hosting)
            {
                m_CurrentMode = ApplicationMode.Ready;
                _ResetStatus();
                return;
            }

            m_CurrentMode = ApplicationMode.Hosting;
            _SetPlatformActive();
        }

        /// <summary>
        /// Handles a user intent to enter a mode where they can input an anchor to be resolved or
        /// exit this mode if already in it.
        /// </summary>
        public void OnEnterResolvingModeClick()
        {
            if (m_CurrentMode == ApplicationMode.Resolving)
            {
                m_CurrentMode = ApplicationMode.Ready;
                _ResetStatus();
                return;
            }

            m_CurrentMode = ApplicationMode.Resolving;
            _SetPlatformActive();
        }

        /// <summary>
        /// Callback indicating that the Cloud Anchor was instantiated and the host request was
        /// made.
        /// </summary>
        /// <param name="isHost">Indicates whether this player is the host.</param>
        public void OnAnchorInstantiated(bool isHost)
        {
            if (m_AnchorAlreadyInstantiated)
            {
                return;
            }

            m_AnchorAlreadyInstantiated = true;
            UIController.OnAnchorInstantiated(isHost);
        }

        /// <summary>
        /// Callback indicating that the Cloud Anchor was hosted.
        /// </summary>
        /// <param name="success">If set to <c>true</c> indicates the Cloud Anchor was hosted
        /// successfully.</param>
        /// <param name="response">The response string received.</param>
        public void OnAnchorHosted(bool success, string response)
        {
            m_AnchorFinishedHosting = success;
            UIController.OnAnchorHosted(success, response);
        }

        /// <summary>
        /// Callback indicating that the Cloud Anchor was resolved.
        /// </summary>
        /// <param name="success">If set to <c>true</c> indicates the Cloud Anchor was resolved
        /// successfully.</param>
        /// <param name="response">The response string received.</param>
        public void OnAnchorResolved(bool success, string response)
        {
            UIController.OnAnchorResolved(success, response);
        }

        private LocalPlayerController GetLocalPlayerController()
        {
            GameObject localPlayer = GameObject.Find("LocalPlayer");
            return localPlayer ? localPlayer.GetComponent<LocalPlayerController>() : null;
        }
        
        private void _BindEvents()
        {
            if (!m_AreEventsBound)
            {
                // Bind health changes
                var localPlayerController = GetLocalPlayerController();
                var gameState = FindObjectOfType<GameState>();
                if (localPlayerController && gameState)
                {
                    localPlayerController.OnStarPlaced += _OnStarPlaced;

                    var healthComponent = localPlayerController.GetComponent<HealthComponent>();
                    healthComponent.OnHealthChanged += _OnHealthChanged;
                    heartsBar.total = healthComponent.MaxHealth;
                    heartsBar.current = healthComponent.GetCurrentHealth();
                    m_AreEventsBound = true;

                    gameState.OnGameModeChanged += _OnGameModeChanged;
                    Debug.Log("Events bound.");
                }
            }
        }

        private void _OnStarPlaced(int starsToPlace)
        {
            if (starsToPlace == 0)
            {
                UIController.SnackbarText.text = "All bits placed. Waiting for other players to be done.";
                Debug.Log("All local stars placed");
            }
            else
            {
                UIController.SnackbarText.text = string.Format("Tap to place and hide your life bits, {0} left to place.", starsToPlace);
            }
        }

        private void _OnHealthChanged(int health)
        {
            heartsBar.current = health;

            if (health == 0)
            {
                UIController.SnackbarText.text = "You lost! All your bits were found.";
            }
        }

        private void _OnGameModeChanged(GameState.GameMode mode)
        {
            switch (mode)
            {
                case GameState.GameMode.Playing:
                    UIController.SnackbarText.text = "Find bits of other players!";
                    break;
                case GameState.GameMode.PostGame:
                    if (heartsBar.current > 0)
                        UIController.SnackbarText.text = "You WIN !!!";
                    break;
            }
        }

        /// <summary>
        /// Instantiates the anchor object at the pose of the m_LastPlacedAnchor Anchor. This will
        /// host the Cloud Anchor.
        /// </summary>
        private void _InstantiateAnchor()
        {
            // The anchor will be spawned by the host, so no networking Command is needed.
            GetLocalPlayerController().SpawnAnchor(Vector3.zero, Quaternion.identity, m_WorldOriginAnchor);
        }

        /// <summary>
        /// Instantiates a star object that will be synchronized over the network to other clients.
        /// </summary>
        private void _InstantiateStar()
        {
            Debug.Log("_InstantiateStar");
            // Star must be spawned in the server so a networking Command is used.
            GetLocalPlayerController().CmdSpawnStar(m_LastHitPose.Value.position, m_LastHitPose.Value.rotation);
        }

        private bool _TrySelectStart()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                Debug.LogError("No camera");
                return false;
            }

            if (m_LastHitPose == null)
            {
                StarComponent star;
                if (Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out var hitInfo, camera.farClipPlane) && (star = hitInfo.collider.GetComponent<StarComponent>()))
                {
                    GetLocalPlayerController().CmdCollectStar(hitInfo.collider.GetComponent<Interactable>().netId);
                    return true;
                }

                return false;
            }

            Vector3 cameraLocation = camera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, camera.nearClipPlane));
            //Debug.DrawLine(cameraLocation, m_LastHitPose.Value.position, Color.red, 10.0f);

            var objects = FindObjectsOfType<Interactable>();
            foreach(var obj in objects)
            {
                Vector3 projection = FindNearestPointOnLine(cameraLocation, m_LastHitPose.Value.position, obj.transform.position);
                float distance = Vector3.Distance(obj.transform.position, projection);

                //Debug.DrawLine(projection, obj.transform.position, Color.blue, 10.0f);

                var interactable = obj.GetComponent<Interactable>();
                var netIdentity = obj.GetComponent<NetworkIdentity>();
                if (distance < interactable.Radius)
                {
                    Debug.Log("Found with distance: " + distance);
                    GetLocalPlayerController().CmdCollectStar(netIdentity.netId);
                    return true;
                }
            }

            return false;
        }

        private Vector3 FindNearestPointOnLine(Vector3 origin, Vector3 end, Vector3 point)
        {
            //Get heading
            Vector3 heading = (end - origin);
            float magnitudeMax = heading.magnitude;
            heading.Normalize();

            //Do projection from the point but clamp it
            Vector3 lhs = point - origin;
            float dotP = Vector3.Dot(lhs, heading);
            dotP = Mathf.Clamp(dotP, 0f, magnitudeMax);
            return origin + heading * dotP;
        }

        /// <summary>
        /// Sets the corresponding platform active.
        /// </summary>
        private void _SetPlatformActive()
        {
            if (Application.platform != RuntimePlatform.IPhonePlayer)
            {
                ARCoreRoot.SetActive(true);
                ARKitRoot.SetActive(false);
            }
            else
            {
                ARCoreRoot.SetActive(false);
                ARKitRoot.SetActive(true);
            }
        }

        bool IsWithin(Collider c, Vector3 point)
        {
            Vector3 closest = c.ClosestPoint(point);
            Vector3 origin = c.bounds.center; //.transform.position + (c.transform.rotation * c.bounds.center);
            Vector3 originToContact = closest - origin;
            Vector3 pointToContact = closest - point;

            // Here we make the magic, originToContact points from the center to the closest point. So if the angle between it and the pointToContact is less than 90, pointToContact is also looking from the inside-out.
            // The angle will probably be 180 or 0, but it's bad to compare exact floats and the rigidbody centerOfMass calculation could add some potential wiggle to the angle, so we use "< 90" to account for any of that.
            return (Vector3.Angle(originToContact, pointToContact) < 90);
        }

        /// <summary>
        /// Indicates whether a star can be placed.
        /// </summary>
        /// <returns><c>true</c>, if stars can be placed, <c>false</c> otherwise.</returns>
        private bool _CanPlaceStars(Pose lastHitPose)
        {
            if (m_IsOriginPlaced && m_AnchorCollider && !IsWithin(m_AnchorCollider, lastHitPose.position))
            {
                return false;
            }

            if (m_CurrentMode == ApplicationMode.Resolving)
            {
                return m_IsOriginPlaced;
            }

            if (m_CurrentMode == ApplicationMode.Hosting)
            {
                return m_IsOriginPlaced && m_AnchorFinishedHosting;
            }

            return false;
        }

        private bool _CanCollectStars()
        {
            if (m_CurrentMode == ApplicationMode.Resolving)
            {
                return m_IsOriginPlaced;
            }

            if (m_CurrentMode == ApplicationMode.Hosting)
            {
                return m_IsOriginPlaced && m_AnchorFinishedHosting;
            }

            return false;
        }

        /// <summary>
        /// Resets the internal status.
        /// </summary>
        private void _ResetStatus()
        {
            // Reset internal status.
            m_CurrentMode = ApplicationMode.Ready;
            if (m_WorldOriginAnchor != null)
            {
                Destroy(m_WorldOriginAnchor.gameObject);
            }

            m_WorldOriginAnchor = null;
        }

        /// <summary>
        /// Check and update the application lifecycle.
        /// </summary>
        private void _UpdateApplicationLifecycle()
        {
            if (!m_MatchStarted && m_NetworkManager.IsClientConnected())
            {
                m_MatchStarted = true;
            }

            // Exit the app when the 'back' button is pressed.
            if (Input.GetKey(KeyCode.Escape))
            {
                Application.Quit();
            }

            var sleepTimeout = SleepTimeout.NeverSleep;

#if !UNITY_IOS
            // Only allow the screen to sleep when not tracking.
            if (Session.Status != SessionStatus.Tracking)
            {
                const int lostTrackingSleepTimeout = 15;
                sleepTimeout = lostTrackingSleepTimeout;
            }
#endif

            Screen.sleepTimeout = sleepTimeout;

            if (m_IsQuitting)
            {
                return;
            }

            // Quit if ARCore was unable to connect and give Unity some time for the toast to
            // appear.
            if (Session.Status == SessionStatus.ErrorPermissionNotGranted)
            {
                UIController.ShowErrorMessage(
                    "Camera permission is needed to run this application.");
                m_IsQuitting = true;
                Invoke("_DoQuit", 5.0f);
            }
            else if (Session.Status.IsError())
            {
                UIController.ShowErrorMessage(
                    "ARCore encountered a problem connecting.  Please start the app again.");
                m_IsQuitting = true;
                Invoke("_DoQuit", 5.0f);
            }
            else if (m_MatchStarted && !m_NetworkManager.IsClientConnected())
            {
                UIController.ShowErrorMessage(
                    "Network session disconnected!  Please start the app again.");
                m_IsQuitting = true;
                Invoke("_DoQuit", 5.0f);
            }
        }

        /// <summary>
        /// Actually quit the application.
        /// </summary>
        private void _DoQuit()
        {
            Application.Quit();
        }
    }
}
