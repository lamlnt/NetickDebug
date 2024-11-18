using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Cysharp.Threading.Tasks;
using Netick;
using Netick.Unity;
using UnityEngine;
using NetworkPlayer = Netick.NetworkPlayer;

public class GameController : NetworkBehaviour
{
    protected readonly List<Character> Players = new(2);
    [SerializeField] private float _timeReadyToStart;
    [SerializeField] private float _timeFakePlay;
    [SerializeField] private float _timeToDispose;
    [Networked] protected GameState CurGameState { get; set; }
    [Networked] protected float CurGameTime { get; set; }
    public enum GameState
    {
        None = 0,
        Ready = 1,
        Play = 2,
        Dispose = 3,
    }
    
    public override void NetworkStart() => Sandbox.Events.OnPlayerConnected += OnPlayerConnected;

    public override void NetworkDestroy() => Sandbox.Events.OnPlayerConnected -= OnPlayerConnected;
    
    private void OnPlayerConnected(NetworkSandbox sandbox, NetworkPlayer player)
    {
        if (CurGameState >= GameState.Ready)
            return;
        CurGameState = GameState.Ready;
        CurGameTime = _timeReadyToStart;
    }
    
    public void RegisterPlayer(Character character)
    {
        Players.Add(character);
    }

    public override void NetworkFixedUpdate()
    {
        base.NetworkFixedUpdate();
        
        if (CurGameState == GameState.None) return;
        
        CurGameTime -= Sandbox.FixedDeltaTime;
        
        if (CurGameTime > 0) return;
        
        if (CurGameState == GameState.Play)
        {
            CurGameState = GameState.Dispose;
            CurGameTime = _timeToDispose;
            return;
        }

        if (CurGameState == GameState.Dispose)
        {
            CleanController();
            CurGameState = GameState.None;
            return;
        }

        if (CurGameState == GameState.Ready)
        {
            CurGameState = GameState.Play;
            CurGameTime = _timeFakePlay;
            return;
        }
        
    }

    private void CleanController()
    {
        if (!IsServer)
            return;
        Debug.Log($"Server {Sandbox.Engine.Port.ToString()} - CleanController");
        foreach (var sNetworkObject in Sandbox.Objects.Values)
            if (sNetworkObject != Object && !IsCharacterObject(sNetworkObject))
                Sandbox.Destroy(sNetworkObject);
        DeSpawnPlayer().Forget();
    }
    
    protected bool IsCharacterObject(NetworkObject networkObject)
    {
        foreach (var player in Players)
            if (player.Object == networkObject)
                return true;
        return false;
    }
    
    private async UniTask DeSpawnPlayer()
    {
        foreach (var player in Players)
        {
            if (player.Object is not null)
            {
                Debug.Log($"{Sandbox.Engine.Port} --- Destroy: {player.Object.name} ID: {player.Object.GetInstanceID().ToString()}");
                Sandbox.Destroy(player.Object);
            }
            await UniTask.WaitForSeconds(1);
        }
        foreach (var client in Sandbox.ConnectedClients)
            Sandbox.Kick(client);
        Debug.Log($"Server {Sandbox.Engine.Port.ToString()} - ServerMatchClearPayload public");

        
        foreach (var sNetworkObject in Sandbox.Objects.Values)
            if (!sNetworkObject.IsSceneObject)
            {
                Debug.Log($"{Sandbox.Engine.Port} --- Miss DepSpawn {sNetworkObject.name} IsCharacterObject: { IsCharacterObject (sNetworkObject)} ID: {sNetworkObject.GetInstanceID().ToString()}");
                Sandbox.Destroy(sNetworkObject);
            }
        Players.Clear();
    }
}