using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class T : MonoBehaviour
{
    private int bR = 0;
    private int sR = 0;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(sR);
        Debug.Log(bR);
        if (bR <= -359)
       {
           Debug.Log("here");
           bR = 0;

       }
       else if (sR <= -359)
       {
           sR = 0;
       }
       
        FirstClock();
        SecondClock();
    }

    void FirstClock()
    {
        if (bR == -270 && sR == -90)
        {
            Debug.Log("play music");
            //clock.SetActive(false);
            //livingRoomMain.SetActive(true);
        }

    }
    void SecondClock()
    {
        //track = 7;
        if (bR == -240 && sR == -180)
        {
            //clockOpen.SetActive(true);
            //clock.SetActive(false);

        }
    }
}
