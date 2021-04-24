using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;


namespace Player
{
    public class PlayerController : NetworkBehaviour
    {
        public PlayerPhysics physics;
        [SerializeField]
        private PlayerJuice juice;
        public float iHorz;


        #region Networking vars
        public TextMesh playerNameText;
        public GameObject floatingInfo;
        private Material playerMaterialClone;

        [SyncVar(hook = nameof(OnNameChanged))]
        public string playerName;

        [SyncVar(hook = nameof(OnColorChanged))]
        public Color playerColor = Color.white;
        #endregion

        #region Networking methods
        void OnNameChanged(string _Old, string _New)
        {
            playerNameText.text = playerName;
        }

        void OnColorChanged(Color _Old, Color _New)
        {
            playerNameText.color = _New;
            playerMaterialClone = new Material(GetComponent<Renderer>().material);
            playerMaterialClone.color = _New;
            GetComponent<Renderer>().material = playerMaterialClone;
        }

        public override void OnStartLocalPlayer()
        {
            //Disabled until figure out how to config when model is not on current gameObject

            // floatingInfo.transform.localPosition = new Vector3(0, -0.3f, 0.6f);
            // floatingInfo.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

            // string name = "Player" + Random.Range(100, 999);
            // Color color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
            // CmdSetupPlayer(name, color);
        }

        [Command]
        public void CmdSetupPlayer(string _name, Color _col)
        {
            // player info sent to server, then server updates sync vars which handles it on all clients
            playerName = _name;
            playerColor = _col;
        }
        #endregion

        void Update()
        {
            if (!isLocalPlayer)
            {
                // make non-local players run this
                floatingInfo.transform.LookAt(Camera.main.transform);
                return;
            }
        }

        public void OnHorizontal(InputAction.CallbackContext context)
        {
            if (!isLocalPlayer) { return; }
            iHorz = context.ReadValue<float>();
        }
        public void OnCharge(InputAction.CallbackContext context)
        {
            if (!isLocalPlayer) { return; }

            if (context.started)//Pressed
            {
                physics.Brake();
                if (juice)
                    juice.OnCharge();
            }
            else if (context.performed)//Released
            {
                physics.StopBraking();
                if (juice)
                    juice.OnBoost();
            }
        }

    }
}