using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct route
{
    public Vector3  position;
    public int clip;
}

public class Robot_Function : MonoBehaviour
{
    public Conveyor_Big BigConveyor;

    public route[] myroute;

    public Transform IK_Goal;
    public float gapTime=1;

    public int index;

    public int signalIndex;

    Clip_Function clipFun_0;

    public bool working ;
    

    void Start()
    {
        clipFun_0=transform.GetComponent<Clip_Function>();
        index = 0;
        working = false;
    }


    IEnumerator ExeRoute()
    {
        if (!working)
        {
            working = true;
            for(int j=0; j < (myroute.Length); j++) 
            {                
                var tempPosition = IK_Goal.transform.localPosition;
                var deltaPositon = myroute[index].position - IK_Goal.transform.localPosition;

                for (int i = 0; i < 60; i++)
                {
                    IK_Goal.transform.Translate(deltaPositon / 60);
                    yield return new WaitForSecondsRealtime(gapTime / 60);
                }

                if (myroute[index].clip == 1)
                {
                    clipFun_0.ClipWorking();
                    yield return new WaitForSecondsRealtime(2f);
                }


                if (index < myroute.Length - 1)
                {
                    index += 1;
                }
                else
                {
                    index = 0;
                }

                if (index == signalIndex)
                {                  
                    BigConveyor.Processing();

                    Debug.Log("waiting");
                    yield return new WaitUntil(() => BigConveyor.finished);
                    Debug.Log("offload...");
                }

            }
            working = false;
        }

        yield return 0;
    }



    public void RobotWorking()
    {
        if (!working)  StartCoroutine("ExeRoute");
    }

    #region debug
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            RobotWorking();
        }
    }
    #endregion

}


