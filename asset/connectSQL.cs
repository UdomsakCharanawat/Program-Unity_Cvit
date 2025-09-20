using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class connectSQL : MonoBehaviour
{
    public InputQR inputQR;
    public GameManager gameManager;
    public BonusTime bonusTime;
    public string _date;
    public string qrCode;
    public int score;
    public string bonus;


    private void Awake()
    {
        //inputQR = this.gameObject.GetComponent<InputQR>();
    }


    public void GetValue()
    {
        qrCode = inputQR.getQr;
        score = gameManager.score;
        bonus = bonusTime.getBonus.ToString();
        StartCoroutine(UploadDataToServer());
    }
    public IEnumerator UploadDataToServer()
    {
        _date = System.DateTime.Now.ToString();
      

        WWWForm form = new WWWForm();
        form.AddField("_date", _date);
        form.AddField("qrCode", qrCode);
        form.AddField("score", score);
        form.AddField("bonus", bonus);

        WWW www = new WWW("http://cvitt.ids.co.th/cvitt.php", form);
        yield return www;

        if(www.text != "0")
        {
            Debug.Log("user created successfully.");
        }
        else
        {
            Debug.Log("user creation failed. Error #" + www.text);
        }
    }

}
