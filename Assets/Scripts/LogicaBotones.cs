using UnityEngine;

public class LogicaBotones : MonoBehaviour
{
    public GameObject[] screens;

    public void CambiarPantalla(int index)
    {
        for(int i = 0; i < screens.Length; i++)
        {
            screens[i].SetActive(i == index);
        }
    }
}