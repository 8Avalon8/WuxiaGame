using System;
using System.Collections;
using System.Collections.Generic;
using GLib;
using HSFrameWork.Common;

namespace HSFrameWork.SPojo
{
    using Inner;
    public abstract partial class Saveable
    {



        private void GenerateSubmit(Hashtable submitRoot, bool depth)
        {
            using (SubmitStatics.TimerThisRound = HSUtils.ExeTimerSilent("GenerateSubmit"))
            {
                SubmitStatics.SkippedSubmitThisRound = 0;
                SubmitStatics.TouchedThisRound = 0;
                SubmitLogger.Reset();

                SafeGenerateSubmitInner(null, submitRoot, true);

                SubmitStatics.SubmitThisRound = submitRoot.Count;
                SubmitStatics.TotalSubmitted += SubmitStatics.SubmitThisRound;
            }

            ++SubmitStatics.SubmitSeq;
            if (DebugFacade.SubmitRuntimeAction != null)
            {
                DebugFacade.SubmitRuntimeAction(SubmitStatics.SubmitSeq, SubmitStatics.SkippedSubmitThisRound, submitRoot, SubmitLogger.ResetLog());
                HSUtils.Log("★★★★" + SubmitStatics.GetSubmitStatics());
            }
        }

        /// <summary>
        /// 开发者内部使用。
        /// </summary>
        public abstract void SafeGenerateSubmitInner(string pre, Hashtable submitRoot, bool depth);
    }

    namespace Inner
    {
        public class SubmitStatics
        {
            /// <summary> 总共访问过多少个SaveablePojo  </summary>
            public static int TotalTouched = 0;
            /// <summary> 总共提交过多少个SaveablePojo  </summary>
            public static int TotalSubmitted = 0;
            /// <summary> 总共按照优化算法忽略过多少个SaveablePojo  </summary>
            public static int TotalSkipped = 0;

            /// <summary> 本轮访问了多少个SaveablePojo  </summary>
            public static int TouchedThisRound = 0;
            /// <summary> 本轮提交了多少个SaveablePojo  </summary>
            public static int SubmitThisRound = 0;
            /// <summary> 本轮按照优化算法忽略了多少个SaveablePojo  </summary>
            public static int SkippedSubmitThisRound = 0;

            /// <summary> 最后一次提交的序号（从0开始）</summary>
            public static int SubmitSeq = -1;

            /// <summary> 本轮花费时间 </summary>
            public static ExeTimer TimerThisRound;

            public static string GetSubmitStatics()
            {
                return "第 [#{0}] 次遍历，花费 [{1}]，遍历 [{2}] 个，提交 [{3}] 个，忽略 [{4}] 个。累计遍历 [{5}]， 提交 [{6}], 忽略 [{7}]。".Eat(
                            SubmitSeq, TimerThisRound == null ? "NA" : TimerThisRound.ResultStr,
                            TouchedThisRound, SubmitThisRound, SkippedSubmitThisRound,
                            TotalTouched, TotalSubmitted, TotalSkipped);
            }
        }


        /// <summary>
        /// 记录存档增量提交的树状结构，用于测试。如果Disabled，则不会影响运行效率。GG
        /// </summary>
        public static class SubmitLogger
        {
            public static bool Disabled = true;
            public static Func<Hashtable, string> Data2String;

            private static List<string> _saveNameStack = new List<string>(20);
            private static List<bool> _printStack = new List<bool>(20);
            private static List<string> _Logs = new List<string>();

            public static List<string> ResetLog()
            {
                List<string> ret = _Logs;
                _Logs = new List<string>();
                return ret;
            }

            public static void Reset()
            {
                _saveNameStack.Clear();
                _printStack.Clear();
                _Logs = new List<string>();
            }

            public static void In(string sn)
            {
                if (Disabled) return;
                _saveNameStack.Add(sn);
                _printStack.Add(false);
            }

            public static void Submit(Hashtable data)
            {
                if (Disabled) return;
                for (int i = 0; i < _printStack.Count; i++)
                {
                    if (!_printStack[i])
                    {
                        _Logs.Add(new string('\t', i) + _saveNameStack[i]);
                    }
                    _printStack[i] = true;
                }
                _Logs.Add(new string('\t', _printStack.Count) + Data2String(data));
            }

