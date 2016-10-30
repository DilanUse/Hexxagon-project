using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;


// espacio de nombres del Juego
namespace Juego
{
    // structura que representa un punto 
    struct Point
    {
        public int x, y;

        public Point( int px, int py )
        {
            this.x = px;
            this.y = py;
        } // fin del construcor
    } // fin de Point


    // scripts que se encarga de controlar el juego
    public class Juego : MonoBehaviour
    {
        public Sprite[] spritesJuego; // sprites usados en el juego
        enum IndexSprite { VACIO = 0, RUBI = 1, PERLA = 2, BORDE_SALTO = 3, BORDE_CLONACION = 4 }; // posiciones de los sprites 
        Casilla[][] casillas; // casillas del tablero 
        List<Point> adyacentes; // Casillas adyacentes a una Casilla
        List<Point> coAdyacentes; // Casillas co-adyacentes a una Casilla


        // se llama antes de la primera actualizacion grafica
        void Start()
        {
            adyacentes = new List<Point>();
            coAdyacentes = new List<Point>();
            int cantidadFilas = 5; // cantidad de filas a crear por cada columna
            int posicion = 0; // posicion de las Casillas
            TipoFicha tipoF = 0; // tipo de dicha de las Casillas
            casillas = new Casilla[9][]; // creo nueve columnas 


            // crea las filas de cada una de las columnas de casillas
            for (int  i = 0; i < casillas.Length; i++)
            {
                casillas[i] = new Casilla[cantidadFilas]; // creo filas

                if (i < 4)
                    cantidadFilas++;
                else
                    cantidadFilas--;
            } // fin del for


            // creo todas las Casillas del tablero
            for (int i = 0; i < casillas.Length; i++)
            {
                for (int j = 0; j < casillas[i].Length; j++)
                {
                    // si son las posiciones de un Rubi, si no si son de la perla, si no, es vacio
                    if ((i == 0 && j == 0) || (i == 8 && j == 0) || (i == 4 && j == 8))
                        tipoF = TipoFicha.RUBI;
                    else if ((i == 4 && j == 0) || (i == 0 && j == 4) || (i == 7 && j == 5))
                        tipoF = TipoFicha.PERLA;
                    else
                        tipoF = TipoFicha.VACIO;


                    // si la casilla es un vacio no la instancio, si no, la instancio
                    if ((i == 3 && j == 4) || (i == 4 && j == 3) || (i == 5 && j == 4))
                        casillas[i][j] = null;
                    else
                    {
                        casillas[i][j] = new Casilla(posicion, tipoF);
                        posicion++; // aumento la posicion para la siguiente Casilla
                    }
                } // fin del for
            } // fin del for

            /*
            Debug.Log("Tamaño vectorP: " + casillas.Length);

            for (int i = 0; i < casillas.Length; i++)
            {
                Debug.Log("Tamaño vectorS: " + casillas[i].Length);
                for (int j = 0; j < casillas[i].Length; j++)
                {
                    if (casillas[i][j] != null)
                        Debug.Log(casillas[i][j].Posicion);
                    else
                        Debug.Log("Vacio");
                } // fin del for
            } // fin del for */
        } // fin de Start

        // Update is called once per frame
        void Update()
        {

        }


        // se llama cuando se hace clic en una casilla
        public void Casilla_Click(int pos)
        {
            this.adyacentes.Clear();
            Point mypoint;

            Debug.Log("Click" + pos);

            mypoint = CoordenadasPorPosicion(pos);

            Debug.Log(mypoint.x + "," + mypoint.y);

            
            BuscarAdyacentes(mypoint, adyacentes, null);

            foreach (var item in adyacentes)
            {
                GameObject.Find("Borde" + casillas[item.x][item.y].Posicion).GetComponent<Image>().sprite =
                    spritesJuego[(int)IndexSprite.BORDE_CLONACION];
                Debug.Log(casillas[item.x][item.y].Posicion);
            } 

        } // fin de Casilla_Click


