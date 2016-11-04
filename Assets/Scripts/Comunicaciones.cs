using UnityEngine;
using System.Collections;
using UnityEngine.UI;


public class Comunicaciones : MonoBehaviour
{
    public Toggle servidorSel; // selector de la GUI
    public bool Servidor { get; set; } // determina si el Juego debe actuar como servidor o no(cliente)


    // inicializa los atributos de las comunicaciones
    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
        this.Servidor = false;
    } // fin de Awake


    // establece si se aloja o no una partida
    public void establecerServidor()
    {
        this.Servidor = servidorSel.isOn;
    } // fin de establecerServidor 
} // fin de Comunicaciones
