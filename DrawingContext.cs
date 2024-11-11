using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace New_XPPen_Auxiliary_Driver
{
    //残存時間なども込みの1枚の描画
    internal class DrawingContextBlock
    {
        public List<DrawingContext> Contexts;
        public double PriorityGradient;
        public double Priority;
        public DrawingContextBlock()
        {
            Contexts = new List<DrawingContext>();
            PriorityGradient = 0;
            Priority = 0;
        }
        public DrawingContextBlock(DrawingContextBlock input)
        {
            Contexts = new List<DrawingContext>();
            for (int i = 0; i < input.Contexts.Count; i++)
            {
                Contexts.Add(input.Contexts[i]);
            }
            PriorityGradient = input.PriorityGradient;
            Priority = input.Priority;
        }
        public double GetPriority()
        {
            double priority = Priority;
            return priority;
        }
        public void UpCounter()
        {
            if (PriorityGradient < 0 && Priority > 0)
            {
                Priority = Priority + PriorityGradient;
            }
        }
        public void Draw(ref PaintEventArgs paintEventArgs)
        {

            for (int i = 0; i < Contexts.Count; i++)
            {
                Contexts[i].Draw(ref paintEventArgs);
            }
        }
        public bool isEmpty()
        {
            return Contexts.Count == 0;
        }
        public void Clear()
        {
            Contexts.Clear();
        }
        public void AddString(float x,float y,string content,Color color)
        {
            DrawingContextChar toAdd = new DrawingContextChar(x, y, color, content);
            Contexts.Add(toAdd);
        }
        public void AddLine(float x1, float y1, float x2, float y2, Color color)
        {
            DrawingContextLine toAdd = new DrawingContextLine(x1,y1,x2,y2,color);
            Contexts.Add(toAdd);
        }



        //以下は優先度の設定を行う関数
        public void SetPriority(double initial, double gradient)
        {
            Priority = initial;
            PriorityGradient = gradient;
        }
        public void SetZeroPriority()
        {
            Priority = 0;
            PriorityGradient = 0;
        }
        public void SetConstantPriority(double priority)
        {
            Priority = priority;
            PriorityGradient = 0;
        }
        public void SetWhileCommandPriority()
        {
            Priority = 5;
            PriorityGradient = 0;
        }
        public void SetAfterCommandTogglePriority()
        {
            Priority = 3;
            PriorityGradient = -1.0 / 10000000.0;
        }
        public void SetAfterCommandPushPriority()
        {
            Priority = 3;
            PriorityGradient = -3.0 / 30.0;
        }
    }
    //paintEventArgsを受け取って描画する
    //個々の描画部分
    internal class DrawingContext
    {
        public DrawingContext() { }
        virtual public void Draw(ref PaintEventArgs paintEventArgs)
        {

        }
    }
    internal class DrawingContextLine : DrawingContext
    {
        float X1;
        float X2;
        float Y1;
        float Y2;
        Color C;
        public DrawingContextLine(float x1,float y1,float x2,float y2,Color c) : base()
        {
            X1 = x1;
            X2 = x2;
            Y1 = y1;
            Y2 = y2;
            C = c;
        }
        override public void Draw(ref PaintEventArgs paintEventArgs)
        {
            int width = paintEventArgs.ClipRectangle.Width;
            int height = paintEventArgs.ClipRectangle.Height;
            float adjX1 = X1 * width / 2 + width / 2;
            float adjX2 = X2 * width / 2 + width / 2;
            float adjY1 = Y1 * width / 2 + width / 2;
            float adjY2 = Y2 * width / 2 + width / 2;
            Pen pen = new Pen(C);
            paintEventArgs.Graphics.DrawLine(pen, adjX1, adjY1, adjX2, adjY2);
        }
    }
    internal class DrawingContextChar : DrawingContext
    {
        float X;
        float Y;
        string Content;
        Color C;
        public DrawingContextChar(float x, float y, Color c,string content) : base()
        {
            X = x;
            Y = y;
            C = c;
            Content = content;
        }
        override public void Draw(ref PaintEventArgs paintEventArgs)
        {
            int width = paintEventArgs.ClipRectangle.Width;
            int height = paintEventArgs.ClipRectangle.Height;
            SolidBrush brush = new SolidBrush(C);
            FontFamily fontFamily = new FontFamily("ＭＳ ゴシック");
            int size = 20;
            if (Content.Length > 3)
            {
                size = size * 3 / Content.Length;
            }
            float x = width * (1 + X) / 2;
            float y = height * (1 - Y) / 2;
            x = x - Content.Length * size / 2;
            y = y - size / 2;
            Font font = new Font(fontFamily, size, FontStyle.Bold, GraphicsUnit.World);
            paintEventArgs.Graphics.DrawString(Content, font, brush, x, y);
        }
    }
}
