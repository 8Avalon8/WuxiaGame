using System;
using HSFrameWork.ConfigTable.Editor.Impl;
using BeanDict = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, HSFrameWork.ConfigTable.BaseBean>>;
using HSFrameWork.ConfigTable;
using System.Collections.Generic;
using HSFrameWork.ConfigTable.Trans;
using HSFrameWork.ConfigTable.Editor;

public class HSConfigTableInitHelperEditor : HSConfigTableInitHelperPhone
{
    public new static IInitHelper Create()
    {
        return new HSConfigTableInitHelperEditor();
    }

    protected HSConfigTableInitHelperEditor() { }

    public override Action<BeanDict> ResourceManagerLoadDesignModeDelegate
    {
        get
        {
            return (beanDict) =>
            {
                XMLBDUpdater.Instance.Reset();
                using (ProgressBarAutoHide.Get(0))
                    XMLBDUpdater.Instance.UpdateChanged(beanDict, (d, p) => MenuHelper.SafeDisplayProgressBar("正在加载XML", d, p));
            };
        }
    }

    protected override void BuildTextFinders(object arg)
    {

    }
}

