using System;
using System.IO;
using UnityEngine;
using TMPro;
using UniRx;

public class Manager : MonoBehaviour
{
    // キャラクターのスケルトン
    [SerializeField]
    private Animator animator;
    // OSC
    [SerializeField]
    private OscServer oscServer;
    [SerializeField]
    private OscClient oscClient;
    // UI
    [SerializeField]
    private TMP_Text versionText;
    [SerializeField]
    private TMP_InputField receivePort;
    [SerializeField]
    private TMP_InputField sendIP;
    [SerializeField]
    private TMP_InputField sendPort;

    // 設定ファイルパス
    private const string SettingsPath = "./Settings.json";
    // 設定
    private Settings settings = new Settings();
    
    private bool LoadSettings()
    {
        // ファイルがなければ処理しない
        if (!File.Exists(SettingsPath))
        {
            return false;
        }
        // ファイル読み込み
        var contents = File.ReadAllText(SettingsPath);
        // JSON 変換
        try
        {
            JsonUtility.FromJsonOverwrite(contents, this.settings);
        }catch(Exception)
        {
            return false;
        }

        return true;
    }
    private void SaveSettings()
    {
        // JSON 変換
        var contents = JsonUtility.ToJson(this.settings, true);
        // ファイル書き込み
        File.WriteAllText(SettingsPath, contents);
    }
    private void Start()
    {
        // 設定ファイル
        if (!LoadSettings())
        {
            SaveSettings();
        }

        // 
        this.versionText.text = Const.Version;

        // UI イベント
        SetupUI();

        // イベント変更
        SetupMotionEvent();

        // 通信開始
        this.oscServer.Run(this.settings.ReceivePort);
        this.oscClient.Run(this.settings.SendIP, this.settings.SendPort);
    }
    private void OnApplicationQuit()
    {
        End();
    }
    private void OnDestroy()
    {
        End();
    }
    private void End()
    {
        // 通信終了
        this.oscClient.Stop();
        this.oscServer.Stop();
    }
    private void SetupUI()
    {
        // 受信ポート
        SetupReceivePortUI();
        // 送信 IP
        SetupSendIPUI();
        // 送信ポート
        SetupSendPortUI();
    }
    private void SetupReceivePortUI()
    {
        // イベント登録
        IDisposable disposable = null;
        this.receivePort.onValueChanged.AddListener((text) => 
        {
            // 値のバリデーション
            var number = 0;
            if (!int.TryParse(text, out number))
            {
                return;
            }
            // 設定ファイル更新
            this.settings.ReceivePort = number;
            // 設定ファイル保存
            SaveSettings();

            // OSC サーバー更新
            disposable?.Dispose();
            disposable = Observable.Timer(TimeSpan.FromSeconds(Const.SaveSettingsDelay))
            .TakeUntilDestroy(this)
            .Subscribe(_ =>
            {
                this.oscServer.Stop();
                this.oscServer.Run(this.settings.ReceivePort);
            });
        });
        // 初期値設定
        this.receivePort.SetTextWithoutNotify(this.settings.ReceivePort.ToString());
    }
    private void SetupSendIPUI()
    {
        // イベント登録
        IDisposable disposable = null;
        this.sendIP.onValueChanged.AddListener((text) => 
        {
            // 設定ファイル更新
            this.settings.SendIP = text;
            // 設定ファイル保存
            SaveSettings();

            // OSC クライアント更新
            disposable?.Dispose();
            disposable = Observable.Timer(TimeSpan.FromSeconds(Const.SaveSettingsDelay))
            .TakeUntilDestroy(this)
            .Subscribe(_ =>
            {
                this.oscClient.Stop();
                this.oscClient.Run(this.settings.SendIP, this.settings.SendPort);
            });
        });
        // 初期値設定
        this.sendIP.SetTextWithoutNotify(this.settings.SendIP);
    }
    private void SetupSendPortUI()
    {
        // イベント登録
        IDisposable disposable = null;
        this.sendPort.onValueChanged.AddListener((text) => 
        {
            // 値のバリデーション
            var number = 0;
            if (!int.TryParse(text, out number))
            {
                return;
            }
            // 設定ファイル更新
            this.settings.SendPort = number;
            // 設定ファイル保存
            SaveSettings();

            // OSC クライアント更新
            disposable?.Dispose();
            disposable = Observable.Timer(TimeSpan.FromSeconds(Const.SaveSettingsDelay))
            .TakeUntilDestroy(this)
            .Subscribe(_ =>
            {
                this.oscClient.Stop();
                this.oscClient.Run(this.settings.SendIP, this.settings.SendPort);
            });
        });
        // 初期値設定
        this.sendPort.SetTextWithoutNotify(this.settings.SendPort.ToString());
    }
    private void SetupMotionEvent()
    {
        this.oscServer.onDataReceived.AddListener((message) => 
        {
            try
            {
                switch (message.address)
                {
                    // ルート
                    case "/VMC/Ext/Root/Pos":
                    {
                        OnReceivedVMCRoot((string)message.values[0], new Vector3((float)message.values[1], (float)message.values[2], (float)message.values[3]), new Quaternion((float)message.values[4], (float)message.values[5], (float)message.values[6], (float)message.values[7]));
                        break;
                    }
                    // ボーン
                    case "/VMC/Ext/Bone/Pos":
                    {
                        OnReceivedVMCBone((string)message.values[0], new Vector3((float)message.values[1], (float)message.values[2], (float)message.values[3]), new Quaternion((float)message.values[4], (float)message.values[5], (float)message.values[6], (float)message.values[7]));
                        break;
                    }
                }
            }
            catch(Exception e)
            {
                Debug.LogError(e.ToString());
            }
        });
    }
    private void OnReceivedVMCRoot(string name, Vector3 position, Quaternion rotation)
    {
        this.animator.transform.localPosition = position;
        this.animator.transform.localRotation = rotation;
    }
    private void OnReceivedVMCBone(string name, Vector3 position, Quaternion rotation)
    {
        foreach (var bone in Enum.GetValues(typeof(HumanBodyBones)))
        {
            if (bone.ToString() == name)
            {
                var boneTransform = this.animator.GetBoneTransform((HumanBodyBones)bone);
                boneTransform.localPosition = position;
                boneTransform.localRotation = rotation;
                return;
            }
        }
    }
}
