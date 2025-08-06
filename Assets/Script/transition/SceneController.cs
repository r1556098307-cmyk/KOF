using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;


public class SceneController : Singleton<SceneController>,IEndGameObserver
{
    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(this);
    }
    private void Start()
    {
        if(GameManager.Instance!=null)
        GameManager.Instance.AddObserver(this);
    }


    public void TransitionToSelectScene()
    {
        StartCoroutine(LoadScene("SelectScene"));
    }

    public void TransitionToMenuScene()
    {
        Time.timeScale = 1f;
        StartCoroutine(LoadScene("MenuScene"));
    }

    public void TransitionToGameScene()
    {
        Time.timeScale = 1f;
        StartCoroutine(LoadScene("GameScene"));
    }


    IEnumerator LoadScene(string scene)
    {

        if (scene != "")
        {
            yield return SceneManager.LoadSceneAsync(scene);
            yield break;
        }

    }

    IEnumerator LoadMain()
    {
        yield return SceneManager.LoadSceneAsync("MenuScene");

        yield break;
    }


    public void EndNotify(PlayerID id)
    {
        //if (id == PlayerID.Player1)
        //    StartCoroutine(LoadMain());
    }
}
