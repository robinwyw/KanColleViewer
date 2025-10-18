using System;
using System.Net;
using System.Threading.Tasks;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;
using Titanium.Web.Proxy.Http;

namespace Grabacr07.KanColleWrapper
{
	class SimpleProxy
	{
		private ProxyServer proxyServer;
		private ExplicitProxyEndPoint explicitEndPoint;

		public async Task StartAsync(int port = 37564)
		{
			proxyServer = new ProxyServer();

			// 可选：日志/异常回调
			proxyServer.ExceptionFunc = exception => Console.WriteLine("Proxy exception: " + exception);

			// 创建并信任根证书（第一次运行可能会提示安装）
			proxyServer.CertificateManager.CreateRootCertificate(true);
			proxyServer.CertificateManager.TrustRootCertificate(true);

			// 创建显式监听端点（启用Https = true）
			explicitEndPoint = new ExplicitProxyEndPoint(IPAddress.Loopback, port, true);

			proxyServer.AddEndPoint(explicitEndPoint);

			// 请求/响应事件
			proxyServer.BeforeRequest += OnRequest;
			proxyServer.BeforeResponse += OnResponse;

			proxyServer.Start();

			// 将其设置为系统代理（可选，需注意权限与环境）
			proxyServer.SetAsSystemHttpProxy(explicitEndPoint);
			proxyServer.SetAsSystemHttpsProxy(explicitEndPoint);

			Console.WriteLine($"Proxy started on {explicitEndPoint.IpAddress}:{explicitEndPoint.Port}, HTTPS enabled");
		}

		public async Task StopAsync()
		{
			if (proxyServer != null)
			{
				proxyServer.BeforeRequest -= OnRequest;
				proxyServer.BeforeResponse -= OnResponse;
				proxyServer.Stop();
				proxyServer.Dispose();
			}
		}

		private async Task OnRequest(object sender, SessionEventArgs e)
		{
			// 打印请求 URL（HTTP 或 HTTPS）
			Console.WriteLine("Request: " + e.HttpClient.Request.Url);
			// 可以在这里读取/修改请求体、头部等
		}

		private async Task OnResponse(object sender, SessionEventArgs e)
		{
			Console.WriteLine("Response: " + e.HttpClient.Request.Url + " - " + e.HttpClient.Response.StatusCode);
			// 读取响应体示例（注意性能 / 大体量）
			// var body = await e.GetResponseBodyAsString();
		}
	}
}
