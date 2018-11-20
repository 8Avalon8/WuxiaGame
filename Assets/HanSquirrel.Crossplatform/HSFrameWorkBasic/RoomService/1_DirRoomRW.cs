#if HSFRAMEWORK_NET_ABOVE_4_5
using GLib;
using HSFrameWork.Common;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HSFrameWork.RoomService
{
    /// <summary>
    /// Windows/UnityEditor
    /// GameClient GameServer RoomServer三者的非房间消息通讯接口实现。
    /// 仅仅是因为代码比较相似而在一个类里面实现。实际运行中三个接口的实现代码大概率分别运行在不同的进程中。
    /// </summary>
    public class DirRoomRW
    {
        protected static readonly IHSLogger _Logger = HSLogManager.GetLogger("DirRoomRW");
        public String Status { get; private set; }
        public Task MainTask { get { return _TCS.Task; } }
        public string Desc { get { return "监控特定目录寻找任务文件"; } }
        public string Name { get { return "DirRoomRW"; } }
        public string Version { get { return "V0.0.1"; } }

        public string CurrentInterfaceType { get; protected set; }

        private TaskCompletionSource<bool> _TCS = new TaskCompletionSource<bool>();
        private StartStopHelper _SSH = new StartStopHelper();
        public Task RunAsync()
        {
            _SSH.Start_ExceptionIfStarted("RunAsync不可重入");
            WrapOp(() => "EnableRaisingEvents=true", () => { EnableAll(true); });
            _Logger.Info("▼ {0} 开始运行。", CurrentInterfaceType);
            _TCS.Task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                    _Logger.Fatal(t.Exception, "▲ {0} 异常结束", CurrentInterfaceType);
                else
                    _Logger.Info("▲ {0} 正常结束。Status=[{1}]", CurrentInterfaceType, t.Status);
                if (_RoomTaskWatcher != null)
                    _RoomTaskWatcher.Dispose();
                if (_RoomClientSetupPublishWatcher != null)
                    _RoomClientSetupPublishWatcher.Dispose();
                if (_RoomTaskRetWatcher != null)
                    _RoomTaskRetWatcher.Dispose();
            });
            return _TCS.Task;
        }

        public void SignalStop()
        {
            if (_SSH.Stop_ExceptionIfNotStarted("任务还没有启动。"))
                return;

            lock (_TCS)
            {
                //因为_TCS的结果会影响到可能多线程调用的多个地方，因此都需要LOCK
                if (_TCS.Task.IsCompleted)
                    return;

                WrapOp(() => "EnableRaisingEvents=false", () => { EnableAll(false); });
                _TCS.TrySetResult(true); //上一个调用可能会引发异常。
            }
        }

        public bool IsDummy
        {
            get
            {
                return MainTask == null || MainTask.IsCompleted || MonitorDisabled;
            }
        }


        private volatile bool _ReadingDisable = false;
        public bool MonitorDisabled
        {
            get
            {
                return _ReadingDisable;
            }
            set
            {
                _ReadingDisable = value;
                lock (_TCS)
                    WrapOp(() => "EnableRaisingEvents={0}".f(value), () => { EnableAll(value); });
            }
        }

        private void EnableAll(bool enable)
        {
            if (_RoomTaskWatcher != null)
                _RoomTaskWatcher.EnableRaisingEvents = enable;
            if (_RoomClientSetupPublishWatcher != null)
                _RoomClientSetupPublishWatcher.EnableRaisingEvents = enable;
            if (_RoomTaskRetWatcher != null)
                _RoomTaskRetWatcher.EnableRaisingEvents = enable;
        }

        private void Clean_SetTaskToException(string msg, Exception e)
        {
            lock (_TCS)
            {
                if (_TCS.Task.IsCompleted)
                    return;

                try
                {
                    EnableAll(false);
                }
                catch { }
                Status = msg;
                _TCS.TrySetException(new Exception(msg, e));
            }
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            Clean_SetTaskToException("FileSystemWatcher底层出现异常", e.GetException());
        }

        protected string _RoomTaskPath, _RoomClientSetupPublishPath, _RoomTaskRetPath;
        protected FileSystemWatcher _RoomTaskWatcher, _RoomClientSetupPublishWatcher, _RoomTaskRetWatcher;

        /// <summary>
        /// unity下面，如果有多个变更消息，系统会通过多个线程同时回调，因此会造成内部一些多线程故障。故此需要锁定下。
        /// </summary>
        private object _LockInvokeEvent = new object();
        protected void CreateAndBind(string path, out FileSystemWatcher watcher, FileSystemEventHandler evtHandle)
        {
            watcher = new FileSystemWatcher(path, "*.bin");
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Changed += (a, b) => { lock (_LockInvokeEvent) evtHandle(a, b); }; //在Unity里面如果有一个老文件被覆盖，才会产生Changed消息。
            watcher.Created += (a, b) => { lock (_LockInvokeEvent) evtHandle(a, b); }; //在Unity里面如果是新文件，不会产生Changed消息，只会产生Created消息。
                                                                                       /*
                                                                                       watcher.Created += (object source, FileSystemEventArgs e) =>
                                                                                       {
                                                                                           _Logger.Debug("File Created: [{0}].", e.FullPath);
                                                                                       };*/
            watcher.Error += OnError;
        }

        protected void WrapEventCall(string eventName, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Clean_SetTaskToException("程序编写错误：Event处理代码需要自行捕获异常 :{0}".f(eventName), e);
            }
        }

        protected void WrapOp(Func<string> msg, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Clean_SetTaskToException(msg(), e);
            }
        }

