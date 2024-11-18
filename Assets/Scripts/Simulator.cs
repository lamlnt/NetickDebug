using System;
using System.Threading.Tasks;
using Core.Singleton;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Simulator : PersistentSingleton<Simulator>
{
    [SerializeField] private int _loopBattleCount = 10;
    private bool _isSimulating;
    private bool _isBattleInProgress;
    private int _currentIndex;

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 150, 30), "Start Simulator"))
        {
            StartSimulation();
        }
    }

    private void Update()
    {
        if (!_isSimulating) return;
        if (_currentIndex >= _loopBattleCount) return;
        if (_isBattleInProgress) return;  // Don't start new battle if current one is running
        
        _isSimulating = false;
        SimulateNextBattle().Forget();
    }

    private async UniTaskVoid SimulateNextBattle()
    {
        _isBattleInProgress = true;
        
        SceneManager.LoadScene(SceneName.HostScene);
        
        // Wait for battle to complete - you need to set this flag when battle ends
        await UniTask.WaitUntil(() => IsBattleComplete());
        
        await CleanBattle();
    
        _currentIndex++;
        _isBattleInProgress = false;
        _isSimulating = true;
    }
    
    private bool IsBattleComplete()
    {
        // Add your battle completion check logic here
        // For example:
        // return GameManager.Instance.IsBattleEnded;
        return false; // Placeholder
    }

    public void StartSimulation()
    {
        _currentIndex = 0;
        _isSimulating = true;
        _isBattleInProgress = false;
    }

    private async UniTask CleanBattle()
    {
        SceneManager.LoadScene(SceneName.EmptyScene);
        
        if (!Netick.Unity.Network.IsRunning) return;
            
        Netick.Unity.Network.ShutdownImmediately();
        await UniTask.WaitUntil(() => !Netick.Unity.Network.IsRunning);
        await UniTask.DelayFrame(3);
        Debug.LogError("Netick shutdown success");
    }
}
    

public static class SceneName
{
    public const string EmptyScene = "EmptyScene";
    public const string HostScene = "HostScene";
}

/// sample flow
/// Simulator Init - Load Empty Scene
/// Iteration :
//// Simulator load Sample Scene Host
//// GameController Fake Play => Publish Event Battle End
//// Simulator CleanUp Scene 