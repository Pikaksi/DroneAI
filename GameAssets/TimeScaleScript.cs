using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeScaleScript : MonoBehaviour
{
    [SerializeField] [Range(0.1f, 10f)] float timeSpeed = 1f;

    // Update is called once per frame
    void Update()
    {
        Time.timeScale = timeSpeed;
    }
}
