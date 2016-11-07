using UnityEngine;
using System.Collections;
using System;


namespace Juego
{
    // representa un movimiento del Juego 
    [Serializable]
    public class Movimiento
    {
        public string tipo; // tipo de movimiento a realizar 
        public int[] nodos; // nodos del movimiento 


        // inicializa un Movimiento 
        public Movimiento(string t, int[] n)
        {
            this.tipo = t;
            this.nodos = n;
        } // fin del constructor 
    } // fin de la clase movimiento
} // fin del espacio de nombres Juego
