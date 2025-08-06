using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public PlayerStats player1Stats;
    public PlayerStats player2Stats;

    List<IEndGameObserver> endGameObservers = new List<IEndGameObserver>();

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(this);
    }
    public void RigisterPlayer(PlayerStats player,PlayerID playerId)
    {
        if (playerId == PlayerID.Player1)
        {
            Debug.Log("Íæ¼Ò1×¢²á");
            player1Stats = player;
        }
          
        else if(playerId == PlayerID.Player2)
        {
            Debug.Log("Íæ¼Ò2×¢²á");
            player2Stats = player;
        }
         
    }

    public void AddObserver(IEndGameObserver observer)
    {
        endGameObservers.Add(observer);
    }

    public void RemoveObserver(IEndGameObserver observer)
    {
        endGameObservers.Remove(observer);
    }

    public void NotifyObservers(PlayerID id)
    {
        foreach (var observer in endGameObservers)
        {
            observer.EndNotify(id);
        }
    }

}
