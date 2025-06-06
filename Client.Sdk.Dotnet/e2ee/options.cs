using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Sdk.Dotnet.e2ee
{
    /// <summary>
    /// 端到端加密类型
    /// </summary>
    public enum EncryptionType
    {
        /// <summary>
        /// 无加密
        /// </summary>
        None,

        /// <summary>
        /// 使用GCM模式的加密
        /// </summary>
        Gcm,

        /// <summary>
        /// 自定义加密
        /// </summary>
        Custom
    }

    /// <summary>
    /// 端到端加密选项
    /// </summary>
    public class E2EEOptions
    {
        /// <summary>
        /// 密钥提供程序
        /// </summary>
        public BaseKeyProvider KeyProvider { get; }

        /// <summary>
        /// 加密类型，默认为GCM
        /// </summary>
        public EncryptionType EncryptionType { get; } = EncryptionType.Gcm;

        /// <summary>
        /// 创建端到端加密选项
        /// </summary>
        /// <param name="keyProvider">密钥提供程序</param>
        public E2EEOptions(BaseKeyProvider keyProvider)
        {
            KeyProvider = keyProvider;
        }
    }
}
