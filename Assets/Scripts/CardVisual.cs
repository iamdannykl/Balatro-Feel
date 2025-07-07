using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CardVisual : MonoBehaviour
{
    private bool initalize; // 是否已初始化

    [Header("Card")]
    public Card parentCard; // 关联的父级卡牌
    private Transform cardTransform; // 卡牌的Transform
    private Vector3 rotationDelta; // 旋转偏移量
    private int savedIndex; // 保存的索引
    Vector3 movementDelta; // 移动偏移量
    private Canvas canvas; // 当前Canvas

    [Header("References")]
    public Transform visualShadow; // 卡牌阴影
    private float shadowOffset = 20; // 阴影偏移量
    private Vector2 shadowDistance; // 阴影距离
    private Canvas shadowCanvas; // 阴影的Canvas
    [SerializeField] private Transform shakeParent; // 抖动父级
    [SerializeField] private Transform tiltParent; // 倾斜父级
    [SerializeField] private Image cardImage; // 卡牌图片

    [Header("Follow Parameters")]
    [SerializeField] private float followSpeed = 30; // 跟随速度

    [Header("Rotation Parameters")]
    [SerializeField] private float rotationAmount = 20; // 旋转幅度
    [SerializeField] private float rotationSpeed = 20; // 旋转速度
    [SerializeField] private float autoTiltAmount = 30; // 自动倾斜幅度
    [SerializeField] private float manualTiltAmount = 20; // 手动倾斜幅度
    [SerializeField] private float tiltSpeed = 20; // 倾斜速度

    [Header("Scale Parameters")]
    [SerializeField] private bool scaleAnimations = true; // 是否启用缩放动画
    [SerializeField] private float scaleOnHover = 1.15f; // 悬停时缩放比例
    [SerializeField] private float scaleOnSelect = 1.25f; // 选中时缩放比例
    [SerializeField] private float scaleTransition = .15f; // 缩放过渡时间
    [SerializeField] private Ease scaleEase = Ease.OutBack; // 缩放动画曲线

    [Header("Select Parameters")]
    [SerializeField] private float selectPunchAmount = 20; // 选中时的抖动幅度

    [Header("Hober Parameters")]
    [SerializeField] private float hoverPunchAngle = 5; // 悬停时的抖动角度
    [SerializeField] private float hoverTransition = .15f; // 悬停过渡时间

    [Header("Swap Parameters")]
    [SerializeField] private bool swapAnimations = true; // 是否启用交换动画
    [SerializeField] private float swapRotationAngle = 30; // 交换时的旋转角度
    [SerializeField] private float swapTransition = .15f; // 交换过渡时间
    [SerializeField] private int swapVibrato = 5; // 交换抖动次数

    [Header("Curve")]
    [SerializeField] private CurveParameters curve; // 曲线参数

    private float curveYOffset; // 曲线Y偏移量
    private float curveRotationOffset; // 曲线旋转偏移量
    private Coroutine pressCoroutine; // 按下协程

    private void Start()
    {
        shadowDistance = visualShadow.localPosition; // 初始化阴影位置
    }

    public void Initialize(Card target, int index = 0)
    {
        // 初始化卡牌视觉
        parentCard = target;
        cardTransform = target.transform;
        canvas = GetComponent<Canvas>();
        shadowCanvas = visualShadow.GetComponent<Canvas>();

        // 注册事件
        parentCard.PointerEnterEvent+=PointerEnter;
        parentCard.PointerExitEvent+=PointerExit;
        parentCard.BeginDragEvent+=BeginDrag;
        parentCard.EndDragEvent+=EndDrag;
        parentCard.PointerDownEvent+=PointerDown;
        parentCard.PointerUpEvent+=PointerUp;
        parentCard.SelectEvent+=Select;

        initalize = true;
    }

    public void UpdateIndex(int length)
    {
        // 更新视觉索引
        transform.SetSiblingIndex(parentCard.transform.parent.GetSiblingIndex());
    }

    void Update()
    {
        if (!initalize || parentCard == null) return;

        HandPositioning(); // 更新手牌位置
        SmoothFollow(); // 平滑跟随
        FollowRotation(); // 跟随旋转
        CardTilt(); // 卡牌倾斜
    }

    private void HandPositioning()
    {
        // 计算手牌曲线位置
        curveYOffset = (curve.positioning.Evaluate(parentCard.NormalizedPosition()) * curve.positioningInfluence) * parentCard.SiblingAmount();
        curveYOffset = parentCard.SiblingAmount() < 5 ? 0 : curveYOffset;
        curveRotationOffset = curve.rotation.Evaluate(parentCard.NormalizedPosition());
    }

    private void SmoothFollow()
    {
        // 平滑跟随父级位置
        Vector3 verticalOffset = (Vector3.up * (parentCard.isDragging ? 0 : curveYOffset));
        transform.position = Vector3.Lerp(transform.position, cardTransform.position + verticalOffset, followSpeed * Time.deltaTime);
    }

    private void FollowRotation()
    {
        // 跟随父级旋转
        Vector3 movement = (transform.position - cardTransform.position);
        movementDelta = Vector3.Lerp(movementDelta, movement, 25 * Time.deltaTime);
        Vector3 movementRotation = (parentCard.isDragging ? movementDelta : movement) * rotationAmount;
        rotationDelta = Vector3.Lerp(rotationDelta, movementRotation, rotationSpeed * Time.deltaTime);
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, Mathf.Clamp(rotationDelta.x, -60, 60));
    }

    private void CardTilt()
    {
        // 卡牌倾斜效果
        savedIndex = parentCard.isDragging ? savedIndex : parentCard.ParentIndex();
        float sine = Mathf.Sin(Time.time + savedIndex) * (parentCard.isHovering ? .2f : 1);
        float cosine = Mathf.Cos(Time.time + savedIndex) * (parentCard.isHovering ? .2f : 1);

        Vector3 offset = transform.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float tiltX = parentCard.isHovering ? ((offset.y * -1) * manualTiltAmount) : 0;
        float tiltY = parentCard.isHovering ? ((offset.x) * manualTiltAmount) : 0;
        float tiltZ = parentCard.isDragging ? tiltParent.eulerAngles.z : (curveRotationOffset * (curve.rotationInfluence * parentCard.SiblingAmount()));

        float lerpX = Mathf.LerpAngle(tiltParent.eulerAngles.x, tiltX + (sine * autoTiltAmount), tiltSpeed * Time.deltaTime);
        float lerpY = Mathf.LerpAngle(tiltParent.eulerAngles.y, tiltY + (cosine * autoTiltAmount), tiltSpeed * Time.deltaTime);
        float lerpZ = Mathf.LerpAngle(tiltParent.eulerAngles.z, tiltZ, tiltSpeed / 2 * Time.deltaTime);

        tiltParent.eulerAngles = new Vector3(lerpX, lerpY, lerpZ);
    }

    private void Select(Card card, bool state)
    {
        // 选中动画
        DOTween.Kill(2, true);
        float dir = state ? 1 : 0;
        shakeParent.DOPunchPosition(shakeParent.up * selectPunchAmount * dir, scaleTransition);
        shakeParent.DOPunchRotation(Vector3.forward * (hoverPunchAngle/2), hoverTransition, 20).SetId(2);

        if(scaleAnimations)
            transform.DOScale(scaleOnHover, scaleTransition).SetEase(scaleEase);
    }

    public void Swap(float dir = 1)
    {
        // 卡牌交换动画
        if (!swapAnimations)
            return;

        DOTween.Kill(2, true);
        shakeParent.DOPunchRotation((Vector3.forward * swapRotationAngle) * dir, swapTransition, swapVibrato).SetId(3);
    }

    private void BeginDrag(Card card)
    {
        // 开始拖拽动画
        if(scaleAnimations)
            transform.DOScale(scaleOnSelect, scaleTransition).SetEase(scaleEase);

        canvas.overrideSorting = true;
    }

    private void EndDrag(Card card)
    {
        // 结束拖拽动画
        canvas.overrideSorting = false;
        transform.DOScale(1, scaleTransition).SetEase(scaleEase);
    }

    private void PointerEnter(Card card)
    {
        // 鼠标进入动画
        if(scaleAnimations)
            transform.DOScale(scaleOnHover, scaleTransition).SetEase(scaleEase);

        DOTween.Kill(2, true);
        shakeParent.DOPunchRotation(Vector3.forward * hoverPunchAngle, hoverTransition, 20).SetId(2);
    }

    private void PointerExit(Card card)
    {
        // 鼠标离开动画
        if (!parentCard.wasDragged)
            transform.DOScale(1, scaleTransition).SetEase(scaleEase);
    }

    private void PointerUp(Card card, bool longPress)
    {
        // 鼠标抬起动画
        if(scaleAnimations)
            transform.DOScale(longPress ? scaleOnHover : scaleOnSelect, scaleTransition).SetEase(scaleEase);
        canvas.overrideSorting = false;

        visualShadow.localPosition = shadowDistance;
        shadowCanvas.overrideSorting = true;
    }

    private void PointerDown(Card card)
    {
        // 鼠标按下动画
        if(scaleAnimations)
            transform.DOScale(scaleOnSelect, scaleTransition).SetEase(scaleEase);
            
        visualShadow.localPosition += (-Vector3.up * shadowOffset);
        shadowCanvas.overrideSorting = false;
    }
}