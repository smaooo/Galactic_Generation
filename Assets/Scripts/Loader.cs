using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Loader : MonoBehaviour
{
    public void LoadLevel()
    {
        var text = FindObjectOfType<Text>();
        StartCoroutine(Buffer(text));
    }

    private IEnumerator Buffer(Text text)
    {
        yield return null;
        var l = SceneManager.LoadSceneAsync("Galaxy");
        l.allowSceneActivation = false;
        while (!l.isDone)
        {
            text.text = (l.progress).ToString();
            if (l.progress >= 0.9f)
            {
                l.allowSceneActivation = true;
            }
            yield return null;

        }
    }
}
