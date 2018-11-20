using System;
using System.Collections;
using System.Collections.Generic;
using BeanDict = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, HSFrameWork.ConfigTable.BaseBean>>;

namespace HSFrameWork.ConfigTable
{
    using Inner;
    /// <summary>
    /// ���ñ������ն�Ψһ�ӿ�
    /// </summary>
    public class ConfigTable
    {
        /// <summary>
        /// ʵ�ʵĵ����������ڲ���������ʹ�á�
        /// </summary>
        public static readonly ConfigTableBasic Instance = new ConfigTableBasic();

        /// <summary>
        /// ���ü��ض��������ñ��ļ��Ľӿڡ�
        /// </summary>
        public static Func<byte[]> DoLoadData
        {
            set
            {
                Instance.DoLoadData = value;
            }
        }

        /// <summary>
        /// ���ü��ض��������ñ��ļ��Ľӿڣ�ValueBundleName����
        /// </summary>
        public static Func<string, byte[]> DoLoadDataV2
        {
            set
            {
                Instance.DoLoadDataV2 = value;
            }
        }

        /// <summary>
        /// �첽�������ñ�tagΪValueBundleTag
        /// </summary>
        static public void StartInitAsync(string tag = null)
        {
            Instance.StartInitAsync(tag);
        }

        /// <summary>
        /// ǿ�����¼��ض��������ñ�
        /// </summary>
        /// <param name="tag">ValueBundleTag</param>
        static public LoadStatus ForceReSync(string tag = null)
        {
            return Instance.ForceReSync(tag);
        }

        /// <summary>
        /// �ȴ����ñ��첽�������
        /// </summary>
        static public IEnumerator WaitForAsyncInit(Action<LoadStatus> callback)
        {
            return Instance.WaitForAsyncInit(callback);
        }

        /// <summary>
        /// ͬ���������ñ�
        /// </summary>
        /// <param name="tag">ValueBundleTag</param>
        /// <returns>�������ñ����״̬</returns>
        static public LoadStatus InitSync(string tag = null)
        {
            return Instance.InitSync(tag);
        }

        /// <summary>
        /// �ж����ñ��Ƿ��ʼ�����
        /// </summary>
        static public bool IsInited()
        {
            return Instance.IsInited();
        }

        /// <summary>
        /// �Ƿ�����������
        /// </summary>
        static public bool Has<T>(string key) where T : BaseBean
        {
            return Instance.Has<T>(key);
        }

        /// <summary>
        /// �����ñ��л�ȡ�����Ͷ�Ӧ��id������
        /// </summary>
        /// <typeparam name="T">BasePoje����</typeparam>
        /// <param name="id">���ñ��ж�Ӧ��id</param>
        /// <returns>���ñ��ж�Ӧ���ݵ�������</returns>
        static public T Get<T>(int id) where T : BaseBean
        {
            return Instance.Get<T>(id.ToString());
        }

        /// <summary>
        /// �����ñ��л�ȡ�����Ͷ�Ӧ��key������
        /// </summary>
        /// <typeparam name="T">BasePoje����</typeparam>
        /// <param name="key">���ñ��ж�Ӧ��key</param>
        /// <returns>���ñ��ж�Ӧ���ݵ�������</returns>
        static public T Get<T>(string key) where T : BaseBean
        {
            return Instance.Get<T>(key);
        }

        /// <summary>
        /// �����ñ��л�ȡ���������е�����
        /// </summary>
        /// <typeparam name="T">BasePoje����</typeparam>
        /// <returns>���ñ��д��������ݵ����������༯</returns>
        public static IEnumerable<T> GetAll<T>() where T : BaseBean
        {
            return Instance.GetAll<T>();
        }

        /// <summary>
        /// ���ȡ��һ������Ϊ T ������
        /// </summary>
        static public T GetRandom<T>() where T : BaseBean
        {
            return Instance.GetRandom<T>();

        }

        /// <summary>
        /// �����ñ��л�ȡ��Ӧ���ͼ���Ӧkey������
        /// ����BaseBean����Ҫʹ�ö�Ӧ���͵ı�������Ҫ����ǿת
        /// </summary>
        /// <param name="key">���ñ��ж�Ӧ��key</param>
        /// <param name="typeName">BasePoje���� Ϊ�ַ�������Ҫ���������ռ�</param>
        /// <returns>���ñ��ж�Ӧ���ݵ�������</returns>
        static public BaseBean Get(string key, string typeName)
        {
            return Instance.Get(key, typeName);
        }

        /// <summary>
        /// ���ñ༭ģʽ�¼������ñ��ֵ�ĺ���
        /// </summary>
        public static Action<BeanDict> LoadDesignModeDelegate { set { Instance.LoadDesignModeDelegate = value; } }
    
        /// <summary>
        /// ���ñ༭ģʽ�¼������ñ��ֵ�ĺ�����valueBundleTag��
        /// </summary>
        public static Action<string, BeanDict> LoadDesignModeDelegateV2 { set { Instance.LoadDesignModeDelegateV2 = value; } }

        /// <summary>
        /// �ڱ༭ģʽ�¼������ñ��ֵ�
        /// </summary>
        public static void LoadDesignMode()
        {
            Instance.LoadDesignMode();
        }

        /// <summary>
        /// �ڱ༭ģʽ�¼������ñ��ֵ䣨ValueBundleTag��
        /// </summary>
        public static void LoadDesignModeV2(string tag)
        {
            Instance.LoadDesignModeV2(tag);
        }

        /// <summary>
        /// �ڲ�����ʹ�á��������ñ��ֵ䡣
        /// </summary>
        public static void VisitValues(Action<BeanDict> visitor)
        {
            Instance.VisitValues(visitor);
        }
    }
}
