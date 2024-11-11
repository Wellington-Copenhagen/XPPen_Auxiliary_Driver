using SignAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace New_XPPen_Auxiliary_Driver
{
    internal class EachKeyStateBlock
    {

        List<EachKeyState> KeyboardStates;
        List<EachKeyState> PentabletKeyStates;
        List<EachKeyState> PentabletBtnStates;
        public EachKeyStateBlock()
        {
            KeyboardStates = new List<EachKeyState>();
            PentabletBtnStates = new List<EachKeyState>();
            PentabletKeyStates = new List<EachKeyState>();
            for (int i = 0; i < Parser.AvailableKeys.Count; i++)
            {
                EachKeyState toAppend = new EachKeyState(ControlKind.KeyboardKey,(int)Parser.AvailableKeys[i]);
                KeyboardStates.Add(toAppend);
            }
            for (int i = 0; i < 8; i++)
            {
                EachKeyState toAppend = new EachKeyState(ControlKind.PentabletKey,i);
                PentabletKeyStates.Add(toAppend);
            }
            for (int i = 0; i < 2; i++)
            {
                EachKeyState toAppend = new EachKeyState(ControlKind.PenButton, i);
                PentabletBtnStates.Add(toAppend);
            }
        }
        public void ApplyCurrentState(DATAPACKET packet)
        {
            for (int i = 0; i < KeyboardStates.Count; i++)
            {
                KeyboardStates[i].ApplyCurrentState(packet);
            }
            for (int i = 0; i < PentabletBtnStates.Count; i++)
            {
                PentabletBtnStates[i].ApplyCurrentState(packet);
            }
            for (int i = 0; i < PentabletKeyStates.Count; i++)
            {
                PentabletKeyStates[i].ApplyCurrentState(packet);
            }
        }
        public bool Search(InputType inputType)
        {
            switch (inputType.Kind)
            {
                case (ControlKind.PenButton):
                    return PentabletBtnStates[inputType.Num].IsPushed;
                case (ControlKind.PentabletKey):
                    return PentabletKeyStates[inputType.Num].IsPushed;
                case (ControlKind.KeyboardKey):
                    //KeyboardStates上のアドレスであることに注意
                    int start=0;
                    int end = (int)inputType.Key;
                    if(end>= KeyboardStates.Count)
                    {
                        end = KeyboardStates.Count - 1;
                    }
                    if (KeyboardStates[end].Type.Key == inputType.Key)
                    {
                        return KeyboardStates[end].IsPushed;
                    }
                    while (start+1<end)
                    {
                        int center = (int)Math.Floor((start + end) / 2.0);
                        if(KeyboardStates[center].Type.Key == inputType.Key)
                        {
                            return KeyboardStates[center].IsPushed;
                        }
                        if (KeyboardStates[center].Type.Key < inputType.Key)
                        {
                            start = center;
                            continue;
                        }
                        if (KeyboardStates[center].Type.Key < inputType.Key)
                        {
                            end = center;
                            continue;
                        }
                    }
                    return false;
                    default:
                    return false;

            }
        }
    }
    //各入力に対応した状態を記録する型
    internal class EachKeyState
    {
        public bool IsPushed;
        //入力の分類
        public InputType Type;
        public EachKeyState(InputType inputType)
        {
            Type = new InputType(inputType);
            IsPushed = false;
        }
        public EachKeyState(ControlKind controlKind,int keyNum)
        {
            Type = new InputType(controlKind,keyNum);
            IsPushed = false;
        }
        public void ApplyCurrentState(DATAPACKET packet)
        {
            switch(Type.Kind)
            {
                case(ControlKind.PentabletKey):
                    if(packet.eventtype == EventType.EventType_Key)
                    {
                        if ((packet.physical_key >> Type.Num & 0x01) == 0x01)
                        {
                            if (packet.keystatus == KeyStatus.KeyStatus_Down)
                            {
                                IsPushed = true;
                            }
                            if (packet.keystatus == KeyStatus.KeyStatus_Up)
                            {
                                IsPushed = false;
                            }
                        }
                        if ((packet.virtual_key >> Type.Num & 0x01) == 0x01)
                        {
                            if (packet.keystatus == KeyStatus.KeyStatus_Down)
                            {
                                IsPushed = true;
                            }
                            if (packet.keystatus == KeyStatus.KeyStatus_Up)
                            {
                                IsPushed = false;
                            }
                        }
                    }
                    break;
                case(ControlKind.PenButton):
                    if (packet.eventtype == EventType.EventType_Pen)
                    {
                        if (Type.Num == 1)
                        {
                            if (packet.button == 1)
                            {
                                IsPushed = true;
                            }
                            else
                            {
                                IsPushed = false;
                            }
                        }
                        else
                        {
                            if (packet.button == 3)
                            {
                                IsPushed = true;
                            }
                            else
                            {
                                IsPushed = false;
                            }
                        }
                    }
                    break;
            }
        }
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);
        public void ApplyCurrentState()
        {
            bool PushedNow = GetKeyState((int)Type.Key) >> 7 == -1;
            if (PushedNow)
            {
                IsPushed = true;
            }
            else
            {
                IsPushed = false;
            }
        }
    }
}
