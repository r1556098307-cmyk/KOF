using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;


public class SceneController : Singleton<SceneController>
{
    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(this);
    }
    private void Start()
    {

    }


    public void TransitionToSelectScene()
    {
        StartCoroutine(LoadScene("SelectScene"));
    }

    public void TransitionToMenuScene()
    {
        StartCoroutine(LoadScene("MenuScene"));
    }

    public void TransitionToGameScene()
    {
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

}
