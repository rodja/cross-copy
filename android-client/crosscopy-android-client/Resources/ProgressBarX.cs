
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using Android.Util;

namespace CrossCopy.AndroidClient
{
        public class ProgressBarX : ProgressBar
        {                
                public ProgressBarX (Context context) : base(context)
                { 
                }
                
                public ProgressBarX (Context context, IAttributeSet attrs) : base(context, attrs)
                {
                }
                
                public ProgressBarX (Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
                {
                }

                protected override void OnDraw (Canvas canvas)
                {
                        base.OnDraw (canvas);
                        //create an instance of class Paint, set color and font size
                        var textPaint = new Paint { 
                                AntiAlias = true,
                                Color = _textColor,
                                TextSize = _textSize };
                        //In order to show text in a middle, we need to know its size
                        var bounds = new Rect ();
                        textPaint.GetTextBounds (_text, 0, _text.Length, bounds);
                        //Now we store font size in bounds variable and can calculate it's position
                        var x = (Width / 2) - bounds.CenterX ();
                        var y = (Height / 2) - bounds.CenterY ();
                        //drawing text with appropriate color and size in the center
                        canvas.DrawText (_text, x, y, textPaint);
                }

                private string _text;
                public string Text {
                        get { return _text; }
                        set { 
                                _text = (value != null) ? value : String.Empty;
                                PostInvalidate ();
                        }
                }
                
                private Color _textColor = Color.Black;
                public Color TextColor {
                        get { return _textColor; }
                        set {
                                _textColor = value;
                                PostInvalidate ();
                        }
                }
                
                private float _textSize = 15;
                public float TextSize {
                        get { return _textSize; }
                        set { 
                                _textSize = value;
                                PostInvalidate ();
                        }
                }
        }
}

