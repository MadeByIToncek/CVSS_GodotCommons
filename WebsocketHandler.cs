using System.Diagnostics.CodeAnalysis;
using Godot;

namespace CVSS_GodotCommons;

public partial class WebsocketHandler(ApiHandler api) : Control {
    private readonly WebSocketPeer _overlaySocket = new();
    private readonly WebSocketPeer _eventSocket = new();
    private readonly WebSocketPeer _timeSocket = new();
    public event EventHandler<OverlayCommand>? CommandReceived;
    public event EventHandler<Event>? EventReceived;
    public event EventHandler<int>? TimeReceived;
    
    public override void _Ready() {
        Error e1 = _overlaySocket.ConnectToUrl(api.GetOverlayStreamAddress());
        if (e1 != Error.Ok) {
            GD.PrintErr("Unable to connect!" + e1);
            Remove();
        }
        
        Error e2 = _eventSocket.ConnectToUrl(api.GetEventStreamAddress());
        if (e2 != Error.Ok) {
            GD.PrintErr("Unable to connect!" + e2);
            Remove();
        }
        
        Error e3 = _timeSocket.ConnectToUrl(api.GetTimeStreamAddress());
        if (e3 != Error.Ok) {
            GD.PrintErr("Unable to connect!" + e3);
            Remove();
        }
    }

    public override void _Process(double delta) {
        ProcessOverlaySocket();
        ProcessEventSocket();
        ProcessTimeSocket();
    }

    private void ProcessOverlaySocket() {
        _overlaySocket.Poll();
        switch (_overlaySocket.GetReadyState()) {
            case WebSocketPeer.State.Open:
                while (_overlaySocket.GetAvailablePacketCount() > 0) {
                    string s = _overlaySocket.GetPacket().GetStringFromUtf8();
                    if (Enum.TryParse(s, true, out OverlayCommand command)) {
                        GD.Print($"Received {command}");
                        CommandReceived?.Invoke(this,command);
                    }
                    else {
                        GD.PrintErr($"Unknown command {s}");
                    }
                }

                break;
            case WebSocketPeer.State.Closed:
                GD.PrintErr($"WS closed! {_overlaySocket.GetCloseCode()}, because {_overlaySocket.GetCloseReason()}");
                Remove();
                break;
            case WebSocketPeer.State.Connecting:
            case WebSocketPeer.State.Closing:
            default:
                break;
        }
    }

    private void ProcessEventSocket() {
        _eventSocket.Poll();
        switch (_eventSocket.GetReadyState()) {
            case WebSocketPeer.State.Open:
                while (_eventSocket.GetAvailablePacketCount() > 0) {
                    string s = _eventSocket.GetPacket().GetStringFromUtf8();
                    if (Enum.TryParse(s, true, out Event command)) {
                        GD.Print($"Received {command}");
                        EventReceived?.Invoke(this,command);
                    }
                    else {
                        GD.PrintErr($"Unknown command {s}");
                    }
                }

                break;
            case WebSocketPeer.State.Closed:
                GD.PrintErr($"WS closed! {_eventSocket.GetCloseCode()}, because {_eventSocket.GetCloseReason()}");
                Remove();
                break;
            case WebSocketPeer.State.Connecting:
            case WebSocketPeer.State.Closing:
            default:
                break;
        }
    }

    private void ProcessTimeSocket() {
        _timeSocket.Poll();
        switch (_timeSocket.GetReadyState()) {
            case WebSocketPeer.State.Open:
                while (_timeSocket.GetAvailablePacketCount() > 0) {
                    string s = _timeSocket.GetPacket().GetStringFromUtf8();
                    TimeReceived?.Invoke(this,int.Parse(s));
                }
                break;
            case WebSocketPeer.State.Closed:
                GD.PrintErr($"WS closed! {_timeSocket.GetCloseCode()}, because {_timeSocket.GetCloseReason()}");
                Remove();
                break;
            case WebSocketPeer.State.Connecting:
            case WebSocketPeer.State.Closing:
            default:
                break;
        }
    }


    public void Remove() {
        SetProcess(false);
        _overlaySocket.Dispose();
        _eventSocket.Dispose();
        _timeSocket.Dispose();
        //GetParent().RemoveChild(this);
        QueueFree();
    }
}

[SuppressMessage("ReSharper", "InconsistentNaming")] // 'cause it's java
public enum OverlayCommand {
    SHOW_RIGHT,
    HIDE_RIGHT,
    SHOW_LEFT,
    HIDE_LEFT,
    SHOW_TIME,
    HIDE_TIME
}
[SuppressMessage("ReSharper", "InconsistentNaming")] // 'cause it's java (again)
public enum Event {
    TEAM_UPDATE_EVENT,
    MATCH_UPDATE_EVENT,
    MATCH_ARM,
    MATCH_RESET,
    MATCH_START,
    MATCH_RECYCLE,
    MATCH_END,
    SCORE_CHANGED
}