        // obtiene las corrdenadas de una Casilla de acuerdo a una posicion dada
        private Point CoordenadasPorPosicion(int pos)
        {
            for (int i = 0; i < casillas.Length; i++)
                for (int j = 0; j < casillas[i].Length; j++)
                    if (casillas[i][j] != null && casillas[i][j].Posicion == pos)
                        return new Point(i, j);

            return new Point(-1, -1); // no se encontro la posicion 
        } // fin de CoordenadasPorPosicion


        // buscas las Casillas adyacentes de otra Casilla en las coordenadas indicadas
        // y las añade a la lista adyacentes sin incluir las Casillas de la lista excluidos
        private void BuscarAdyacentes( Point coordenadas, List<Point> adyacentes, List<Point> excluidos )
        {
            coordenadas.y -= 1; // busco adyacente arriba
            // si la fila es mayor a cero y no existen excluidos o la coordenada adyacente no esta excluida
            if( coordenadas.y > 0 && (excluidos == null || (excluidos != null && excluidos.IndexOf( coordenadas) != -1 )) 
                && casillas[coordenadas.x][coordenadas.y] != null )
                adyacentes.Add(coordenadas);

            coordenadas.y += 2; // busco adyacente abajo
            // si la fila es menor a la longitud maxima y no existen excluidos o la coordenada adyacente no esta excluida
            if (coordenadas.y < casillas[coordenadas.x].Length && casillas[coordenadas.x][coordenadas.y] != null && 
                (excluidos == null || (excluidos != null && excluidos.IndexOf(coordenadas) != -1)))
                adyacentes.Add(coordenadas);
            coordenadas.y -= 1; // reestabllezo las coordenadas iniciales


            // si la coordenada x es mayor a cero entonces existen adyacentes por la izquierda
            if( coordenadas.x > 0 )
            {
                coordenadas.x -= 1; // busco adyacentes por la izquierda
                // si no existen excluidos o la coordenada adyacente no esta excluida
                if ( casillas[coordenadas.x][coordenadas.y] != null && 
                    (excluidos == null || (excluidos != null && excluidos.IndexOf(coordenadas) != -1)))
                    adyacentes.Add(coordenadas);

                // si la columna izquierda es menor, subo una fila, sino, bajo una fila
                coordenadas.y += (coordenadas.x < 4 ? -1 : 1); // busco otro adyacente por la izquierda 
                // si la fila es mayor a cero y menor al maximo tamaño
                // y no existen excluidos o la coordenada adyacente no esta excluida
                if (coordenadas.y > 0 && coordenadas.y < casillas[coordenadas.x].Length &&
                    casillas[coordenadas.x][coordenadas.y] != null &&
                    (excluidos == null || (excluidos != null && excluidos.IndexOf(coordenadas) != -1)))
                    adyacentes.Add(coordenadas);

                // reestablezco las coordenadas iniciales
                coordenadas.y += (coordenadas.x < 4 ? 1 : -1);
                coordenadas.x += 1;
            } // fin del if


            // si la coordenada x es menor al maximo tamaño posible entonces existen adyacentes por la derecha
            if ( coordenadas.x < casillas.Length )
            {
                coordenadas.x += 1; // busco adyacentes por la derecha
                // si no existen excluidos o la coordenada adyacente no esta excluida
                if ( casillas[coordenadas.x][coordenadas.y] != null &&
                    (excluidos == null || (excluidos != null && excluidos.IndexOf(coordenadas) != -1)))
                    adyacentes.Add(coordenadas);

                // si la columna derecha es mayor, bajo una fila, sino, subo una fila
                coordenadas.y += (coordenadas.x > 4 ? -1 : +1); // busco otro adyacente por la derecha
                // si la fila es mayor a cero y menor al maximo tamaño
                // y no existen excluidos o la coordenada adyacente no esta excluida
                if (coordenadas.y > 0 && coordenadas.y < casillas[coordenadas.x].Length &&
                    casillas[coordenadas.x][coordenadas.y] != null &&
                    (excluidos == null || (excluidos != null && excluidos.IndexOf(coordenadas) != -1)))
                    adyacentes.Add(coordenadas);

                // reestablezco las coordenadas iniciales
                coordenadas.y += (coordenadas.x > 4 ? 1 : -1);
                coordenadas.x -= 1;
            } 

        } // fin de buscarAdyacentes
    } // fin de la clase Juego
} // fin del espacio de nombres de Juego