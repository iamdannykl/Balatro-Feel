using UnityEngine;

public class VisualCardsHandler : MonoBehaviour
{
    public static VisualCardsHandler instance; // 单例模式实例

    private void Awake()
    {
        instance = this; // 初始化单例
    }

    void Start()
    {
        // 启动时的初始化逻辑（当前为空）
    }

    void Update()
    {
        // 每帧更新逻辑（当前为空）
    }
}
