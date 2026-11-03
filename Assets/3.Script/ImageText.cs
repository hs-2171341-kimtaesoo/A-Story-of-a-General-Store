using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImageText : MonoBehaviour
{
    [SerializeField] private GameObject[] image;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            image[0].SetActive(true);
            image[1].SetActive(false);
            image[2].SetActive(false);
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            image[1].SetActive(true);
            image[0].SetActive(false);
            image[2].SetActive(false);
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            image[2].SetActive(true);
            image[1].SetActive(false);
            image[0].SetActive(false);
        }
    }
}
