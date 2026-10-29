using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UText = UnityEngine.UI.Text;

namespace IslandRocketRumble
{
    /// <summary>Result: K.O.! / TIME UP! / SUDDEN DEATH! + winner, HP lines, confetti, REMATCH / TITLE.</summary>
    public class ResultScreen : UIScreen
    {
        RectTransform card;
        RectTransform titleRt;
        UText titleText;
        RectTransform winnerRt;
        UText winnerText;
        UText winnerThai;
        UText line1, line2, rule;
        UIPillButton[] buttons;
        UIMenuNav nav;
        UIConfetti confetti;
        Coroutine intro;
        float lastConfirm = -10f;
        bool buttonsReady;

        protected override void Build()
        {
            UIFactory.StretchImg(rect, "Dim", null, UIFactory.WithAlpha(Palette.Ink, 0.5f));

            Image fill;
            card = UIFactory.Panel(rect, "Card", UIFactory.Center, new Vector2(0f, -60f), new Vector2(980f, 600f), Palette.Cream, out fill, 44f, 6f, true);

            winnerText = UIFactory.Txt(card, "Winner", "P1 WINS!", 100, Palette.PipBlue, TextAnchor.MiddleCenter, UIFactory.Center,
                new Vector2(0f, 150f), new Vector2(960f, 130f), true);
            UIFactory.Chunky(winnerText, 5f, 8f);
            winnerRt = winnerText.rectTransform;

            winnerThai = UIFactory.Txt(card, "WinnerThai", "", 36, Palette.Ink, TextAnchor.MiddleCenter, UIFactory.Center,
                new Vector2(0f, 70f), new Vector2(960f, 50f), true);

            line1 = UIFactory.Txt(card, "Line1", "", 36, Palette.Ink, TextAnchor.MiddleCenter, UIFactory.Center, new Vector2(0f, 10f),
                new Vector2(960f, 48f), true);
            line2 = UIFactory.Txt(card, "Line2", "", 36, Palette.Ink, TextAnchor.MiddleCenter, UIFactory.Center, new Vector2(0f, -40f),
                new Vector2(960f, 48f), true);
            rule = UIFactory.Txt(card, "Rule", "", 28, UIFactory.WithAlpha(Palette.Ink, 0.7f), TextAnchor.MiddleCenter, UIFactory.Center,
                new Vector2(0f, -90f), new Vector2(960f, 40f));

            buttons = new[]
            {
                new UIPillButton(card, "REMATCH", Palette.HpP1, Color.white, UIFactory.Center, new Vector2(-190f, -195f), new Vector2(330f, 96f)),
                new UIPillButton(card, "TITLE", Palette.Cream, Palette.Ink, UIFactory.Center, new Vector2(190f, -195f), new Vector2(330f, 96f)),
            };
            nav = new UIMenuNav(buttons.Length);

            titleText = UIFactory.Txt(rect, "Title", "K.O.!", 210, Palette.Danger, TextAnchor.MiddleCenter, UIFactory.Center,
                new Vector2(0f, 330f), new Vector2(1600f, 260f), true);
            UIFactory.Chunky(titleText, 9f, 14f);
            titleRt = titleText.rectTransform;

            var confettiRect = UIFactory.Stretch(rect, "Confetti");
            confetti = confettiRect.gameObject.AddComponent<UIConfetti>();
        }

        protected override void OnShow()
        {
            Refresh(true);
        }

        protected override void OnHide()
        {
            if (intro != null) StopCoroutine(intro);
            intro = null;
            confetti.Clear();
        }

