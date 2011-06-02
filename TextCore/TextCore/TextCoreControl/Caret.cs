﻿using System;
using System.Windows;
using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;
using System.Runtime.InteropServices;

namespace TextCoreControl
{
    public class Caret
    {
        [DllImport("User32.dll")]
        static extern bool CreateCaret(IntPtr hWnd, int hBitmap, int nWidth, int nHeight);

        [DllImport("User32.dll")]
        static extern bool SetCaretPos(int x, int y);

        [DllImport("User32.dll")]
        static extern bool DestroyCaret();

        [DllImport("User32.dll")]
        static extern bool ShowCaret(IntPtr hWnd);

        [DllImport("User32.dll")]
        static extern bool HideCaret(IntPtr hWnd);

        public Caret(HwndRenderTarget renderTarget, int defaultHeight)
        {
            this.caretHeight = defaultHeight;
            windowHandle = renderTarget.WindowHandle;

            CreateCaret(windowHandle, 0, 1, caretHeight);
            ShowCaret(windowHandle);
        }

        ~Caret()
        {
            HideCaret(windowHandle);
            DestroyCaret();
        }

        public void MoveCaret(VisualLine visualLine, Document document, int ordinal)
        {
            float x = visualLine.CharPosition(document, ordinal);
            if (visualLine.Height != caretHeight)
            {
                this.caretHeight = (int) visualLine.Height;
                CreateCaret(windowHandle, 0, 1, caretHeight);
                ShowCaret(windowHandle);
            }

            SetCaretPos((int)(visualLine.Position.X + x), (int)visualLine.Position.Y);
            this.ordinal = ordinal;
        }

        public int Ordinal
        {
            get { return this.ordinal; }
        }

        int ordinal;
        int caretHeight;
        IntPtr windowHandle;
    }
}
