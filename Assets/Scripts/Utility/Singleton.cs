using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    /// <summary>
    /// シングルトンインスタンス
    /// </summary>
    public static T Instance { get; private set; }

    /// <summary>
    /// インスタンスが存在するかどうか
    /// </summary>
    public static bool HasInstance => Instance != null;

    /// <summary>
    /// シーン遷移時にオブジェクトを保持するかどうか
    /// 継承クラスでオーバーライド可能
    /// </summary>
    protected virtual bool PersistAcrossScenes => false;

    /// <summary>
    /// 重複インスタンスが見つかった時の動作
    /// 継承クラスでオーバーライド可能
    /// </summary>
    protected virtual bool DestroyDuplicateInstance => true;

    protected virtual void Awake()
    {
        // シングルトンの初期化
        if (Instance == null)
        {
            Instance = this as T;
            
            if (PersistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
            
            // 初期化処理
            OnSingletonAwake();
        }
        else if (Instance != this)
        {
            // 既にインスタンスが存在する場合
            if (DestroyDuplicateInstance)
            {
                Debug.LogWarning($"{typeof(T).Name} の重複インスタンスを検出しました。{gameObject.name} を削除します。");
                Destroy(gameObject);
                return;
            }
            else
            {
                Debug.LogWarning($"{typeof(T).Name} の重複インスタンスが存在しますが、削除設定が無効になっています。");
            }
        }
    }

    protected virtual void OnDestroy()
    {
        // インスタンスがこのオブジェクトの場合のみクリア
        if (Instance == this)
        {
            OnSingletonDestroy();
            Instance = null;
        }
    }

    /// <summary>
    /// シングルトン初期化時に呼ばれる
    /// 継承クラスでオーバーライドして初期化処理を記述
    /// </summary>
    protected virtual void OnSingletonAwake()
    {
    }

    /// <summary>
    /// シングルトン破棄時に呼ばれる
    /// 継承クラスでオーバーライドしてクリーンアップ処理を記述
    /// </summary>
    protected virtual void OnSingletonDestroy()
    {
    }

    /// <summary>
    /// インスタンスを安全に取得
    /// インスタンスが存在しない場合はnullを返す
    /// </summary>
    /// <returns>インスタンス、または存在しない場合はnull</returns>
    public static T GetInstance()
    {
        return Instance;
    }

    /// <summary>
    /// インスタンスが存在することを確認してから取得
    /// インスタンスが存在しない場合は警告を出力
    /// </summary>
    /// <returns>インスタンス、または存在しない場合はnull</returns>
    public static T GetInstanceSafe()
    {
        if (Instance == null)
        {
            Debug.LogWarning($"{typeof(T).Name} のインスタンスが存在しません。");
        }
        return Instance;
    }
}
