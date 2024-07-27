using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace New_XPPen_Auxiliary_Driver
{
    //paintEventArgsを受け取って描画する
    internal class DrawingContext
    {
        virtual public void Draw(ref PaintEventArgs paintEventArgs)
        {

        }
    }
    internal class DrawingContextLine : DrawingContext
    {
        public DrawingContextLine()
        {

        }
        override public void Draw(ref PaintEventArgs paintEventArgs)
        {

        }
    }
}
