using System.Collections.Generic;
using Netick;
using Netick.Unity;
using UnityEngine;

public class Spawner : NetworkEventsListener
{
    [SerializeField] GameController _gameController;
    [SerializeField] private NetworkObject _playerPrefab;
    private readonly Dictionary<NetworkConnection,Character> _dictionary = new();
    public override void OnStartup(NetworkSandbox sandbox) => Sandbox.InitializePool(_playerPrefab.gameObject, 2);

    public void SpawnPlayer(NetworkConnection client)
    {
        Debug.Log($"SpawnPlayer");
        var player = Sandbox.NetworkInstantiate(_playerPrefab.gameObject, Vector3.zero, Quaternion.identity, client);
        client.PlayerObject = player.gameObject;
        if (!player.TryGetComponent<Character>(out var character)) return;
        _dictionary.Add(client, character);
        _gameController.RegisterPlayer(character);
    }
    
    public void SpawnBot()
    {
        Debug.Log($"SpawnBot");
        var player = Sandbox.NetworkInstantiate(_playerPrefab.gameObject, Vector3.zero, Quaternion.identity);
        if (!player.TryGetComponent<Character>(out var character)) return;
        _gameController.RegisterPlayer(character);
    }

    public override void OnClientDisconnected(NetworkSandbox sandbox, NetworkConnection client,
        TransportDisconnectReason transportDisconnectReason)
    {
        _dictionary.Remove(client);
        foreach (var o in sandbox.Objects)
            if (!o.Value.IsSceneObject)
                Sandbox.Destroy(o.Value);
    } 
}


