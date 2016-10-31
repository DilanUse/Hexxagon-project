using UnityEngine;
using System.Collections;


namespace Juego
{
    // representa los datos iniciales del juego 
    public class DatosJuego : MonoBehaviour
    {
        public bool PartidaLocal { get; set; } // determina si la partida es local o no(en linea)
        public bool Servidor { get; set; } // determina si el Juego debe actuar como servidor o no(cliente)
        public bool HayHumano { get; set; } // determina si en Juego hay un humano 
        public bool HayPC { get; set; } // determina si en el Jueho esta la IA


        // Se llama al cargar el GameObject asosiado
        void Awake()
        {
            DontDestroyOnLoad(this.gameObject);
            this.Servidor = this.HayHumano = this.HayPC = this.PartidaLocal = false;
        } // fin de Awake
    } // fin de DatosJuego
} // fin del espacio de nombres Juego
