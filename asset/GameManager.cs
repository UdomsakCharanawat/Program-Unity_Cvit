using System.Collections;
//using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using MoodMe;
using UnityEditor;
//using UnityEngine.SceneManagement;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager current;
    public EmotionsManager emotionsManager;
    public FaceDetector faceDetector;
    public CameraManager cameraManager;
    public BonusTime bonusTime;
    public connectSQL connectsql;
    public SelectFrame selectFrame;
    //public TimeOut timeOut;
    // public connectVending scriptConnectVending;
    public SendToVending sendToVending;
    public float happy;
    public float sum;
    public int countdownTime;
    private int currentTime;
    public int score; //Save to database  
    public bool check;
    public GameObject spacialBox;
    public InputQR inputQr;
    public GameObject sceneSelectFrame;
    public GameObject sceneTakePhoto;
    public GameObject screensaver;
    public GameObject countDown;
    public AudioSource soundCountdown;
    public AudioSource soundFlash;
    public AudioSource soundPrint;
    public AudioSource soundBGPlay;
    public Image imCountDown;
    public Sprite[] spCountDown;
    public GameObject capture;
    public Button btCapture;
    public RawImage photo;
    public RenderTexture renderTexture;

    public Animator animBottomP3;
    public string[] productID;
    public GameObject scencSmile;
    public GameObject bottomP2;

    public Image flash;
    public ParticleSystem effect;
    //private bool f = false;

    // public float timeForWait;
    //public float _currentTime;
    //public bool start = false;

    public Image frame;
    public Sprite[] spFrame;

    public GameObject[] groupDisable;
    public GUIStyle guiStyle;
    public bool guiShow;
    public string setScore;
    public string score1, score2, score3;
    

    // Start is called before the first frame update
    void Start()
    {
        effect.Stop();
        current = this;
        spacialBox.SetActive(false);
        inputQr = GameObject.Find("InputQR").GetComponent<InputQR>();
        screensaver.SetActive(true);
        sceneSelectFrame.SetActive(false);
        sceneTakePhoto.SetActive(false);

        for(int i = 0; i < groupDisable.Length; i++) groupDisable[i].SetActive(false);

        setNewScore();
    }

    public void setNewScore()
    {
        string tempSetScore = File.ReadAllText(Application.dataPath + "/../score.txt");
        string[] tempScore = tempSetScore.Split(',');
        score1 = tempScore[0];
        score2 = tempScore[1];
        score3 = tempScore[2];
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (!guiShow) guiShow = true;
            else guiShow = false;
        }

        if (Input.GetKeyDown(KeyCode.S)) setNewScore();
    }

    public void openSelectFrame() 
    {
        screensaver.SetActive(false);
        sceneSelectFrame.SetActive(true);
    }

    public void OpenSceneTakePhoto()
    {
        for (int i = 0; i < groupDisable.Length; i++) groupDisable[i].SetActive(true);

        // Debug.Log("OpenSceneTakePhoto");
        soundBGPlay.Play();
        // timeOut.showTime = true;
        RandomFrame();
        btCapture.enabled = true;
        //screensaver.SetActive(false);
        sceneSelectFrame.SetActive(false);
        sceneTakePhoto.SetActive(true);
        countDown.SetActive(false);
        capture.SetActive(true);
        bottomP2.SetActive(true);
        scencSmile.SetActive(false);

    }
    public void RandomFrame()
    {
        frame.sprite = spFrame[selectFrame.statusFrame - 1];

        /*float r = UnityEngine.Random.value;
        if (r > 0.2) //%80 percent chance (1 - 0.2 is 0.8)
        {
            float f = UnityEngine.Random.value;
            if (f > 0.5)
            {
                frame.sprite = spFrame[0];
            }
            else
            {
                frame.sprite = spFrame[1];
            }
        }
        else
        {
            frame.sprite = spFrame[2];
        }*/
    }

    public void ButtonCapture()
    {
        btCapture.enabled = false;
        StartCoroutine(CountdownTakePhoto());
    }

    private IEnumerator CountdownTakePhoto()
    {
        countDown.SetActive(true);
        currentTime = countdownTime;
        soundCountdown.Play();
        while (currentTime > 0)
        {
            currentTime--;
            imCountDown.sprite = spCountDown[currentTime];
            yield return new WaitForSeconds(1f);

        }
        countDown.SetActive(false);
        StartCoroutine(FlashLight());
    }

    IEnumerator FlashLight()
    {
        soundFlash.Play();
        effect.Play();
        yield return new WaitForSeconds(0.15f);
        cameraManager.PauseCamare();
        flash.color = new Color(1, 1, 1, 0.5f);
        yield return new WaitForSeconds(0.05f);
        flash.color = new Color(1, 1, 1, 0);
        yield return new WaitForSeconds(0.25f);
        faceDetector.ExportCrop = false;
        ScreenShot.TakeScreenshot_Static(1080, 720);
        happy = emotionsManager.Happy;
        print(happy);
        check = bonusTime.getCountBonusInHour();
        StartCoroutine(WaitForCheckSmile());
    }

    IEnumerator WaitForCheckSmile()
    {
        yield return new WaitForSeconds(3);
        LevelSmile();
    }


    private void LevelSmile()
    {
        float s = happy;
        int ii = 0;
        
        /*if (s > 0.6)
        {
            float r = UnityEngine.Random.value;
            if (r > 0.4)
            {
                score = 3;
                ii = 2;
                //SetTextLevelSmile(2);
            }
            else
            {
                score = 2;
                ii = 1;
                // SetTextLevelSmile(1);
            }
        }
        else
        {
            score = 1;
            ii = 0;
            //SetTextLevelSmile(0);
        }*/

        if (s >= float.Parse(score1.ToString()) && s < float.Parse(score2.ToString()))
        {
            score = 1;
            ii = 0;
        }
        else if(s >= float.Parse(score2.ToString()) && s < float.Parse(score3.ToString()))
        {
            score = 2;
            ii = 1;
        }
        else if(s >= float.Parse(score3.ToString()))
        {
            score = 3;
            ii = 2;
        }
        SetTextLevelSmile(ii);

    }

    private void SetTextLevelSmile(int id)
    {
        bottomP2.SetActive(false);
        scencSmile.SetActive(true);
        animBottomP3.SetInteger("id", id);
        capture.SetActive(false);
        StopAllCoroutines();
        StartCoroutine(CheckBonus(id));
    }
    IEnumerator CheckBonus(int idProduct)
    {
        sendToVending.SendId(idProduct);
        connectsql.GetValue();
        PrintPicture();   //Print
        soundPrint.Play();
        yield return new WaitForSeconds(30);
        if (check)
        {
            check = false;
            spacialBox.SetActive(true);
        }
        else
        {
            BackToScreenSaver();
        }
       
    }

    static void PrintPicture()
    {
        string path = Application.dataPath + "/../" + "/Photo/CameraScreenshot.png";
        string path2 = @"C:\FolderMill Data\Hot Folders\1\Incoming\CameraScreenshot.png";
        byte[] bytes = File.ReadAllBytes(path);

        Texture2D texture = new Texture2D(1080, 720, TextureFormat.ARGB32, false);
        texture.LoadImage(bytes);
        File.WriteAllBytes(path2, bytes);

    }

    public void ResetAll()
    {
        soundBGPlay.Pause();
        //timeOut.showTime = false;
        //timeOut.currentTime = timeOut.timeForWait;
        cameraManager.PlayCamare();
        faceDetector.ExportCrop = true;
        spacialBox.SetActive(false);
        animBottomP3.SetInteger("id", -1);
        ScreenShot.myCamera.targetTexture = renderTexture;
        scencSmile.SetActive(false);
        spacialBox.SetActive(false);
        sceneTakePhoto.SetActive(false);
        screensaver.SetActive(true);
        inputQr.playing = false;

    }
    public void BackToScreenSaver()
    {
        ResetAll();

    }

    private void OnGUI()
    {
        if (guiShow)
        {
            GUI.Label(new Rect(20, 20, 100, 100), emotionsManager.Happy.ToString(), guiStyle);
            GUI.Label(new Rect(20, 60, 100, 100), "score 1 : " + score1, guiStyle);
            GUI.Label(new Rect(20, 100, 100, 100), "score 2 : " + score2, guiStyle);
            GUI.Label(new Rect(20, 140, 100, 100), "score 3 : " + score3, guiStyle);
        }
    }
}
