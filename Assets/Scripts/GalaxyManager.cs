using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GalaxyObject;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GalaxyManager : MonoBehaviour
{
    [SerializeField]
    private Material planetMaterial;
    [SerializeField]
    private GameObject starPrefab;
    SolarSystem ss;
    bool sceneLoaded = false;
    bool systemLoaded = false;

    [SerializeField]
    private Text text;
    // Start is called before the first frame update
    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        if (sceneLoaded)
            ss.CheckLOD();
    }

    public void LoadScene()
    {
        StartCoroutine(SceneLoader(text));
    }
    
    private IEnumerator SceneLoader(Text text)
    {
        yield return null;
        var l = SceneManager.LoadSceneAsync("Galaxy", LoadSceneMode.Single);
        l.allowSceneActivation = false;
        StartCoroutine(Buffer(l));
        
        while (!l.isDone)
        {
            if (l.progress >= 0.9f && systemLoaded)
            {
                l.allowSceneActivation = true;
                sceneLoaded = true;
                break;
            }
            yield return null;

        }
    }

    private IEnumerator Buffer(AsyncOperation operation)
    {
        bool shouldLoad = true;

        while (!sceneLoaded)
        {
            text.text = operation.progress.ToString();
            if (operation.progress > 0.5f && shouldLoad)
            {
                shouldLoad = false;
                StartCoroutine(LoadGalaxy());
            }
            yield return null;
        }
    }

    private IEnumerator LoadGalaxy()
    {
        yield return null;

        ss = new SolarSystem(this.transform, planetMaterial, starPrefab);
        systemLoaded = true;
        //foreach (var p in ss.Planets)
        //{
        //    FindObjectOfType<Text>().text += p.PlanetObject.name + "\n";
        //}
    }
}
