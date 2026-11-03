using UnityEngine;
using System.IO.Ports;

public class ArduinoManager : MonoBehaviour
{
    public static ArduinoManager Instance;

    [Header("Configuración Serial")]
    public string puertoCOM = "COM3"; // Asegúrate de que es tu puerto
    public int baudrate = 115200;

    [Header("Configuración Joysticks")]
    public float deadzone = 0.15f;

    public enum EjeJoystick { X, Y, InvertirX, InvertirY }

    [Header("Mapeo de Movimiento")]
    public EjeJoystick movimientoHorizontal = EjeJoystick.X;
    public EjeJoystick movimientoVertical = EjeJoystick.Y;

    [Header("Mapeo de Cámara")]
    public EjeJoystick camaraHorizontal = EjeJoystick.X;
    public EjeJoystick camaraVertical = EjeJoystick.Y;

    // Variables de salida
    public Vector2 moveInput;
    public Vector2 lookInput;

    // NUEVAS VARIABLES: Clics de los joysticks
    public bool clickIzquierdo;
    public bool clickDerecho;

    // NUEVA VARIABLE PARA EL SPRINT
    public bool isSprinting;

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

                    // AHORA ESPERAMOS 6 VALORES
                    if (valores.Length == 6)
                    {
                        if (int.TryParse(valores[0], out int xIzq) &&
                            int.TryParse(valores[1], out int yIzq) &&
                            int.TryParse(valores[2], out int xDer) &&
                            int.TryParse(valores[3], out int yDer) &&
                            int.TryParse(valores[4], out int cIzq) &&
                            int.TryParse(valores[5], out int cDer))
                        {
                            float nX = (xIzq - 512) / 512f;
                            float nY = (yIzq - 512) / 512f;
                            float nZ = (xDer - 512) / 512f;
                            float nW = (yDer - 512) / 512f;

                            nX = Mathf.Abs(nX) < deadzone ? 0 : nX;
                            nY = Mathf.Abs(nY) < deadzone ? 0 : nY;
                            nZ = Mathf.Abs(nZ) < deadzone ? 0 : nZ;
                            nW = Mathf.Abs(nW) < deadzone ? 0 : nW;

                            moveInput = new Vector2(
                                ObtenerValor(movimientoHorizontal, nX, nY),
                                ObtenerValor(movimientoVertical, nX, nY)
                            );

                            lookInput = new Vector2(
                                ObtenerValor(camaraHorizontal, nZ, nW),
                                ObtenerValor(camaraVertical, nZ, nW)
                            );

                            // Asignamos los clics (1 es true, 0 es false)
                            clickIzquierdo = (cIzq == 1);
                            clickDerecho = (cDer == 1);

                            // NUEVO: Calcular si estamos haciendo sprint
                            // Si la longitud del vector de movimiento es mayor a 0.85, corremos.
                            // (Puedes ajustar el 0.85 si quieres que corra antes o después).
                            isSprinting = moveInput.magnitude > 0.85f;
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