using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Card : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler
{
    private Canvas canvas; // 父级Canvas组件
    private Image imageComponent; // 卡牌的Image组件
    [SerializeField] private bool instantiateVisual = true; // 是否实例化视觉对象
    private VisualCardsHandler visualHandler; // 视觉管理器
    private Vector3 offset; // 鼠标拖拽时的偏移量

    [Header("Movement")]
    [SerializeField] private float moveSpeedLimit = 50; // 拖拽移动速度限制

    [Header("Selection")]
    public bool selected; // 是否被选中
    public float selectionOffset = 50; // 选中时的偏移量
    private float pointerDownTime; // 鼠标按下时间
    private float pointerUpTime; // 鼠标抬起时间

    [Header("Visual")]
    [SerializeField] private GameObject cardVisualPrefab; // 卡牌视觉预制体
    [HideInInspector] public CardVisual cardVisual; // 卡牌视觉对象

    [Header("States")]
    public bool isHovering; // 是否鼠标悬停
    public bool isDragging; // 是否正在拖拽
    [HideInInspector] public bool wasDragged; // 是否刚被拖拽过
    public bool canBeClick=true;

    [Header("Events")] public Action<Card> PointerEnterEvent; // 鼠标进入事件
    public Action<Card> PointerExitEvent; // 鼠标离开事件
    public Action<Card, bool> PointerUpEvent; // 鼠标抬起事件
    public Action<Card> PointerDownEvent; // 鼠标按下事件
    public Action<Card> BeginDragEvent; // 开始拖拽事件
    public Action<Card> EndDragEvent; // 结束拖拽事件
    public Action<Card, bool> SelectEvent; // 选中事件

    public void EffectOn()
    {
        Debug.Log($"effect on {gameObject.name}");
    }

    void Start()
    {
        canvas = GetComponentInParent<Canvas>(); // 获取父级Canvas
        imageComponent = GetComponent<Image>(); // 获取Image组件

        if (!instantiateVisual)
            return;

        visualHandler = FindObjectOfType<VisualCardsHandler>(); // 查找视觉管理器
        cardVisual = Instantiate(cardVisualPrefab, visualHandler ? visualHandler.transform : canvas.transform).GetComponent<CardVisual>();
        cardVisual.Initialize(this); // 初始化卡牌视觉对象
    }

    void ClampPosition()
    {
        // 限制卡牌在屏幕范围内移动
        Vector2 screenBounds = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, Camera.main.transform.position.z));
        Vector3 clampedPosition = transform.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, -screenBounds.x, screenBounds.x);
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, -screenBounds.y, screenBounds.y);
        transform.position = new Vector3(clampedPosition.x, clampedPosition.y, 0);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if(!canBeClick)return;
        // 开始拖拽
        BeginDragEvent.Invoke(this);
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        offset = mousePosition - (Vector2)transform.position;
        isDragging = true;
        canvas.GetComponent<GraphicRaycaster>().enabled = false; // 禁用射线检测
        imageComponent.raycastTarget = false;

        wasDragged = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 拖拽中（留空，逻辑在Update中处理）
        if(!canBeClick)return;
        ClampPosition(); // 限制卡牌位置

        if (isDragging)
        {
            // 拖拽时更新卡牌位置
            Vector2 targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition) - offset;
            Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
            Vector2 velocity = direction * Mathf.Min(moveSpeedLimit, Vector2.Distance(transform.position, targetPosition) / Time.deltaTime);
            transform.Translate(velocity * Time.deltaTime);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if(!canBeClick)return;
        canBeClick = false;
        // 结束拖拽
        EndDragEvent.Invoke(this);
        isDragging = false;
        canvas.GetComponent<GraphicRaycaster>().enabled = true; // 恢复射线检测
        imageComponent.raycastTarget = true;

        StartCoroutine(FrameWait());

        IEnumerator FrameWait()
        {
            yield return new WaitForEndOfFrame();
            wasDragged = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(!canBeClick)return;
        // 鼠标进入
        PointerEnterEvent?.Invoke(this);
        isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if(!canBeClick)return;
        // 鼠标离开
        PointerExitEvent.Invoke(this);
        isHovering = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if(!canBeClick)return;
        // 鼠标按下
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        PointerDownEvent.Invoke(this);
        pointerDownTime = Time.time;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if(!canBeClick)return;
        // 鼠标抬起
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        pointerUpTime = Time.time;

        PointerUpEvent.Invoke(this, pointerUpTime - pointerDownTime > .2f);

        if (pointerUpTime - pointerDownTime > .2f)
            return;

        if (wasDragged)
            return;

        selected = !selected; // 切换选中状态
        SelectEvent.Invoke(this, selected);

        if (selected)
            transform.localPosition += (cardVisual.transform.up * selectionOffset);
        else
            transform.localPosition = Vector3.zero;
    }

    public void Deselect()
    {
        // 取消选中
        if (selected)
        {
            selected = false;
            if (selected)
                transform.localPosition += (cardVisual.transform.up * 50);
            else
                transform.localPosition = Vector3.zero;
        }
    }

    public int SiblingAmount()
    {
        // 获取同级卡牌数量
        return transform.parent.CompareTag("Slot") ? transform.parent.parent.childCount - 1 : 0;
    }

    public int ParentIndex()
    {
        // 获取父级索引
        return transform.parent.CompareTag("Slot") ? transform.parent.GetSiblingIndex() : 0;
    }

    public float NormalizedPosition()
    {
        // 获取归一化位置
        return transform.parent.CompareTag("Slot") ? ExtensionMethods.Remap(ParentIndex(), 0, transform.parent.parent.childCount - 1, 0, 1) : 0;
    }

    private void OnDestroy()
    {
        // 销毁时清理视觉对象
        if(cardVisual != null)
            Destroy(cardVisual.gameObject);
    }
}