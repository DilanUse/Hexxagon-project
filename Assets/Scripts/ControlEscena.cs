using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;


// se encarga de administrar el control de las escenas del juego
public class ControlEscena : MonoBehaviour
{
    // carga una nueva escena, quitando todas las demas
    public void CargarNuevaEscena(string nomScene)
    {
        SceneManager.LoadScene(nomScene, LoadSceneMode.Single);
    } // fin de CargarNuevaEscena


    // agrega una escena sin quitar las demas
    public void AgregarEscena(string nomScene)
    {
        SceneManager.LoadScene(nomScene, LoadSceneMode.Additive);
    } // fin de AgregarEscena


    // quita una escena del juego
    public void QuitarEscene(string nomScene)
    {
        SceneManager.UnloadScene(nomScene);
    } // fin de QuitarEscena 
} // fin de ControlEscena 
