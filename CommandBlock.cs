using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace New_XPPen_Auxiliary_Driver
{
    internal class CommandBlock
    {
        List<Command> Commands;
        Counter Counter;
        SendDrawStack Stack;
        public bool DrawBlocked;
        public CommandBlock(List<Command> commands)
        {
            Commands = new List<Command>();
            Commands = commands;
            Counter = new Counter();
            Stack = new SendDrawStack(commands.Count());
            DrawBlocked = false;
        }
        public DrawingContextBlock GetDrawBlock()
        {
            int highestPriorityIndex = 0;
            double highestPriority = 0;
            for (int i = 0; i < Stack.DCs.Count; i++)
            {
                double priority = Stack.DCs[i].GetPriority();
                if (priority > highestPriority)
                {
                    highestPriority = priority;
                    highestPriorityIndex = i;
                }
            }
            if (highestPriority > 0)
            {

                return Stack.DCs[highestPriorityIndex];
            }
            else
            {
                DrawingContextBlock empty = new DrawingContextBlock();
                return empty;
            }
        }
        public void UpDownPushed(EachCommandStateBlock state)
        {
            string SendPushed = "";
            string SendDown = "";
            string SendUp = "";
            bool isTogglePushed = false;
            bool isToggleDown = false;
            bool isToggleUp = false;
            //Up Down Pushedの処理
            for (int i = 0; i < Commands.Count; i++)
            {
                Commands[i].toSend = "";
                Counter.CurrentIndex = i;
                if (state.States[i].IsUp)
                {
                    Commands[i].WhenUp(ref Counter,ref Stack, state.States[i]);
                    SendUp = Commands[i].toSend;
                    isToggleUp = Commands[i].isToggle;
                    if (isToggleDown)
                    {
                        Counter.DetectToggleSend(i, Counter.Timing.Up);
                    }
                    break;
                }
            }
            for (int i = 0; i < Commands.Count; i++)
            {
                Commands[i].toSend = "";
                Counter.CurrentIndex = i;
                if (state.States[i].IsDown)
                {
                    Commands[i].WhenDown(ref Counter, ref Stack, state.States[i]);
                    SendDown = Commands[i].toSend;
                    isToggleDown = Commands[i].isToggle;
                    if (isToggleDown)
                    {
                        Counter.DetectToggleSend(i,Counter.Timing.Down);
                    }
                    break;
                }
            }
            for (int i = 0; i < Commands.Count; i++)
            {
                Commands[i].toSend = "";
                Counter.CurrentIndex = i;
                if (state.States[i].IsPushedNow)
                {
                    Commands[i].WhenPushed(ref Counter, ref Stack, state.States[i]);
                    SendPushed = Commands[i].toSend;
                    isTogglePushed = Commands[i].isToggle;
                    if (isToggleDown)
                    {
                        Counter.DetectToggleSend(i, Counter.Timing.Pushed);
                    }
                    break;
                }
            }
            //排他処理かけて各コマンドの描画内容を反映させる
            DrawBlocked = true;
            for (int i = 0; i < Commands.Count; i++)
            {
                if (Commands[i].InvalidateNeeded)
                {
                    Stack.DCs[i] = new DrawingContextBlock(Commands[i].Context);
                    Commands[i].InvalidateNeeded = false;
                }
            }
            DrawBlocked = false;


            //出力Toggle
            {
                if (isToggleDown && SendDown.Length != "".Length)
                {
                    SendKeys.SendWait(SendDown);
                    Stack.CurrentToggle = SendDown;
                    Counter.WhenToggleSent(Counter.Timing.Down);
                }
                else if (isTogglePushed && SendPushed.Length != "".Length)
                {
                    SendKeys.SendWait(SendPushed);
                    Stack.CurrentToggle = SendPushed;
                    Counter.WhenToggleSent(Counter.Timing.Pushed);
                }
                else if (isToggleUp && SendUp.Length != "".Length)
                {
                    SendKeys.SendWait(SendUp);
                    Stack.CurrentToggle = SendUp;
                    Counter.WhenToggleSent(Counter.Timing.Up);
                }
            }
            //出力Push
            {
                if (!isToggleDown && SendDown.Length != "".Length)
                {
                    SendKeys.SendWait(SendDown);
                }
                if (!isTogglePushed && SendPushed.Length != "".Length)
                {
                    SendKeys.SendWait(SendPushed);
                }
                if (!isToggleUp && SendUp.Length != "".Length)
                {
                    SendKeys.SendWait(SendUp);
                }
            }
            for(int i = 0; i < Commands.Count; i++)
            {
                Commands[i].Context.UpCounter();
            }
            for (int i = 0; i < Commands.Count; i++)
            {
                if (state.States[i].IsDown)
                {
                    Counter.DetectDown(i);
                    break;
                }
            }
        }

    }
    internal class Counter
    {
        int Count;
        int LastPushedIndex;
        int ToggleDownIndex;
        int ToggleUpIndex;
        int TogglePushedIndex;
        public int CurrentIndex;
        public Counter()
        {
            Count = 0;
            LastPushedIndex = 0;
        }
        public int GetCount()
        {
            if (LastPushedIndex == CurrentIndex)
            {
                return Count;
            }
            else
            {
                return 0;
            }
        }
        public void DetectDown(int currentIndex)
        {
            if(LastPushedIndex == currentIndex)
            {
                Count++;
            }
        }
        public void DetectToggleSend(int currentIndex,Timing timing)
        {
            if (timing == Timing.Down)
            {
                ToggleDownIndex = currentIndex;
            }
            if (timing == Timing.Pushed)
            {
                TogglePushedIndex = currentIndex;
            }
            if (timing == Timing.Up)
            {
                ToggleUpIndex = currentIndex;
            }
        }
        public void WhenToggleSent(Timing timing)
        {
            if(timing == Timing.Down)
            {
                if(ToggleDownIndex != LastPushedIndex)
                {
                    LastPushedIndex = ToggleDownIndex;
                    Count = 0;
                }
            }
            if (timing == Timing.Pushed)
            {
                if (TogglePushedIndex != LastPushedIndex)
                {
                    Count = 0;
                }
            }
            if (timing == Timing.Up)
            {
                if (ToggleUpIndex != LastPushedIndex)
                {
                    Count = 0;
                }
            }
        }
        public enum Timing
        {
            Down=0, Up=1,Pushed=2
        }
    }
    internal class SendDrawStack
    {
        public string CurrentToggle;
        public List<DrawingContextBlock> DCs;
        public SendDrawStack(int numOfCommands)
        {
            CurrentToggle = "";
            DCs = new List<DrawingContextBlock>();
            for (int i = 0; i < numOfCommands; i++)
            {
                DrawingContextBlock toAdd = new DrawingContextBlock();
                DCs.Add(toAdd);
            }
        }
    }
    internal class Command
    {
        protected List<OutputType> Outputs;
        protected Dictionary<string, int> Attributes;
        //以下3つは出力時用
        public string toSend;
        public bool isToggle;
        public DrawingContextBlock Context;
        public bool InvalidateNeeded;
        public Command(Dictionary<string,int> attributes,List<OutputType> outputs)
        {
            Outputs = outputs;
            Attributes = attributes;
            toSend = "";
            isToggle = false;
            Context = new DrawingContextBlock();
            InvalidateNeeded = false;
        }
        virtual public void WhenUp(ref Counter counter,ref SendDrawStack stack,EachCommandState state) { }
        virtual public void WhenDown(ref Counter counter, ref SendDrawStack stack, EachCommandState state) { }
        virtual public void WhenPushed(ref Counter counter, ref SendDrawStack stack, EachCommandState state) { }
        virtual public void Paint(ref Counter counter, ref SendDrawStack stack, EachCommandState state) { }
        protected int Direction(int x, int y)
        {
            double Direction = Math.Atan2(y, x) * -1;
            Direction = Direction + Math.PI / Outputs.Count;
            while (Direction > Math.PI * 2)
            {
                Direction = Direction - Math.PI * 2;
            }
            while (Direction < 0)
            {
                Direction = Direction + Math.PI * 2;
            }
            Direction = (Direction * Outputs.Count) / (2 * Math.PI);
            return (int)Math.Floor(Direction);
        }
        protected int Distance(int x, int y)
        {
            return (int)Math.Sqrt(x * x + y * y);
        }
        protected void SetSend(int index)
        {
            if (Outputs[index].IsToggle)
            {
                toSend = Outputs[index].ToSend;
                isToggle = true;
            }
            else
            {
                toSend = Outputs[index].ToSend;
                isToggle = true;
            }
        }
    }
    //ここまでが重要な部分でここより下は各操作についての具体的なプログラムとなっている





    //右から反時計回り
    internal class DirectionSelect : Command
    {
        DrawingContextBlock Temporal;
        public DirectionSelect(Dictionary<string,int> attributes,List<OutputType> outputs) : base(attributes,outputs)
        {
            Temporal = new DrawingContextBlock();
        }
        public override void WhenDown(ref Counter counter,ref SendDrawStack stack,EachCommandState state)
        {
            Temporal = new DrawingContextBlock(Context);
        }
        public override void WhenUp(ref Counter counter,ref SendDrawStack stack,EachCommandState state)
        {
            if (Distance(state.CurrentX - state.StartX, state.CurrentY - state.StartY) > 300)
            {
                SetSend(Direction(state.CurrentX - state.StartX, -1 * state.CurrentY + state.StartY));
                InvalidateNeeded = true;
                if (Outputs[Direction(state.CurrentX - state.StartX, -1 * state.CurrentY + state.StartY)].IsToggle)
                {
                    Context.SetAfterCommandTogglePriority();
                }
                else
                {
                    Context = Temporal;
                    Context.SetAfterCommandTogglePriority();
                }
            }
            else
            {
                InvalidateNeeded = true;
                Context = Temporal;
                Context.SetAfterCommandTogglePriority();
            }
        }
        public override void WhenPushed(ref Counter counter,ref SendDrawStack stack,EachCommandState state)
        {
            Context.SetWhileCommandPriority();
            Paint(ref counter,ref stack,state);
            
        }
        public override void Paint(ref Counter counter, ref SendDrawStack stack, EachCommandState state)
        {
            InvalidateNeeded = true;
            Context.Clear();
            for (int i = 0; i < Outputs.Count; i++)
            {
                Pen pen = new Pen(Color.Black);
                double directionM = Math.PI * 2 * (-2 * i) / (2 * Outputs.Count);
                double directionT = Math.PI * 2 * (1 + -2 * i) / (2 * Outputs.Count);
                //線の描画
                Context.AddLine(0, 0, (float)Math.Cos(directionT), (float)Math.Sin(directionT), Color.Black);
                //字の描画
                //Context.AddString((float)Math.Cos(directionM) * (float)0.6, (float)Math.Sin(directionM) * (float)0.6, Outputs[i].Label, Color.Black);
                if (Direction(state.CurrentX - state.StartX, -1 * state.CurrentY + state.StartY) == i &&
                    Distance(state.CurrentX - state.StartX, -1 * state.CurrentY + state.StartY) > 300)
                {
                    Context.AddString((float)Math.Cos(directionM) * (float)0.6, (float)Math.Sin(directionM) * (float)0.6, Outputs[i].Label, Color.Red);
                }
                else
                {
                    Context.AddString((float)Math.Cos(directionM) * (float)0.6, (float)Math.Sin(directionM) * (float)0.6, Outputs[i].Label, Color.Black);
                }
                pen.Dispose();
            }
        }
    }
    //右左上下
    internal class TwoAxis : Command
    {
        int TickFromChange = 0;
        bool RightChanged = false;
        bool LeftChanged = false;
        bool UpChanged = false;
        bool DownChanged = false;
        bool verticalFixed = false;
        bool horizontalFixed = false;
        public TwoAxis(Dictionary<string,int> attributes,List<OutputType> outputs) : base(attributes,outputs) { }
        public override void WhenDown(ref Counter counter,ref SendDrawStack stack,EachCommandState state)
        {
            verticalFixed = false;
            horizontalFixed = false;
            Context.SetWhileCommandPriority();
            Paint(ref counter, ref stack, state);
        }
        public override void WhenUp(ref Counter counter,ref SendDrawStack stack,EachCommandState state)
        {
            Context.SetAfterCommandPushPriority();
            InvalidateNeeded = true;
        }
        public override void WhenPushed(ref Counter counter,ref SendDrawStack stack,EachCommandState state)
        {
            if (!verticalFixed && Math.Round((double)(state.CurrentX - state.StartX) / (double)Attributes["横感度"]) > Math.Round((double)(state.PreviousX - state.StartX) / (double)Attributes["横感度"]))
            {
                SetSend(0);
                RightChanged = true;
                horizontalFixed = true;
                Paint(ref counter,ref stack,state);
                TickFromChange = 0;
            }
            if (!verticalFixed && Math.Round((double)(state.CurrentX - state.StartX) / (double)Attributes["横感度"]) < Math.Round((double)(state.PreviousX - state.StartX) / (double)Attributes["横感度"]))
            {
                SetSend(1);
                LeftChanged = true;
                horizontalFixed = true;
                Paint(ref counter,ref stack,state);
                TickFromChange = 0;
            }
            if (!horizontalFixed && Math.Round((double)(state.CurrentY - state.StartY) / (double)Attributes["縦感度"]) < Math.Round((double)(state.PreviousY - state.StartY) / (double)Attributes["横感度"]))
            {
                SetSend(2);
                UpChanged = true;
                verticalFixed = true;
                Paint(ref counter,ref stack,state);
                TickFromChange = 0;
            }
            if (!horizontalFixed && Math.Round((double)(state.CurrentY - state.StartY) / (double)Attributes["縦感度"]) > Math.Round((double)(state.PreviousY - state.StartY) / (double)Attributes["横感度"]))
            {
                SetSend(3);
                DownChanged = true;
                verticalFixed = true;
                Paint(ref counter,ref stack,state);
                TickFromChange = 0;
            }
            if (TickFromChange > -1)
            {
                TickFromChange++;
            }
            if (TickFromChange > 20)
            {
                Paint(ref counter,ref stack,state);
                TickFromChange = -1;
            }
        }
        public override void Paint(ref Counter counter, ref SendDrawStack stack, EachCommandState state)
        {
            InvalidateNeeded = true;
            Context.Clear();
            if (RightChanged)
            {
                RightChanged = false;
                Context.AddString((float)0.5, 0, Outputs[0].Label, Color.Red);
            }
            else
            {
                if (verticalFixed)
                {
                    Context.AddString((float)0.5, 0, Outputs[0].Label, Color.Gray);
                }
                else
                {
                    Context.AddString((float)0.5, 0, Outputs[0].Label, Color.Black);
                }
            }
            if (LeftChanged)
            {
                LeftChanged = false;
                Context.AddString((float)-0.5, 0, Outputs[1].Label, Color.Red);
            }
            else
            {
                if (verticalFixed)
                {
                    Context.AddString((float)-0.5, 0, Outputs[1].Label, Color.Gray);
                }
                else
                {
                    Context.AddString((float)-0.5, 0, Outputs[1].Label, Color.Black);
                }
            }
            if (UpChanged)
            {
                UpChanged = false;
                Context.AddString(0, (float)0.5, Outputs[2].Label, Color.Red);
            }
            else
            {
                if (horizontalFixed)
                {
                    Context.AddString(0, (float)0.5, Outputs[2].Label, Color.Gray);
                }
                else
                {
                    Context.AddString(0, (float)0.5, Outputs[2].Label, Color.Black);
                }
            }
            if (DownChanged)
            {
                DownChanged = false;
                Context.AddString(0, (float)-0.5, Outputs[3].Label, Color.Red);
            }
            else
            {
                if (horizontalFixed)
                {
                    Context.AddString(0, (float)-0.5, Outputs[3].Label, Color.Gray);
                }
                else
                {
                    Context.AddString(0, (float)-0.5, Outputs[3].Label, Color.Black);
                }
            }
        }
    }
    //0から、表示は右から反時計回り
    internal class Rotation : Command
    {
        public Rotation(Dictionary<string,int> attributes,List<OutputType> outputs) : base(attributes,outputs) { }
        public override void WhenDown(ref Counter counter,ref SendDrawStack stack,EachCommandState state)
        {
            Context.SetWhileCommandPriority();
            SetSend(counter.GetCount() % Outputs.Count);
            Paint(ref counter,ref stack,state);
            
        }
        public override void WhenUp(ref Counter counter,ref SendDrawStack stack,EachCommandState state)
        {
            Context.SetAfterCommandTogglePriority();
            InvalidateNeeded = true;
        }
        public override void WhenPushed(ref Counter counter,ref SendDrawStack stack,EachCommandState state) { }
        public override void Paint(ref Counter counter, ref SendDrawStack stack, EachCommandState state)
        {
            InvalidateNeeded = true;
            Context.Clear();
            for (int i = 0; i < Outputs.Count; i++)
            {
                double directionM = Math.PI * 2 * (2 * i) / (2 * Outputs.Count);
                //字の描画
                if (counter.GetCount() % Outputs.Count == i)
                {
                    Context.AddString((float)0.5 * (float)Math.Cos(directionM), (float)0.5 * (float)Math.Sin(directionM), Outputs[i].Label, Color.Red);
                }
                else
                {
                    Context.AddString((float)0.5 * (float)Math.Cos(directionM), (float)0.5 * (float)Math.Sin(directionM), Outputs[i].Label, Color.Black);
                }
            }
        }
    }
    //中心
    internal class Once : Command
    {
        public Once(Dictionary<string,int> attributes,List<OutputType> outputs) : base(attributes,outputs) { }
        public override void WhenDown(ref Counter counter,ref SendDrawStack stack,EachCommandState state)
        {
            SetSend(0);
            
            Paint(ref counter,ref stack,state);
            Context.SetWhileCommandPriority();
        }
        public override void WhenUp(ref Counter counter,ref SendDrawStack stack,EachCommandState state)
        {
            InvalidateNeeded = true;
            if (Outputs[0].IsToggle)
            {
                Context.SetAfterCommandTogglePriority();
            }
            else
            {
                Context.SetAfterCommandPushPriority();
            }
        }
        public override void WhenPushed(ref Counter counter,ref SendDrawStack stack,EachCommandState state) { }
        public override void Paint(ref Counter counter, ref SendDrawStack stack, EachCommandState state)
        {
            InvalidateNeeded = true;
            Context.Clear();
            Context.AddString(0, 0, Outputs[0].Label, Color.Red);
        }
    }
    internal class WhilePush : Command
    {
        string sendBuffer = "";
        bool finished = false;
        public WhilePush(Dictionary<string,int> attributes,List<OutputType> outputs) : base(attributes,outputs) { }
        public override void WhenDown(ref Counter counter,ref SendDrawStack stack,EachCommandState state)
        {
            Context.SetWhileCommandPriority();
            sendBuffer = stack.CurrentToggle;
            SetSend(0);
            finished = false;
            
            Paint(ref counter,ref stack,state);
        }
        public override void WhenUp(ref Counter counter,ref SendDrawStack stack,EachCommandState state)
        {
            Context.SetZeroPriority();
            finished = true;
            toSend = sendBuffer;
            isToggle = true;
            
            Paint(ref counter,ref stack,state);

        }
        public override void WhenPushed(ref Counter counter,ref SendDrawStack stack,EachCommandState state) { }
        public override void Paint(ref Counter counter, ref SendDrawStack stack, EachCommandState state)
        {
            InvalidateNeeded = true;
            Context.Clear();
            Context.AddString(0, 0, Outputs[0].Label, Color.Red);
        }
    }
}
