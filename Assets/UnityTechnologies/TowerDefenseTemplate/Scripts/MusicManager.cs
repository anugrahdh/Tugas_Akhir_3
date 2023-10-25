using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public string sceneType;

    public AudioClip levelMusic;
    public AudioClip menuMusic;

    public static MusicManager instance;
    private void Awake()
    {
        if(MusicManager.instance)
        {
            if (MusicManager.instance != this)
                Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
        DontDestroyOnLoad(this.gameObject);
    }
    void OnEnable()
    {
        //Tell our 'OnLevelFinishedLoading' function to start listening for a scene change as soon as this script is enabled.
        SceneManager.sceneLoaded += OnLevelFinishedLoading;
    }

    void OnDisable()
    {
        //Tell our 'OnLevelFinishedLoading' function to stop listening for a scene change as soon as this script is disabled. Remember to always have an unsubscription for every delegate you subscribe to!
        SceneManager.sceneLoaded -= OnLevelFinishedLoading;
    }
    void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
    {
        if(scene.name.Contains("Level"))
        {
            if (sceneType == "Menu")
            {
                GetComponent<AudioSource>().clip = levelMusic;
                GetComponent<AudioSource>().Stop();
                GetComponent<AudioSource>().Play();
            }
            else
            {
                GetComponent<AudioSource>().clip = levelMusic;
                GetComponent<AudioSource>().Stop();
                GetComponent<AudioSource>().Play();
            }
            sceneType = "Level";
        }
        else
        {
            if (sceneType == "Menu")
            {
                //GetComponent<AudioSource>().clip = levelMusic;
                //GetComponent<AudioSource>().Play();
            }
            else
            {
                GetComponent<AudioSource>().clip = menuMusic;
                GetComponent<AudioSource>().Stop();
                GetComponent<AudioSource>().Play();
            }
            sceneType = "Menu";
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
