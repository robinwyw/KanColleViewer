using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Grabacr07.KanColleViewer.Models.Settings;
using Livet;
using MetroTrilithon.Lifetime;
using MetroTrilithon.UI.Controls;

namespace Grabacr07.KanColleViewer.Views
{
	partial class KanColleWindow : IDisposableHolder
	{
		private static readonly Size informationDefaultSize = new Size(566, 358);
		private readonly KanColleWindowSettings settings;
		private readonly LivetCompositeDisposable compositeDisposable = new LivetCompositeDisposable();
		private Size? previousBrowserSize;
		private Dock? previousDock;

		[DllImport("user32.dll")]
		private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

		public KanColleWindow()
		{
			this.InitializeComponent();

			this.settings = SettingsHost.Instance<KanColleWindowSettings>();
			this.settings.IsSplit.Subscribe(_ => this.ChangeSizeByDock()).AddTo(this);
			this.settings.Dock.Subscribe(_ => this.ChangeSizeByDock()).AddTo(this);
			this.StateChanged += MainWindow_StateChanged;
		}

		private async void MainWindow_StateChanged(object sender, EventArgs e)
		{
			if (this.WindowState == WindowState.Normal)
			{
				await Task.Delay(150);
				var hwnd = new WindowInteropHelper(this).Handle;
				if (hwnd != IntPtr.Zero)
				{
					// 获取屏幕的 DPI
					var source = PresentationSource.FromVisual(this);
					double dpiX = 96.0, dpiY = 96.0;
					if (source?.CompositionTarget != null)
					{
						dpiX *= source.CompositionTarget.TransformToDevice.M11;
						dpiY *= source.CompositionTarget.TransformToDevice.M22;
					}

					// 将逻辑像素换算为物理像素
					int x = (int)(this.Left * dpiX / 96.0);
					int y = (int)(this.Top * dpiY / 96.0);
					int width = (int)(this.Width * dpiX / 96.0);
					int height = (int)(this.Height * dpiY / 96.0);

					// 轻微移动再复位，触发 WM_MOVE / WM_WINDOWPOSCHANGED
					MoveWindow(hwnd, x + 1, y, width, height, false);
					MoveWindow(hwnd, x, y, width, height, false);

					// 重新通知 CEF
					kanColleHost?.WebBrowser?.GetBrowser()?.GetHost()?.WasResized();
				}
			}
		}

		private void HandleKanColleHostSizeChangeRequested(object sender, Size size)
		{
			this.ChangeSizeByBrowser(size);
		}

		private void ChangeSizeByBrowser(Size browserSize)
		{
			if (!this.settings.AutomaticallyResize) return;

			// サイズ計算のためにいったんリミット外す
			this.MinWidth = .0;
			this.MinHeight = .0;

			if (this.previousBrowserSize != null)
			{
				if (this.previousDock == Dock.Top || this.previousDock == Dock.Bottom)
				{
					var diffW = this.previousBrowserSize.Value.Width - browserSize.Width;
					if (Math.Abs(diffW) > 0.00001 && this.Width - diffW > 0) this.Width -= diffW;
				}
				else if (this.previousDock == Dock.Left || this.previousDock == Dock.Right)
				{
					var diffH = this.previousBrowserSize.Value.Height - browserSize.Height;
					if (Math.Abs(diffH) > 0.00001 && this.Height - diffH > 0) this.Height -= diffH;
				}
			}
			else
			{
				if (this.previousBrowserSize != browserSize)
				{
					this.previousBrowserSize = browserSize;

					if (this.previousDock == Dock.Top || this.previousDock == Dock.Bottom)
					{
						this.Height += browserSize.Height;
					}
					else if (this.previousDock == Dock.Left || this.previousDock == Dock.Right)
					{
						this.Width += browserSize.Width;
					}
				}
			}

			this.MinWidth = browserSize.Width + this.BorderThickness.Left + this.BorderThickness.Right;
			this.MinHeight = browserSize.Height + this.toolbarArea.ActualHeight + this.captionBar.ActualHeight + this.statusBar.ActualHeight;
		}

		private void ChangeSizeByDock()
		{
			this.ChangeSizeByDock(this.settings.IsSplit ? (Dock?)null : this.settings.Dock.Value.Reverse());
		}

		private void ChangeSizeByDock(Dock? dock)
		{
			if (!this.settings.AutomaticallyResize) return;

			var browserSize = this.previousBrowserSize ?? new Size();

			var width = browserSize.Width;
			var height = browserSize.Height + this.toolbarArea.ActualHeight + this.captionBar.ActualHeight + this.statusBar.ActualHeight;

			if (dock == Dock.Top || dock == Dock.Bottom)
			{
				height += Math.Max(this.informationArea.ActualHeight, informationDefaultSize.Height);
			}
			else if (dock == Dock.Left || dock == Dock.Right)
			{
				width += Math.Max(this.informationArea.ActualWidth, informationDefaultSize.Width);
			}

			this.Width = width;
			this.Height = height;

			this.previousDock = dock;
		}

		ICollection<IDisposable> IDisposableHolder.CompositeDisposable => this.compositeDisposable;

		void IDisposable.Dispose()
		{
			this.compositeDisposable.Dispose();
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			((IDisposable)this).Dispose();

			Application.Instance.Shutdown();
		}
	}
}
