using UnityEngine;
using System.IO.Ports;

public class ArduinoManager : MonoBehaviour
{
    public static ArduinoManager Instance;

    [Header("Configuración Serial")]
    public string puertoCOM = "COM3";
    public int baudrate = 115200;

    [Header("Configuración Joysticks")]
    public float deadzone = 0.15f;
    [Range(0f, 1f)] public float umbralSprint = 0.85f;

    public enum EjeJoystick { X, Y, InvertirX, InvertirY }

    [Header("Mapeo de Movimiento")]
    public EjeJoystick movimientoHorizontal = EjeJoystick.X;
    public EjeJoystick movimientoVertical = EjeJoystick.Y;

    [Header("Mapeo de Cámara")]
    public EjeJoystick camaraHorizontal = EjeJoystick.X;
    public EjeJoystick camaraVertical = EjeJoystick.Y;

    public Vector2 moveInput;
    public Vector2 lookInput;
    public bool isSprinting;

    public bool clickIzquierdo;
    public bool clickDerecho;

    // Variables de los Botones
    public bool btnFlash;
    public bool btnZoom;
    public bool btnPause;
    public bool btnSkip;
    public bool btnAgacharse;

    private SerialPort serialPort;
    private bool puertoAbierto = false;

    // Esto avisará a los demás scripts de que se ha pulsado
    public bool clickDerechoPulsadoEsteFrame = false;

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

                    if (valores.Length == 12) // 4 ejes + 2 clics + 5 botones + 1 extra? No, son 11 datos. Arreglado a 11.
                    {
                        // CORRECCIÓN: Son 11 datos reales
                    }

                    // Como el Arduino manda 11 datos separados por coma:
                    if (valores.Length == 11)
                    {
                        if (int.TryParse(valores[0], out int xIzq) &&
                            int.TryParse(valores[1], out int yIzq) &&
                            int.TryParse(valores[2], out int xDer) &&
                            int.TryParse(valores[3], out int yDer) &&
                            int.TryParse(valores[4], out int cIzq) &&
                            int.TryParse(valores[5], out int cDer) &&
                            int.TryParse(valores[6], out int bFlash) &&
                            int.TryParse(valores[7], out int bZoom) &&
                            int.TryParse(valores[8], out int bPause) &&
                            int.TryParse(valores[9], out int bSkip) &&
                            int.TryParse(valores[10], out int bAgacharse))
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

                            isSprinting = moveInput.magnitude > umbralSprint;

                            clickIzquierdo = (cIzq == 1);
                            clickDerecho = (cDer == 1);

                            // NUEVO: Avisar a la puerta final directamente si estamos pulsando
                            if (clickDerecho)
                            {
                                PuertaFinal puerta = FindObjectOfType<PuertaFinal>();
                                if (puerta != null)
                                {
                                    puerta.RecibirClicDerecho();
                                }
                            }

                            btnFlash = (bFlash == 1);
                            btnZoom = (bZoom == 1);
                            btnPause = (bPause == 1);
                            btnSkip = (bSkip == 1);
                            btnAgacharse = (bAgacharse == 1);
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

        // Al final del Update de ArduinoManager
        clickDerechoPulsadoEsteFrame = false;
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
        }
    }
}