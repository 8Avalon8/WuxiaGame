using System;

namespace HSFrameWork.Common.Inner
{
    public interface IFrameUpdater
    {
        event Action OnUpdate;
        event Action OnAppQuit;
    }
}
