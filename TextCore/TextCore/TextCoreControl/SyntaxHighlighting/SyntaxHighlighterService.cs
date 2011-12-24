﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAPICodePack.DirectX.Controls;
using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;

namespace TextCoreControl.SyntaxHighlighting
{
    /// <summary>
    ///     Syntax hightlighting service that the display manager
    ///     will interact with. Maintains state information and 
    ///     drives the SyntaxHighlighterEngine.
    /// </summary>
    internal class SyntaxHighlighterService
    {
        internal SyntaxHighlighterService(string fullSyntaxFilePath, Document document)
        {
            this.syntaxHighlighterEngine = new SyntaxHightlighterEngine(fullSyntaxFilePath);
            this.syntaxHighlighterEngine.highlightRange += new SyntaxHightlighterEngine.HighlightRange(syntaxHighlighterEngine_highlightRange);
            this.colorTable = null;
            this.document = document;
            this.syntaxHighlighterStates = new OrdinalKeyedLinkedList<int>();
            this.syntaxHighlighterStates.Insert(document.FirstOrdinal(), this.syntaxHighlighterEngine.GetInitialState());
            this.dirtySyntaxStateBeginOrdinal = int.MaxValue;
            this.dirtySyntaxHighlightBeginOrdinal = int.MaxValue;
        }

        internal void NotifyOfContentChange(int beginOrdinal, int endOrdinal, string content)
        {
            if (beginOrdinal == Document.UNDEFINED_ORDINAL)
            {
                // Full reset, most likely a new file was loaded.
                this.syntaxHighlighterStates = new OrdinalKeyedLinkedList<int>();
                this.syntaxHighlighterStates.Insert(document.FirstOrdinal(), this.syntaxHighlighterEngine.GetInitialState());
                this.dirtySyntaxStateBeginOrdinal = int.MaxValue;
                this.dirtySyntaxHighlightBeginOrdinal = int.MaxValue;
            }
            else
            {
                this.syntaxHighlighterStates.Delete(beginOrdinal, endOrdinal);
                this.dirtySyntaxStateBeginOrdinal = Math.Min(document.PreviousOrdinal(beginOrdinal), this.dirtySyntaxStateBeginOrdinal);
                this.dirtySyntaxHighlightBeginOrdinal = this.dirtySyntaxStateBeginOrdinal;
            }
        }

        internal bool CanReuseLine(VisualLine visualLine)
        {
            return visualLine.BeginOrdinal <= this.dirtySyntaxHighlightBeginOrdinal;
        }

        internal void InitDisplayResources(HwndRenderTarget hwndRenderTarget)
        {
            // The color table matches SyntaxHightlighterEngine.HighlightStyle Enum.
            SolidColorBrush[] colorTable = {
                /*NONE*/                    null, 
                /*KEYWORD1*/                hwndRenderTarget.CreateSolidColorBrush(new ColorF(   0, 102f/255, 153f/255)), 
                /*KEYWORD2*/                hwndRenderTarget.CreateSolidColorBrush(new ColorF(   0,   0, 128f/255)),
                /*KEYWORD3*/                hwndRenderTarget.CreateSolidColorBrush(new ColorF(   0,   0, 255f/255)), 
                /*KEYWORD4*/                hwndRenderTarget.CreateSolidColorBrush(new ColorF(   0,   0, 255f/255)), 
                /*KEYWORD5*/                hwndRenderTarget.CreateSolidColorBrush(new ColorF(   0,   0, 255f/255)), 
                /*KEYWORD6*/                hwndRenderTarget.CreateSolidColorBrush(new ColorF( 139f/255,   0,   0)), 
                /*PREPROCESSORKEYWORD*/     hwndRenderTarget.CreateSolidColorBrush(new ColorF(   0, 128f/255,   0)),
                /*PREPROCESSOR*/            hwndRenderTarget.CreateSolidColorBrush(new ColorF(   0, 155f/255,  91f/255)),
                /*COMMENT*/                 hwndRenderTarget.CreateSolidColorBrush(new ColorF( 170f/255, 170f/255, 170f/255)), 
                /*OPERATOR*/                hwndRenderTarget.CreateSolidColorBrush(new ColorF( 230f/255,  51f/255,  51f/255)),
                /*BRACKET*/                 hwndRenderTarget.CreateSolidColorBrush(new ColorF( 250f/255,  51f/255,  51f/255)),
                /*NUMBER*/                  hwndRenderTarget.CreateSolidColorBrush(new ColorF( 184f/255, 134f/255,  11f/255)),
                /*STRING*/                  hwndRenderTarget.CreateSolidColorBrush(new ColorF(   0f/255, 100f/255,  0f/255)),
                /*CHAR*/                    hwndRenderTarget.CreateSolidColorBrush(new ColorF(   0f/255, 100f/255,  0f/255))
            };
            this.colorTable = colorTable;
        } 

