using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public PlayerStats player1Stats;
    public PlayerStats player2Stats;

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(this);
    }
    public void RigisterPlayer(PlayerStats player,PlayerID playerId)
    {
        if (playerId == PlayerID.Player1)
            player1Stats = player;
        else if(playerId == PlayerID.Player2)
            player2Stats = player;
    }
}