            public static void Out()
            {
                if (Disabled) return;
                _saveNameStack.RemoveAt(_saveNameStack.Count - 1);
                _printStack.RemoveAt(_printStack.Count - 1);
            }
        }
        public abstract partial class AbstractSaveable<ARGT, ATTRT> : Saveable
        {
            public override void SafeGenerateSubmitInner(string pre, Hashtable submitRoot, bool depth)
            {
                if (IsIgnoreSubmit() || this == null)
                    return;

                if (!SubmitLogger.Disabled)
                    SubmitLogger.In((pre == null ? "" : pre + " - ") + SaveNameG + "★");
                SubmitStatics.TouchedThisRound++;
                SubmitStatics.TotalTouched++;

                if (_changedAttrs != null && _changedAttrs.Count > 0)
                {
                    Hashtable diffData = new Hashtable();

                    foreach (ATTRT attrInt in _changedAttrs)
                        SubmitAttr(attrInt, diffData);

                    if (diffData.Count > 0)
                    {
                        submitRoot.Add(SaveNameG, diffData);
                        SubmitLogger.Submit(diffData);
                    }
                    else
                    {
                        SubmitStatics.SkippedSubmitThisRound++;
                        SubmitStatics.TotalSkipped++;
                    }

                    _changedAttrs = null;
                }

                if (depth)
                    SubmitChildren(submitRoot);

                SubmitLogger.Out();
            }

            private void SubmitAttr(ATTRT attr, Hashtable diffData)
            {
                if (_pojoAttrs != null)
                {
                    //用TryGetValue来减少对Dictionary的反复取值；可以大大提高遍历速度，下同。
                    Saveable attrValue;
                    if (_pojoAttrs.TryGetValue(attr, out attrValue))
                    {
                        //这个成员是：SPojo
                        object lastPojoId;
                        if (!LastSubmitted.TryGetValue(attr, out lastPojoId) || (int)lastPojoId != attrValue.Id())
                        {   //之前存档的ID和目前不是同一个。
                            diffData.Add(AttrToString(attr), attrValue.Id());
                            LastSubmitted[attr] = attrValue.Id();
                        }

                        return;
                    }
                }

                if (_simpleListAttrs != null)
                {
                    IList attrValue;
                    if (_simpleListAttrs.TryGetValue(attr, out attrValue))
                    {
                        //这个成员是：List<struct/string>
                        SmartSubmitList(diffData, attr, attrValue);
                        return;
                    }
                }

                if (_pojoListAttrs != null)
                {
                    IList attrValue;
                    if (_pojoListAttrs.TryGetValue(attr, out attrValue))
                    {
                        //这个成员是：List<SPojo>
                        List<int> list = new List<int>(attrValue.Count);
                        foreach (SaveablePojo item in attrValue)
                            list.Add(item.__id);  //List<SPojo>在存档时，只存id。
                        SmartSubmitList(diffData, attr, list);
                        return;
                    }
                }

                if (_m_data != null)
                {
                    object attrValue;
                    if (_m_data.TryGetValue(attr, out attrValue) && attrValue != null)
                    {
                        //这个成员是：普通Value类型
                        diffData.Add(AttrToString(attr), attrValue.ToString());
                        return;
                    }
                }

                //表明这个是需要被删除的属性。
                diffData.Add(AttrToString(attr), null);
                if (_LastSubmitted != null)
                    _LastSubmitted.Remove(attr);
            }

            private void SubmitChildren(Hashtable submitRoot)
            {
                //遍历 Dictory<*, Saveable>
                if (_pojoAttrs != null)
                    foreach (var kv in _pojoAttrs)
                        kv.Value.SafeGenerateSubmitInner(AttrToString(kv.Key), submitRoot, true);

                //遍历  Dictory<*, List<Saveable>>
                if (_pojoListAttrs != null)
                    foreach (var kv in _pojoListAttrs)
                    {
                        if (!SubmitLogger.Disabled)
                            SubmitLogger.In(AttrToString(kv.Key) + "[]");
                        foreach (Saveable pojo in kv.Value)
                            pojo.SafeGenerateSubmitInner(null, submitRoot, true);
                        SubmitLogger.Out();
                    }
            }

            /// <summary>
            /// 判断服务器上的版本和当前版本是否相同，如果不同再加入到diffData中。
            /// </summary>
            private void SmartSubmitList(Hashtable diffData, ATTRT attr, IList list)
            {
                string json = list.toJson();
                object lastValue;
                if (!LastSubmitted.TryGetValue(attr, out lastValue) || !json.Equals(lastValue))
                { //                      服务器没有                      服务器上的和新提交的不同
                    diffData.Add(AttrToString(attr), json);
                    LastSubmitted[attr] = json; //将m_data[attr]作为存档的镜像
                }
            }
        }
    }
}
