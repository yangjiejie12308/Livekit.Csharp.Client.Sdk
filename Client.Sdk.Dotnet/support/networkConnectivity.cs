using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Client.Sdk.Dotnet.support
{

    /// <summary>
    /// 网络连接状态枚举
    /// </summary>
    public enum ConnectivityResult
    {
        /// <summary>
        /// 无网络连接
        /// </summary>
        None,

        /// <summary>
        /// 移动数据网络连接
        /// </summary>
        Mobile,

        /// <summary>
        /// Wi-Fi 网络连接
        /// </summary>
        WiFi,

        /// <summary>
        /// 有线网络连接
        /// </summary>
        Ethernet,

        /// <summary>
        /// 蓝牙网络连接
        /// </summary>
        Bluetooth,

        /// <summary>
        /// VPN 网络连接
        /// </summary>
        Vpn,

        /// <summary>
        /// 其他类型的网络连接
        /// </summary>
        Other
    }

    /// <summary>
    /// 网络连接状态变更事件参数
    /// </summary>
    public class ConnectivityChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 旧的网络连接状态列表
        /// </summary>
        public List<ConnectivityResult> OldState { get; }

        /// <summary>
        /// 新的网络连接状态列表
        /// </summary>
        public List<ConnectivityResult> NewState { get; }

        /// <summary>
        /// 创建网络连接状态变更事件参数
        /// </summary>
        public ConnectivityChangedEventArgs(List<ConnectivityResult> oldState, List<ConnectivityResult> newState)
        {
            OldState = oldState;
            NewState = newState;
        }
    }

    /// <summary>
    /// 网络连接检测类
    /// </summary>
    public class Connectivity : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string[] _testUrls;
        private readonly Timer _monitorTimer;
        private List<ConnectivityResult> _lastConnectivityResults = new();
        private bool _isDisposed = false;
        private readonly object _lock = new object();

        /// <summary>
        /// 网络连接状态变更事件
        /// </summary>
        public event EventHandler<ConnectivityChangedEventArgs>? ConnectivityChanged;

        /// <summary>
        /// 创建网络连接检测实例
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="monitorIntervalMs">网络监控间隔（毫秒）</param>
        /// <param name="testUrls">网络测试URLs</param>
        public Connectivity(ILogger<Connectivity>? logger = null, int monitorIntervalMs = 10000, string[]? testUrls = null)
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            _testUrls = testUrls ?? new[]
            {
                "https://www.google.com",
                "https://www.microsoft.com",
                "https://www.apple.com"
            };

            // 注册网络变化事件
            NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
            NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;

            // 初始化网络状态监控定时器
            _monitorTimer = new Timer(CheckConnectivityTimerCallback, null, monitorIntervalMs, monitorIntervalMs);

            // 初始化网络状态
            Task.Run(async () =>
            {
                _lastConnectivityResults = await CheckConnectivity();
                Debug.WriteLine($"初始网络状态: {string.Join(", ", _lastConnectivityResults)}");
            });
        }

        /// <summary>
        /// 检查当前是否有可用的网络连接
        /// </summary>
        /// <returns>如果有可用网络连接返回true，否则返回false</returns>
        public async Task<bool> NetworkIsAvailable()
        {
            // 首先检查网络接口状态
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                return false;
            }

            // 如果网络接口可用，再检查网络连接类型
            var results = await CheckConnectivity();
            return results.Count > 0 && !results.Contains(ConnectivityResult.None);
        }

        /// <summary>
        /// 检查当前的网络连接状态
        /// </summary>
        /// <returns>网络连接状态列表</returns>
        public async Task<List<ConnectivityResult>> CheckConnectivity()
        {
            var results = new List<ConnectivityResult>();

            // 如果网络不可用，直接返回None
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                results.Add(ConnectivityResult.None);
                return results;
            }

            // 获取所有活动的网络接口
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up);

            foreach (var networkInterface in networkInterfaces)
            {
                switch (networkInterface.NetworkInterfaceType)
                {
                    case NetworkInterfaceType.Wireless80211:
                        results.Add(ConnectivityResult.WiFi);
                        break;
                    case NetworkInterfaceType.Ethernet:
                        results.Add(ConnectivityResult.Ethernet);
                        break;
                    case NetworkInterfaceType.Ppp:
                        if (networkInterface.Description.Contains("VPN", StringComparison.OrdinalIgnoreCase))
                        {
                            results.Add(ConnectivityResult.Vpn);
                        }
                        else
                        {
                            results.Add(ConnectivityResult.Mobile); // 通常是移动数据
                        }
                        break;
                    case NetworkInterfaceType.Tunnel:
                        results.Add(ConnectivityResult.Vpn);
                        break;
                    case NetworkInterfaceType.AsymmetricDsl:
                    case NetworkInterfaceType.BasicIsdn:
                    case NetworkInterfaceType.FastEthernetFx:
                    case NetworkInterfaceType.FastEthernetT:
                    case NetworkInterfaceType.Fddi:
                    case NetworkInterfaceType.GenericModem:
                    case NetworkInterfaceType.GigabitEthernet:
                    case NetworkInterfaceType.HighPerformanceSerialBus:
                    case NetworkInterfaceType.Isdn:
                    case NetworkInterfaceType.Loopback:
                    case NetworkInterfaceType.MultiRateSymmetricDsl:
                    case NetworkInterfaceType.PrimaryIsdn:
                    case NetworkInterfaceType.RateAdaptDsl:
                    case NetworkInterfaceType.Slip:
                    case NetworkInterfaceType.SymmetricDsl:
                    case NetworkInterfaceType.TokenRing:
                    case NetworkInterfaceType.Wman:
                    case NetworkInterfaceType.Wwanpp:
                    case NetworkInterfaceType.Wwanpp2:
                        results.Add(ConnectivityResult.Other);
                        break;
                }

                // 检查是否有蓝牙网络
                if (networkInterface.Description.Contains("Bluetooth", StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(ConnectivityResult.Bluetooth);
                }
            }

            // 如果没有检测到任何网络连接类型，但网络接口指示有可用网络，添加Other类型
            if (results.Count == 0 && NetworkInterface.GetIsNetworkAvailable())
            {
                results.Add(ConnectivityResult.Other);
            }

            // 如果仍然没有网络连接类型，添加None
            if (results.Count == 0)
            {
                results.Add(ConnectivityResult.None);
            }

            // 额外检查网络连通性
            if (!results.Contains(ConnectivityResult.None))
            {
                bool canReachInternet = await CheckInternetConnectivity();
                if (!canReachInternet)
                {
                    Debug.WriteLine("检测到网络接口可用，但无法连接到互联网");
                }
            }

            return results;
        }

        /// <summary>
        /// 获取网络连接状态变更事件的可观察对象
        /// </summary>
        /// <returns>网络连接状态变更事件的可观察对象</returns>
        public IObservable<List<ConnectivityResult>> OnConnectivityChanged()
        {
            return new ConnectivityObservable(this);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            // 注销事件
            NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
            NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged;

            // 释放定时器
            _monitorTimer?.Dispose();

            // 释放HTTP客户端
            _httpClient?.Dispose();
        }

        // 网络可用性变更事件处理
        private void OnNetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            Debug.WriteLine($"网络可用性发生变化: {e.IsAvailable}");
            CheckConnectivityAndRaiseEvent();
        }

        // 网络地址变更事件处理
        private void OnNetworkAddressChanged(object sender, EventArgs e)
        {
            Debug.WriteLine("网络地址发生变化");
            CheckConnectivityAndRaiseEvent();
        }

        // 定时器回调
        private void CheckConnectivityTimerCallback(object? state)
        {
            CheckConnectivityAndRaiseEvent();
        }

        // 检查网络连通性并触发事件
        private async void CheckConnectivityAndRaiseEvent()
        {
            try
            {
                var newResults = await CheckConnectivity();

                // 检查是否与上次结果相同
                lock (_lock)
                {
                    if (!AreConnectivityResultsEqual(_lastConnectivityResults, newResults))
                    {
                        Debug.WriteLine($"网络连接状态变更: {string.Join(", ", _lastConnectivityResults)} -> {string.Join(", ", newResults)}");
                        var oldResults = new List<ConnectivityResult>(_lastConnectivityResults);
                        _lastConnectivityResults = newResults;

                        // 触发事件
                        ConnectivityChanged?.Invoke(this, new ConnectivityChangedEventArgs(oldResults, newResults));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检查网络连接时出错: {ex.Message}");
            }
        }

        // 检查网络列表是否相等
        private bool AreConnectivityResultsEqual(List<ConnectivityResult> list1, List<ConnectivityResult> list2)
        {
            if (list1.Count != list2.Count)
                return false;

            return list1.OrderBy(x => x).SequenceEqual(list2.OrderBy(x => x));
        }

        // 检查互联网连通性
        private async Task<bool> CheckInternetConnectivity()
        {
            foreach (string url in _testUrls)
            {
                try
                {
                    var response = await _httpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"尝试连接到 {url} 失败: {ex.Message}");
                }
            }

            return false;
        }

    }

    /// <summary>
    /// 网络连接变化的可观察对象实现
    /// </summary>
    internal class ConnectivityObservable : IObservable<List<ConnectivityResult>>
    {
        private readonly Connectivity _connectivity;

        public ConnectivityObservable(Connectivity connectivity)
        {
            _connectivity = connectivity;
        }

        public IDisposable Subscribe(IObserver<List<ConnectivityResult>> observer)
        {
            var subscription = new ConnectivitySubscription(_connectivity, observer);
            return subscription;
        }
    }

    /// <summary>
    /// 网络连接变化的订阅实现
    /// </summary>
    internal class ConnectivitySubscription : IDisposable
    {
        private readonly Connectivity _connectivity;
        private readonly IObserver<List<ConnectivityResult>> _observer;
        private bool _isDisposed = false;

        public ConnectivitySubscription(Connectivity connectivity, IObserver<List<ConnectivityResult>> observer)
        {
            _connectivity = connectivity;
            _observer = observer;

            // 订阅事件
            _connectivity.ConnectivityChanged += OnConnectivityChanged;

            // 立即发送当前状态
            SendInitialState();
        }

        private async void SendInitialState()
        {
            try
            {
                var initialState = await _connectivity.CheckConnectivity();
                _observer.OnNext(initialState);
            }
            catch (Exception ex)
            {
                _observer.OnError(ex);
            }
        }

        private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
        {
            if (!_isDisposed)
            {
                _observer.OnNext(e.NewState);
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _connectivity.ConnectivityChanged -= OnConnectivityChanged;
            }
        }
    }
}
