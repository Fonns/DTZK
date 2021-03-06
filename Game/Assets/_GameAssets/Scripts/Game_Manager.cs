﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using UnityEngine.Networking;

public class Game_Manager : MonoBehaviour
{
    [SerializeField] private Transform[] spawns;
    [SerializeField] private GameObject zombiePrefab;
    [SerializeField] private GameObject mainCamera;
    [SerializeField] private GameObject gameOverCamera;
    [Space(10)]
    [SerializeField] private GameObject ui_PlayerHUDObject;
    [SerializeField] private GameObject ui_GameOverObject;
    [SerializeField] private GameObject ui_DamageIndicator;
    [Space(10)]
    [SerializeField] private TextMeshProUGUI ui_AmmoText;
    [SerializeField] private TextMeshProUGUI ui_AmmoReservText;
    [SerializeField] private TextMeshProUGUI ui_RoundText;
    [SerializeField] private TextMeshProUGUI ui_ScoreText;
    [SerializeField] private TextMeshProUGUI ui_HighscoreText;
    [SerializeField] private TextMeshProUGUI ui_SurvivedRounds;
    [SerializeField] private TextMeshProUGUI ui_ScoreAtEnd;
    [Space(10)]
    [SerializeField] private AudioSource EndRoundZombieSound;

    private int roundNumber;
    private int zombiesInMap;
    private int playerScore;
    private int avaiableScore;

    private bool hasSentToDB;

    private GameObject player;

    private bool spawningDone;

    private Game_zGunShoot gunShoot;
    private List<Class_Score> scores;

    public enum RoundState
    {
        Starting,
        OnGoing,
        Ending,
        Ended,
        PlayerDied
    }
    [Space(10)]
    public RoundState currentState;
    public Queue<Class_Score> scoreQueue;

    public static event Action<Game_ManagerUI.UIText> UITextChange;

    private void Start()
    {
        player = GameObject.Find("/Player");

        roundNumber = 1;
        playerScore = 0;
        avaiableScore = 0;
        zombiesInMap = 0;
        hasSentToDB = false;
        currentState = RoundState.Starting;

        scoreQueue = new Queue<Class_Score>();
        scores = new List<Class_Score>();

        UITextChange(Game_ManagerUI.UIText.score);
        UITextChange(Game_ManagerUI.UIText.round);
    }

    private void Update()
    {
        StateSwitcher();
        AddScore();
    }

    private void StateSwitcher()
    {
        switch (currentState)
        {
            case RoundState.Starting:

                RoundStart();
                currentState = RoundState.OnGoing;
                break;

            case RoundState.OnGoing:

                if (player.GetComponent<Game_PlayerHealth>().isPlayerDead()) currentState = RoundState.PlayerDied;
                if (HasRoundEnded()) currentState = RoundState.Ending;
                break;

            case RoundState.Ending:

                StartCoroutine(RoundEnding());
                currentState = RoundState.Ended;
                break;

            case RoundState.Ended:

                currentState = RoundState.Starting;
                break;

            case RoundState.PlayerDied:

                FPlayerHasDied();
                break;

            default:
                break;
        }
    }

