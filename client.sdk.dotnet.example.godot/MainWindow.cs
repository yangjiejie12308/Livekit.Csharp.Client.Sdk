using Client.Sdk.Dotnet.core;
using Godot;
using LiveKit.Proto;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

public partial class MainWindow : Node2D
{
	private Client.Sdk.Dotnet.core.Engine engine;
	private GridContainer grid;
	public override void _Ready()
	{
		// 创建UI层
		var canvasLayer = new CanvasLayer();
		canvasLayer.Layer = 1; // UI层级
		AddChild(canvasLayer);

		// 创建控制根节点
		var uiRoot = new Control();
		uiRoot.AnchorsPreset = (int)Control.LayoutPreset.FullRect;
		canvasLayer.AddChild(uiRoot);

		// 创建GridContainer
		grid = new GridContainer();
		grid.Columns = 2;
		grid.AnchorsPreset = (int)Control.LayoutPreset.FullRect;
		grid.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		grid.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		uiRoot.AddChild(grid);

		// 为GridContainer添加边框以便调试
		var styleBox = new StyleBoxFlat();
		styleBox.BgColor = new Color(0, 0, 0, 0); // 透明背景
		styleBox.BorderWidthBottom = styleBox.BorderWidthLeft =
		styleBox.BorderWidthRight = styleBox.BorderWidthTop = 2;
		styleBox.BorderColor = new Color(1, 0, 0); // 红色边框

		// 注意：在Godot 4.x中，主题样式设置方法如下
		grid.AddThemeStyleboxOverride("panel", styleBox);

		// 设置单元格间距
		grid.AddThemeConstantOverride("hseparation", 10); // 水平间距
		grid.AddThemeConstantOverride("vseparation", 10); // 垂直间距
		engine = new Client.Sdk.Dotnet.core.Engine("ws://127.0.0.1:7880", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE3NTA5ODg1MDgsImlzcyI6ImRldmtleSIsIm5hbWUiOiJ0ZXN0X3VzZXIzIiwibmJmIjoxNzUwNjQyOTA4LCJzdWIiOiJ0ZXN0X3VzZXIzIiwidmlkZW8iOnsicm9vbSI6InRlc3Rfcm9vbSIsInJvb21Kb2luIjp0cnVlfX0.TbYGQf6fPCdaesiJ-zco1B2_NGFphO1tecWwWvPj_No");
		//// 添加9个按钮到网格中，并确保它们填充单元格
		InitAsync().ConfigureAwait(false);
	}

	private async Task InitAsync()
	{
		engine.RemoteParticipantUpdated += Engine_RemoteParticipantUpdated;
		//engine.onVideoTrackAdded += Engine_onVideoTrackAdded;
		//engine.onVideoTrackRemoved += Engine_onVideoTrackRemoved;
		//engine.onAudioTrackAdded += Engine_onAudioTrackAdded;
		//engine.onAudioTrackRemoved += Engine_onAudioTrackRemoved;
		//engine.onAudioTrackUnMuted += Engine_onAudioTrackUnMuted;
		//engine.onAudioTrackMuted += Engine_onAudioTrackMuted;
		//engine.onVideoTrackMuted += Engine_onVideoTrackMuted;
		//engine.onVideoTrackUnMuted += Engine_onVideoTrackUnMuted;
		//engine.onParticipantConnectionQualityUpdated += Engine_onParticipantConnectionQualityUpdated;
		//engine.onSpeakersChangedEvent += Engine_onSpeakersChangedEvent;
		await engine.ConnectAsync();
	}



	private void Engine_onSpeakersChangedEvent(object? sender, List<SpeakerInfo> e)
	{
		foreach (var item in e)
		{
			var control = grid.GetNode<LkUserControl>($"{item.Sid}");
			control.Speaking();
		}
	}

	private void Engine_onParticipantConnectionQualityUpdated(object? sender, string e)
	{
		var control = grid.GetNode<LkUserControl>($"{e}");
		if (control != null)
		{
			control.QualityUpdated();
		}
	}

	private void Engine_onVideoTrackUnMuted(object? sender, (string, string) e)
	{
		var control = grid.GetNode<LkUserControl>($"{e.Item1}");
		if (control != null)
		{
			control.UnMuteVideo(e.Item2);
		}
	}

	private void Engine_onVideoTrackMuted(object? sender, (string, string) e)
	{
		var control = grid.GetNode<LkUserControl>($"{e.Item1}");
		if (control != null)
		{
			control.MuteVideo(e.Item2);
		}
	}

	private void Engine_onAudioTrackMuted(object? sender, (string, string) e)
	{
		var control = grid.GetNode<LkUserControl>($"{e.Item1}");
		if (control != null)
		{
			control.MuteAudio(e.Item2);
		}
	}

	private void Engine_onAudioTrackUnMuted(object? sender, (string, string) e)
	{
		var control = grid.GetNode<LkUserControl>($"{e.Item1}");
		if (control != null)
		{
			control.UnMuteAudio(e.Item2);
		}
	}

	private void Engine_onAudioTrackRemoved(object? sender, (string, string) e)
	{
		var control = grid.GetNode<LkUserControl>($"{e.Item1}");
		if (control != null)
		{
			control.RemoveAudio(e.Item2);
		}
	}

	private void Engine_onAudioTrackAdded(object? sender, (string, string) e)
	{
		var control = grid.GetNode<LkUserControl>($"{e.Item1}");
		if (control != null)
		{
			control.AddAudio(e.Item2);
		}
	}

	private void Engine_onVideoTrackRemoved(object? sender, (string, string) e)
	{
		var control = grid.GetNode<LkUserControl>($"{e.Item1}");
		if (control != null)
		{
			control.RemoveVideo(e.Item2);
		}
	}

	private void Engine_onVideoTrackAdded(object? sender, (string, string) e)
	{
		var control = grid.GetNode<LkUserControl>($"{e.Item1}");
		if (control != null)
		{
			control.AddVideo(e.Item2);
		}
	}


	private void Engine_RemoteParticipantUpdated(object? sender, ParticipantInfo e)
	{
		Debug.WriteLine($"Remote participant updated: {e.ToString()}");

		CallDeferred(nameof(AddControl), e.Sid);
	}

	void AddControl(string sid)
	{
		var control = grid.FindChild(sid) as LkUserControl;
		if (control == null)
		{
			var scren = GD.Load<PackedScene>("res://lk_user_control.tscn");
			var button = scren.Instantiate<LkUserControl>();
			button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			button.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
			button.CustomMinimumSize = new Vector2(400, 400); // 设置按钮最小尺寸
			button.Name = $"{sid}";
			button.Init(engine.RemoteParticipants.FirstOrDefault(v => v.Sid == sid), engine);
			grid.AddChild(button);
		}
	}
}
