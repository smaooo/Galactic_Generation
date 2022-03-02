using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GalaxyObject;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GalaxyManager : MonoBehaviour
{
    [System.Serializable]
    public struct Data
    {
        public string name;
        public Vector3 position;
    }
    [SerializeField]
    private Material planetMaterial;
    [SerializeField]
    private GameObject starPrefab;
    public SolarSystem ss;
    bool sceneLoaded = false;
    public bool systemLoaded = false;
    [SerializeField]
    private List<Data> locs;

    [SerializeField]
    private Text text;
    // Start is called before the first frame update
    void Awake()
    {
        //DontDestroyOnLoad(this.gameObject);
        StartCoroutine(LoadGalaxy());
    }

    // Update is called once per frame
    void Update()
    {

        if (systemLoaded)
            ss.CheckLOD();

        if (Input.GetKeyDown(KeyCode.R))
        {
            ss.Destroy();
            systemLoaded = false;
            FindObjectOfType<SpaceShip>().assigned = false;
            StartCoroutine(LoadGalaxy());

        }
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
        for (int i = 0; i < 1; i++)
        {
            var g = new GameObject();
            g.transform.position = new Vector3(Random.Range(-1000, 1000), Random.Range(-1000, 1000), Random.Range(-1000, 1000));
            ss = new SolarSystem(g.transform, planetMaterial, starPrefab);
            locs.Add(new Data
            {
                name = ss.Star.ObjectData.name,
                position = ss.Star.ObjectData.position
            });
            foreach (var p in ss.Planets)
            {
                locs.Add(new Data
                {
                    name = p.ObjectData.name,
                    position = p.ObjectData.position
                });
                foreach (var m in p.Moons)
                {
                    locs.Add(new Data
                    {
                        name = m.ObjectData.name,
                        position = m.ObjectData.position
                    });
                }
            }
        }


        
        systemLoaded = true;
        //foreach (var p in ss.Planets)
        //{
        //    FindObjectOfType<Text>().text += p.PlanetObject.name + "\n";
        //}
    }
}
