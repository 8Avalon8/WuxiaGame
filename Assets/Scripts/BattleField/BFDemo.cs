using HanSquirrel.ResourceManager;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Demo
{
    public class BFDemo : MonoBehaviour
    {

        public int _maxX;
        public int _maxY;
        public Transform camera;

        // Use this for initialization
        void Start()
        {
            Battle battle = new Battle() { maxX = _maxX, maxY = _maxY };
            Init(battle);
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void Init(Battle battle)
        {
            InitMoveBlocks(battle.maxX, battle.maxY);
            camera.localPosition = new Vector2(ToScreenX(battle.maxX / 2), ToScreenY(battle.maxY / 2));
        }

        private BattleBlock2D[,] _blocks { set; get; }


        private void InitMoveBlocks(int maxX,int maxY)
        {
            _blocks = new BattleBlock2D[maxX, maxY];
            for (int i = 0; i < maxX; ++i)
            {
                for (int j = 0; j < maxY; ++j)
                {
                    _blocks[i, j] = MakeBlock(i, j);
                }
            }
        }

        BattleBlock2D MakeBlock(int x, int y)
        {
            GameObject b = ResourceLoader.CreatePrefabInstance("Assets/BuildSource/BattleField/Prefab/block2DPrefab.prefab");
            b.transform.SetParent(transform);
            b.transform.localPosition = new Vector3(ToScreenX(x), ToScreenY( y), 0);
            b.transform.localScale = Vector3.one;
            b.name = string.Format("block{0}_{1}", x, y);
            b.GetComponent<BattleBlock2D>().X = x;
            b.GetComponent<BattleBlock2D>().Y = y;
            b.SetActive(true);
            b.GetComponent<BattleBlock2D>()._DoAction = ClickBlock;
            return b.GetComponent<BattleBlock2D>();
        }


        public int ToScreenX(int x)
        {
            return (x-1) * 60;
        }

        public int ToScreenY(int y)
        {
            return (y-1)  * 60;
        }

        public void ClickBlock(BlockActionType type, BattleBlock2D block)
        {
            Debug.Log("点击："+ block.X + "," + block.Y);
        }
    }
}