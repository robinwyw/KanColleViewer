using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Grabacr07.KanColleWrapper.Models;
using Nekoxy;
using Titanium.Web.Proxy.EventArguments;

namespace Grabacr07.KanColleWrapper
{
	public static class KanColleProxyExtensions
	{
		/// <summary>
		/// Nekoxy でフックした <see cref="Session"/> オブジェクトの <see cref="Session.Response"/> データを
		/// <typeparamref name="TResult"/> 型にパースします。
		/// </summary>
		public static IObservable<SvData<TResult>> TryParse<TResult>(this IObservable<SessionEventArgs> source)
		{
			Func<SessionEventArgs, SvData<TResult>> converter = session =>
			{
				SvData<TResult> result;
				return SvData.TryParse(session, out result) ? result : null;
			};

			return source.Select(converter).Where(x => x != null && x.IsSuccess);
		}


		/// <summary>
		/// Nekoxy でフックした <see cref="Session" /> オブジェクトの <see cref="Session.Response" /> データを
		/// <see cref="SvData" /> 型にパースします。
		/// </summary>
		public static IObservable<SvData> TryParse(this IObservable<SessionEventArgs> source)
		{
			Func<SessionEventArgs, SvData> converter = session =>
			{
				SvData result;
				return SvData.TryParse(session, out result) ? result : null;
			};

			return source.Select(converter).Where(x => x != null && x.IsSuccess);
		}

	}
}
