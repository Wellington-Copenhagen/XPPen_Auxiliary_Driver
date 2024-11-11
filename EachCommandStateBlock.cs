using SignAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace New_XPPen_Auxiliary_Driver
{
    internal class EachCommandStateBlock
    {
        public List<EachCommandState> States;
        int X;
        int Y;
        public EachCommandStateBlock(List<EachCommandState> states)
        {
            States = new List<EachCommandState>();
            States = states;
        }
        //一つ以上が押されている状況にならないようにする
        void ExeclusiveProcess()
        {
            bool thereIsAlreadyPushed = false;
            for (int i = 0; i < States.Count; i++)
            {
                if (States[i].IsPushedNow)
                {
                    thereIsAlreadyPushed = true;
                }
                else if (thereIsAlreadyPushed)
                {
                    States[i].IsPushedNow = false;
                }
            }
        }
        public void ApplyCurrentState(EachKeyStateBlock keyStateBlock,DATAPACKET packet)
        {
            if(packet.eventtype == EventType.EventType_Pen)
            {
                X = packet.x;
                Y = packet.y;
            }
            for (int i=0;i<States.Count;i++)
            {
                States[i].InitializeForEveryTick();
                States[i].ApplyCurrentPushed(keyStateBlock);
            }
            ExeclusiveProcess();
            for (int i = 0; i < States.Count; i++)
            {
                States[i].applyOtherParams(X,Y);
            }
        }
    }
    internal class EachCommandState
    {
        //そのコマンドに属する入力の一覧(ANDで処理)
        public List<InputType> NeededInputs;
        public bool IsPushedNow;
        public bool IsPushedPrev;
        public bool IsUp;
        public bool IsDown;
        public int StartX;
        public int StartY;
        public int CurrentX;
        public int CurrentY;
        public int PreviousX;
        public int PreviousY;
        public EachCommandState()
        {
            NeededInputs = new List<InputType>();
            IsPushedNow = false;
            IsPushedPrev = false;
            IsUp = false;
            IsDown = false;
            StartX = 0;
            StartY = 0;
            CurrentX = 0;
            CurrentY = 0;
            PreviousX = 0;
            PreviousY = 0;
        }
        public void InitializeForEveryTick()
        {
            IsPushedPrev = IsPushedNow;
            IsDown = false;
            IsUp = false;
            PreviousX = CurrentX;
            PreviousY = CurrentY;
        }
        public void ApplyCurrentPushed(EachKeyStateBlock eachKey)
        {
            IsPushedNow=true;
            for(int i = 0; i < NeededInputs.Count; i++)
            {
                if (!eachKey.Search(NeededInputs[i]))
                {
                    IsPushedNow = false;
                    break;
                }
            }
        }
        public void applyOtherParams(int X,int Y)
        {
            CurrentX = X; CurrentY = Y;
            if (IsPushedNow)
            {
                if (IsPushedPrev)
                {

                }
                else
                {
                    IsDown = true;
                    StartX = X;
                    StartY = Y;
                }
            }
            else
            {
                if (IsPushedPrev)
                {
                    IsUp = true;
                }
                else
                {

                }
            }
        }
        public void Add(Keys keys)
        {
            InputType toAdd = new InputType(ControlKind.KeyboardKey,(int)keys);
            NeededInputs.Add(toAdd);
        }
        public void Add(ControlKind kind,int num)
        {
            InputType toAdd = new InputType(kind, num);
            NeededInputs.Add(toAdd);
        }
    }
}
