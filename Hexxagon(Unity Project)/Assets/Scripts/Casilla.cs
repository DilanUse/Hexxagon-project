using UnityEngine;
using System.Collections;


namespace Juego
{
    // constantes que representan el tipo de ficha que almacena una Casilla
    public enum TipoFicha { RUBI, PERLA, VACIO };


    // representa una Casilla del tablero
    public class Casilla
    {
        public int Posicion { get; private set; } // representa la posicion de la Casilla en el tablero 
        TipoFicha Tipo { get; set; } // el tipo de ficha que contiene la Casilla

        // constructor de Casilla
        public Casilla( int pos, TipoFicha t )
        {
            this.Posicion = pos;
            this.Tipo = t; 
        } // fin del constructor Casilla
    } // fin de la clase Casilla
} // fin del espacio de nombres de Juego