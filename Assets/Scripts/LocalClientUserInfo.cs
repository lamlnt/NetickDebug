using Netick;
using Netick.Unity;
using UnityEngine;

public class LocalClientUserInfo : NetworkBehaviour
{
    [SerializeField] private GameMatching _gameMatching;
    [SerializeField] int _id;
    
    public override void NetworkStart()
    {
        if (!Sandbox.IsPlayer)
            return;
        RpcMatchInfoCustom(_id);
    }


    [Rpc(isReliable: true, target: RpcPeers.Owner)]
    private void RpcMatchInfoCustom(int matchInfo)
    {
        if (!IsServer)
            return;
        _gameMatching.OnPlayerGetPlayerInfo(Engine.CurrentRpcCaller.PlayerId, matchInfo);
    }
}