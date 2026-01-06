using UnityEngine;
using UnityEngine.UI;

public class MeetSelectionScript : MonoBehaviour
{
    [Header("Panels")]
    public GameObject Meet;
    public GameObject Autowin;
    public GameObject Setting;
    public GameObject SelectStyle;
    public GameObject Level;

    private void disablePanel ()
    {
        Meet.SetActive(false);
        Autowin.SetActive(false);
        Setting.SetActive(false);
        SelectStyle.SetActive(false);
        Level.SetActive(false);
    }

    public void OpenMeet ()
    {
        disablePanel();
        Meet.SetActive(true);
    }
    public void OpenAutoWin ()
    {
        disablePanel();
        Autowin.SetActive(true);
    }
    public void OpenSetting ()
    {
        disablePanel();
        Setting.SetActive(true);
    }
    public void OpenSelectStyle ()
    {
        disablePanel();
        SelectStyle.SetActive(true);
    }
    public void OpenLevel ()
    {
        disablePanel();
        Level.SetActive(true);
    }
}