
namespace HSFrameWork.ConfigTable.Editor.Inner
{
    /// <summary>
    /// HSCTC的虚拟基类，用于在不同平台共享。
    /// </summary>
    public class HSCTCAbstract
    {
        public enum CompressMode
        {
            LZMA_TDES_RAW, HS_IONIC_ZIP_EASYDES, HS_LZMA_EASYDES
        }
    }
}
