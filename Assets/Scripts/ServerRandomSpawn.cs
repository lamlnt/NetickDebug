using Netick.Unity;
using UnityEngine;

public class ServerRandomSpawn : NetworkBehaviour
{
    [SerializeField] GameObject _prefab;
    [SerializeField] int _count;
    
    public override void NetworkStart()
    {
        base.NetworkStart();
        
        if (!IsServer) return;
        
        for (var i = 0; i < _count; i++)
        {
            Sandbox.NetworkInstantiate(_prefab, Vector3.zero, Quaternion.identity);
        }
    }
}