using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace New_XPPen_Auxiliary_Driver
{
    internal class InputType
    {
        //入力のキーなのかペンタブのボタンなのかなどの種別
        public ControlKind Kind;
        //入力のボタンの場合の番号
        public int Num;
        //入力のキーの場合のkeys
        public Keys Key;
        public static bool operator ==(InputType preceding, InputType subsequent)
        {
            if (preceding.Kind != subsequent.Kind)
            {
                return false;
            }
            if (preceding.Kind == ControlKind.PentabletKey)
            {
                if (preceding.Key == subsequent.Key)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (preceding.Num == subsequent.Num)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        public static bool operator !=(InputType preceding, InputType subsequent)
        {
            if (preceding.Kind != subsequent.Kind)
            {
                return true;
            }
            if (preceding.Kind == ControlKind.PentabletKey)
            {
                if (preceding.Key == subsequent.Key)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                if (preceding.Num == subsequent.Num)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
        public InputType(ControlKind controlKind,int keyNum)
        {
            if (controlKind == ControlKind.KeyboardKey)
            {
                Kind = controlKind;
                Num = 0;
                Key = (Keys)keyNum;
            }
            else
            {
                Kind = controlKind;
                Num = keyNum;
                Key = 0;
            }
        }
        public InputType(InputType inputType)
        {
            Kind = inputType.Kind;
            Num = inputType.Num;
            Key = inputType.Key;
        }
    }
    enum ControlKind
    {
        PentabletKey = 0,
        KeyboardKey = 1,
        PenButton = 2
    }
    internal class OutputType
    {
        public string Label;
        public string ToSend;
        public bool IsToggle;
        public OutputType(string label,string toSend,bool isToggle)
        {
            Label = label;
            ToSend = toSend;
            IsToggle = isToggle;
        }
    }
}
