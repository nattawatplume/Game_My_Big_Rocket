using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UText = UnityEngine.UI.Text;

namespace IslandRocketRumble
{
    /// <summary>
    /// Settings overlay (gear button or Esc). Sound, screen and match options, plus resume / title / quit.
    /// Keyboard: W/S or arrows pick a row, A/D or arrows change it, Space/Enter confirms. Every control is also clickable.
    /// </summary>
    public class SettingsScreen : UIScreen
    {
        const int OptionRows = 7;
        const int ButtonRow = OptionRows;

        static readonly string[] Labels = { "เสียงรวม", "เพลง", "เอฟเฟกต์", "เต็มจอ", "สั่นจอ", "เวลาต่อเกม", "เลือดเกาะ" };
        static readonly float[] RowY = { 265f, 200f, 135f, 5f, -60f, -190f, -255f };

        class Row
        {
            public Image highlight;
            public UText value;
            public Image[] bars;
        }

        readonly Row[] rows = new Row[OptionRows];
        readonly UIPillButton[] buttons = new UIPillButton[3];
        RectTransform card;
        int selected;
        int buttonIndex;

        bool InMatch => root.Phase != GamePhase.Title;

        /// <summary>A browser tab cannot be closed by the game, so the web build has no quit button.</summary>
        static bool CanQuit => Application.platform != RuntimePlatform.WebGLPlayer;

        bool Available(int button) => button == 0 || (button == 1 && InMatch) || (button == 2 && CanQuit);

        protected override void Build()
        {
            var dim = UIFactory.StretchImg(rect, "Dim", null, UIFactory.WithAlpha(Palette.Ink, 0.55f));
            dim.raycastTarget = true;

            Image fill;
            card = UIFactory.Panel(rect, "Card", UIFactory.Center, new Vector2(0f, -5f), new Vector2(960f, 940f), Palette.Cream, out fill, 44f, 6f, true);

            var ribbon = UIFactory.Panel(card, "Ribbon", UIFactory.TopCenter, new Vector2(0f, 6f), new Vector2(560f, 96f), Palette.HelmetYellow, 48f, 6f, true);
            var title = UIFactory.Txt(ribbon, "Title", "SETTINGS · ตั้งค่า", 50, Color.white, TextAnchor.MiddleCenter, UIFactory.Center,
                new Vector2(0f, 2f), new Vector2(540f, 90f), true);
            UIFactory.Chunky(title, 3f, 4f);

            Section("เสียง", 330f);
            Section("หน้าจอ", 70f);
            Section("เกม", -125f);
            for (int i = 0; i < OptionRows; i++) rows[i] = BuildRow(i);

            UIFactory.Txt(card, "Note", "เวลาต่อเกมและเลือดเกาะ ใช้ตอนเริ่มเกมใหม่", 22, UIFactory.WithAlpha(Palette.Ink, 0.6f),
                TextAnchor.MiddleCenter, UIFactory.Center, new Vector2(0f, -305f), new Vector2(880f, 32f));

            string[] names = { "กลับเข้าเกม", "หน้าแรก", "ออกเกม" };
            Color[] fills = { Palette.HpP1, Palette.Cream, Palette.Danger };
            Color[] texts = { Color.white, Palette.Ink, Color.white };
            for (int i = 0; i < buttons.Length; i++)
            {
                int index = i;
                buttons[i] = new UIPillButton(card, names[i], fills[i], texts[i], UIFactory.Center, new Vector2((i - 1) * 290f, -375f), new Vector2(260f, 80f));
                buttons[i].Fill.raycastTarget = true;
                Clickable(buttons[i].Fill.gameObject, () => { selected = ButtonRow; buttonIndex = index; Activate(index); });
            }
            if (!CanQuit)
            {
                buttons[2].Root.gameObject.SetActive(false);
                buttons[0].Root.anchoredPosition = new Vector2(-145f, -375f);
                buttons[1].Root.anchoredPosition = new Vector2(145f, -375f);
            }

            UIFactory.Txt(card, "Hint", "W/S เลือก · A/D ปรับ · SPACE/ENTER ตกลง · ESC ปิด", 22, UIFactory.WithAlpha(Palette.Ink, 0.7f),
                TextAnchor.MiddleCenter, UIFactory.Center, new Vector2(0f, -440f), new Vector2(880f, 30f));
        }

        void Section(string text, float y)
        {
            UIFactory.Txt(card, "Section", text, 26, Palette.CraftRing, TextAnchor.MiddleLeft, UIFactory.Center,
                new Vector2(-260f, y), new Vector2(380f, 36f), true);
        }

        Row BuildRow(int i)
        {
            var row = new Row();
            var rt = UIFactory.Rect(card, "Row_" + i, UIFactory.Center, new Vector2(0f, RowY[i]), new Vector2(840f, 58f));
            row.highlight = UIFactory.StretchImg(rt, "Highlight", UISprites.Rounded, UIFactory.WithAlpha(Palette.HelmetYellow, 0.5f));
            UIFactory.SetRadius(row.highlight, 20f);
            UIFactory.Txt(rt, "Label", Labels[i], 30, Palette.Ink, TextAnchor.MiddleLeft, UIFactory.MidLeft, new Vector2(30f + 150f, 0f),
                new Vector2(300f, 50f), true);

            int index = i;
            ArrowButton(rt, new Vector2(90f, 0f), "◀", () => { selected = index; Change(index, -1); });
            ArrowButton(rt, new Vector2(390f, 0f), "▶", () => { selected = index; Change(index, 1); });

            if (i <= 2)
            {
                row.bars = new Image[10];
                for (int b = 0; b < row.bars.Length; b++)
                {
                    row.bars[b] = UIFactory.Img(rt, "Bar" + b, UISprites.Rounded, Color.white, UIFactory.Center,
                        new Vector2(132f + b * 24f, 0f), new Vector2(18f, 34f));
                    UIFactory.SetRadius(row.bars[b], 5f);
                }
            }
            else
            {
                row.value = UIFactory.Txt(rt, "Value", "", 30, Palette.Ink, TextAnchor.MiddleCenter, UIFactory.Center, new Vector2(240f, 0f),
                    new Vector2(240f, 50f), true);
            }
            return row;
        }

