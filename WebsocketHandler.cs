using System.Diagnostics.CodeAnalysis;
using Godot;

namespace CVSS_GodotCommons;

public abstract partial class WebsocketHandler(ApiHandler api) : Control {
    private readonly WebSocketPeer _overlaySocket = new();
    private readonly WebSocketPeer _timeSocket = new();
    protected abstract void ReceivedCommand(OverlayCommand cmd);
    protected abstract void ReceivedTime(int time);
    
    public override void _Ready() {
        Error e1 = _overlaySocket.ConnectToUrl(api.GetOverlayStreamAddress());
        if (e1 != Error.Ok) {
            GD.PrintErr("Unable to connect!");
            Remove();
        }
        
        Error e2 = _timeSocket.ConnectToUrl(api.GetTimeStreamAddress());
        if (e2 != Error.Ok) {
            GD.PrintErr("Unable to connect!");
            Remove();
        }
    }

    public override void _Process(double delta) {
        ProcessOverlaySocket();
        ProcessTimeSocket();
    }

    private void ProcessOverlaySocket() {
        _overlaySocket.Poll();
        switch (_overlaySocket.GetReadyState()) {
            case WebSocketPeer.State.Open:
                while (_overlaySocket.GetAvailablePacketCount() > 0) {
                    string s = _overlaySocket.GetPacket().GetStringFromUtf8();
                    if (Enum.TryParse(s, true, out OverlayCommand command)) {
                        GD.Print($"Received {s}");
                        ReceivedCommand(command);
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

    private void ProcessTimeSocket() {
        _timeSocket.Poll();
        switch (_timeSocket.GetReadyState()) {
            case WebSocketPeer.State.Open:
                while (_timeSocket.GetAvailablePacketCount() > 0) {
                    string s = _timeSocket.GetPacket().GetStringFromUtf8();
                    ReceivedTime(int.Parse(s));
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
        //GetParent().RemoveChild(this);
        QueueFree();
    }
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum OverlayCommand {
    SHOW_RIGHT,
    HIDE_RIGHT,
    SHOW_LEFT,
    HIDE_LEFT,
    SHOW_TIME,
    HIDE_TIME
}
