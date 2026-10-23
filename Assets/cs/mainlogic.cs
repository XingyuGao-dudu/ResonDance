using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class SoundData
{
    public AudioClip ac;
    public Sprite sp;
    public string name;
}

public class mainlogic : MonoBehaviour
{
    public Text songTitleText;
    public Image albumArtImage;
    public Button playButton;
    public Button prevButton;
    public Button nextButton;
    public Button menuButton;
    public Button menuHideButton;
    public GameObject MenuPanel;

    public GameObject itemPrefab;
    public Transform gridLayoutParent;

    private AudioSource audioSource;
    //private List<AudioClip> audioClips = new List<AudioClip>();
    //private List<Sprite> albumArtSprites = new List<Sprite>();

    
    public List<SoundData> SoundDatas = new List<SoundData>();

    private int currentSongIndex = 0;
    private bool isPlaying = false;

    private AudioProcessor audioProcessor;
    private BleThing bleThing;

    void Start()
    {
        MenuPanel.SetActive(false);
        audioSource = GetComponent <AudioSource>();
        audioProcessor = GetComponent <AudioProcessor>();
        bleThing = GetComponent <BleThing>();
        //LoadSongs();
        DisplayCurrentSongInfo();
        PopulateGrid();

        playButton.onClick.AddListener(TogglePlay);
        prevButton.onClick.AddListener(PlayPreviousSong);
        nextButton.onClick.AddListener(PlayNextSong);
        menuButton.onClick.AddListener(ShowMenu);
        menuHideButton.onClick.AddListener(HideMenu);
    }

    void LoadSongs()
    {
        Debug.LogWarning("LoadSongs");
        string[] mp3Files = Directory.GetFiles(Application.streamingAssetsPath, "*.wav");
        foreach (string mp3File in mp3Files)
        {
            Debug.LogWarning(mp3File);
            string pngFile = Path.ChangeExtension(mp3File, ".png");
            if (File.Exists(pngFile))
            {
                byte[] audioData = File.ReadAllBytes(mp3File);
                //AudioClip audioClip = WavUtility.ToAudioClip(audioData, Path.GetFileNameWithoutExtension(mp3File));

                AudioClip audioClip = ConvertBytesToAudioClip(audioData);
                if (audioClip != null)
                {
                    SoundData sd = new SoundData();
                    sd.ac = audioClip;
                    sd.sp = LoadTexture(pngFile);
                    sd.name = Path.GetFileNameWithoutExtension(mp3File);
                    SoundDatas.Add(sd);
                    //albumArtSprites.Add(LoadTexture(pngFile));
                }
                else
                {
                    Debug.LogWarning("Failed to load audio clip: " + mp3File);
                }
            }
            else
            {
                Debug.LogWarning("No corresponding PNG file found for: " + mp3File);
            }
        }
    }

    private AudioClip ConvertBytesToAudioClip(byte[] audioData)
    {
        // 创建临时的 WAV 文件路径
        string tempPath = Path.Combine(Application.persistentDataPath, "temp.wav");

        // 将 MP3 数据写入临时的 WAV 文件
        File.WriteAllBytes(tempPath, audioData);

        // 加载临时的 WAV 文件为 AudioClip
        WWW www = new WWW("file://" + tempPath);
        while (!www.isDone) { }

        // 检查是否有错误
        if (!string.IsNullOrEmpty(www.error))
        {
            Debug.LogError("Failed to load audio data: " + www.error);
            return null;
        }

        // 返回 AudioClip 对象
        return www.GetAudioClip(false, false, AudioType.WAV);
    }

    Sprite LoadTexture(string filePath)
    {
        Texture2D tex = new Texture2D(512, 512);
        byte[] fileData = File.ReadAllBytes(filePath);
        tex.LoadImage(fileData);
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
    }

    void DisplayCurrentSongInfo()
    {
        if (SoundDatas.Count > 0)
        {
            songTitleText.text = SoundDatas[currentSongIndex].name;
            albumArtImage.sprite = SoundDatas[currentSongIndex].sp;
        }
        else
        {
            songTitleText.text = "No songs found";
        }
    }

    void ShowMenu()
    {
        MenuPanel.SetActive(true);
    }

    void HideMenu()
    {
        MenuPanel.SetActive(false);
    }

    void TogglePlay()
    {
        Debug.Log("TogglePlay");

        if (SoundDatas.Count == 0)
            return;

        isPlaying = !isPlaying;

        if (isPlaying)
        {
            audioSource.clip = SoundDatas[currentSongIndex].ac;
            audioProcessor.Dotart();
            audioSource.Play();
            playButton.GetComponentInChildren<Text>().text = "点击停止";
        }
        else
        {
            audioSource.Stop();
            playButton.GetComponentInChildren<Text>().text = "点击起舞";
        }
    }

    void PlayPreviousSong()
    {
        currentSongIndex--;
        if (currentSongIndex < 0)
            currentSongIndex = SoundDatas.Count - 1;

        if (isPlaying)
        {
            audioSource.Stop();
            audioSource.clip = SoundDatas[currentSongIndex].ac;
            audioProcessor.Dotart();
            audioSource.Play();
        }

        DisplayCurrentSongInfo();
    }

    void PlayNextSong()
    {
        currentSongIndex++;
        if (currentSongIndex >= SoundDatas.Count)
            currentSongIndex = 0;

        if (isPlaying)
        {
            audioSource.Stop();
            audioSource.clip = SoundDatas[currentSongIndex].ac;
            audioProcessor.Dotart();
            audioSource.Play();
        }

        DisplayCurrentSongInfo();
    }

    void PopulateGrid()
    {
        for (int i = 0; i < SoundDatas.Count; i++)
        {
            SoundData soundData = SoundDatas[i];

            GameObject newItem = Instantiate(itemPrefab, gridLayoutParent);
            newItem.transform.SetParent(gridLayoutParent, false);

            // 设置图像
            Image image = newItem.transform.Find("Image").GetComponent<Image>();
            image.sprite = soundData.sp;

            // 设置名称
            Text nameText = newItem.transform.Find("Name").GetComponent<Text>();
            nameText.text = soundData.name;

            // 设置按钮点击事件
            int id = new();
            id = i;
            Button playBtn = newItem.transform.Find("PlayBtn").GetComponent<Button>();
            playBtn.onClick.AddListener(() => PlaySound(id));
        }
    }

    void PlaySound(int id)
    {
        Debug.Log("Playing sound: " + id);
        // 在这里添加播放音频的逻辑

        currentSongIndex = id;

        if (isPlaying)
        {
            audioSource.Stop();
            audioSource.clip = SoundDatas[currentSongIndex].ac;
            audioProcessor.Dotart();
            audioSource.Play();
        }

        DisplayCurrentSongInfo();

        HideMenu();
    }

    int ttt = 0;
    public void OnBeat()
    {
        bleThing.OnLED();
        //Debug.Log("OnBeat: " + ttt++);
        
    }
}