    private void FPlayerHasDied()
    {
        if (!(PlayerPrefs.HasKey("PHighscore")) || (PlayerPrefs.GetInt("PHighscore") < playerScore))
        {
            PlayerPrefs.SetInt("PHighscore", playerScore);
            if (!hasSentToDB)
            {
                SendToDB();
                hasSentToDB = true;
            }
        }
        mainCamera.SetActive(false);
        player.GetComponent<Game_PlayerMovement>().enabled = false;
        player.GetComponent<Game_PlayerCamera>().enabled = false;
        player.GetComponent<Game_PlayerHealth>().enabled = false;
        gameOverCamera.SetActive(true);
        ui_PlayerHUDObject.SetActive(false);
        ui_DamageIndicator.SetActive(false);
        ui_GameOverObject.SetActive(true);
        UITextChange(Game_ManagerUI.UIText.highscore);
        UITextChange(Game_ManagerUI.UIText.finalRounds);
        UITextChange(Game_ManagerUI.UIText.finalScore);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private IEnumerator SpawnZombies(float zombiesToSpawn, float zombieHealth)
    {
        for (int i = (int)zombiesToSpawn; i > 0; i--)
        {
            zombiesInMap += 1;
            yield return new WaitForSeconds(3f);
            int spawnIndex = UnityEngine.Random.Range(0, spawns.Length);
            GameObject zombie = Instantiate(zombiePrefab, spawns[spawnIndex].position, new Quaternion());
            zombie.GetComponent<Game_ZombieHealth>().SetHealth(zombieHealth);
        }
        spawningDone = true;
    }

    private float ZombiesToSpawn()
    {
        if (roundNumber < 20)
        {
            int[] initialNumber = { 6, 8, 13, 18, 24, 27, 28, 28, 29, 33, 34, 36, 39, 41, 44, 47, 50, 53, 56 };
            return initialNumber[roundNumber - 1];
        }
        return Mathf.Round(.9f * roundNumber * roundNumber - .0029f * roundNumber + 23.958f);
    }

    private float ZombiesHealth()
    {
        if(roundNumber < 10)
        {
            int[] initialNumber = { 150, 250, 350, 450, 550, 650, 750, 850, 950 };
            return initialNumber[roundNumber - 1];
        }
        return 950 * Mathf.Pow(1.1f, roundNumber - 9);
    }

    public void DescreaseZombiesOnMap()
    {
        zombiesInMap -= 1;
    }

    private void RoundStart()
    {
        spawningDone = false;
        StartCoroutine(SpawnZombies(ZombiesToSpawn(), ZombiesHealth()));
    }

    private bool HasRoundEnded()
    {
        if ((zombiesInMap <= 0) && (spawningDone)) return true;
        return false;
    }

    private IEnumerator RoundEnding()
    {
        EndRoundZombieSound.Play();
        roundNumber += 1;
        UITextChange(Game_ManagerUI.UIText.round);
        yield return null;
    }

    public int GetRoundNumber()
    {
        return roundNumber;
    }

    public int GetAvaiableScore()
    {
        return avaiableScore;
    }

    public int GetFinalScore()
    {
        return playerScore;
    }

    private void AddScore()
    {
        if (scoreQueue.Count == 0) return;
        Class_Score scoreObj = scoreQueue.Dequeue();
        playerScore += scoreObj.scoreValue;
        avaiableScore += scoreObj.scoreValue;
        UITextChange(Game_ManagerUI.UIText.score);
        scores.Add(scoreObj);
    }

    public void AddToScoreQueue(Class_Score.ScoreID idParam, int scoreValueParam, string scoreDescParam)
    {
        scoreQueue.Enqueue(new Class_Score
        {
            id = idParam,
            scoreValue = scoreValueParam,
            scoreDesc = scoreDescParam
        });
    }

    public void RemoveScore(int scoreToRemove)
    {
        avaiableScore -= scoreToRemove;
        UITextChange(Game_ManagerUI.UIText.score);
    }

    public bool HasEnoughPoints(int toCompara)
    {
        if (toCompara <= avaiableScore) return true;
        return false;
    }

    public void PlayAgain()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(0);
    }

    private void SendToDB()
    {
        string jsonString = "{"+'"'+"gen_id"+'"'+':'+'"'+PlayerPrefs.GetString("genid")+'"'+','+
            '"'+"username"+'"'+':'+'"'+PlayerPrefs.GetString("Username")+ '"' + ','+
            '"' + "score" +'"'+':'+'"'+PlayerPrefs.GetInt("PHighscore")+'"'+','+
            '"' + "secret"+'"'+':'+'"'+ "REDACTED" + '"'+'}';
        StartCoroutine(Post("http://ts.aspesports.com:8080/dtzk/api/add", jsonString));
    }

    private IEnumerator Post(string url, string jsonString)
    {
        UnityWebRequest req = new UnityWebRequest(url, "POST");
        byte[] rawJsonString = System.Text.Encoding.UTF8.GetBytes(jsonString);
        req.uploadHandler = (UploadHandler) new UploadHandlerRaw(rawJsonString);
        req.downloadHandler = (DownloadHandler) new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();
    }

    public void PostToFacebook()
    {
        Application.OpenURL("http://www.facebook.com/sharer/sharer.php?u=https://github.com/Fonnnn/DTZK/releases/latest&quote=I just did " + playerScore + " points in DTZK by João Fonseca ( https://joaoffonseca.pt ). Check it out!&display=popup");
    }

    public void PostToTwitter()
    {
        Application.OpenURL("https://twitter.com/intent/tweet?url=https://github.com/Fonnnn/DTZK/releases/latest&text=I just did " + playerScore + " points in DTZK by @ASPFon . Check it out!&hashtags=dtzk&related=ASPFon");
    }
}