        void ArrowButton(Transform parent, Vector2 pos, string label, System.Action onClick)
        {
            var img = UIFactory.Img(parent, "Arrow", UISprites.Circle, Color.white, UIFactory.Center, pos, new Vector2(48f, 48f));
            UIFactory.AddOutline(img, Palette.Ink, 3f);
            img.raycastTarget = true;
            UIFactory.Txt(img.transform, "L", label, 24, Palette.Ink, TextAnchor.MiddleCenter, UIFactory.Center, new Vector2(1f, 1f),
                new Vector2(48f, 48f), true);
            Clickable(img.gameObject, onClick);
        }

        static void Clickable(GameObject go, System.Action onClick)
        {
            var button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() =>
            {
                // Keep Space/Enter from re-pressing the clicked control later.
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
                onClick();
            });
        }

        // ------------------------------------------------------------------ lifecycle

        protected override void OnShow()
        {
            group.blocksRaycasts = true;
            group.interactable = true;
            selected = 0;
            buttonIndex = 0;
            Refresh();
            UITween.PopIn(card, 0.3f, 0f);
        }

        protected override void OnHide()
        {
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && Time.unscaledTime - shownAt > 0.1f)
            {
                bool up = kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame;
                bool down = kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame;
                bool left = kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame;
                bool right = kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame;
                bool confirm = kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame;

                if (up) Select(selected - 1);
                if (down) Select(selected + 1);
                if (selected < ButtonRow)
                {
                    if (left) Change(selected, -1);
                    if (right) Change(selected, 1);
                    if (confirm && (selected == 3 || selected == 4)) Change(selected, 1);
                }
                else
                {
                    if (left) MoveButton(-1);
                    if (right) MoveButton(1);
                    if (confirm) Activate(buttonIndex);
                }
            }

            float t = Time.unscaledTime;
            for (int i = 0; i < buttons.Length; i++) buttons[i].Tick(t);
        }

        void Select(int row)
        {
            selected = (row + OptionRows + 1) % (OptionRows + 1);
            Blip(GameSfx.UiMove);
            Refresh();
        }

        void MoveButton(int delta)
        {
            do buttonIndex = (buttonIndex + delta + buttons.Length) % buttons.Length;
            while (!Available(buttonIndex));
            Blip(GameSfx.UiMove);
            Refresh();
        }

        void Change(int row, int delta)
        {
            switch (row)
            {
                case 0: GameSettings.Master += delta; break;
                case 1: GameSettings.Music += delta; break;
                case 2: GameSettings.Sfx += delta; break;
                case 3: GameSettings.Fullscreen = !GameSettings.Fullscreen; break;
                case 4: GameSettings.ScreenShake = !GameSettings.ScreenShake; break;
                case 5: GameSettings.MatchIndex += delta; break;
                case 6: GameSettings.HpIndex += delta; break;
            }
            // Nothing is under way on the title screens, so new match rules can apply now (How To Play shows them).
            if (row >= 5 && (root.Phase == GamePhase.Title || root.Phase == GamePhase.HowToPlay))
                GameSettings.ApplyToConfig(root.Config);
            Blip(GameSfx.UiTarget);
            Refresh();
        }

        void Activate(int index)
        {
            switch (index)
            {
                case 0:
                    root.CloseSettings();
                    break;
                case 1:
                    if (!InMatch) return;
                    root.CloseSettings();
                    GameEvents.RaiseRequestTitle();
                    break;
                case 2:
                    if (!CanQuit) return;
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                    break;
            }
        }

        static void Blip(AudioClip clip)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.Play(clip, 0.5f, 0f, 0f, minInterval: 0.02f);
        }

        void Refresh()
        {
            int[] volumes = { GameSettings.Master, GameSettings.Music, GameSettings.Sfx };
            for (int i = 0; i < OptionRows; i++)
            {
                var row = rows[i];
                row.highlight.enabled = i == selected;
                if (row.bars != null)
                {
                    for (int b = 0; b < row.bars.Length; b++)
                        row.bars[b].color = b < volumes[i] ? Palette.Leaf : UIFactory.WithAlpha(Palette.Ink, 0.15f);
                }
            }
            rows[3].value.text = GameSettings.Fullscreen ? "เปิด" : "ปิด";
            rows[4].value.text = GameSettings.ScreenShake ? "เปิด" : "ปิด";
            rows[5].value.text = GameSettings.MatchMinutes[GameSettings.MatchIndex] + " นาที";
            rows[6].value.text = GameSettings.IslandHpOptions[GameSettings.HpIndex] + " HP";

            for (int i = 0; i < buttons.Length; i++)
                buttons[i].SetSelected(selected == ButtonRow && i == buttonIndex);
            var titleLabel = buttons[1].Label;
            titleLabel.color = UIFactory.WithAlpha(Palette.Ink, InMatch ? 1f : 0.3f);
        }
    }
}
