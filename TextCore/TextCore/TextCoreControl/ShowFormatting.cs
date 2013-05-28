﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAPICodePack.DirectX.DirectWrite;
using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;

namespace TextCoreControl
{
    public static class ShowFormatting
    {
        private class ShowFormattingInlineObject : ICustomInlineObject
        {
            public ShowFormattingInlineObject(TextLayout displayText, bool shouldDrawSimple)
            {
                this.displayText = displayText;
                this.shouldDrawSimple = shouldDrawSimple;
            }

            public void Draw(float originX, float originY, bool isSideways, bool isRightToLeft, Brush clientDrawingEffect)
            {
                if (this.shouldDrawSimple)
                {
                    renderTarget.DrawTextLayout(new Point2F(originX + showFormattingPadding, originY), this.displayText, showFormattingBrush);
                }
                else
                {
                    renderTarget.FillRoundedRectangle(new RoundedRect(new RectF(originX + 1.0f, originY, originX + this.Width + 1.0f, originY + displayText.Metrics.Height), showFormattingPadding, showFormattingPadding), showFormattingBrush);
                    renderTarget.DrawTextLayout(new Point2F(originX + showFormattingPadding + 1.0f, originY), this.displayText, ShowFormatting.showFormattingBrushAlt);
                }
            }

            public BreakCondition BreakConditionAfter { get { return BreakCondition.Neutral; } }
            public BreakCondition BreakConditionBefore { get { return BreakCondition.Neutral; } }
            public OverhangMetrics OverhangMetrics { get { return new OverhangMetrics(0,0,0,0); } }
            public InlineObjectMetrics Metrics
            {
                get
                {
                    return new InlineObjectMetrics(this.Width + 2.0f , this.Height, displayText.LineMetrics.First().Baseline, false);
                }
            }
            internal float Width
            {
                get
                {
                    return displayText.Metrics.Width + showFormattingPadding + showFormattingPadding;
                }
            }
            internal float Height
            {
                get
                {
                    return displayText.Metrics.Height + showFormattingPadding + showFormattingPadding;
                }
            }

            TextLayout displayText;
            bool shouldDrawSimple;
        }
        
        #region ApplyShowFormatting

        internal static void ApplyShowFormatting(string lineText, DWriteFactory dwriteFactory, TextLayout textLayout)
        {
            for (int i = 0; i < lineText.Length; i++)
            {
                if (textLayout.Text[i] == '*')
                {
                    char ch = lineText[(int)i];
                    if (IsStandardControlCharacter(ch))
                    {
                        // We have to format this character
                        TextLayout formattingTextLayout;
                        lock (formattingTextLayouts)
                        {
                            if (!formattingTextLayouts.TryGetValue(ch, out formattingTextLayout))
                            {
                                formattingTextLayout = dwriteFactory.CreateTextLayout(StandardControlCharacter[(int)ch], Settings.DefaultShowFormattingTextFormat, int.MaxValue, int.MaxValue);
                                formattingTextLayouts.Add(ch, formattingTextLayout);
                            }
                        }
                        textLayout.SetInlineObject(new ShowFormattingInlineObject(formattingTextLayout, StandardControlCharacterDrawSimple[(int)ch]), new TextRange((uint)i, 1));
                    }
                }
            }
        }

        internal static string PrepareShowFormatting(string lineText, bool ignoreLastCharacter)
        {
            StringBuilder stringBuilder = new StringBuilder();
            int maximumIndex = ignoreLastCharacter ? lineText.Length - 1: lineText.Length;
            for (int i = 0; i < maximumIndex; i++)
            {
                char ch = lineText[(int)i];
                if (IsStandardControlCharacter(ch))
                {
                    stringBuilder.Append('*');
                }
                else
                {
                    stringBuilder.Append(ch);
                }
            }
            if (ignoreLastCharacter && lineText.Length > 0)
            {
                stringBuilder.Append(lineText[lineText.Length - 1]);
            }
            return stringBuilder.ToString();
        }

        private static bool IsStandardControlCharacter(char ch)
        {
            return ch >= (char)0 && ch <= (char)32;
        }

        private static string[] StandardControlCharacter = { 
            "NUL", 
            "SOH", 
            "STX", 
            "ETX", 
            "EOT", 
            "ENQ", 
            "ACK", 
            "BEL", 
            "BS", 
            " >> ", 
            "\\n", 
            "VTAB", 
            "NP", 
            "\\r", 
            "SO", 
            "SI", 
            "DLE", 
            "DC1", 
            "DC2", 
            "DC3", 
            "DC4", 
            "NAK", 
            "SYN", 
            "ETB", 
            "CAN", 
            "EM", 
            "EOF", 
            "ESC", 
            "FS", 
            "GS", 
            "RS", 
            "US", 
            "."
        };

        private static bool[] StandardControlCharacterDrawSimple = { 
            false, // "NUL", 
            false, //"SOH", 
            false, //"STX", 
            false, //"ETX", 
            false, //"EOT", 
            false, //"ENQ", 
            false, //"ACK", 
            false, //"BEL", 
            false, //"BS", 
            true, //" >> ", 
            true, //"\\n", 
            false, //"VTAB", 
            false, //"NP", 
            true, //"\\r", 
            false, //"SO", 
            false, //"SI", 
            false, //"DLE", 
            false, //"DC1", 
            false, //"DC2", 
            false, //"DC3", 
            false, //"DC4", 
            false, //"NAK", 
            false, //"SYN", 
            false, //"ETB", 
            false, //"CAN", 
            false, //"EM", 
            false, //"EOF", 
            false, //"ESC", 
            false, //"FS", 
            false, //"GS", 
            false, //"RS", 
            false, //"US", 
            true //"."
        };
        #endregion

        #region Initialization
        static internal void InitDisplayResources(RenderTarget renderTarget)
        {
            ShowFormatting.renderTarget = renderTarget;
            ShowFormatting.showFormattingBrush = renderTarget.CreateSolidColorBrush(Settings.DefaultShowFormattingColor);
            ShowFormatting.showFormattingBrushAlt = renderTarget.CreateSolidColorBrush(Settings.DefaultShowFormattingColorAlt); 
            formattingTextLayouts = new Dictionary<char, TextLayout>();
        }
        static internal void NotifyOfSettingsChanged()
        {
            ShowFormatting.showFormattingBrush = ShowFormatting.renderTarget.CreateSolidColorBrush(Settings.DefaultShowFormattingColor);
            ShowFormatting.showFormattingBrushAlt = renderTarget.CreateSolidColorBrush(Settings.DefaultShowFormattingColorAlt); 
            formattingTextLayouts = new Dictionary<char, TextLayout>();
        }
        #endregion

        private static RenderTarget renderTarget;
        private static Brush showFormattingBrush;
        private static Brush showFormattingBrushAlt;
        private static Dictionary<char, TextLayout> formattingTextLayouts;
        private const float showFormattingPadding = 2.0f;
    }
   
}
