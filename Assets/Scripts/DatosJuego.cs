using UnityEngine;
using System.Collections;
using UnityEngine.UI;


// espacio de nombres del Juego
namespace Juego
{
    // representa los datos iniciales del juego 
    public class DatosJuego : MonoBehaviour
    {
        public Toggle humanoSel; // selector de la GUI
        public Toggle pcSel; // selector de la GUI
        public bool PartidaLocal { get; set; } // determina si la partida es local o no(en linea)
        public bool HayHumano { get; set; } // determina si en Juego hay un humano 
        public bool HayPC { get; set; } // determina si en el Jueho esta la IA


        // Se llama al cargar el GameObject asosiado
        void Awake()
        {
            DontDestroyOnLoad(this.gameObject);
            this.HayHumano = true; // Se juega como humano por defecto
            this.HayPC = this.PartidaLocal = false;
        } // fin de Awake


        // establece los tipos de jugadores
        public void EstablecerJugadores()
        {
            this.HayHumano = humanoSel.isOn;
            this.HayPC = pcSel.isOn;
        } // fin de EstablecerJugadores


        // imprime datos por proposito de depuracion
        public void imprimir()
        {
            Debug.Log("Partida Local: " + this.PartidaLocal);
            Debug.Log("Hay humano: " + this.HayHumano);
            Debug.Log("Hay PC: " + this.HayPC);
        } // fin de imprimir
    } // fin de DatosJuego
} // fin del espacio de nombres Juego
