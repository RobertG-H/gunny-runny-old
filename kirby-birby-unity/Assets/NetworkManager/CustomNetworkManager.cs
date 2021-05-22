using UnityEngine;
using Mirror;

public class CustomNetworkManager : NetworkManager
{
    private static CustomNetworkManager instance;
    public static CustomNetworkManager Instance
    {
        get
        {
            return instance;
        }
    }
    public static event System.Action<CustomNetworkManager> OnServerStarted = delegate { };
    public override void Awake()
    {
        base.Awake();
        if (instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
    }
    public override void OnStartServer()
    {
        base.OnStartServer();
        OnServerStarted(this);
    }

}