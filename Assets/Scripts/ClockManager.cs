using UnityEngine;
using TMPro;
using System.Collections;

public class ClockManager : MonoBehaviour
{
    [Header("Referencias")]
    public TextMeshProUGUI hourDateText;

    [Header("Configuración del Reloj")]
    public int startHour = 3;
    public int startMinute = 33;
    public float minuteDuration = 60f;
    public bool autoStart = true;

    [Header("Formato")]
    public bool useAMPM = true;

    private int currentHour;
    private int currentMinute;
    private float timer = 0f;
    private bool isRunning = false;

    private VHSTextEffect vhsEffect;

    void Start()
    {
        if (hourDateText == null)
        {
            TextMeshProUGUI[] allTexts = GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var txt in allTexts)
            {
                if (txt.gameObject.name == "HourDate")
                {
                    hourDateText = txt;
                    break;
                }
            }
        }

        vhsEffect = GetComponent<VHSTextEffect>();

        ResetClock();

        if (autoStart)
        {
            StartClock();
        }
    }

    void Update()
    {
        if (!isRunning || hourDateText == null) return;

        timer += Time.deltaTime;

        if (timer >= minuteDuration)
        {
            timer -= minuteDuration;
            AddMinute();
        }
    }

    void AddMinute()
    {
        currentMinute++;

        if (currentMinute >= 60)
        {
            currentMinute = 33;
            currentHour++;

            if (currentHour > 3)
            {
                currentHour = startHour;
            }
        }

        UpdateClockText();
    }

    void UpdateClockText()
    {
        if (hourDateText == null) return;

        string hourStr = currentHour.ToString();
        string minuteStr = currentMinute.ToString("D2");

        string timeString = "";

        if (useAMPM)
        {
            string ampm = currentHour >= 12 ? "PM" : "AM";
            int displayHour = currentHour > 12 ? currentHour - 12 : currentHour;
            displayHour = displayHour == 0 ? 12 : displayHour;
            timeString = ampm + ". " + displayHour + ":" + minuteStr;
        }
        else
        {
            timeString = hourStr + ":" + minuteStr;
        }

        hourDateText.text = timeString;

        if (vhsEffect != null)
        {
            vhsEffect.OnTextChanged();
        }
    }

    public void ResetClock()
    {
        currentHour = startHour;
        currentMinute = startMinute;
        timer = 0f;
        UpdateClockText();
    }

    public void StartClock()
    {
        isRunning = true;
    }

    public void PauseClock()
    {
        isRunning = false;
    }

    public void ResumeClock()
    {
        isRunning = true;
    }

    public void SetSpeed(float newDuration)
    {
        minuteDuration = newDuration;
    }

    public string GetCurrentTime()
    {
        return currentHour + ":" + currentMinute.ToString("D2");
    }

    public int GetCurrentMinute()
    {
        return currentMinute;
    }

    public int GetCurrentHour()
    {
        return currentHour;
    }
}