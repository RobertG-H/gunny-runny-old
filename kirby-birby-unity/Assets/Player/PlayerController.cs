using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;


namespace Player
{
    public class PlayerController : NetworkBehaviour, Damageable
    {
        [SerializeField] private PlayerPhysics physics;
        [SerializeField] private TransformSyncInterpolate transformSyncInterpolate;
        [SerializeField] private Renderer model;
        [SerializeField] private GameObject playerCamera;

        [ReadOnly]
        public float iHorz;

        #region Gameplay vars
        [SerializeField] float maxHealth;
        [ReadOnly, SerializeField] float currentHealth;
        #endregion

        #region Networking vars
        [Header("Networking")]
        public TextMesh playerNameText;
        public GameObject playerNameObject;
        private Material playerMaterial;

        [SyncVar(hook = nameof(OnNameChanged))]
        public string playerName;

        [SyncVar(hook = nameof(OnColorChanged))]
        public Color playerColor = Color.white;
        #endregion

        #region Debug
        [SerializeField] bool isDebug;
        #endregion

        void Awake()
        {
            playerCamera.SetActive(false);
            currentHealth = maxHealth;
        }

        #region Networking methods
        void OnNameChanged(string _Old, string _New)
        {
            playerNameText.text = playerName;
        }

        void OnColorChanged(Color _Old, Color _New)
        {
            playerNameText.color = _New;
            playerMaterial = new Material(model.material);
            playerMaterial.color = _New;
            model.material = playerMaterial;
        }

        public override void OnStartLocalPlayer()
        {
            string name = "Player" + Random.Range(100, 999);
            Color color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
            CmdSetupPlayer(name, color);
            InitializeCamera();
        }

        [Command]
        public void CmdSetupPlayer(string name, Color color)
        {
            playerName = name;
            playerColor = color;
        }

        [Command]
        void CmdSendInputs(float iHorz)
        {
            this.iHorz = iHorz;
        }

        [Command]
        void CmdBrake()
        {
            physics.Brake();
        }

        [Command]
        void CmdStopBraking()
        {
            physics.StopBraking();
        }
        #endregion


        public void OnHorizontal(InputAction.CallbackContext context)
        {
            if (!isLocalPlayer) return;
            iHorz = context.ReadValue<float>();
            CmdSendInputs(iHorz);
        }
        public void OnCharge(InputAction.CallbackContext context)
        {
            if (!isLocalPlayer) return;
            if (context.started)//Pressed
            {
                CmdBrake();
            }
            else if (context.performed)//Released
            {
                CmdStopBraking();
            }
        }

        private void InitializeCamera()
        {
            playerCamera.SetActive(true);
            playerCamera.transform.parent = null;
        }

        #region Interface methods
        void Damageable.TakeDamage(float amount)
        {
            currentHealth -= amount;
            if (currentHealth <= 0)
            {
                Debug.Log("Dead!");
            }
        }
        #endregion

        #region Public Getters

        public float GetSpeed()
        {
            if (isServer) return physics.GetVelocity().magnitude;
            return transformSyncInterpolate.GetVelocity().magnitude;
        }
        #endregion

        #region Debug
        void OnGUI()
        {
            if (isDebug)
            {
                Rect rectPos = new Rect(10, 10, 100, 20);
                GUI.Label(rectPos, string.Format("Health: {0}", currentHealth));
            }
        }
        #endregion
    }
}