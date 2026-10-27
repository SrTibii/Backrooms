using UnityEngine;
using System.IO.Ports;

public class ArduinoManager : MonoBehaviour
{
    public static ArduinoManager Instance;

    [Header("Configuración Serial")]
    public string puertoCOM = "COM4"; // Recuerda poner el COM correcto del Arduino Uno
    public int baudrate = 115200;

    [Header("Configuración Joysticks")]
    public float deadzone = 0.15f;

    // ==========================================
    // ENUMS PARA SELECCIONAR EJES EN EL INSPECTOR
    // ==========================================
    public enum EjeJoystick { X, Y, InvertirX, InvertirY }

    [Header("Mapeo de Movimiento (Joystick Izquierdo)")]
    public EjeJoystick movimientoHorizontal = EjeJoystick.X;
    public EjeJoystick movimientoVertical = EjeJoystick.Y;

    [Header("Mapeo de Cámara (Joystick Derecho)")]
    public EjeJoystick camaraHorizontal = EjeJoystick.X;
    public EjeJoystick camaraVertical = EjeJoystick.Y;

    public Vector2 moveInput;
    public Vector2 lookInput;

    private SerialPort serialPort;
    private bool puertoAbierto = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        try
        {
            serialPort = new SerialPort(puertoCOM, baudrate);
            serialPort.ReadTimeout = 50;
            serialPort.Open();
            puertoAbierto = true;
            Debug.Log("? ¡CONECTADO AL PUERTO " + puertoCOM + "!");
        }
        catch (System.Exception e)
        {
            Debug.LogError("? ERROR AL ABRIR EL PUERTO: " + e.Message);
        }
    }

    void Update()
    {
        if (puertoAbierto && serialPort != null && serialPort.IsOpen)
        {
            try
            {
                string data = serialPort.ReadLine();

                if (data.StartsWith("D"))
                {
                    string cleanData = data.Substring(1);
                    string[] valores = cleanData.Split(',');

                    if (valores.Length == 4)
                    {
                        if (int.TryParse(valores[0], out int xIzq) &&
                            int.TryParse(valores[1], out int yIzq) &&
                            int.TryParse(valores[2], out int xDer) &&
                            int.TryParse(valores[3], out int yDer))
                        {
                            // Normalizamos los valores (0-1023 a -1 a 1)
                            float nX_Izq = (xIzq - 512) / 512f;
                            float nY_Izq = (yIzq - 512) / 512f;
                            float nX_Der = (xDer - 512) / 512f;
                            float nY_Der = (yDer - 512) / 512f;

                            // Aplicar Deadzone
                            nX_Izq = Mathf.Abs(nX_Izq) < deadzone ? 0 : nX_Izq;
                            nY_Izq = Mathf.Abs(nY_Izq) < deadzone ? 0 : nY_Izq;
                            nX_Der = Mathf.Abs(nX_Der) < deadzone ? 0 : nX_Der;
                            nY_Der = Mathf.Abs(nY_Der) < deadzone ? 0 : nY_Der;

                            // Asignar valores según el desplegable del Inspector
                            moveInput = new Vector2(
                                ObtenerValor(movimientoHorizontal, nX_Izq, nY_Izq),
                                ObtenerValor(movimientoVertical, nX_Izq, nY_Izq)
                            );

                            lookInput = new Vector2(
                                ObtenerValor(camaraHorizontal, nX_Der, nY_Der),
                                ObtenerValor(camaraVertical, nX_Der, nY_Der)
                            );
                        }
                    }
                }
            }
            catch (System.TimeoutException) { }
            catch (System.Exception e)
            {
                Debug.LogWarning("Error leyendo datos: " + e.Message);
            }
        }
    }

    // Función auxiliar para traducir los desplegables a valores
    private float ObtenerValor(EjeJoystick eje, float valX, float valY)
    {
        switch (eje)
        {
            case EjeJoystick.X: return valX;
            case EjeJoystick.Y: return valY;
            case EjeJoystick.InvertirX: return -valX;
            case EjeJoystick.InvertirY: return -valY;
            default: return 0;
        }
    }

    void OnDestroy()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
            Debug.Log("Puerto cerrado.");
        }
    }
}