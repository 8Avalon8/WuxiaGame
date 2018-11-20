using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;


namespace Demo
{
    public enum BattleBlockStatus
    {
        Hide,
        AttackRange,
        AttackDistance,
        AttackTarget,
        CurrentAttackRange,
        MoveTarget,
        MoveRange,
        MoveRangeEnemy
    }

    public enum BlockActionType
    {
        Enter,
        Exit,
        Action,
        LongPress
    }

    public class BattleBlock2D : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        //public const float BASIC_SCALE = 1.25f;
        public int X;
        public int Y;

        public static int CurrentX;
        public static int CurrentY;
        public static bool ShowAll = true;

        private static BattleBlock2D currentBlock;

        public Image Image;
        public GameObject Animator;
        public GameObject Enemy;
        public Sprite attackRangeSp;
        public Sprite attackDistanceSp;
        public Sprite attackTargetSp;
        public Sprite currentAttackRangeSp;
        public Sprite moveTargetSp;
        public Sprite moveRangeSp;
        public Sprite moveRangeEnemySp;
        public Sprite noneSp;

        [HideInInspector]
        public Action<BlockActionType, BattleBlock2D> _DoAction;

        public void OnPointerDown(PointerEventData data)
        {
            if (IsActive)
            {
                _DoAction(BlockActionType.Enter, this);
                currentBlock = this;
            }

            LongPress();
        }

        public void OnPointerUp(PointerEventData data)
        {
            if (currentBlock != null && IsActive)
            {
                _DoAction(BlockActionType.Action, currentBlock);
                currentBlock = null;
            }
        }

        public void OnPointerEnter(PointerEventData data)
        {
            if (Input.touchCount == 1 || Input.GetMouseButton(0))
            {
                if (IsActive)
                {
                    _DoAction(BlockActionType.Enter, this);
                    currentBlock = this;
                }

            }
        }

        private void LongPress()
        {
            _DoAction(BlockActionType.LongPress, this);
        }

        public void OnPointerExit(PointerEventData data)
        {
            if (currentBlock != null && IsActive)
            {
                _DoAction(BlockActionType.Exit, this);
                currentBlock = null;
            }
        }

        public void Reset()
        {
            IsActive = false;
            Animator.SetActive(false);
            Enemy.SetActive(false);
            Status = BattleBlockStatus.Hide;
            transform.localScale = Vector3.one;
        }

        private void ChangeSprite(Image img, Sprite sprite)
        {
            img.sprite = sprite;
            img.SetNativeSize();
        }

        public void MarkEnemy()
        {
            Enemy.SetActive(true);
        }

        public void Hightlight()
        {
            Animator.SetActive(true);
        }

        public BattleBlockStatus Status
        {
            set
            {
                _status = value;

                switch (_status)
                {
                    case BattleBlockStatus.Hide:
                        ChangeSprite(Image, noneSp);
                        this.gameObject.SetActive(ShowAll);
                        break;
                    case BattleBlockStatus.AttackRange:
                        ChangeSprite(Image, attackRangeSp);
                        this.gameObject.SetActive(true);
                        break;
                    case BattleBlockStatus.AttackDistance:
                        ChangeSprite(Image, attackDistanceSp);
                        this.gameObject.SetActive(true);
                        break;
                    case BattleBlockStatus.AttackTarget:
                        ChangeSprite(Image, attackTargetSp);
                        this.gameObject.SetActive(true);
                        break;
                    case BattleBlockStatus.CurrentAttackRange:
                        ChangeSprite(Image, currentAttackRangeSp);
                        this.gameObject.SetActive(true);
                        break;
                    case BattleBlockStatus.MoveTarget:
                        ChangeSprite(Image, moveTargetSp);
                        this.gameObject.SetActive(true);
                        break;
                    case BattleBlockStatus.MoveRange:
                        ChangeSprite(Image, moveRangeSp);
                        this.gameObject.SetActive(true);
                        break;
                    case BattleBlockStatus.MoveRangeEnemy:
                        ChangeSprite(Image, moveRangeEnemySp);
                        this.gameObject.SetActive(true);
                        break;
                    default:
                        break;
                }
            }
            get
            {
                return _status;
            }
        }
        BattleBlockStatus _status;

        public bool IsActive { get; set; }

    }
}