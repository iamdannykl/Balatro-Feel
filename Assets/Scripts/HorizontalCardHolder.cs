using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

public class HorizontalCardHolder : MonoBehaviour
{

    [SerializeField] private Card selectedCard;
    [SerializeReference] private Card hoveredCard;

    [SerializeField] private GameObject slotPrefab;
    private RectTransform rect;

    [Header("Spawn Settings")]
    [SerializeField] private int cardsToSpawn = 7;
    public List<Card> cards;

    bool isCrossing;
    [SerializeField] private bool tweenCardReturn = true;
    public RectTransform discardPile; // 丢弃牌堆

    bool isScretch=true;
    public bool canScretch = false;
    public bool IsScretch
    {
        get => isScretch;
        set
        {
            isScretch = value;
            if (isScretch)
            {
                rect.sizeDelta = new Vector2(500, rect.sizeDelta.y);
            }
            else
            {
                rect.sizeDelta = new Vector2(100, rect.sizeDelta.y);
            }
        }
    }
    // 初始化卡牌槽和卡牌，注册事件
    void Start()
    {
        for (int i = 0; i < cardsToSpawn; i++)
        {
            Instantiate(slotPrefab, transform);
        }

        rect = GetComponent<RectTransform>();
        cards = GetComponentsInChildren<Card>().ToList();

        int cardCount = 0;

        foreach (Card card in cards)
        {
            // 注册卡牌的各种事件
            card.PointerEnterEvent+=CardPointerEnter;
            card.PointerExitEvent+=CardPointerExit;
            card.BeginDragEvent+=BeginDrag;
            card.EndDragEvent+=EndDrag;
            card.PointerUpEvent += CardUp;
            card.name = cardCount.ToString();
            cardCount++;
        }

        // 延迟一帧后更新卡牌视觉索引
        StartCoroutine(Frame());

        IEnumerator Frame()
        {
            yield return new WaitForSecondsRealtime(.1f);
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].cardVisual != null)
                    cards[i].cardVisual.UpdateIndex(transform.childCount);
            }
        }
    }

    private void CardUp(Card card, bool arg2)
    {
        if (canScretch&&!card.isDragging)
        {
            IsScretch = true;
        }
    }

    // 拖拽开始时记录选中的卡牌
    private void BeginDrag(Card card)
    {
        selectedCard = card;
    }

    // 拖拽结束时重置卡牌位置和状态
    /*void EndDrag(Card card)
    {
        if (selectedCard == null)
            return;

        // 动画返回选中/未选中的偏移
        selectedCard.transform.DOLocalMove(selectedCard.selected ? new Vector3(0,selectedCard.selectionOffset,0) : Vector3.zero, tweenCardReturn ? .15f : 0).SetEase(Ease.OutBack);

        // 强制刷新布局
        rect.sizeDelta += Vector2.right;
        rect.sizeDelta -= Vector2.right;

        selectedCard = null;
    }*/
    void EndDrag(Card card)
    {
        if (selectedCard == null)
            return;
        Card currentCard = selectedCard;
        currentCard.isDragging = false;
        currentCard.EffectOn();
        if (canScretch)
        {
            IsScretch = false;
        }
        currentCard.transform.DOMove(discardPile.position, 0.8f)
            .SetEase(Ease.OutBack)
            .OnComplete(() => {
                cards.Remove(currentCard);
                //Destroy(currentCard.transform.parent.gameObject);
                currentCard = null;
            });
    
        // 强制刷新布局
        rect.sizeDelta += Vector2.right;
        rect.sizeDelta -= Vector2.right;
    }

    
    void CardPointerEnter(Card card)
    {
        hoveredCard = card;
    }

    // 鼠标离开卡牌时清空
    void CardPointerExit(Card card)
    {
        hoveredCard = null;
    }

    // 主循环，处理删除、右键取消选中、拖拽排序等
    void Update()
    {
        // 按Delete键删除悬停卡牌
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            if (hoveredCard != null)
            {
                Destroy(hoveredCard.transform.parent.gameObject);
                cards.Remove(hoveredCard);
            }
        }

        // 右键取消所有卡牌选中
        if (Input.GetMouseButtonDown(1))
        {
            foreach (Card card in cards)
            {
                card.Deselect();
            }
        }

        if (selectedCard == null)
            return;

        if (isCrossing)
            return;

        // 拖拽时判断是否需要与其他卡牌交换位置
        for (int i = 0; i < cards.Count; i++)
        {
            if(!cards[i].canBeClick)continue;
            if (selectedCard.transform.position.x > cards[i].transform.position.x)
            {
                if (selectedCard.ParentIndex() < cards[i].ParentIndex())
                {
                    Swap(i);
                    break;
                }
            }

            if (selectedCard.transform.position.x < cards[i].transform.position.x)
            {
                if (selectedCard.ParentIndex() > cards[i].ParentIndex())
                {
                    Swap(i);
                    break;
                }
            }
        }
    }

    // 交换两张卡牌的位置和父级
    void Swap(int index)
    {
        isCrossing = true;

        Transform focusedParent = selectedCard.transform.parent;
        Transform crossedParent = cards[index].transform.parent;

        // 交换父级
        cards[index].transform.SetParent(focusedParent);
        cards[index].transform.localPosition = cards[index].selected ? new Vector3(0, cards[index].selectionOffset, 0) : Vector3.zero;
        selectedCard.transform.SetParent(crossedParent);

        isCrossing = false;

        if (cards[index].cardVisual == null)
            return;

        // 播放交换动画
        bool swapIsRight = cards[index].ParentIndex() > selectedCard.ParentIndex();
        cards[index].cardVisual.Swap(swapIsRight ? -1 : 1);

        // 更新所有卡牌的视觉索引
        foreach (Card card in cards)
        {
            card.cardVisual.UpdateIndex(transform.childCount);
        }
    }

}