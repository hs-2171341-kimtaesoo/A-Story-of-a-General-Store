using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class imagePackText : MonoBehaviour
{
    [SerializeField] private GameObject[] imagePack;
    private GameObject[] image;
    private Image imageimage;
    int num = 0;
    private void Start()
    {
        for (int i = 0; i < imagePack.Length; i++)
        {
            imagePack[i].SetActive(false);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            for (int i = 0; i < imagePack.Length; i++)
            {
                imagePack[i].SetActive(false);
            }
            imagePack[0].SetActive(true);
            imagePack[0].TryGetComponent(out imageimage);
            image = imageimage.image;
            num = 0;
        }
        else if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            for (int i = 0; i < imagePack.Length; i++)
            {
                imagePack[i].SetActive(false);
            }
            imagePack[1].SetActive(true);
            imagePack[1].TryGetComponent(out imageimage);
            image = imageimage.image;
            num = 0;


        }
        else if(Input.GetKeyDown(KeyCode.Alpha2))
        {
            for(int i = 0; i < imagePack.Length; i++)
            {
                imagePack[i].SetActive(false);
            }
            imagePack[2].SetActive(true);
            imagePack[2].TryGetComponent(out imageimage);
            image = imageimage.image;
            num = 0;
        }
        else if(Input.GetKeyDown(KeyCode.Alpha3))
        {
            for (int i = 0; i < imagePack.Length; i++)
            {
                imagePack[i].SetActive(false);
            }
            imagePack[3].SetActive(true);
            imagePack[3].TryGetComponent(out imageimage);
            image = imageimage.image;
            num = 0;
        }
        else if(Input.GetKeyDown(KeyCode.Alpha4))
        {
            for (int i = 0; i < imagePack.Length; i++)
            {
                imagePack[i].SetActive(false);
            }
            imagePack[4].SetActive(true);
            imagePack[4].TryGetComponent(out imageimage);
            image = imageimage.image;
            num = 0;
        }
        else if(Input.GetKeyDown(KeyCode.Alpha5))
        {
            for (int i = 0; i < imagePack.Length; i++)
            {
                imagePack[i].SetActive(false);
            }
            imagePack[5].SetActive(true);
            imagePack[5].TryGetComponent(out imageimage);
            image = imageimage.image;
            num = 0;
        }
        else if(Input.GetKeyDown(KeyCode.Alpha6))
        {
            for (int i = 0; i < imagePack.Length; i++)
            {
                imagePack[i].SetActive(false);
            }
            imagePack[6].SetActive(true);
            imagePack[6].TryGetComponent(out imageimage);
            image = imageimage.image;
            num = 0;
        }
        else if(Input.GetKeyDown(KeyCode.Alpha7))
        {
            for (int i = 0; i < imagePack.Length; i++)
            {
                imagePack[i].SetActive(false);
            }
            imagePack[7].SetActive(true);
            imagePack[7].TryGetComponent(out imageimage);
            image = imageimage.image;
            num = 0;
        }
        else if(Input.GetKeyDown(KeyCode.Alpha8))
        {
            for (int i = 0; i < imagePack.Length; i++)
            {
                imagePack[i].SetActive(false);
            }
            imagePack[8].SetActive(true);
            imagePack[8].TryGetComponent(out imageimage);
            image = imageimage.image;
            num = 0;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            for (int i = 0; i < imagePack.Length; i++)
            {
                imagePack[i].SetActive(false);
            }
            imagePack[9].SetActive(true);
            imagePack[9].TryGetComponent(out imageimage);
            image = imageimage.image;
            num = 0;
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            for (int i = 0; i < imagePack.Length; i++)
            {
                imagePack[i].SetActive(false);
            }
            imagePack[10].SetActive(true);
            imagePack[10].TryGetComponent(out imageimage);
            image = imageimage.image;
            num = 0;
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            for (int i = 0; i < imagePack.Length; i++)
            {
                imagePack[i].SetActive(false);
            }
            imagePack[11].SetActive(true);
            imagePack[11].TryGetComponent(out imageimage);
            image = imageimage.image;
            num = 0;
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            for (int i = 0; i < imagePack.Length; i++)
            {
                imagePack[i].SetActive(false);
            }
            imagePack[12].SetActive(true);
            imagePack[12].TryGetComponent(out imageimage);
            image = imageimage.image;
            num = 0;
        }

        if (imageimage != null && Input.GetKeyDown(KeyCode.A))
        {
            image[num].SetActive(false);
            if (num == 0)
            {
                num = image.Length - 1;
            }
            else
            {
                num -= 1;
            }
            image[num].SetActive(true);
            
        }
        else if (imageimage != null && Input.GetKeyDown(KeyCode.D))
        {
            image[num].SetActive(false);
            if (num == image.Length - 1)
            {
                num = 0;
            }
            else
            {
                num += 1;
            }
            image[num].SetActive(true);
        }

    }
}
