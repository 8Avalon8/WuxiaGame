namespace HSFrameWork.SPojo.Inner
{
    public abstract partial class AbstractSaveable<ARGT, ATTRT> : Saveable
    {
        /// <summary>
        /// 除了ID外的所有东西都会被删除。被删除的成员会记录在changedAttrs里面，在Submit时会通知服务器删除该成员。
        /// </summary>
        public virtual void Clear()
        {
            if (IsIgnoreSubmit())
            {
                _changedAttrs = null;
                _m_data = null;
                _pojoAttrs = null;
                _simpleListAttrs = null;
                _pojoListAttrs = null;
            }
            else
            {
                if (_simpleListAttrs != null)
                    foreach (var attrInt in _simpleListAttrs.Keys)
                        SetChanged(attrInt);
                _simpleListAttrs = null;

                if (_pojoListAttrs != null)
                    foreach (var attrInt in _pojoListAttrs.Keys)
                        SetChanged(attrInt);
                _pojoListAttrs = null;

                if (_pojoAttrs != null)
                    foreach (var attrInt in _pojoAttrs.Keys)
                        SetChanged(attrInt);
                _pojoAttrs = null;

                if (_m_data != null)
                {
                    int tempId = int.MinValue;
                    if (_m_data.ContainsKey(ATTR_NAME_ID))
                    {
                        tempId = (int)_m_data[ATTR_NAME_ID];
                        _m_data.Remove(ATTR_NAME_ID);
                    }

                    //这里不Clear changedAttrs，是以为其里面有可能会存有之前更新过的Attr
                    foreach (ATTRT key in _m_data.Keys)
                        changedAttrs.Add(key);

                    if (tempId != int.MinValue)
                    {
                        m_data.Clear();
                        _m_data[ATTR_NAME_ID] = tempId;
                    }
                    else
                    {
                        _m_data = null;
                    }
                }
            }
        }

        /// <summary>
        /// 仅仅全部清空所有成员为null而已。
        /// </summary>
        public override void Reset()
        {
            _simpleListAttrs = null;
            _pojoListAttrs = null;
            _pojoAttrs = null;
            _LastSubmitted = null;
            _changedAttrs = null;
            _m_data = null;
            base.Reset();
        }
    }
}