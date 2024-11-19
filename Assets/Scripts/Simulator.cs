using System;
using System.Threading.Tasks;
using Core.Singleton;
using Cysharp.Threading.Tasks;
using SuperMaxim.Messaging;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Simulator : PersistentSingleton<Simulator>
{
    private bool _isSimulating;
    private bool _isBattleInProgress;
    private bool _isBattleComplete;
    private int _currentIndex;

    private void OnEnable()
    {
        Messenger.Default.Subscribe<GameOverPayload>(OnGameOver);
    }

    private void OnDisable()
    {
        Messenger.Default.Unsubscribe<GameOverPayload>(OnGameOver);
    }

    private void OnGameOver(GameOverPayload payload)
    {
        if (payload.BattleIndex == _currentIndex)
        {
            _isBattleComplete = true;
        }
    }

    private async UniTaskVoid SimulateNextBattle()
    {
        _isBattleInProgress = true;
        _isBattleComplete = false;
        
        SceneManager.LoadScene(SceneName.HostScene);
        
        await UniTask.NextFrame();
        
        var gameControllers = GameObject.FindObjectsOfType<GameController>();
        GameController gameController = null;
        var firstScene = SceneManager.GetSceneAt(0);
        
        for (int i = 0; i < gameControllers.Length; i++)
        {
            if (gameControllers[i].gameObject.scene == firstScene)
            {
                gameController = gameControllers[i];
                break;
            }
        }
        
        if (gameController != null)
        {
            gameController.SetBattleIndex(_currentIndex);
        }
        else
        {
            Debug.LogError("GameController not found in first scene!");
        }
        
        await UniTask.WaitUntil(() => IsBattleComplete());
        
        await CleanBattle();

        _currentIndex++;
        _isBattleInProgress = false;
        _isSimulating = true;
    }

    private bool IsBattleComplete()
    {
        return _isBattleComplete;
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