        internal void HighlightLine(ref VisualLine visualLine)
        {
            this.currentVisualLine = visualLine;
            this.dirtySyntaxHighlightBeginOrdinal = Math.Max(this.dirtySyntaxHighlightBeginOrdinal, visualLine.BeginOrdinal);

            int opaqueStateIn;
            int ordinalFound;
            int stateNeededOrdinal = visualLine.BeginOrdinal;
            if (stateNeededOrdinal > this.dirtySyntaxStateBeginOrdinal)
            {
                System.Diagnostics.Debug.Assert(this.dirtySyntaxStateBeginOrdinal != int.MaxValue);
                stateNeededOrdinal = this.dirtySyntaxStateBeginOrdinal;
            }
            bool found;
            if (stateNeededOrdinal == document.LastOrdinal())
            {
                found = this.syntaxHighlighterStates.Find(Document.UNDEFINED_ORDINAL, out ordinalFound, out opaqueStateIn);
            }
            else
            {
                found = this.syntaxHighlighterStates.Find(stateNeededOrdinal, out ordinalFound, out opaqueStateIn);
            }
            System.Diagnostics.Debug.Assert(found, "Atleast the first ordinal is available in the states collection.");

            if (ordinalFound != visualLine.BeginOrdinal)
            {
                // Need to synthesize forward
                System.Diagnostics.Debug.Assert(ordinalFound < visualLine.BeginOrdinal);
                StringBuilder stringBuilder = new StringBuilder();
                int beginOrdinal = visualLine.BeginOrdinal;
                int currentOrdinal = ordinalFound;
                while (currentOrdinal <= beginOrdinal)
                {
                    stringBuilder.Append(document.CharacterAt(currentOrdinal));
                    currentOrdinal = document.NextOrdinal(currentOrdinal);
#if DEBUG
                    DebugHUD.IterationsSynthesizingSyntaxState++;
#endif
                }
                string text = stringBuilder.ToString();
                opaqueStateIn = this.syntaxHighlighterEngine.SynthesizeStateForward(text, opaqueStateIn);
                this.syntaxHighlighterStates.Insert(beginOrdinal, opaqueStateIn);
                this.dirtySyntaxStateBeginOrdinal = Math.Max(this.dirtySyntaxStateBeginOrdinal, beginOrdinal);
            }

            // Call to highlight text, the engine will now use callbacks 
            // to highlight the text passed in.
            int opaqueStateOut;
            syntaxHighlighterEngine.HighlightText(visualLine.Text, opaqueStateIn, out opaqueStateOut);

            if (visualLine.NextOrdinal == Document.UNDEFINED_ORDINAL)
            {
                this.syntaxHighlighterStates.Insert(document.LastOrdinal(), opaqueStateOut);
                this.dirtySyntaxStateBeginOrdinal = int.MaxValue;
            }
            else
            {
                bool insertedDiffentValue = this.syntaxHighlighterStates.Insert(visualLine.NextOrdinal, opaqueStateOut);

                if (visualLine.NextOrdinal > this.dirtySyntaxStateBeginOrdinal)
                {
                    if (insertedDiffentValue)
                    {
                        this.dirtySyntaxStateBeginOrdinal = Math.Max(this.dirtySyntaxStateBeginOrdinal, visualLine.NextOrdinal);
                    }
                    else
                    {
                        // more dirtiness can be wiped out
                        this.dirtySyntaxStateBeginOrdinal = int.MaxValue;
                        this.dirtySyntaxHighlightBeginOrdinal = int.MaxValue;
                    }
                }
            }
        }

        private void syntaxHighlighterEngine_highlightRange(uint beginOffset, uint length, SyntaxHightlighterEngine.HighlightStyle style)
        {
            if (colorTable != null)
            {
                currentVisualLine.SetDrawingEffect(this.colorTable[(int)style], beginOffset, length);
            }
        }

        VisualLine currentVisualLine;
        SyntaxHightlighterEngine syntaxHighlighterEngine;
        SolidColorBrush[] colorTable;
        Document document;
        OrdinalKeyedLinkedList<int> syntaxHighlighterStates;
        
        /// <summary>
        ///     All ordinals greater than dirtySyntaxHighlighterStatesBeginOrdinal
        ///     in syntaxHighlighterStates have invalid state data.
        /// </summary>
        int dirtySyntaxStateBeginOrdinal;

        /// <summary>
        ///     All lines that begin after this ordinal need to be rehighlighted.
        /// </summary>
        int dirtySyntaxHighlightBeginOrdinal;
    }
}