#region 文件操作
        /// <summary>
        /// 如果失败仅仅会写日志。
        /// </summary>
        protected void DeleteTmpFile(string tmpFile)
        {
            try
            {
                tmpFile.Delete();
            }
            catch
            {
                _Logger.Warn("{0} 删除临时文件失败 [{1}] 。", CurrentInterfaceType, tmpFile);
            }
        }

        /// <summary>
        /// 如果失败主任务会异常退出。
        /// </summary>
        protected void WrapDeleteFile(string file)
        {
            try
            {
                file.Delete();
            }
            catch (Exception e)
            {
                Clean_SetTaskToException("{0} 删除文件失败 {1}".f(CurrentInterfaceType, file), e);
            }
        }

        /// <summary>
        /// 在Net3.5没有Interlocked.
        /// </summary>
        private object _LockTempId = new object();
        private int _TempId;
        private string _Guid;
        protected DirRoomRW()
        {
            _Guid = Guid.NewGuid().ToString().Replace("-", "") + "-";
        }

        protected bool TakeOwnerThenDelete(string file)
        {
            //刚开始直接Move到GetTmpFilePath发现有时候会有多个进程同时都返回而不会抛出异常；
            //以为是Move到其他目录的时候会存在这种情况。现在发现即使是同一个目录
            //也会如此。代码既然已经写好，就现如此。
            int tempid;
            lock (_LockTempId)
                tempid = _TempId++;
            var tmpFile = new FileInfo(file).Directory.ToString().StandardSub(_Guid + tempid);

            if (SafeMoveFile(file, tmpFile))
            {
                _Logger.Trace("{0} [{1}] > [{2}] OK", CurrentInterfaceType, file, tmpFile);
                DeleteTmpFile(tmpFile);
                return true;
            }
            else
            {
                //抢夺任务失败。
                _Logger.Debug("{0} STEP3失败 [抢夺任务] {1} > {2}", CurrentInterfaceType, file, tmpFile);
                return false;
            }
        }

        /// <summary>
        /// 有时候会出现奇怪的BUG，没有仔细看，故此先如此。
        /// </summary>
        private object _LockFileRW = new object();

        protected bool SafeMoveFile(string src, string dst)
        {
            try
            {
                lock (_LockFileRW)
                    File.Move(src, dst);
                return dst.ExistsAsFile();
                //有时候File.Move不抛出异常，然而实际上不成功。
                //有可能是用了MemDisk，没有仔细去研究。先如此判断下。
            }
            catch
            {
                return false;
            }
        }

        protected void WrapWriteFile(string file, object obj)
        {
            try
            {
                lock (_LockFileRW)
                    File.WriteAllBytes(file, DirectProtoBufTools.Serialize(obj));
                _Logger.Debug("{0} 文件写完：{1}", CurrentInterfaceType, file);
            }
            catch (Exception e)
            {
                Clean_SetTaskToException("写文件出错 {0}".f(file), e);
            }
        }

        /// <summary>
        /// 如果文件会被多个DirRoomRW抢夺，则必须使用tmpFile。
        /// </summary>
        protected T WrapReadFile<T>(bool useTmpFile, string file) where T : class
        {
            string file2Read;
            if (useTmpFile)
            {
                file2Read = Path.GetTempFileName();
                try
                {
                    lock (_LockFileRW)
                        File.Copy(file, file2Read, true);
                }
                catch (Exception)
                {
                    _Logger.Trace("{0} STEP1失败 [副本读取] [{1}]，估计是被其他程序抢走了。", CurrentInterfaceType, file);
                    DeleteTmpFile(file2Read);
                    return null; //没有抢到
                }
            }
            else
            {
                file2Read = file;
            }

            try
            {
                lock (_LockFileRW)
                    return DirectProtoBufTools.Deserialize<T>(file2Read.ReadAllBytes());
            }
            catch (Exception e)
            {
                Clean_SetTaskToException("读文件出错 {0}".f(file), e);
                return null;
            }
            finally
            {
                if (useTmpFile)
                    DeleteTmpFile(file2Read);
            }
        }
#endregion
    }
}
#endif
