using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Net.Sockets;
using System.Net;
using System;
using System.Text;
using System.IO;
using System.Threading;


namespace Juego
{
    public class Comunicaciones : MonoBehaviour
    {
        public Text mensaje; // muestra al cliente un mensaje del estado de la coneccion
        public Toggle servidorSel; // selector de la GUI
        public InputField inputIP; // direccion IP ingresada por el usuario 
        public GameObject controlEscena; // carga nuevas escenas 
        public bool Servidor { get; set; } // determina si el Juego debe actuar como servidor o no(cliente)
        TcpListener servidor; // escucha peticiones de conexion
        TcpClient cliente; // cliente para conexiones  
        NetworkStream stream; // flujo de comunicaciones
        byte[] datos; // representacion para los datos 
        string IP; // la direccion ip del equipo que ejecuta el juego 
        Thread hiloRecepcion; // proceso para la recepcion de datos 
        public Movimiento jugada; // almacena las jugadas en las comunicaciones 
        public bool jugadaRecibida; // dice si ya se recibio o no una Jugada


        void Awake()
        {
            DontDestroyOnLoad(this.gameObject);
        }

            // inicializa los atributos de las comunicaciones
            void Star()
        {

            this.Servidor = false;
            this.servidor = null;
            this.cliente = null;
            this.stream = null;
            this.datos = new byte[256];
            this.hiloRecepcion = null;
            this.jugadaRecibida = false;
        } // fin de Awake


        // se llama para cada frame 
        void Update()
        {
            // si ya se creo el servidor y hay una solicitud de conexion
            if (servidor != null && servidor.Pending())
            {
                Debug.Log("se conecto un cliente");
                this.cliente = servidor.AcceptTcpClient(); // acepto el cliente 
                this.stream = this.cliente.GetStream(); // obtengo el flujo de datos
                stream.ReadTimeout = 100; // establezco el tiempo maximo para una lectura

                IPEndPoint nuevocliente = (IPEndPoint)this.cliente.Client.RemoteEndPoint;
                this.mensaje.text = "Conectado: " + nuevocliente.Address.ToString() +
                    ", Puerto" + nuevocliente.Port;


                this.controlEscena.GetComponent<ControlEscena>().CargarNuevaEscena("Juego");
            } // fin del if


            // si el cliente ya se creo y aun no se ha iniciado el hilo de recepcion de datos
            if( this.cliente != null && hiloRecepcion == null )
            {
                try
                {
  //                  Debug.Log("Esperando Jugaadas");
                    this.datos = new byte[256]; // doy tamaño para leer datos 
                    int x = this.stream.Read(this.datos, 0, this.datos.Length); // leo datos
                    string jugadaJSON = Encoding.ASCII.GetString(this.datos, 0, x); // obtengo JSON
                    this.jugada = JsonUtility.FromJson<Movimiento>(jugadaJSON); // deserializo 
                    jugadaRecibida = true; // recibi una jugada
                                           //                   Debug.Log("Jugada recibida");
                    Debug.Log("Recibida: " + jugadaJSON);
                }
                catch (IOException)
                {
   //                 Debug.Log("No ha llegado Jugada");
                }
                /*
                // creo hilo de recepcion de datos 
                this.hiloRecepcion = new Thread(o =>
                {
                    while (true)
                    {
                        Debug.Log("Esperando Jugaadas");
                        this.datos = new byte[256]; // doy tamaño para leer datos 
                        int x = this.stream.Read(this.datos, 0, this.datos.Length); // leo datos
                        string jugadaJSON = Encoding.ASCII.GetString(this.datos, 0, x); // obtengo JSON
                        this.jugada = JsonUtility.FromJson<Movimiento>(jugadaJSON); // deserializo 
                        jugadaRecibida = true; // recibi una jugada
                        Debug.Log("Jugada recibida");
                    } // fin del while 
                }); // fin del proceso 

                this.hiloRecepcion.Start(); // inicio el proceso para la recepcion de datos */
            }
        } // fin de Update


        // establece si se aloja o no una partida
        public void establecerServidor()
        {
            this.Servidor = servidorSel.isOn;
        } // fin de establecerServidor 


        // inicia las comunicaciones
        public void iniciarComunicaciones()
        {
            // si se escojio alojar una partida
            if (Servidor)
            {
                // manejo las posibles excepciones
                try
                {
                    this.IP = this.ObtenerIPAddress(); // obtiene la direccion del equipo
                    IPAddress ipAddress = IPAddress.Parse(this.IP); // creo repreentacion de la IP del equipo
                    this.servidor = new TcpListener(ipAddress, 5000); // creo servidor 
                    servidor.Start(); // inicio el servidor para que escuche 
                    this.mensaje.text = "Esperando Conexciones..."; // informa al usuario el estado de la coneccion
                }
                catch (Exception e)
                {
                    this.mensaje.text = e.Message; // mensaje de error al usuario
                } // fin del try...catch
            }
            else
            {
                // manejo las posibles excepciones
                try
                {
                    this.cliente = new TcpClient(this.inputIP.text, 5000); // solicito conexion 
                    this.mensaje.text = "Conectado..."; // aviso al usuario que la conexion fue exitosa
                    this.stream = cliente.GetStream(); // obtengo flujo de datos 
                    stream.ReadTimeout = 100; // establezco el tiempo maximo para una lectura
                    this.controlEscena.GetComponent<ControlEscena>().CargarNuevaEscena("Juego");
                }
                catch (Exception e)
                {
                    this.mensaje.text = e.Message; // mensaje de error al usuario
                } // fin del try...catch
            } // fin del if...else
        } // iniciarComunicaciones 


        // retorna la direccion ip del equipo que ejecuta el juego
        private string ObtenerIPAddress()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("10.0.2.4", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint.Address.ToString();
            } // fin del using 
        } // getIPAdress


        // envia una jugada al cliente
        public void enviarJugada(string tipo, int[] nodos)
        {
            Movimiento jugada = new Movimiento(tipo, nodos); // creo la jugada 
            string jugadaJSON = JsonUtility.ToJson(jugada); // serializo la jugada 

  //          Debug.Log("Voy a enviar: " + jugadaJSON);
            // manejo las posibles excepciones
            try
            {
                this.datos = Encoding.ASCII.GetBytes(jugadaJSON); // obtengo los bytes de la jugada
                this.stream.Write(this.datos, 0, this.datos.Length); // envio 
            }
            catch (IOException e)
            {
                Debug.Log(e.Message);
            } // fin del try...catch

            //    Debug.Log("Jugada enviada");
            Debug.Log("Enviada: " + jugadaJSON);
        } // fin de enviarJugada


        // imprime los datos por propositos de depuracion
        public void imprimir()
        {
            Debug.Log("Servidor: " + Servidor);
        } // fin de imprimir
    } // fin de Comunicaciones
} // fin del espacio de nombres del Juego