        /// <summary>Fills texts from UIRoot's stored result. replayIntro restarts the slam animation.</summary>
        public void Refresh(bool replayIntro)
        {
            int hp1 = root.Hp[0];
            int hp2 = root.Hp[1];

            if (!root.HasResult)
            {
                titleText.text = "GAME!";
                titleText.color = Palette.HelmetYellow;
                winnerText.text = "";
                winnerThai.text = "";
                line1.text = "P1 HP " + hp1;
                line2.text = "P2 HP " + hp2;
                rule.text = "";
            }
            else
            {
                var w = root.LastWinner;
                var reason = root.LastReason;
                winnerText.text = UIText.PlayerTag(w) + " WINS!";
                winnerText.color = Palette.Player(w);
                winnerThai.text = UIText.PlayerThai(w) + " ชนะ!";

                switch (reason)
                {
                    case MatchEndReason.KnockOut:
                        titleText.text = "K.O.!";
                        titleText.color = Palette.Danger;
                        line1.text = "เกาะของ " + UIText.PlayerThai(w.Opponent()) + " แตกกระจาย!";
                        line2.text = UIText.PlayerTag(w) + " HP " + root.Hp[(int)w] + " เหลืออยู่";
                        rule.text = "HP เกาะ = 0 → K.O.";
                        break;
                    case MatchEndReason.TimeUp:
                        titleText.text = "TIME UP!";
                        titleText.color = Palette.HelmetOrange;
                        line1.text = HpLine(PlayerId.P1, hp1, w == PlayerId.P1);
                        line2.text = HpLine(PlayerId.P2, hp2, w == PlayerId.P2);
                        rule.text = "HP น้อยกว่า = แพ้";
                        break;
                    default:
                        titleText.text = "SUDDEN DEATH!";
                        titleText.color = Palette.Danger;
                        line1.text = UIText.PlayerThai(w) + " ยิงโดนก่อน!";
                        line2.text = "P1 HP " + hp1 + "  ·  P2 HP " + hp2;
                        rule.text = "HP เท่ากัน → ใครยิงโดนก่อนชนะ";
                        break;
                }
            }

            if (replayIntro && isActiveAndEnabled)
            {
                if (intro != null) StopCoroutine(intro);
                intro = StartCoroutine(Intro(root.HasResult));
            }
        }

        string HpLine(PlayerId id, int hp, bool win)
        {
            string tag = win ? UIText.Colored("(ชนะ)", UIText.Darker(Palette.Leaf, 0.8f)) : UIText.Colored("(แพ้)", Palette.Danger);
            return UIText.Colored(UIText.PlayerTag(id), Palette.Player(id)) + " HP " + hp + " " + tag;
        }

        IEnumerator Intro(bool celebrate)
        {
            buttonsReady = false;
            nav.Index = 0;
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].SetSelected(i == 0);
                buttons[i].Root.localScale = Vector3.zero;
            }
            card.localScale = Vector3.zero;
            winnerRt.localScale = Vector3.zero;

            UITween.Slam(titleRt, 3.2f, 0.32f);
            yield return Wait(0.32f);
            UITween.Shake(card, 22f, 0.45f);
            UITween.Shake(titleRt, 18f, 0.4f);
            UITween.PopIn(card, 0.45f, 0.05f);
            yield return Wait(0.3f);
            UITween.PopIn(winnerRt, 0.5f, 0f);
            if (celebrate) confetti.Burst(140);
            yield return Wait(0.5f);
            UITween.PopIn(buttons[0].Root, 0.35f, 0f);
            UITween.PopIn(buttons[1].Root, 0.35f, 0.08f);
            yield return Wait(0.4f);
            buttonsReady = true;
            if (celebrate) confetti.Burst(60);
            intro = null;
        }

        static IEnumerator Wait(float s)
        {
            float t = 0f;
            while (t < s)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        void Update()
        {
            float t = Time.unscaledTime;
            for (int i = 0; i < buttons.Length; i++) buttons[i].Tick(t);
            winnerRt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2.5f) * 3f);

            if (!buttonsReady || !InputReady(1.2f)) return;
            bool moved;
            int confirmed = nav.Poll(out moved);
            if (moved)
            {
                for (int i = 0; i < buttons.Length; i++) buttons[i].SetSelected(i == nav.Index);
                UITween.Punch(buttons[nav.Index].Root, 0.15f, 0.3f);
            }
            if (confirmed >= 0 && Time.unscaledTime - lastConfirm > 0.5f)
            {
                lastConfirm = Time.unscaledTime;
                UITween.Punch(buttons[confirmed].Root, 0.25f, 0.35f);
                if (confirmed == 0) GameEvents.RaiseRequestRematch();
                else GameEvents.RaiseRequestTitle();
            }
        }
    }

}
