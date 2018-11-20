using HSFrameWork.Common;
using HSFrameWork.ConfigTable;
using JianghuX;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStatr : MonoBehaviour {


    private void Awake()
    {
        HSBootApp.ColdBind(ConStr.GLOBAL_DESKEY, HSConfigTableInitHelperPhone.Create(), ConStr.PrefabPoolConfig);
        Container.Register<IStoryManager>(x => new StoryManager(), ReuseScope.Container);

        StartCoroutine(test());
    }

    IEnumerator test()
    {
        yield return new WaitForSeconds(1);
        ConfigTable.StartInitAsync();
        yield return new WaitForSeconds(3);
        var res = ConfigTable.Get<ResourceDTO>("音乐欣赏");
        Debug.Log(res.Value);
    }
}
