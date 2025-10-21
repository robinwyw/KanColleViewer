using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using StatefulModel;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;

using System.Runtime.InteropServices;

namespace Grabacr07.KanColleWrapper
{
	public partial class KanColleProxy : IDisposable
	{
		private readonly ProxyServer proxyServer;
		private ExplicitProxyEndPoint endPoint;

		public event Action<SessionEventArgs> AfterSessionComplete;

		private IConnectableObservable<SessionEventArgs> apiSource;
		public IObservable<SessionEventArgs> ApiSessionSource => this.apiSource;

		private IDisposable _conn=null;



		//[DllImport("kernel32.dll")]
		//static extern bool AllocConsole();

		#region UpstreamProxySettingsプロパティ

		private IProxySettings _UpstreamProxySettings;

		public IProxySettings UpstreamProxySettings
		{
			get { return this._UpstreamProxySettings; }
			set
			{
				this._UpstreamProxySettings = value;
				this.ApplyUpstreamProxySettings();
			}
		}

		#endregion

		public int ListeningPort { get; private set; } = 37564;

		public KanColleProxy()
		{
			//AllocConsole();
			proxyServer = new ProxyServer
			{
				EnableHttp2 = true,
				EnableConnectionPool = true
			};

			// 证书支持 HTTPS 解密
			proxyServer.CertificateManager.CreateRootCertificate(true);
			proxyServer.CertificateManager.TrustRootCertificate(true);
			this.InitializeApiStream();

		}


		public void Startup(int proxy = 37564)
		{
			this.ListeningPort = proxy;

			//设置上游代理
			this.ApplyUpstreamProxySettings();


			// 注册事件
			proxyServer.BeforeRequest += OnBeforeRequest;
			proxyServer.BeforeResponse += OnBeforeResponse;

			// 启动监听
			endPoint = new ExplicitProxyEndPoint(IPAddress.Loopback, proxy, true);
			proxyServer.AddEndPoint(endPoint);
			proxyServer.Start();

			if (_conn == null)
			{
				_conn = this.apiSource.Connect();
			}

		}

		private async Task OnBeforeRequest(object sender, SessionEventArgs e)
		{
			// 预留：请求前日志
			try
			{
				if (e.HttpClient.Request.HasBody)
				{
					await e.GetRequestBodyAsString();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[Proxy] Failed to read request body: {ex.Message}");
			}
		}

		private async Task OnBeforeResponse(object sender, SessionEventArgs e)
		{
			if (e.HttpClient.Response.HasBody)
			{
				await e.GetResponseBodyAsString();
			}

			var url = e.HttpClient.Request.Url;
			var mime = e.HttpClient.Response.ContentType ?? "";

			// ✅ 保留原过滤条件：路径 /kcsapi 且 MIME 为 text/plain
			if (url.Contains("/kcsapi") && mime.Contains("text/plain"))
			{
				Console.WriteLine($"[API] {url}  {e.HttpClient.Response.StatusCode}");
				AfterSessionComplete?.Invoke(e);
			}
		}

		private void InitializeApiStream()
		{
			// ✅ 不再二次过滤
			this.apiSource = Observable
				.FromEvent<Action<SessionEventArgs>, SessionEventArgs>(
					h => h,
					h => this.AfterSessionComplete += h,
					h => this.AfterSessionComplete -= h)
				.Publish();

		}

		public void Shutdown()
		{
			proxyServer.BeforeRequest -= OnBeforeRequest;
			proxyServer.BeforeResponse -= OnBeforeResponse;
			proxyServer.Stop();
		}

		public void Dispose() => this.Shutdown();

		/// <summary>
		/// 上流プロキシを設定
		/// </summary>
		private void ApplyUpstreamProxySettings()
		{
			var s = this.proxyServer;
			switch (this.UpstreamProxySettings?.Type)
			{
				case ProxyType.DirectAccess:
					Console.WriteLine("[ProxyConfig] Direct connection (no upstream proxy)");
					break;
				case ProxyType.SystemProxy:
					Console.WriteLine("[ProxyConfig] Use system proxy");
					var sysProxy = WebRequest.GetSystemWebProxy();
					var uri = sysProxy.GetProxy(new Uri("http://example.com"));
					if (uri != null)
					{
						s.UpStreamHttpProxy = new ExternalProxy { HostName = uri.Host, Port = uri.Port };
						s.UpStreamHttpsProxy = s.UpStreamHttpProxy;
					}
					break;
				case ProxyType.SpecificProxy:
					Console.WriteLine($"[ProxyConfig] Use special proxy");
					s.UpStreamHttpProxy = new ExternalProxy { HostName = this.UpstreamProxySettings.HttpHost, Port = this.UpstreamProxySettings.HttpPort };
					s.UpStreamHttpsProxy = new ExternalProxy { HostName = this.UpstreamProxySettings.HttpHost, Port = this.UpstreamProxySettings.HttpsPort };
					break;
				default:
					Console.WriteLine("[ProxyConfig] Use system proxy");
					sysProxy = WebRequest.GetSystemWebProxy();
					uri = sysProxy.GetProxy(new Uri("http://example.com"));
					if (uri != null)
					{
						s.UpStreamHttpProxy = new ExternalProxy { HostName = uri.Host, Port = uri.Port };
						s.UpStreamHttpsProxy = s.UpStreamHttpProxy;
					}
					break;
			}
		}
	}
}
