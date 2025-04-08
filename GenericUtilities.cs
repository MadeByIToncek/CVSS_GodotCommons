using Godot;

namespace CVSS_GodotCommons;

public abstract class GenericUtilities {
	public static LabelSettings GenerateLabelSettings(int fontSize = 180, String fontPath = "res://fonts/regular.ttf") {
		return new LabelSettings {
			FontSize = fontSize,
			Font = GD.Load<FontFile>(fontPath)
		};
	}
	
	public static float GetStringLength(string str, LabelSettings ls) {
		return ls.Font.GetStringSize(str, fontSize: ls.FontSize).X;
	}
}