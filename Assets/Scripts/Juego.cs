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


        public override string ToString()
        {
            return "(" + this.x + ", " + this.y + ")";
        }
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
                    //        int espaciosVacios; // cantidad de espacios vacios en el tablero
        Point fichaSelecionada; // coordenadas de una ficha seleccionada por el usuario
                                //       Point casillaMovimiento; // coordenadas de una casilla seleccionada por el usuario para mover una ficha seleccionada
        Thread procesoClick; // hilo para procesar la entrada del usuario
        Thread procesoMovimiento; // hilo para procesar los movimientos de fichas en el Juego
        Thread procesoValidacionJuego; // hilo para procesar las validacioes del juego
        Thread procesoDeIA; // hilo para procesar la jugada de la IA
        bool winRubis; // determina si ganaron las perlas
        bool winPerlas; // determina si ganaron los rubis
        bool empate; // determina si hay empate
        bool rubisBloqueados; // determina si los rubis se quedaron sin movmientos
        bool perlasBloqueadas; // determina si las perlas se quedaron sin movimientos
        bool juegoTerminado; // determina si ya se finalizo el juego
        public GameObject winRubisAviso; // ventana para indicar que ganaron los rubis en la GUI
        public GameObject winPerlasAviso; // ventana para indicar que ganaron las perlas en la GUI 
        float tiempoJugada; // almacena el momento en que se finalizo una jugada


        // se llama antes de la primera actualizacion grafica
        void Start()
        {
            this.datos = GameObject.Find("DatosJuego").GetComponent<DatosJuego>(); // obtengo datos del Juego
            Destroy(GameObject.Find("DatosJuego")); // elimino objeto de Datos del Juego
            this.comunicaciones = GameObject.Find("Comunicaciones").GetComponent<Comunicaciones>();

            // si la partida es local, elimino canal de comunicaciones
            if (this.datos.PartidaLocal)
                Destroy(GameObject.Find("Comunicaciones"));


            this.juegoTerminado = false; // el juego no se ha terminado(apenas va a empezar)
            this.empate = this.winRubis = this.winPerlas = false; // inicio indicadores
            this.rubisBloqueados = this.perlasBloqueadas = false; // inicio indicadores
            this.procesoDeIA = null; // inicio hilo en null
            this.procesoValidacionJuego = null; // inicio el hilo en null
            this.procesoMovimiento = null; // inicio el hilo en null
            this.procesoClick = null; // inicio el hilo en null
            this.fichaSelecionada = new Point(-1, -1); // inicio la ficha selecionada sin seleccion valida
 //           this.casillaMovimiento = new Point(-1, -1); // inicio la casilla de movimiento sin seleccion valida
            this.rubies = this.perlas = 3; // inicio con tres rubies y tres perlas
 //           this.espaciosVacios = 52; // el tablero inicia con 52 espacios vacios
            this.turnoJ1 = true; // el primer turno es de J1
            this.adyacentes = new List<Point>(); // inicio lista de adyacentes
            this.coAdyacentes = new List<Point>(); // inicio lista de coAdyacentes
            this.auxiliarList = new List<Point>(); //inicio lista auxiliar
            int cantidadFilas = 5; // cantidad de filas a crear por cada columna
            int posicion = 0; // posicion de las Casillas ( 0 - 57)
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
                foreach ( var casilla in GameObject.FindGameObjectsWithTag("Ficha"))
                    casilla.GetComponent<Image>().raycastTarget = false;



            
            Debug.Log("J1: " + this.J1);
            Debug.Log("J2: " + this.J2);
            Debug.Log("turnoJ1: " + this.turnoJ1);
            Debug.Log("PartidaLocal: " + this.datos.PartidaLocal);
            Debug.Log("Hay Humano" + this.datos.HayHumano);
            Debug.Log("Hay PC" + this.datos.HayPC);





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
                this.ActualizarBordes(false);
                this.procesoClick = null; // dejo al hilo sin referencia
            } // fin del if
            

            // si se proceso un movimiento y el proceso ya termino
            if ( this.procesoMovimiento != null && !this.procesoMovimiento.IsAlive )
            {
                this.turnoJ1 = !this.turnoJ1; // cambio de turno
                this.ActualizarFichas(); // actualizo fichas


                // proceso para validar el Juego
                this.procesoValidacionJuego = new Thread(o =>
                {
                    this.ValidarJuego();
                }); // fin del metodo


                this.procesoValidacionJuego.Start(); // inicio proceso de validacion
                this.procesoMovimiento = null; // dejo al hilo sin referencia
            } // fin del if


            // si se proceso una validacion y el proceso termino
            if( this.procesoValidacionJuego != null && !this.procesoValidacionJuego.IsAlive )
            {
                // si se gano por bloqueo o por extincion se actualizan las fichas llenando el tablero
                if (this.rubisBloqueados || this.perlasBloqueadas || this.rubies == 0 || this.perlas == 0)
                    this.ActualizarFichas();


                // si ganaron los rubis, las perlas o se empato, lo anuncio
                if (this.winRubis)
                {
                    this.winRubisAviso.SetActive(true);
                    this.juegoTerminado = true;
                }
                else if (this.winPerlas)
                {
                    this.winPerlasAviso.SetActive(true);
                    this.juegoTerminado = true; 
                }
                else if (this.empate)
                {
                    Debug.Log("Empate");
                    this.juegoTerminado = true;
                } // fin del if...else


                
                tiempoJugada = Time.time;//////////////
                this.procesoValidacionJuego = null;
            } // fin del if


            // si se preoceso una jugada de la IA y el proceso termino
            if(this.procesoDeIA != null && !this.procesoDeIA.IsAlive)
            {
                Debug.Log("Finalizo proceso de la IA");
                this.procesoDeIA = null;
            } // fin del if


            // si el turno es de la PC y no se esta procesando aún un click, un movimiento o una validacion
            // y no se ha terminado el juego y han pasado 5 segundos desde la ultima jugada
            if (((this.turnoJ1 && this.J1 == TipoJugador.PC) || (!this.turnoJ1 && this.J2 == TipoJugador.PC)) &&
                this.procesoClick == null && this.procesoMovimiento == null && procesoValidacionJuego == null && 
                this.procesoDeIA == null && !this.juegoTerminado && Time.time - tiempoJugada > 1)
            {
                Debug.Log("Es el turno de la IA");
                // proceso para realizar la jugada de la IA
                this.procesoDeIA = new Thread(o =>
               {
                   Debug.Log("antes de llamar a JugadaIA");
                   this.JugadaIA();
               });

                
                Debug.Log("Inicio proceso de la IA");
                this.procesoDeIA.Start(); // inicio el proceso de la IA
            } // fin del if
        } // fin de Update


        // actualiza los bordes del tablero que señalan las jugadas validas al usuario
        private void ActualizarBordes( bool limpiar )
        {
            foreach (var adyacente in this.adyacentes)
                GameObject.Find("Borde" + casillas[adyacente.x][adyacente.y].Posicion).GetComponent<Image>().sprite =
                    spritesJuego[(limpiar ? (int)IndexSprite.VACIO : (int)IndexSprite.BORDE_CLONACION)];

            foreach (var coAdyacente in coAdyacentes)
                GameObject.Find("Borde" + casillas[coAdyacente.x][coAdyacente.y].Posicion).GetComponent<Image>().sprite =
                    spritesJuego[(limpiar ? (int)IndexSprite.VACIO : (int)IndexSprite.BORDE_SALTO)];
        } // fin de actualizarBordes


        // actualiza las fichas del tablero y las cantidades de las fichas en la GUI
        private void ActualizarFichas()
        {
            int ficha = 0; // ficha actual de la GUI

            // actualizo las cantidades de las Fichas graficamente
            GameObject.Find("Text_CantRubis").GetComponent<Text>().text = this.rubies.ToString();
            GameObject.Find("Text_CantPerlas").GetComponent<Text>().text = this.perlas.ToString();


            // recorro todas las casillas del tablero y las actualizo graficamente
            for (int i = 0; i < this.casillas.Length; i++)
            {
                for (int j = 0; j < this.casillas[i].Length; j++)
                {
                    if (this.casillas[i][j].Tipo == TipoFicha.RUBI)
                    {
                        GameObject.Find("Ficha" + this.casillas[i][j].Posicion ).GetComponent<Image>().sprite =
                            spritesJuego[(int)IndexSprite.RUBI];
                        ficha++;
                    }
                    else if (this.casillas[i][j].Tipo == TipoFicha.PERLA)
                    {
                        GameObject.Find("Ficha" + this.casillas[i][j].Posicion).GetComponent<Image>().sprite =
                            spritesJuego[(int)IndexSprite.PERLA];
                        ficha++;
                    }
                    else if (this.casillas[i][j].Tipo != TipoFicha.INVALIDA)
                    {
                        GameObject.Find("Ficha" + this.casillas[i][j].Posicion).GetComponent<Image>().sprite =
                            spritesJuego[(int)IndexSprite.VACIO];
                        ficha++;
                    } // fin del if...else
                } // fin del for
            } // fin del for 
        } // fin de ActualizarFichas


        // valida si hay un ganador en el Juego
        private void ValidarJuego()
        {
            Point coorProcesar; // coordenadas a procesar en la busqueda de movimientos
            TipoFicha fichaRellenar; // ficha que se usara para llenar el tablero en caso de bloqueo o extincion de una ficha


            // si el tablero esta lleno, determino 
            // si ganan los rubis o las perlas o se empato
            if (this.rubies + this.perlas == 58)
            {
                this.DeterminarGanador();
            }
            else if( this.rubies > 0 && this.perlas > 0) // si ambos tienen fichas
            {
                // supongo que la ficha del turno actual esta bloqueada
                if (turnoJ1)
                    this.rubisBloqueados = true;
                else
                    this.perlasBloqueadas = true;


                // proceso todas las casillas del tablero mientras no encuentre un movimiento valido
                for (int i = 0; i < casillas.Length && (this.rubisBloqueados || this.perlasBloqueadas); i++)
                {
                    for (int j = 0; j < casillas[i].Length && (this.rubisBloqueados || this.perlasBloqueadas); j++)
                    {
                        // si la casilla es valida y tiene una ficha del turno actual
                        if( casillas[i][j].Tipo != TipoFicha.INVALIDA && 
                            (this.turnoJ1 && this.casillas[i][j].Tipo == TipoFicha.RUBI ) ||
                            (!this.turnoJ1 && this.casillas[i][j].Tipo == TipoFicha.PERLA) )
                        {
                            coorProcesar = this.CoordenadasPorPosicion(casillas[i][j].Posicion); // obtengo coordenadas
                            this.auxiliarList.Clear(); // limpio lista auxiliar
                            this.adyacentes.Clear(); // limpio lista anterior de adyacentes
                            this.coAdyacentes.Clear(); // limpio lista anterior de coAdyacentes
                            this.BuscarAdyacentes(coorProcesar, this.adyacentes, null); // busco adyacentes


                            // busco las coAdyacentes
                            foreach (var adyacente in this.adyacentes)
                            {
                                this.auxiliarList.Clear(); // limpio lista auxiliar
                                this.auxiliarList.AddRange(this.adyacentes); // agrego a la lista auxiliar las adyacentes
                                this.auxiliarList.Add(coorProcesar); // agrego a la lista auxiliar la ficha seleccionada
                                this.auxiliarList.AddRange(this.coAdyacentes); // agrego a la lista auxiliar las coAdyacentes

                                BuscarAdyacentes(adyacente, this.coAdyacentes, this.auxiliarList); // busco coAdyacentes
                            } // fin del foreach


                            this.FiltrarAdyacentes(this.adyacentes, TipoFicha.VACIO);
                            this.FiltrarAdyacentes(this.coAdyacentes, TipoFicha.VACIO);


                            // si la lista de adyacentes o la de coAdyacentes no esta vacia
                            if (this.adyacentes.Count > 0 || this.coAdyacentes.Count > 0)
                            {
                                this.rubisBloqueados = this.perlasBloqueadas = false; // no hay bloqueo
                                break; // no proceso mas adyacencias o coAdyacencias
                            } // fin del if
                        } // fin del if
                    } // fin del for
                } // fin del for 
            } // fin del if...else


            // si se quedo bloqueado alguien o si alguien quedo sin fichas
            if( this.rubisBloqueados || this.perlasBloqueadas || this.rubies == 0 || this.perlas == 0 )
            {
                // si alguien se quedo bloqueado
                if (this.rubisBloqueados || this.perlasBloqueadas)
                    fichaRellenar = (this.rubisBloqueados ? TipoFicha.PERLA : TipoFicha.RUBI);
                else // si no entonces alguien se quedo sin fichas
                    fichaRellenar = (this.rubies == 0 ? TipoFicha.PERLA : TipoFicha.RUBI);

                // sumo a las fichas no bloqueadas las casillas vacias
                if (fichaRellenar == TipoFicha.RUBI)
                    this.rubies += (58 - this.rubies - this.perlas); 
                else
                    this.perlas += (58 - this.rubies - this.perlas);


                // lleno todas las casillas vacias con la ficha que no esta bloqueada o la no extinta
                for (int i = 0; i < casillas.Length; i++)
                {
                    for (int j = 0; j < casillas[i].Length; j++)
                    {
                        // si la casilla es valida y esta vacia
                        if (this.casillas[i][j].Tipo != TipoFicha.INVALIDA && this.casillas[i][j].Tipo == TipoFicha.VACIO)
                            this.casillas[i][j].Tipo = fichaRellenar; // la casilla se vuelve la ficha no bloqueada
                    } // fin del for
                } // fin del for 


                this.DeterminarGanador();
            } // fin del if 
        } // fin de ValidarJuego


        // determina el ganador suponiendo que el tablero esta lleno
        private void DeterminarGanador()
        {
            if (this.rubies > this.perlas)
                this.winRubis = true;
            else if (this.perlas > this.rubies)
                this.winPerlas = true;
            else
                this.empate = true;
        } // fin de DeterminarGanador


        // se llama cuando se hace clic en una casilla
        public void Casilla_Click(int pos)
        {
            // si el turno es de un humano y no se esta procesando aún un click o un movimiento
            // o una validacion o una IA, y no se ha terminado el juego
            if( ((this.turnoJ1 && this.J1 == TipoJugador.HUMANO) || (!this.turnoJ1 && this.J2 == TipoJugador.HUMANO)) &&
                this.procesoClick == null && this.procesoMovimiento == null && procesoValidacionJuego == null &&
                this.procesoDeIA == null && !this.juegoTerminado)
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
                        // Proceso que se encarga de Procesar el click del usuario
                        this.procesoClick = new Thread(o => // inicio nuevo proceso para encontrar adyacentes
                        {
                            this.adyacentes.Clear(); // limpio lista anterior de adyacentes
                            this.coAdyacentes.Clear(); // limpio lista anterior de coAdyacentes
                            BuscarAdyacentes(coorSelect, this.adyacentes, null); // busco adyacentes


                            // busco las coAdyacentes
                            foreach (var adyacente in this.adyacentes)
                            {
                                this.auxiliarList.Clear(); // limpio lista auxiliar
                                this.auxiliarList.AddRange(this.adyacentes); // agrego a la lista auxiliar las adyacentes
                                this.auxiliarList.Add(coorSelect); // agrego a la lista auxiliar la ficha seleccionada
                                this.auxiliarList.AddRange(this.coAdyacentes); // agrego a la lista auxiliar las coAdyacentes

                                BuscarAdyacentes(adyacente, this.coAdyacentes, this.auxiliarList); // busco coAdyacentes
                            } // fin del foreach
                            auxiliarList.Clear(); // limpio lista auxiliar


                            this.FiltrarAdyacentes(this.adyacentes, TipoFicha.VACIO);
                            this.FiltrarAdyacentes(this.coAdyacentes, TipoFicha.VACIO);
                            /*
                            Point aux; // coordenada auxiliar
                            // desecho los huecos y las que no esten vacias
                            for (int i = 0; i < this.adyacentes.Count; i++)
                            {
                                aux = this.adyacentes[i];

                                if ((aux.x == 3 && aux.y == 4) || (aux.x == 4 && aux.y == 3) || (aux.x == 5 && aux.y == 4) ||
                                    this.casillas[aux.x][aux.y].Tipo != TipoFicha.VACIO)
                                {
                                    this.adyacentes.RemoveAt(i);
                                    i--; // disminuye el indice ya que el tamaño de la lista disminuye
                                }
                            } // fin del for


                            // desecho los huecos  y las que no estan vacias
                            for (int i = 0; i < this.coAdyacentes.Count; i++)
                            {
                                aux = this.coAdyacentes[i];

                                if ((aux.x == 3 && aux.y == 4) || (aux.x == 4 && aux.y == 3) || (aux.x == 5 && aux.y == 4) ||
                                     this.casillas[aux.x][aux.y].Tipo != TipoFicha.VACIO)
                                {
                                    this.coAdyacentes.RemoveAt(i);
                                    i--; // disminuye el indice ya que el tamaño de la lista disminuye
                                }
                            } // fin del for
                            */
                        }); // fin de Hilo


                        this.fichaSelecionada = coorSelect; // cambio la ficha seleccionada
                        this.ActualizarBordes(true);
                        this.procesoClick.Start(); // inicio proceso
                    }
                    else // sino, se seleciono la misma ficha
                    {
                        // deselecciono la ficha antes selecionada por el usuario eliminando recuadros de adyacentes
                        this.ActualizarBordes(true);
                        this.fichaSelecionada.x = this.fichaSelecionada.y = -1;
                    } // fin del if...else
                } // si no, si ya hay una ficha seleccionada y jugo en una posicion valida 
                else if((this.fichaSelecionada.x != -1 && this.fichaSelecionada.y != -1) && 
                    ( ( this.adyacentes.IndexOf(coorSelect) != -1 ) || (this.coAdyacentes.IndexOf(coorSelect) != -1)))
                {
                    Debug.Log("Hago jugada");
                    int[] movimientos = { this.casillas[this.fichaSelecionada.x][this.fichaSelecionada.y].Posicion,
                                          this.casillas[coorSelect.x][coorSelect.y].Posicion};

                    string tipo = (this.adyacentes.IndexOf(coorSelect) != -1 ? "clonar" : "saltar");

                    this.ActualizarBordes(true);
                    this.RealizarMovimiento( tipo, movimientos);
                    this.fichaSelecionada.x = this.fichaSelecionada.y = -1;
                   

                    
                    ///////////////////////////////////////////////////////////////////////////////////
   /*                 casillas[coorSelect.x][coorSelect.y].Tipo = (this.turnoJ1 ? TipoFicha.RUBI : TipoFicha.PERLA);
                    GameObject.Find("Ficha" + casillas[coorSelect.x][coorSelect.y].Posicion).GetComponent<Image>().sprite =
                    spritesJuego[( turnoJ1 ? (int)IndexSprite.RUBI : (int)IndexSprite.PERLA)];
                    this.turnoJ1 = !this.turnoJ1; */
                } // fin del if...else
            } // fin del if           
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


        // filtrar una lista de adyacencias de acuerdo al parametro filtro
        private void FiltrarAdyacentes( List<Point> adyacentesP, TipoFicha filtro )
        {
            Point coord; // coordenada auxiliar

            // desecho los huecos y el tipo de Ficha establecido
            for (int i = 0; i < adyacentesP.Count; i++)
            {
                coord = adyacentesP[i];

                if ((coord.x == 3 && coord.y == 4) || (coord.x == 4 && coord.y == 3) || (coord.x == 5 && coord.y == 4) ||
                    this.casillas[coord.x][coord.y].Tipo != filtro)
                {
                    adyacentesP.RemoveAt(i);
                    i--; // disminuye el indice ya que el tamaño de la lista disminuye
                } // fin del if
            } // fin del for
        } // fin de FiltrarAdyacentes


        // buscas las Casillas adyacentes de otra Casilla en las coordenadas indicadas
        // y las añade a la lista adyacentes sin incluir las Casillas de la lista excluidos
        private void BuscarAdyacentes( Point coordenadas, List<Point> adyacentesP, List<Point> excluidos )
        {
            // atrapa excepciones al salirse de la representacion matricial del tablero
            try
            {
                coordenadas.y -= 1; // busco adyacente arriba
                // si la fila es mayor a cero y no existen excluidos o la coordenada adyacente no esta excluida
                if (coordenadas.y >= 0 && (excluidos == null || (excluidos != null && excluidos.IndexOf(coordenadas) == -1)))
                    adyacentesP.Add(coordenadas);

                coordenadas.y += 2; // busco adyacente abajo
                // si la fila es menor a la longitud maxima y no existen excluidos o la coordenada adyacente no esta excluida
                if (coordenadas.y < casillas[coordenadas.x].Length && 
                    (excluidos == null || (excluidos != null && excluidos.IndexOf(coordenadas) == -1)))
                    adyacentesP.Add(coordenadas);
                coordenadas.y -= 1; // reestabllezo las coordenadas iniciales


                // si la coordenada x es mayor a cero entonces existen adyacentes por la izquierda
                if (coordenadas.x > 0 )
                {
                    coordenadas.x -= 1; // busco adyacentes por la izquierda
                    // si no existen excluidos o la coordenada adyacente no esta excluida
                    if (coordenadas.y < casillas[coordenadas.x].Length && 
                        (excluidos == null || (excluidos != null && excluidos.IndexOf(coordenadas) == -1)))
                        adyacentesP.Add(coordenadas);

                    // si la columna izquierda es menor, subo una fila, sino, bajo una fila
                    coordenadas.y += (coordenadas.x < 4 ? -1 : 1); // busco otro adyacente por la izquierda 
                    // si la fila es mayor a cero y menor al maximo tamaño
                    // y no existen excluidos o la coordenada adyacente no esta excluida
                    if (coordenadas.y >= 0 && coordenadas.y < casillas[coordenadas.x].Length &&
                        (excluidos == null || (excluidos != null && excluidos.IndexOf(coordenadas) == -1)))
                        adyacentesP.Add(coordenadas);

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
                        (excluidos == null || (excluidos != null && excluidos.IndexOf(coordenadas) == -1)))
                        adyacentesP.Add(coordenadas);

                    // si la columna derecha es mayor, bajo una fila, sino, subo una fila
                    coordenadas.y += (coordenadas.x > 4 ? -1 : +1); // busco otro adyacente por la derecha
                    // si la fila es mayor a cero y menor al maximo tamaño
                    // y no existen excluidos o la coordenada adyacente no esta excluida
                    if (coordenadas.y >= 0 && coordenadas.y < casillas[coordenadas.x].Length &&
                        (excluidos == null || (excluidos != null && excluidos.IndexOf(coordenadas) == -1)))
                        adyacentesP.Add(coordenadas);


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


        // realiza el movimiento de fichas en el tablero de acuerdo al tipo de 
        // movimiento y la casillas del movimiento
        private void RealizarMovimiento( string tipo, int[] movimientos )
        {
            List<Point> robadas = new List<Point>(); // lista de fichas robadas tras realizar el movimiento
            Point ficha = CoordenadasPorPosicion(movimientos[0]); // obtengo la ficha a manipular
            Point casilla = CoordenadasPorPosicion(movimientos[1]); // obtengo la casilla a donde realizar movimiento


            // proceso para realizar el movimiento
            this.procesoMovimiento = new Thread(o => 
            {
                BuscarAdyacentes( casilla, robadas, null); // busco adyacentes a robar
                this.FiltrarAdyacentes(robadas, (this.turnoJ1 ? TipoFicha.PERLA : TipoFicha.RUBI)); // filtro fichas contrarias
                this.DecrementarFicha(false, robadas.Count); // decremento la ficha del contrario
                this.AumentarFicha(true, robadas.Count); // aumento la ficha del turno actual

                // cambio las fichas robadas por la ficha de turno que las robo
                foreach (var robada in robadas)
                    this.casillas[robada.x][robada.y].Tipo = (this.turnoJ1 ? TipoFicha.RUBI : TipoFicha.PERLA);
            }); // fin del proceso


            // si el movimiento es de salto, elimino la ficha en la posicion inicial
            if (tipo == "saltar")
            {
                this.casillas[ficha.x][ficha.y].Tipo = TipoFicha.VACIO;
                this.DecrementarFicha(true); // decremento la ficha de turno
            } // fin del if


            //  en la casilla seleccionada a realizar el movimiento coloco la ficha de turno
            this.casillas[casilla.x][casilla.y].Tipo = (this.turnoJ1 ? TipoFicha.RUBI : TipoFicha.PERLA);
            this.AumentarFicha(true); // aumento la ficha de turno
            this.procesoMovimiento.Start(); // inicio proceso para realizar movimiento
        } // fin de realizarMovimiento


        // decrementa la cantidad de fichas que este de turno o no dependiendo de lo especificado
        private void DecrementarFicha( bool turno, int cant = 1 )
        {
            // si se especifico decrementar el de turno y el turno es de los rubies
            // o si se especifico decrementar el que no esta de turno y no es el turno de los rubies
            // en caso contrario se decrementan las perlas
            if ((turno && turnoJ1) || !turno && !turnoJ1)
                rubies -= cant;
            else 
                perlas -= cant;
        } // fin de decrementarFicha


        // aumenta la cantidad de fichas que este de turno o no dependiendo de lo especificado
        private void AumentarFicha(bool turno, int cant = 1)
        {
            // si se especifico aumentar el de turno y el turno es de los rubies
            // o si se especifico aumentar el que no esta de turno y no es el turno de los rubies
            // en caso contrario se aumentan las perlas
            if ((turno && turnoJ1) || !turno && !turnoJ1)
                rubies += cant;
            else
                perlas += cant;
        } // fin de AumentarFicha


        // escoje la mejor jugada posible para la PC
        private void JugadaIA()
        {
            Debug.Log("Inicio de JugadaIA");
            TipoFicha FichaDeTurno = (this.turnoJ1 ? TipoFicha.RUBI : TipoFicha.PERLA); // ficha de turno
            TipoFicha FichaSigTurno = (this.turnoJ1 ? TipoFicha.PERLA : TipoFicha.RUBI); // ficha del seiguiente turno
            List<Point> adyacenetesFichaDeturno = new List<Point>(); // lista de adaycentes de la ficha de turno
            List<Point> coAdyacentesFichaDeTurno = new List<Point>(); // lista de coAdyacentes de la ficha de turno
            List<Point> adyacentesFichaSigTurno = new List<Point>(); // lista de adyacentes de la ficha del sig turno
            List<Point> coAdyacentesFichaSigTurno = new List<Point>(); // lista de coAdycentes de la ficha del sig turno
            List<Point> listaRobadasTurnoActual = new List<Point>(); // lista de fichas roobadas por la ficha del turno actual
            List<Point> listaRobadasTurnoSig = new List<Point>(); // lista de fichas roobadas por la ficha del turno siguiente
            Point posTurnoActual = new Point(); // posicion inicial de una ficha del turno actual
            Point posTurnoSiguiente = new Point(); // posicion inicial de una ficha del turno siguiente 
//            Point posF_TurnoActual = new Point(); // posicion final de una ficha del turno actual
            int robadasTurnoActual; // fichas robadas por la ficha del turno actual
            int robadasTurnoSig; // fichas robadas por la ficha del turno siguiente
            int maxRobadasTurnoSig; // maxima cantidad de fichas robadas por la ficha del turno siguiente en una jugada
            int mejorJugada = Int32.MinValue; // cantidad que representa la mejor jugada de la IA(las fichas robadas de la IA vs las del rival)
            string tipo_DM = string.Empty; // tipo de movimiento seleccionado para realizar el moviento
            int[] movimientos_DM = new int[2]; // posiciones seleccionadas para realizar el movimiento
            int tipoLista = 0; // contador que determina el indice entre los tipos de lista para el turno actual
            List<Point> listaTurnoActual = new List<Point>();
            bool creadaNueva; // determina si con un movimiento se crea una nueva ficha  

            // para todas las casillas del tablero 
            for (int i = 0; i < this.casillas.Length; i++)
            {
                for (int j = 0; j < this.casillas[i].Length; j++)
                {
        //            Debug.Log( "inicio: " + i + "," + j );
                    // si es una ficha del turno actual 
                    if( this.casillas[i][j].Tipo == FichaDeTurno )
                    {
     ///                   Debug.Log("Ficha de turno encontrada");
                        posTurnoActual.x = i; // guardo coordenada x de la posicion inicial
                        posTurnoActual.y = j; // guardo coordenada y de la posicion inicial
                        listaTurnoActual.Clear();
                        adyacenetesFichaDeturno.Clear();
                        coAdyacentesFichaSigTurno.Clear();

                        // busco adyacentes y coAdyacentes de las casillas vacias en torno a la ficha actual
                        this.buscarAdyacentesCoAdyacentes(posTurnoActual, adyacenetesFichaDeturno, coAdyacentesFichaDeTurno);


       //                 Debug.Log("Tamaño lista ady: " + adyacenetesFichaDeturno.Count);
     //                   Debug.Log("Tamaño lista coAdy: " + coAdyacentesFichaDeTurno.Count);

                        // uno las dos lista en una sola para procesarlas juntas
                        listaTurnoActual.AddRange(adyacenetesFichaDeturno);
                        listaTurnoActual.AddRange(coAdyacentesFichaDeTurno);

  //                      Debug.Log("Tamaño lista ady: " + adyacenetesFichaDeturno.Count);
    //                    Debug.Log("Tamaño lista coAdy: " + coAdyacentesFichaDeTurno.Count);
      //                  Debug.Log("Tamaño lista total: " + listaTurnoActual.Count);
                        // para todas las adyacentes de la ficha actual
                        foreach (var adyacenteFt in listaTurnoActual)
                        {
                            // simulo la jugada dependiendo del tipo de lista del turno actual y veo cuantos fichas robo
                            if( tipoLista < adyacenetesFichaDeturno.Count)
                                robadasTurnoActual = this.SimularJugada("clonar", posTurnoActual, adyacenteFt, FichaDeTurno, 
                                    listaRobadasTurnoActual);
                            else
                                robadasTurnoActual = this.SimularJugada("saltar", posTurnoActual, adyacenteFt, FichaDeTurno,
                                    listaRobadasTurnoActual);

                            maxRobadasTurnoSig = Int32.MinValue; // inicio las maximas robadas del turno siguiente

                            // para todas las casillas del tablero 
                            for (int k = 0; k < this.casillas.Length; k++)
                            {
                                for (int l = 0; l < this.casillas[k].Length; l++)
                                {
                                    // si es una ficha del turno siguiente
                                    if (this.casillas[k][l].Tipo == FichaSigTurno)
                                    {
                                        posTurnoSiguiente.x = k; // guardo coordenada x de la posicion inicial
                                        posTurnoSiguiente.y = l; // guardo coordenada y de la posicion inicial
                                        adyacentesFichaSigTurno.Clear();
                                        coAdyacentesFichaSigTurno.Clear();

                                        // busco adyacentes y coAdyacentes de las casillas vacias
                                        // en torno a la ficha del siguiente turno
                                        this.buscarAdyacentesCoAdyacentes(posTurnoSiguiente, adyacentesFichaSigTurno,
                                            coAdyacentesFichaSigTurno);


                                        // para todas las adyacentes del turno siguiente
                                        foreach (var adyacenteFtSig in adyacentesFichaSigTurno)
                                        {
                                            // simulo la jugada del turno siguiente y veo cuantas roba
                                            robadasTurnoSig = this.SimularJugada("clonar", posTurnoSiguiente,
                                                adyacenteFtSig, FichaSigTurno, listaRobadasTurnoSig);

                                            // si robo mas que la mayor cantidad robada en un turno lo modifico
                                            if (robadasTurnoSig > maxRobadasTurnoSig)
                                                maxRobadasTurnoSig = robadasTurnoSig;


                                            // deshago la ultima jugada del urno siguiente restableciendo el tablero original
                                            this.DeshacerJugada("clonar", posTurnoSiguiente,
                                                adyacenteFtSig, FichaSigTurno, listaRobadasTurnoSig);
                                        } // fin del foreach


                                        // para todas las coAdyacentes del turno siguiente
                                        foreach (var coAdyacentesFtSig in coAdyacentesFichaSigTurno)
                                        {
                                            // simulo la jugada del turno siguiente y veo cuantas roba
                                            robadasTurnoSig = this.SimularJugada("saltar", posTurnoSiguiente,
                                                coAdyacentesFtSig, FichaSigTurno, listaRobadasTurnoSig);

                                            // si robo mas que la mayor cantidad robada en un turno lo modifico
                                            if (robadasTurnoSig > maxRobadasTurnoSig)
                                                maxRobadasTurnoSig = robadasTurnoSig;


                                            // deshago la ultima jugada del urno siguiente restableciendo el tablero original
                                            this.DeshacerJugada("saltar", posTurnoSiguiente,
                                                coAdyacentesFtSig, FichaSigTurno, listaRobadasTurnoSig);
                                        } // fin del foreach
                                    } // fin del if para la ficha del turno siguiente
                                } // fin del for
                            } // fin del for 

                            
                            if (tipoLista < adyacenetesFichaDeturno.Count)
                                this.DeshacerJugada("clonar", posTurnoActual, adyacenteFt, FichaDeTurno,
                                    listaRobadasTurnoActual);
                            else
                                this.DeshacerJugada("saltar", posTurnoActual, adyacenteFt, FichaDeTurno,
                                    listaRobadasTurnoActual);


                            // si no robo con esta jugada y voy a saltar, entonces mejor no lo hago(mejor me clono)
                            if( robadasTurnoActual == 0 && tipoLista >= adyacenetesFichaDeturno.Count)
                            {
                                tipoLista++;
                                continue;
                            }
                            
                            // si las robadas por la ficha de turno actual menos las maximas robadas por la ficha del turno
                            // siguiente es mayor a la mejor jugada hasta ahora, entonces es la mejor jugada
                            if ( robadasTurnoActual - maxRobadasTurnoSig > mejorJugada )
                            {
                                mejorJugada = robadasTurnoActual - maxRobadasTurnoSig;
                                tipo_DM = (tipoLista < adyacenetesFichaDeturno.Count ? "clonar" : "saltar");
                                movimientos_DM[0] = this.casillas[posTurnoActual.x][posTurnoActual.y].Posicion;
                                movimientos_DM[1] = this.casillas[adyacenteFt.x][adyacenteFt.y].Posicion;
                            } // fin del if

                            
                            tipoLista++; // aumento el indice de la lista
                        } // fin del foreach para la lista de adyacentes y coAdyacentes del turno actual

   //                     Debug.Log("Aqui");
                    } // fin del if
                    

 //                   Debug.Log("Fin: " + i + "," + j);
                } // fin del for
            } // fin del for 

            Debug.Log("Finalizada JugadaIA");
            Debug.Log("Tipo: " + tipo_DM);
            Debug.Log("Origen: " + movimientos_DM[0]);
            Debug.Log("Final :" + movimientos_DM[1]);

            this.RealizarMovimiento(tipo_DM, movimientos_DM); // realizo movimiento escojido por la IA
        } // fin de JuagadaIA


        // busca y guardas una lista de adyacentes y coAdyacentes del tipo de casilla especificada
        // en las coordenadas especificadas
        private void buscarAdyacentesCoAdyacentes( Point coord, List<Point> adyacentes, List<Point> coAdyacentes)
        {
            this.BuscarAdyacentes(coord, adyacentes, null); // busco adyacentes

            // busco las coAdyacentes
            foreach (var adyacente in adyacentes)
            {
                this.auxiliarList.Clear(); // limpio lista auxiliar
                this.auxiliarList.AddRange(adyacentes); // agrego a la lista auxiliar las adyacentes
                this.auxiliarList.Add(coord); // agrego a la lista auxiliar la ficha seleccionada
                this.auxiliarList.AddRange(coAdyacentes); // agrego a la lista auxiliar las coAdyacentes

                this.BuscarAdyacentes(adyacente, coAdyacentes, this.auxiliarList); // busco coAdyacentes
            } // fin del foreach
            this.auxiliarList.Clear(); // limpio lista auxiliar

 //           Debug.Log("antes de filtrar");
  //          Debug.Log("Adyacentes: " + adyacentes.Count);
    //        Debug.Log("Coadyacentes : " + coAdyacentes.Count);
            this.FiltrarAdyacentes(adyacentes, TipoFicha.VACIO);
            this.FiltrarAdyacentes(coAdyacentes, TipoFicha.VACIO);

      //      Debug.Log("despues de filtrar");
        //    Debug.Log("Adyacentes: " + adyacentes.Count);
        //    Debug.Log("Coadyacentes : " + coAdyacentes.Count);
        } // fin de buscarAdyacentesCoAdyacentes

        
        // simula una jugada en el tablero y retorna la cantidad de fichas contrarias capturadas
        // la jugada se simulada de acuerdo al tipo de movimiento, los puntos iniciales y finales
        // y la ficha que esta de turno
        private int SimularJugada(string tipo, Point Po, Point Pf, TipoFicha fichaTurno, List<Point> robadas)
        {
            // si el movimiento es de salto, elimino la ficha en la posicion inicial
            if (tipo == "saltar")
                this.casillas[Po.x][Po.y].Tipo = TipoFicha.VACIO;


            //  en la casilla seleccionada a realizar el movimiento coloco la ficha de turno
            this.casillas[Pf.x][Pf.y].Tipo = fichaTurno;
            this.BuscarAdyacentes(Pf, robadas, null); // busco adyacentes a robar filtro fichas contrarias a robar
            this.FiltrarAdyacentes(robadas, ( fichaTurno == TipoFicha.RUBI ? TipoFicha.PERLA : TipoFicha.RUBI)); 


            // cambio las fichas robadas por la ficha de turno que las robo
            foreach (var robada in robadas)
                this.casillas[robada.x][robada.y].Tipo = (fichaTurno == TipoFicha.RUBI ? TipoFicha.RUBI : TipoFicha.PERLA);

            return robadas.Count; // retorno cantidad de fichas contrarias robadas
        } // fin de simularJugada


        // deshace una jugada en el tablero de acuerdo al tipo de movimiento, los
        // puntos iniciales y finales y la ficha que esta de turno
        private void DeshacerJugada(string tipo, Point Po, Point Pf, TipoFicha fichaTurno, List<Point> robadas)
        {
            // si el movimiento es de salto, restablezco la ficha en la posicion inicial
            if (tipo == "saltar")
                this.casillas[Po.x][Po.y].Tipo = fichaTurno;


            //  en la casilla seleccionada a realizar el movimiento la restablezco como vacia
            this.casillas[Pf.x][Pf.y].Tipo = TipoFicha.VACIO;

            // cambio las fichas robadas a su ficha original
            foreach (var robada in robadas)
                this.casillas[robada.x][robada.y].Tipo = (fichaTurno == TipoFicha.RUBI ? TipoFicha.PERLA : TipoFicha.RUBI);


            robadas.Clear(); // limpio la lista de robadas 
        } // fin de simularJugada


        public void EliminarCanalComunicaciones()
        {
            // si la partida no es local
            if(!this.datos.PartidaLocal)
                Destroy(GameObject.Find("Comunicaciones"));
        }
    } // fin de la clase Juego
} // fin del espacio de nombres de Juego