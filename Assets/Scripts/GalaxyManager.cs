using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GalaxyObject;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
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

    [SerializeField]
    private TMP_Dropdown planetDropDown;
    [SerializeField]
    private TMP_Dropdown moonDropDown;

    [SerializeField]
    private Button shipButton;
    [SerializeField]
    private Button exploreButton;

    [SerializeField]
    private SpaceShip spaceShip;

    [SerializeField]
    private GameObject exploreCamera;

    private bool exploreMode = true;

    void Awake()
    {
        //DontDestroyOnLoad(this.gameObject);
        StartCoroutine(LoadGalaxy());
    }

    private void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        if (systemLoaded)
            ss.CheckLOD();

        if(exploreMode)
        {
            MoveExploreCamera();
        }
        
    }

    private void MoveExploreCamera()
    {
        Camera.main.transform.position += Input.GetAxis("Mouse ScrollWheel") * Camera.main.transform.forward * 100;

        if (Input.GetKey(KeyCode.Mouse0))
        {
            Camera.main.transform.position += Input.GetAxis("Mouse X") * Camera.main.transform.right + Input.GetAxis("Mouse Y") * Camera.main.transform.up;
            Cursor.visible = false;
        }
        else
        {
            Cursor.visible = true;
        }

        if (Input.GetKey(KeyCode.Mouse1))
        {
            Camera.main.transform.Rotate(new Vector3(Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"), 0));
            Cursor.visible = false;

        }
        else
        {
            Cursor.visible = true;

        }
    }

    public void LoadScene()
    {
        StartCoroutine(SceneLoader(text));
    }

    public void SwitchToSpaceShip()
    {
        exploreCamera.SetActive(false);
        spaceShip.gameObject.SetActive(true);
        exploreButton.gameObject.SetActive(true);
        shipButton.gameObject.SetActive(false);
        spaceShip.transform.position = exploreCamera.transform.position;
        spaceShip.transform.rotation = exploreCamera.transform.rotation;
        exploreMode = false;

    }

    public void SwitchToExploration()
    {
        exploreMode = true;
        exploreButton.gameObject.SetActive(false);
        shipButton.gameObject.SetActive(true);
        exploreCamera.SetActive(true);
        spaceShip.gameObject.SetActive(false);
        GoToStar();
    }

    public void Regenerate()
    {
        ss.Destroy();
        systemLoaded = false;
        //FindObjectOfType<SpaceShip>().assigned = false;
        StartCoroutine(LoadGalaxy());
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

    public void GoToStar()
    {
        StartCoroutine(MoveCamera(ss.Star.ObjectData.position));
    }
    public void GoToPlanet()
    {
        FillMoonList();
        if (planetDropDown.options[0].text == "Select a Planet!")
            planetDropDown.options.RemoveAt(0);
        StartCoroutine(MoveCamera(ss.Planets[planetDropDown.value].ObjectData.position));
    }

    private void FillMoonList()
    {
        if (ss.Planets[planetDropDown.value].Moons.Length > 0)
        {
            moonDropDown.gameObject.SetActive(true);
            List<TMP_Dropdown.OptionData> moonOptions = new List<TMP_Dropdown.OptionData>();
            moonOptions.Add(new TMP_Dropdown.OptionData { text = "Select a Moon!" });

            foreach (var m in ss.Planets[planetDropDown.value].Moons)
            {
                moonOptions.Add(new TMP_Dropdown.OptionData { text = m.ObjectData.name });
            }
            moonDropDown.options = moonOptions;
        }
        else
        {
            moonDropDown.gameObject.SetActive(false);
        }
    }
    public void GoToMoon()
    {
        
        if (moonDropDown.options[0].text == "Select a Moon!")
            moonDropDown.options.RemoveAt(0);
        StartCoroutine(MoveCamera(ss.Planets[planetDropDown.value].Moons[moonDropDown.value].ObjectData.position));

    }

    private IEnumerator MoveCamera(Vector3 target)
    {
        print("Cal");
        Vector3 pos = new Vector3(target.x + 150, target.y, target.z + 150);
        

        while (Vector3.Distance(Camera.main.transform.position, pos) > 10)
        {
            yield return new WaitForEndOfFrame();
            Camera.main.transform.position = Vector3.MoveTowards(Camera.main.transform.position, pos, 100);
        
        }
        Camera.main.transform.rotation = Quaternion.LookRotation(target - Camera.main.transform.position);

    }

    private IEnumerator LoadGalaxy()
    {
        yield return null;
        ss = new SolarSystem(this.transform, planetMaterial, starPrefab);
        locs.Add(new Data
        {
            name = ss.Star.ObjectData.name,
            position = ss.Star.ObjectData.position
        });
        List<TMP_Dropdown.OptionData> planetOptions = new List<TMP_Dropdown.OptionData>();
        planetOptions.Add(new TMP_Dropdown.OptionData { text = "Select a Planet!" });
        foreach (var p in ss.Planets)
        {
            planetOptions.Add(new TMP_Dropdown.OptionData { text = p.ObjectData.name });
            locs.Add(new Data
            {
                name = p.ObjectData.name,
                position = p.ObjectData.position
            });;
            foreach (var m in p.Moons)
            {
                locs.Add(new Data
                {
                    name = m.ObjectData.name,
                    position = m.ObjectData.position
                });
            }
        }
        planetDropDown.options = planetOptions;

        FillMoonList();
        GoToStar();
        systemLoaded = true;
       
    }
}
