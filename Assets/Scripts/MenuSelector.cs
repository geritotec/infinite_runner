using UnityEngine;
using UnityEngine.UI;

public class MenuSelector : MonoBehaviour
{
    public Image[] botones;

    public float escalaNormal = 1f;
    public float escalaSeleccionada = 1.15f;

    int seleccionado;

    void Start()
    {
        seleccionado = PlayerPrefs.GetInt("MenuSeleccionado", 0);
        ActualizarBotones();
    }

    public void Seleccionar(int index)
    {
        seleccionado = index;
        PlayerPrefs.SetInt("MenuSeleccionado", index);
        ActualizarBotones();
    }

    void ActualizarBotones()
    {
        for (int i = 0; i < botones.Length; i++)
        {
            if (i == seleccionado)
            {
                botones[i].transform.localScale = Vector3.one * escalaSeleccionada;
            }
            else
            {
                botones[i].transform.localScale = Vector3.one * escalaNormal;
            }
        }
    }
}