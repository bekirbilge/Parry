using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Game.Unity
{
    /// <summary>Opt-in capture gallery. Does not supply input or edit the simulation.</summary>
    public sealed class AnimationSamplePreview : MonoBehaviour
    {
        private NinjaAnimationAtlas _atlas;
        private int _column;
        private GUIStyle _title, _label;
        private readonly string[] _names = { "IDLE", "RUN", "BOOST", "GUARD", "PARRY", "RECOIL" };

        private IEnumerator Start()
        {
            _atlas = NinjaAnimationAtlas.Load();
            string output = Path.Combine(Application.persistentDataPath, "AnimationPreview");
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "--parry-output") output = args[i + 1];
            Directory.CreateDirectory(output);
            if (_atlas == null) { Debug.LogError("Animation preview could not load atlas."); Application.Quit(1); yield break; }
            for (_column = 0; _column < 4; _column++)
            {
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "animation-" + _column + ".png"));
                yield return new WaitForEndOfFrame();
            }
            yield return new WaitForSecondsRealtime(.2f);
            File.WriteAllText(Path.Combine(output, "animation-preview.json"),
                "{\"completed\":true,\"frames\":4,\"poses\":6,\"atlasLoaded\":true}");
            Application.Quit(0);
        }

        private void OnGUI()
        {
            if (_atlas == null) return;
            int previousDepth = GUI.depth;
            GUI.depth = -100;
            if (_title == null)
            {
                _title = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter,
                    fontSize = 24, fontStyle = FontStyle.Bold };
                _label = new GUIStyle(_title) { fontSize = 18 };
            }
            GUI.color = new Color(.025f, .035f, .06f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(0, 6, Screen.width, 40), "NINJA — FIRST ANIMATION SAMPLE", _title);
            float width = Screen.width / 3f, height = (Screen.height - 52) / 2f;
            for (int row = 0; row < 6; row++)
            {
                Rect card = new Rect((row % 3) * width + 8, 52 + (row / 3) * height,
                    width - 16, height - 8);
                GUI.Box(card, "");
                GUI.Label(new Rect(card.x, card.y + 2, card.width, 28), _names[row], _label);
                // Match the same measured scale/sole anchoring used by the playable view.
                Rect stage = new Rect(card.x, card.y + 36, card.width, card.height - 44);
                Rect art = _atlas.FighterRect(stage, new Game.Core.Vec2(5, 2.5), 10, 6,
                    2.5f, row, Mathf.Min(3, _column), true);
                GUI.DrawTextureWithTexCoords(art, _atlas.Texture,
                    _atlas.Uv(row, Mathf.Min(3, _column), true), true);
            }
            GUI.depth = previousDepth;
        }
    }
}
