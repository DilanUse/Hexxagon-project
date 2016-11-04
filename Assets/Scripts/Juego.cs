using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;
using System.Threading;

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
        enum TipoJugador { HUMANO, PC, INTERNET }; // determina el tipo de un Jugador
        Casilla[][] casillas; // casillas del tablero 
        List<Point> adyacentes; // Casillas adyacentes a una Casilla
        List<Point> coAdyacentes; // Casillas co-adyacentes a una Casilla
        List<Point> auxiliarList; // lista para operaciones auxiliares 
        TipoJugador J1; // jugador numero uno(Empieza Jugando, siempre con los Rubis)
        TipoJugador J2; //Jugador numero dos( Juego con las perlas)
        bool turnoJ1; // determina si el turno es del J1
        DatosJuego datos; // datos del Juego
        Comunicaciones comunicaciones; // comunicaciones del juego
        int rubies; // cantidad de rubies en el tablero
        int perlas; // cantidad de perlas en el tablero
        Point fichaSelecionada; // coordenadas de una ficha seleccionada por el usuario
        Point casillaMovimiento; // coordenadas de una casilla seleccionada por el usuario para mover una ficha seleccionada



        Thread procesoClick; // hilo para procesar la entrada del usuario

        // se llama antes de la primera actualizacion grafica
        void Start()
        {

            procesoClick = null;////////////////////////////////////////
            this.datos = GameObject.Find("DatosJuego").GetComponent<DatosJuego>(); // obtengo datos del Juego
            Destroy(GameObject.Find("DatosJuego")); // elimino objeto de Datos del Juego
            this.comunicaciones = GameObject.Find("Comunicaciones").GetComponent<Comunicaciones>();

            // si la partida es local, elimino canal de comunicaciones
            if (this.datos.PartidaLocal)
                Destroy(GameObject.Find("Comunicaciones"));

            this.fichaSelecionada = new Point(-1, -1); // inicio la ficha selecionada sin seleccion valida
            this.casillaMovimiento = new Point(-1, -1); // inicio la casilla de movimiento sin seleccion valida
            this.rubies = this.perlas = 3; // inicio con tres rubies y tres perlas
            this.turnoJ1 = true; // el primer turno es de J1
            this.adyacentes = new List<Point>(); // inicio lista de adyacentes
            this.coAdyacentes = new List<Point>(); // inicio lista de coAdyacentes
            this.auxiliarList = new List<Point>(); //inicio lista auxiliar
            int cantidadFilas = 5; // cantidad de filas a crear por cada columna
            int posicion = 0; // posicion de las Casillas
            TipoFicha tipoF = 0; // tipo de dicha de las Casillas
            this.casillas = new Casilla[9][]; // creo nueve columnas 


            // crea las filas de cada una de las columnas de casillas
            for (int  i = 0; i < this.casillas.Length; i++)
            {
                this.casillas[i] = new Casilla[cantidadFilas]; // creo filas

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
                    else if ((i == 4 && j == 0) || (i == 0 && j == 4) || (i == 8 && j == 4))
                        tipoF = TipoFicha.PERLA;
                    else
                        tipoF = TipoFicha.VACIO;


                    // si la casilla es un vacio(HUECO), si no, es una posicion valida
                    if ((i == 3 && j == 4) || (i == 4 && j == 3) || (i == 5 && j == 4))
                    {
                        casillas[i][j] = new Casilla( -1, TipoFicha.INVALIDA );
                    }
                    else
                    {
                        casillas[i][j] = new Casilla(posicion, tipoF);
                        posicion++; // aumento la posicion para la siguiente Casilla
                    } // fin del if...else
                } // fin del for
            } // fin del for


            // si la partida es local
            if(this.datos.PartidaLocal)
            {
                // si no juega la IA en la partida, ambos jugadores son humanos
                if(!this.datos.HayPC)
                {
                    this.J1 = TipoJugador.HUMANO;
                    this.J2 = TipoJugador.HUMANO;
                }
                else if(this.datos.HayHumano) // si juega la IA y humano
                {
                    this.J1 = TipoJugador.HUMANO;
                    this.J2 = TipoJugador.PC;
                }
                else // si no, solo hay IA
                {
                    this.J1 = TipoJugador.PC;
                    this.J2 = TipoJugador.PC;
                } // fin de los if...else
            }
            else // si no, la partida es online
            {
                // si soy el anfitrion, el J2 es el jugador externo
                if(this.comunicaciones.Servidor)
                {
                    this.J1 = (this.datos.HayHumano ? TipoJugador.HUMANO : TipoJugador.PC);
                    this.J2 = TipoJugador.INTERNET;
                }
                else // si no, el J1 es el jugador externo
                {
                    this.J1 = TipoJugador.INTERNET;
                    this.J2 = (this.datos.HayHumano ? TipoJugador.HUMANO : TipoJugador.PC);
                }
            } // fin del if...else


            // si no hay Humanos en la partida, desactivo la entrada de eventos
            if( !this.datos.HayHumano)
                foreach ( var item in GameObject.FindGameObjectsWithTag("Ficha"))
                    item.GetComponent<Image>().raycastTarget = false;

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
            // si se proceso un click del usuario y el proceso ya termino
            if( this.procesoClick != null && !procesoClick.IsAlive )
            {

                this.actualizarBordes(false);
                /*  foreach (var item in adyacentes)
                  {
                      GameObject.Find("Borde" + casillas[item.x][item.y].Posicion).GetComponent<Image>().sprite =
                          spritesJuego[(int)IndexSprite.BORDE_CLONACION];
                      Debug.Log(casillas[item.x][item.y].Posicion);
                  }

                  foreach (var item in coAdyacentes)
                  {
                      GameObject.Find("Borde" + casillas[item.x][item.y].Posicion).GetComponent<Image>().sprite =
                          spritesJuego[(int)IndexSprite.BORDE_SALTO];
                      Debug.Log(casillas[item.x][item.y].Posicion);
                  } */

                this.procesoClick = null;
            }
        } // fin de Update


        // actualiza los bordes del tablero que señalan las jugadas validas al usuario
        private void actualizarBordes( bool limpiar )
        {
            foreach (var adyacente in this.adyacentes)
                GameObject.Find("Borde" + casillas[adyacente.x][adyacente.y].Posicion).GetComponent<Image>().sprite =
                    spritesJuego[(limpiar ? (int)IndexSprite.VACIO : (int)IndexSprite.BORDE_CLONACION)];

            foreach (var coAdyacente in coAdyacentes)
                GameObject.Find("Borde" + casillas[coAdyacente.x][coAdyacente.y].Posicion).GetComponent<Image>().sprite =
                    spritesJuego[(limpiar ? (int)IndexSprite.VACIO : (int)IndexSprite.BORDE_SALTO)];
        } // fin de actualizarBordes


        // se llama cuando se hace clic en una casilla
        public void Casilla_Click(int pos)
        {
            // si el turno es de un humano y no se esta procesando aún un click
            if( (this.turnoJ1 && this.J1 == TipoJugador.HUMANO) || (!this.turnoJ1 && this.J2 == TipoJugador.HUMANO) &&
                this.procesoClick == null)
            {
                // obtengo las coordenadas seleccionadas por el usuario
                Point coorSelect = this.CoordenadasPorPosicion(pos);


                // si el turno es de los rubis y se selecciono un rubi, o si es de las perlas y se selecciono una perla
                if ( this.turnoJ1 && this.casillas[coorSelect.x][coorSelect.y].Tipo == TipoFicha.RUBI ||
                    !this.turnoJ1 && this.casillas[coorSelect.x][coorSelect.y].Tipo == TipoFicha.PERLA )
                {
                    // si no hay ficha seleccionada aun ó se seleciono una ficha diferente a la antes seleccionada
                    if( (this.fichaSelecionada.x == -1 && this.fichaSelecionada.y == -1) || 
                        (coorSelect.x != this.fichaSelecionada.x || coorSelect.y != this.fichaSelecionada.y))
                    {
                        this.procesoClick = new Thread(o => // inicio nuevo proceso para encontrar adyacentes
                        {
                            this.adyacentes.Clear(); // limpio lista anterior de adyacentes
                            this.coAdyacentes.Clear(); // limpio lista anterior de coAdyacentes
                            BuscarAdyacentes(coorSelect, adyacentes, null); // busco adyacentes


                            // busco las coAdyacentes
                            foreach (var adyacente in this.adyacentes)
                            {
                                this.auxiliarList.Clear(); // limpio lista auxiliar
                                this.auxiliarList.AddRange(this.adyacentes); // agrego a la lista auxiliar las adyacentes
                                this.auxiliarList.Add(coorSelect); // agrego a la lista auxiliar la ficha seleccionada
                                this.auxiliarList.AddRange(coAdyacentes); // agrego a la lista auxiliar las coAdyacentes

                                BuscarAdyacentes(adyacente, coAdyacentes, auxiliarList); // busco coAdyacentes
                            } // fin del foreach
                            auxiliarList.Clear(); // limpio lista auxiliar


                            Point aux; // coordenada auxiliar
                            // desecho los huecos y las que no esten vacias
                            for (int i = 0; i < adyacentes.Count; i++)
                            {
                                aux = adyacentes[i];

                                if ((aux.x == 3 && aux.y == 4) || (aux.x == 4 && aux.y == 3) || (aux.x == 5 && aux.y == 4) ||
                                    casillas[aux.x][aux.y].Tipo != TipoFicha.VACIO )
                                    adyacentes.RemoveAt(i);
                            } // fin del for


                            // desecho los huecos  y las que no estan vacias
                            for (int i = 0; i < coAdyacentes.Count; i++)
                            {
                                aux = coAdyacentes[i];

                                if ((aux.x == 3 && aux.y == 4) || (aux.x == 4 && aux.y == 3) || (aux.x == 5 && aux.y == 4) ||
                                     casillas[aux.x][aux.y].Tipo != TipoFicha.VACIO)
                                    coAdyacentes.RemoveAt(i);
                            } // fin del for
                        }); // fin de Hilo


                        this.fichaSelecionada = coorSelect; // cambio la ficha seleccionada
                        this.actualizarBordes(true);
                        this.procesoClick.Start(); // inicio proceso
                    }
                    else // sino, se seleciono la misma ficha
                    {
                        // deselecciono la ficha antes selecionada por el usuario eliminando recuadros de adyacentes
                        this.actualizarBordes(true);
                  /*      foreach (var item in adyacentes)
                            GameObject.Find("Borde" + casillas[item.x][item.y].Posicion).GetComponent<Image>().sprite =
                                spritesJuego[(int)IndexSprite.VACIO];

                        // deselecciono la ficha antes selecionada por el usuario eliminando recuadros de coAdyacentes
                        foreach (var item in coAdyacentes)
                            GameObject.Find("Borde" + casillas[item.x][item.y].Posicion).GetComponent<Image>().sprite =
                                spritesJuego[(int)IndexSprite.VACIO]; */

                        // deselecciono la ficha antes selecionada por el usuario
                        this.fichaSelecionada.x = this.fichaSelecionada.y = -1;
                    } // fin del if...else
                } // si no, si ya hay una ficha seleccionada y jugo en una posicion valida 
                else if((this.fichaSelecionada.x != -1 && this.fichaSelecionada.y != -1) && 
                    ( ( this.adyacentes.IndexOf(coorSelect) != -1 ) || (this.coAdyacentes.IndexOf(coorSelect) != -1)))
                {
                    this.fichaSelecionada.x = this.fichaSelecionada.y = -1;
                    this.actualizarBordes(true);
                    Debug.Log("Hago jugada");

                    ///////////////////////////////////////////////////////////////////////////////////
                    casillas[coorSelect.x][coorSelect.y].Tipo = (this.turnoJ1 ? TipoFicha.RUBI : TipoFicha.PERLA);
                    GameObject.Find("Ficha" + casillas[coorSelect.x][coorSelect.y].Posicion).GetComponent<Image>().sprite =
                    spritesJuego[( turnoJ1 ? (int)IndexSprite.RUBI : (int)IndexSprite.PERLA)];
                    this.turnoJ1 = !this.turnoJ1;
                } // fin del if...else
            } // fin del if



        /*    Debug.Log("Click");
            foreach (var item in adyacentes)
            {
                GameObject.Find("Borde" + casillas[item.x][item.y].Posicion).GetComponent<Image>().sprite =
                    spritesJuego[(int)IndexSprite.VACIO];
            }

            foreach (var item in coAdyacentes)
            {
                GameObject.Find("Borde" + casillas[item.x][item.y].Posicion).GetComponent<Image>().sprite =
                    spritesJuego[(int)IndexSprite.VACIO];
            }


            t = new Thread(o =>
            {
                Point mypoint;
                mypoint = CoordenadasPorPosicion(pos);
                this.adyacentes.Clear();
                BuscarAdyacentes(mypoint, adyacentes, null);

                
                this.coAdyacentes.Clear();
                foreach (var item in adyacentes)
                {
                    auxiliarList.Clear();
                    auxiliarList.AddRange(adyacentes);
                    auxiliarList.Add(mypoint);
                    auxiliarList.AddRange(coAdyacentes);

                    BuscarAdyacentes(item, coAdyacentes, auxiliarList);
                }
                auxiliarList.Clear();



                Point aux;
                // desecho los huecos
                for (int i = 0; i < adyacentes.Count; i++)
                {
                    aux = adyacentes[i];

                    if ((aux.x == 3 && aux.y == 4) || (aux.x == 4 && aux.y == 3) || (aux.x == 5 && aux.y == 4))
                        adyacentes.RemoveAt(i);
                }
                // desecho los huecos
                for (int i = 0; i < coAdyacentes.Count; i++)
                {
                    aux = coAdyacentes[i];

                    if ((aux.x == 3 && aux.y == 4) || (aux.x == 4 && aux.y == 3) || (aux.x == 5 && aux.y == 4))
                        coAdyacentes.RemoveAt(i);
                }

            });

            t.Start();
            */
            

        } // fin de Casilla_Click


        // obtiene las coordenadas de una Casilla de acuerdo a una posicion dada
        private Point CoordenadasPorPosicion(int pos)
        {
            for (int i = 0; i < casillas.Length; i++)
                for (int j = 0; j < casillas[i].Length; j++)
                    if (casillas[i][j].Tipo != TipoFicha.INVALIDA  && casillas[i][j].Posicion == pos)
                        return new Point(i, j);

            return new Point(-1, -1); // no se encontro la posicion 
        } // fin de CoordenadasPorPosicion


        // buscas las Casillas adyacentes de otra Casilla en las coordenadas indicadas
        // y las añade a la lista adyacentes sin incluir las Casillas de la lista excluidos
        private void BuscarAdyacentes( Point coordenadas, List<Point> adyacentes, List<Point> excluidos )
        {
            // atrapa excepciones al salirse de la representacion matricial del tablero
            try
            {
                coordenadas.y -= 1; // busco adyacente arriba
                // si la fila es mayor a cero y no existen excluidos o la coordenada adyacente no esta excluida
                if (coordenadas.y >= 0 && (excluidos == null || (excluidos != null && excluidos.IndexOf(coordenadas) == -1)))
          //          && casillas[coordenadas.x][coordenadas.y].Tipo != TipoFicha.INVALIDA)
                    adyacentes.Add(coordenadas);

                coordenadas.y += 2; // busco adyacente abajo
                // si la fila es menor a la longitud maxima y no existen excluidos o la coordenada adyacente no esta excluida
                if (coordenadas.y < casillas[coordenadas.x].Length && 
      //              casillas[coordenadas.x][coordenadas.y].Tipo != TipoFicha.INVALIDA &&
                    (excluidos == null || (excluidos != null && excluidos.IndexOf(coordenadas) == -1)))
                    adyacentes.Add(coordenadas);
                coordenadas.y -= 1; // reestabllezo las coordenadas iniciales


                // si la coordenada x es mayor a cero entonces existen adyacentes por la izquierda
                if (coordenadas.x > 0 )
                {
                    coordenadas.x -= 1; // busco adyacentes por la izquierda
                    // si no existen excluidos o la coordenada adyacente no esta excluida
                    if (coordenadas.y < casillas[coordenadas.x].Length && 
             //           casillas[coordenadas.x][coordenadas.y].Tipo != TipoFicha.INVALIDA &&
                        (excluidos == null || (excluidos != null && excluidos.IndexOf(coordenadas) == -1)))
                        adyacentes.Add(coordenadas);

                    // si la columna izquierda es menor, subo una fila, sino, bajo una fila
                    coordenadas.y += (coordenadas.x < 4 ? -1 : 1); // busco otro adyacente por la izquierda 
                    // si la fila es mayor a cero y menor al maximo tamaño
                    // y no existen excluidos o la coordenada adyacente no esta excluida
                    if (coordenadas.y >= 0 && coordenadas.y < casillas[coordenadas.x].Length &&
          //              casillas[coordenadas.x][coordenadas.y].Tipo != TipoFicha.INVALIDA &&
                        (excluidos == null || (excluidos != null && excluidos.IndexOf(coordenadas) == -1)))
                        adyacentes.Add(coordenadas);

                    // reestablezco las coordenadas iniciales
                    coordenadas.y += (coordenadas.x < 4 ? 1 : -1);
                    coordenadas.x += 1;
                } // fin del if


                // si la coordenada x es menor al maximo tamaño posible entonces existen adyacentes por la derecha
                if (coordenadas.x < casillas.Length -1)
                {
                    coordenadas.x += 1; // busco adyacentes por la derecha
                    // si no existen excluidos o la coordenada adyacente no esta excluida
                    if (coordenadas.y < casillas[coordenadas.x].Length &&
           //             casillas[coordenadas.x][coordenadas.y].Tipo != TipoFicha.INVALIDA &&
                        (excluidos == null || (excluidos != null && excluidos.IndexOf(coordenadas) == -1)))
                        adyacentes.Add(coordenadas);

                    // si la columna derecha es mayor, bajo una fila, sino, subo una fila
                    coordenadas.y += (coordenadas.x > 4 ? -1 : +1); // busco otro adyacente por la derecha
                    // si la fila es mayor a cero y menor al maximo tamaño
                    // y no existen excluidos o la coordenada adyacente no esta excluida
                    if (coordenadas.y >= 0 && coordenadas.y < casillas[coordenadas.x].Length &&
         //               casillas[coordenadas.x][coordenadas.y].Tipo != TipoFicha.INVALIDA &&
                        (excluidos == null || (excluidos != null && excluidos.IndexOf(coordenadas) == -1)))
                        adyacentes.Add(coordenadas);


                    // reestablezco las coordenadas iniciales
                    coordenadas.y += (coordenadas.x > 4 ? 1 : -1);
                    coordenadas.x -= 1;
                } // fin del if
            }
            catch(IndexOutOfRangeException e)
            {
                Debug.Log(e.Message);
                Debug.Log(coordenadas.x + ", " + coordenadas.y);
            } // fin del try...catch
        } // fin de buscarAdyacentes
    } // fin de la clase Juego
} // fin del espacio de nombres de Juego