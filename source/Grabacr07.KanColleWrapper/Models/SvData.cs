using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Grabacr07.KanColleWrapper.Internal;
using Grabacr07.KanColleWrapper.Models.Raw;
using Newtonsoft.Json;
using Titanium.Web.Proxy.EventArguments;

namespace Grabacr07.KanColleWrapper.Models
{
	public class SvData<T> : RawDataWrapper<svdata<T>>
	{
		public NameValueCollection Request { get; private set; }

		public bool IsSuccess => this.RawData.api_result == 1;

		public T Data => this.RawData.api_data;

		public kcsapi_deck[] Fleets => this.RawData.api_data_deck;

		public SvData(svdata<T> rawData, string reqBody)
			: base(rawData)
		{
			this.Request = HttpUtility.ParseQueryString(reqBody);
		}
	}

	public class SvData : RawDataWrapper<svdata>
	{
		public NameValueCollection Request { get; private set; }

		public bool IsSuccess => this.RawData.api_result == 1;

		public SvData(svdata rawData, string reqBody)
			: base(rawData)
		{
			this.Request = HttpUtility.ParseQueryString(reqBody);
		}


		#region Parse methods (generic)

		public static SvData<T> Parse<T>(SessionEventArgs e)
		{
			try
			{
				var responseJson = e.GetResponseBodyAsString().GetAwaiter().GetResult();
				var requestBody = e.GetRequestBodyAsString().GetAwaiter().GetResult();

				if (string.IsNullOrWhiteSpace(responseJson))
					throw new InvalidOperationException("Response is empty.");
				if (responseJson.StartsWith("svdata="))
				{
					responseJson = responseJson.Substring("svdata=".Length);
				}
				var rawResult = JsonConvert.DeserializeObject<svdata<T>>(responseJson);
				return new SvData<T>(rawResult, requestBody);
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"[SvData<T>] Parse failed: {ex}");
				throw;
			}
		}

		public static bool TryParse<T>(SessionEventArgs session, out SvData<T> result)
		{
			try
			{
				result = Parse<T>(session);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
				result = null;
				return false;
			}

			return true;
		}

		#endregion

		#region Parse methods (non generic)

		public static SvData Parse(SessionEventArgs e)
		{
			try
			{
				var responseJson = e.GetResponseBodyAsString().GetAwaiter().GetResult();
				var requestBody = e.GetRequestBodyAsString().GetAwaiter().GetResult();

				if (string.IsNullOrWhiteSpace(responseJson))
					throw new InvalidOperationException("Response is empty.");
				if (responseJson.StartsWith("svdata="))
				{
					responseJson = responseJson.Substring("svdata=".Length);
				}
				var rawResult = JsonConvert.DeserializeObject<svdata>(responseJson);
				return new SvData(rawResult, requestBody);
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"[SvData] Parse failed: {ex}");
				throw;
			}
		}

		public static bool TryParse(SessionEventArgs session, out SvData result)
		{
			try
			{
				result = Parse(session);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
				result = null;
				return false;
			}

			return true;
		}

		#endregion
	}
}
