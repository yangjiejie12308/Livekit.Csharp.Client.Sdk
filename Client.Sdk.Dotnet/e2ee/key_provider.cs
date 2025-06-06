using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Client.Sdk.Dotnet.e2ee
{
    /// <summary>
    /// 默认加密相关常量
    /// </summary>
    public static class EncryptionDefaults
    {
        public const string RatchetSalt = "LKFrameEncryptionKey";
        public const string MagicBytes = "LK-ROCKS";
        public const int RatchetWindowSize = 16;
        public const int FailureTolerance = -1;
        public const int KeyRingSize = 16;
        public const bool DiscardFrameWhenCryptorNotReady = false;
    }

    /// <summary>
    /// 密钥信息
    /// </summary>
    public class KeyInfo
    {
        /// <summary>
        /// 参与者ID
        /// </summary>
        public string ParticipantId { get; }

        /// <summary>
        /// 密钥索引
        /// </summary>
        public int KeyIndex { get; }

        /// <summary>
        /// 密钥数据
        /// </summary>
        public byte[] Key { get; }

        /// <summary>
        /// 创建密钥信息实例
        /// </summary>
        /// <param name="participantId">参与者ID</param>
        /// <param name="keyIndex">密钥索引</param>
        /// <param name="key">密钥数据</param>
        public KeyInfo(string participantId, int keyIndex, byte[] key)
        {
            ParticipantId = participantId;
            KeyIndex = keyIndex;
            Key = key;
        }
    }

    /// <summary>
    /// 密钥提供程序接口
    /// </summary>
    public interface IKeyProvider
    {
        /// <summary>
        /// 设置共享密钥
        /// </summary>
        /// <param name="key">密钥字符串</param>
        /// <param name="keyIndex">可选的密钥索引</param>
        Task SetSharedKeyAsync(string key, int? keyIndex = null);

        /// <summary>
        /// 更新共享密钥
        /// </summary>
        /// <param name="keyIndex">可选的密钥索引</param>
        /// <returns>更新后的密钥</returns>
        Task<byte[]> RatchetSharedKeyAsync(int? keyIndex = null);

        /// <summary>
        /// 导出共享密钥
        /// </summary>
        /// <param name="keyIndex">可选的密钥索引</param>
        /// <returns>导出的密钥</returns>
        Task<byte[]> ExportSharedKeyAsync(int? keyIndex = null);

        /// <summary>
        /// 设置密钥
        /// </summary>
        /// <param name="key">密钥字符串</param>
        /// <param name="participantId">可选的参与者ID</param>
        /// <param name="keyIndex">可选的密钥索引</param>
        Task SetKeyAsync(string key, string? participantId = null, int? keyIndex = null);

        /// <summary>
        /// 设置原始密钥
        /// </summary>
        /// <param name="key">原始密钥数据</param>
        /// <param name="participantId">可选的参与者ID</param>
        /// <param name="keyIndex">可选的密钥索引</param>
        Task SetRawKeyAsync(byte[] key, string? participantId = null, int? keyIndex = null);

        /// <summary>
        /// 更新特定参与者的密钥
        /// </summary>
        /// <param name="participantId">参与者ID</param>
        /// <param name="keyIndex">可选的密钥索引</param>
        /// <returns>更新后的密钥</returns>
        Task<byte[]> RatchetKeyAsync(string participantId, int? keyIndex = null);

        /// <summary>
        /// 导出特定参与者的密钥
        /// </summary>
        /// <param name="participantId">参与者ID</param>
        /// <param name="keyIndex">可选的密钥索引</param>
        /// <returns>导出的密钥</returns>
        Task<byte[]> ExportKeyAsync(string participantId, int? keyIndex = null);

        /// <summary>
        /// 设置SIF尾部
        /// </summary>
        /// <param name="trailer">尾部数据</param>
        Task SetSifTrailerAsync(byte[] trailer);

        /// <summary>
        /// 获取底层密钥提供程序
        /// </summary>
        IWebRTCKeyProvider WebRTCKeyProvider { get; }
    }

    /// <summary>
    /// 底层WebRTC密钥提供程序接口
    /// </summary>
    public interface IWebRTCKeyProvider
    {
        Task SetSharedKeyAsync(byte[] key, int index);
        Task<byte[]> RatchetSharedKeyAsync(int index);
        Task<byte[]> ExportSharedKeyAsync(int index);
        Task SetKeyAsync(string participantId, int index, byte[] key);
        Task<byte[]> RatchetKeyAsync(string participantId, int index);
        Task<byte[]> ExportKeyAsync(string participantId, int index);
        Task SetSifTrailerAsync(byte[] trailer);
    }

    /// <summary>
    /// WebRTC密钥提供程序选项
    /// </summary>
    public class KeyProviderOptions
    {
        /// <summary>
        /// 是否使用共享密钥
        /// </summary>
        public bool SharedKey { get; set; }

        /// <summary>
        /// 密钥推导用的盐值
        /// </summary>
        public byte[] RatchetSalt { get; set; }

        /// <summary>
        /// 密钥窗口大小
        /// </summary>
        public int RatchetWindowSize { get; set; }

        /// <summary>
        /// 未加密的魔术字节
        /// </summary>
        public byte[] UncryptedMagicBytes { get; set; }

        /// <summary>
        /// 容错值
        /// </summary>
        public int FailureTolerance { get; set; }

        /// <summary>
        /// 密钥环大小
        /// </summary>
        public int KeyRingSize { get; set; }

        /// <summary>
        /// 加密器未就绪时是否丢弃帧
        /// </summary>
        public bool DiscardFrameWhenCryptorNotReady { get; set; }

        /// <summary>
        /// 创建密钥提供程序选项
        /// </summary>
        public KeyProviderOptions(
            bool sharedKey,
            byte[] ratchetSalt,
            int ratchetWindowSize,
            byte[] uncryptedMagicBytes,
            int failureTolerance,
            int keyRingSize,
            bool discardFrameWhenCryptorNotReady)
        {
            SharedKey = sharedKey;
            RatchetSalt = ratchetSalt;
            RatchetWindowSize = ratchetWindowSize;
            UncryptedMagicBytes = uncryptedMagicBytes;
            FailureTolerance = failureTolerance;
            KeyRingSize = keyRingSize;
            DiscardFrameWhenCryptorNotReady = discardFrameWhenCryptorNotReady;
        }
    }

    /// <summary>
    /// 基础密钥提供程序实现
    /// </summary>
    public class BaseKeyProvider : IKeyProvider
    {
        private readonly Dictionary<string, int> _latestSetIndex = new();
        private readonly Dictionary<string, Dictionary<int, byte[]>> _keys = new();
        private byte[]? _sharedKey;
        private readonly IWebRTCKeyProvider _keyProvider;

        /// <summary>
        /// 获取底层WebRTC密钥提供程序
        /// </summary>
        public IWebRTCKeyProvider WebRTCKeyProvider => _keyProvider;

        /// <summary>
        /// 提供程序选项
        /// </summary>
        public KeyProviderOptions Options { get; }

        /// <summary>
        /// 共享密钥
        /// </summary>
        public byte[]? SharedKey => _sharedKey;

        /// <summary>
        /// 获取参与者的最新密钥索引
        /// </summary>
        /// <param name="participantId">参与者ID</param>
        /// <returns>最新的密钥索引</returns>
        public int GetLatestIndex(string participantId)
        {
            return _latestSetIndex.TryGetValue(participantId, out var index) ? index : 0;
        }

        /// <summary>
        /// 创建基础密钥提供程序
        /// </summary>
        /// <param name="keyProvider">底层密钥提供程序</param>
        /// <param name="options">提供程序选项</param>
        /// <param name="logger">日志记录器</param>
        public BaseKeyProvider(
            IWebRTCKeyProvider keyProvider,
            KeyProviderOptions options,
            ILogger<BaseKeyProvider>? logger = null)
        {
            _keyProvider = keyProvider;
            Options = options;
        }

        /// <summary>
        /// 创建基础密钥提供程序实例
        /// </summary>
        /// <param name="frameCryptorFactory">帧加密器工厂</param>
        /// <param name="sharedKey">是否使用共享密钥</param>
        /// <param name="ratchetSalt">密钥推导用的盐值</param>
        /// <param name="uncryptedMagicBytes">未加密的魔术字节</param>
        /// <param name="ratchetWindowSize">密钥窗口大小</param>
        /// <param name="failureTolerance">容错值</param>
        /// <param name="keyRingSize">密钥环大小</param>
        /// <param name="discardFrameWhenCryptorNotReady">加密器未就绪时是否丢弃帧</param>
        /// <param name="logger">日志记录器</param>
        public static async Task<BaseKeyProvider> CreateAsync(
            IFrameCryptorFactory frameCryptorFactory,
            bool sharedKey = true,
            string? ratchetSalt = null,
            string? uncryptedMagicBytes = null,
            int? ratchetWindowSize = null,
            int? failureTolerance = null,
            int? keyRingSize = null,
            bool? discardFrameWhenCryptorNotReady = null,
            ILogger<BaseKeyProvider>? logger = null)
        {
            var options = new KeyProviderOptions(
                sharedKey: sharedKey,
                ratchetSalt: Encoding.UTF8.GetBytes(ratchetSalt ?? EncryptionDefaults.RatchetSalt),
                ratchetWindowSize: ratchetWindowSize ?? EncryptionDefaults.RatchetWindowSize,
                uncryptedMagicBytes: Encoding.UTF8.GetBytes(uncryptedMagicBytes ?? EncryptionDefaults.MagicBytes),
                failureTolerance: failureTolerance ?? EncryptionDefaults.FailureTolerance,
                keyRingSize: keyRingSize ?? EncryptionDefaults.KeyRingSize,
                discardFrameWhenCryptorNotReady: discardFrameWhenCryptorNotReady ?? EncryptionDefaults.DiscardFrameWhenCryptorNotReady
            );

            var keyProvider = await frameCryptorFactory.CreateDefaultKeyProviderAsync(options);
            return new BaseKeyProvider(keyProvider, options, logger);
        }

        /// <inheritdoc/>
        public async Task SetSharedKeyAsync(string key, int? keyIndex = null)
        {
            _sharedKey = Encoding.UTF8.GetBytes(key);
            await _keyProvider.SetSharedKeyAsync(_sharedKey, keyIndex ?? 0);
        }

        /// <inheritdoc/>
        public async Task<byte[]> RatchetSharedKeyAsync(int? keyIndex = null)
        {
            if (_sharedKey == null)
            {
                throw new InvalidOperationException("Shared key not set");
            }
            _sharedKey = await _keyProvider.RatchetSharedKeyAsync(keyIndex ?? 0);
            return _sharedKey;
        }

        /// <inheritdoc/>
        public async Task<byte[]> ExportSharedKeyAsync(int? keyIndex = null)
        {
            if (_sharedKey == null)
            {
                throw new InvalidOperationException("Shared key not set");
            }
            return await _keyProvider.ExportSharedKeyAsync(keyIndex ?? 0);
        }

        /// <inheritdoc/>
        public Task<byte[]> RatchetKeyAsync(string participantId, int? keyIndex = null)
        {
            return _keyProvider.RatchetKeyAsync(participantId, keyIndex ?? 0);
        }

        /// <inheritdoc/>
        public Task<byte[]> ExportKeyAsync(string participantId, int? keyIndex = null)
        {
            return _keyProvider.ExportKeyAsync(participantId, keyIndex ?? 0);
        }

        /// <inheritdoc/>
        public async Task SetKeyAsync(string key, string? participantId = null, int? keyIndex = null)
        {
            if (Options.SharedKey)
            {
                await SetSharedKeyAsync(key, keyIndex);
                return;
            }

            var keyInfo = new KeyInfo(
                participantId: participantId ?? "",
                keyIndex: keyIndex ?? 0,
                key: Encoding.UTF8.GetBytes(key)
            );

            await SetKeyInternalAsync(keyInfo);
        }

        /// <inheritdoc/>
        public async Task SetRawKeyAsync(byte[] key, string? participantId = null, int? keyIndex = null)
        {
            await SetKeyAsync(
                Encoding.UTF8.GetString(key),
                participantId,
                keyIndex
            );
        }

        private async Task SetKeyInternalAsync(KeyInfo keyInfo)
        {
            if (!_keys.TryGetValue(keyInfo.ParticipantId, out var participantKeys))
            {
                participantKeys = new Dictionary<int, byte[]>();
                _keys[keyInfo.ParticipantId] = participantKeys;
            }

            Debug.WriteLine(
                "Setting key for {ParticipantId}, index: {KeyIndex}, key: {Key}",
                keyInfo.ParticipantId,
                keyInfo.KeyIndex,
                Convert.ToBase64String(keyInfo.Key)
            );

            participantKeys[keyInfo.KeyIndex] = keyInfo.Key;
            _latestSetIndex[keyInfo.ParticipantId] = keyInfo.KeyIndex;

            await _keyProvider.SetKeyAsync(
                keyInfo.ParticipantId,
                keyInfo.KeyIndex,
                keyInfo.Key
            );
        }

        /// <inheritdoc/>
        public Task SetSifTrailerAsync(byte[] trailer)
        {
            return _keyProvider.SetSifTrailerAsync(trailer);
        }
    }

    /// <summary>
    /// 帧加密器工厂接口
    /// </summary>
    public interface IFrameCryptorFactory
    {
        /// <summary>
        /// 创建默认密钥提供程序
        /// </summary>
        /// <param name="options">密钥提供程序选项</param>
        Task<IWebRTCKeyProvider> CreateDefaultKeyProviderAsync(KeyProviderOptions options);
    }

}
