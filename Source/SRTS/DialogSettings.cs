using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace SRTS
{
    public static class DialogSettings
    {
        static Color textColor = Color.white;

        public static void Settings_IntegerBox(this Listing_Standard lister, string text, ref int value, float labelLength, float padding, int min = int.MinValue, int max = int.MaxValue)
        {
            lister.Gap(12f);
            Rect rect = lister.GetRect(Text.LineHeight);

            Rect rectLeft = new Rect(rect.x, rect.y, labelLength, rect.height);
            Rect rectRight = new Rect(rect.x + labelLength + padding, rect.y, rect.width - labelLength - padding, rect.height);

            Color color = GUI.color;
            Widgets.Label(rectLeft, text);

            var align = Text.CurTextFieldStyle.alignment;

            string buffer = value.ToString();
            Widgets.TextFieldNumeric(rectRight, ref value, ref buffer, min, max);

            Text.CurTextFieldStyle.alignment = align;
            GUI.color = color;
        }

        public static void Settings_SliderLabeled(this Listing_Standard lister, string label, string endSymbol, ref float value, float min, float max, float multiplier = 1f, int decimalPlaces = 2)
        {
            lister.Gap(12f);
            Rect rect = lister.GetRect(24f);
            string format = string.Format("{0}" + endSymbol, Math.Round(value * multiplier, decimalPlaces));
            value = Widgets.HorizontalSlider(rect, value, min, max, false, null, label, format);
        }

        public static void Settings_SliderLabeled(this Listing_Standard lister, string label, string endSymbol, ref int value, int min, int max, int endValue = -1)
        {
            lister.Gap(12f);
            Rect rect = lister.GetRect(24f);
            string format = string.Format("{0}" + endSymbol, value);
            value = (int)Widgets.HorizontalSlider(rect, value, min, max, false, null, label, format);
            if(endValue > 0 && value == max)
                value = endValue;
        }

        public static void Settings_Header(this Listing_Standard lister, string header, Color highlight, GameFont fontSize = GameFont.Medium)
        {
            var textSize = Text.Font;
            Text.Font = fontSize;

            Rect rect = lister.GetRect(Text.CalcHeight(header, lister.ColumnWidth));
            GUI.color = highlight;
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
            GUI.color = textColor;

            var anchor = Text.Anchor;
            Widgets.Label(rect, header);
            Text.Font = textSize;   
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Anchor = anchor;
            lister.Gap(12f);
        }
    }
}
