using System.Collections.Generic;
using Netick;
using Netick.Unity;
using UnityEngine;

public class GameMatching : NetworkEventsListener
{
    [SerializeField] private Spawner _playerSpawner;
    private readonly Dictionary<int, NetworkConnection> _networkConnections = new();

    public override void OnClientConnected(NetworkSandbox sandbox, NetworkConnection player)
    {
        base.OnClientConnected(sandbox, player);
        _networkConnections[player.PlayerId] = player;
        SpawnBot();
    }

    private void SpawnBot()
    {
        _playerSpawner.SpawnBot();
    }

    public override void OnClientDisconnected(NetworkSandbox sandbox, NetworkConnection client,
        TransportDisconnectReason _)
        => _networkConnections.Remove(client.PlayerId);
    
    public void OnPlayerGetPlayerInfo(int playerId, int demoId)
    {
        if (!_networkConnections.TryGetValue(playerId, out var client))
            return;
        _playerSpawner.SpawnPlayer(client);
    }
}