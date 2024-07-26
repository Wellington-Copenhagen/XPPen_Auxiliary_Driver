using SignAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Drawing;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Reflection.Emit;
using New_XPPen_Auxiliary_Driver;
using System.Xml;
using static System.Windows.Forms.AxHost;

namespace XPPen_Auxilliary_Driver
{
    internal class Total_Processer
    {
        public static bool hasSyntaxError;
        public static string ErrorString;
        public static List<Output_Sender> Senders = new List<Output_Sender>();
        public static string ToggleToSend = "";
        public static string ToggleLabel = "";
        public static int InvalidationRequireIndex = -1;
        public static bool stopped = false;
        public static Paint_Data P_Data = new Paint_Data();
        public static Paint_Data Mode_Paint = new Paint_Data();
        public static int counter = 0;
        public Total_Processer()
        {

        }
        public static void ExecuteWithoutPentablet()
        {
            while (true)
            {
                DATAPACKET packet = new DATAPACKET();
                packet.eventtype = EventType.EventType_Pen;
                packet.physical_key = 0;
                packet.virtual_key = 0;
                packet.keystatus = 0;
                packet.penstatus = 0;
                packet.x = Cursor.Position.X;
                packet.y = Cursor.Position.Y;
                packet.pressure = 0;
                packet.wheel_direction = 0;
                packet.button = 0;
                Execute(packet);
            }
        }
        //XMLより
        public static void Initialize()
        {
            hasSyntaxError = false;
            ErrorString = "";
            XElement fromConfig = XElement.Load("config.txt",LoadOptions.SetLineInfo);
            IEnumerable<string> Names = from name in fromConfig.Elements("一覧").Elements("名前") select name.Value;
            foreach (string name in Names)
            {
                if ((from types in fromConfig.Elements(name)select types).Count() < 1)
                {
                    ErrorLogger(name + "がありません。");
                    return;
                }
                if ((from types in fromConfig.Elements(name).Elements("種別") select types).Count() < 1)
                {
                    ErrorLogger((from types in fromConfig.Elements(name) select types).First(),name + "に「種別」がありません。");
                    return;
                }
                IEnumerable<string> type = from types in fromConfig.Elements(name).Elements("種別") select types.Value;
                if (sameString(type.First(), "方向"))
                {
                    DirectionSelect toAdd = new DirectionSelect();
                    toAdd.parseTypeArray(from types in fromConfig.Elements(name).Elements("入力") select types.Value);
                    int i = 1;
                    while (true)
                    {
                        bool keyNumNotFound = false;
                        toAdd.parseOnePair(i, name, fromConfig,ref keyNumNotFound);
                        if (keyNumNotFound)
                        {
                            break;
                        }
                        i++;
                    }
                    toAdd.CheckAttributes((from types in fromConfig.Elements(name).Elements("種別") select types).First());
                    Senders.Add(toAdd);
                }
                if (sameString(type.First(), "2軸"))
                {
                    TwoAxis toAdd = new TwoAxis();
                    toAdd.parseTypeArray(from types in fromConfig.Elements(name).Elements("入力") select types.Value);
                    int i = 1;
                    while (true)
                    {
                        bool keyNumNotFound = false;
                        toAdd.parseOnePair(i, name, fromConfig, ref keyNumNotFound);
                        if (keyNumNotFound)
                        {
                            break;
                        }
                        i++;
                    }
                    toAdd.CheckAttributes((from types in fromConfig.Elements(name).Elements("種別") select types).First());
                    Senders.Add(toAdd);
                }
                if (sameString(type.First(), "ループ"))
                {
                    Rotation toAdd = new Rotation();
                    toAdd.parseTypeArray(from types in fromConfig.Elements(name).Elements("入力") select types.Value);
                    int i = 1;
                    while (true)
                    {
                        bool keyNumNotFound = false;
                        toAdd.parseOnePair(i, name, fromConfig, ref keyNumNotFound);
                        if (keyNumNotFound)
                        {
                            break;
                        }
                        i++;
                    }
                    toAdd.CheckAttributes((from types in fromConfig.Elements(name).Elements("種別") select types).First());
                    Senders.Add(toAdd);
                }
                if (sameString(type.First(), "単発"))
                {
                    Once toAdd = new Once();
                    toAdd.parseTypeArray(from types in fromConfig.Elements(name).Elements("入力") select types.Value);
                    int i = 1;
                    while (true)
                    {
                        bool keyNumNotFound = false;
                        toAdd.parseOnePair(i, name, fromConfig, ref keyNumNotFound);
                        if (keyNumNotFound)
                        {
                            break;
                        }
                        i++;
                    }
                    toAdd.CheckAttributes((from types in fromConfig.Elements(name).Elements("種別") select types).First());
                    Senders.Add(toAdd);
                }
                if (sameString(type.First(), "長押し"))
                {
                    WhilePush toAdd = new WhilePush();
                    toAdd.parseTypeArray(from types in fromConfig.Elements(name).Elements("入力") select types.Value);
                    int i = 1;
                    while (true)
                    {
                        bool keyNumNotFound = false;
                        toAdd.parseOnePair(i, name, fromConfig, ref keyNumNotFound);
                        if (keyNumNotFound)
                        {
                            break;
                        }
                        i++;
                    }
                    toAdd.CheckAttributes((from types in fromConfig.Elements(name).Elements("種別") select types).First());
                    Senders.Add(toAdd);
                }
            }
        }
        public static int Execute(DATAPACKET packet)
        {
            if (!stopped)
            {
                int PushedIndex = -1;
                for (int i = 0; i < Senders.Count; i++)
                {
                    Senders[i].Type.InfluenceEvents(packet);
                    Senders[i].Type.CheckTotallyPushed(packet);
                }
                for (int i = 0; i < Senders.Count; i++)
                {
                    if (Senders[i].Type.IsPushed)
                    {
                        PushedIndex = i;
                        break;
                    }
                }
                if (PushedIndex!=-1)
                {
                    for (int i = 0; i < Senders.Count; i++)
                    {
                        if (i != PushedIndex)
                        {
                            Senders[i].Type.WhenAnyDown(packet);
                        }
                    }
                }
                for (int i = 0; i < Senders.Count; i++)
                {
                    if (Senders[i].Type.IsPushed&& !Senders[i].isPushedNow)
                    {
                        Senders[i].isPushedNow = true;
                        Senders[i].WhenDown(ref P_Data);
                        counter = -1;
                    }
                    if (!Senders[i].Type.IsPushed && Senders[i].isPushedNow)
                    {
                        Senders[i].isPushedNow = false;
                        Senders[i].WhenUp(ref P_Data);
                        counter = 30;
                    }
                    if (Senders[i].Type.IsPushed)
                    {
                        Senders[i].WhenPushed(ref P_Data);
                    }
                    if (Senders[i].InvalidateNeeded)
                    {
                        InvalidationRequireIndex = i;
                        Senders[i].InvalidateNeeded = false;
                    }
                    string mode = "";
                    Senders[i].GetSend(ref mode);
                    if (mode != "")
                    {
                        Mode_Paint = P_Data.Copy();
                        ToggleToSend = mode;
                        for (int j = 0; j < Senders.Count; j++)
                        {
                            if (j != i)
                            {
                                Senders[j].Type.ResetCount(packet);
                            }
                        }
                    }
                }

            }
            return 0;
        }
        public static void Paint(ref PaintEventArgs eventArgs)
        {
            P_Data.Draw(eventArgs);
            InvalidationRequireIndex = -1;
            Debug.WriteLine("Paint");
        }
        static bool sameString(string A, string B)
        {
            if (A.Count() != B.Count())
            {
                return false;
            }
            for (int i = 0; i < A.Count(); i++)
            {
                if (A[i] != B[i])
                {
                    return false;
                }
            }
            return true;
        }
        static void ErrorLogger(XObject input, string errorStr)
        {
            hasSyntaxError = true;
            StreamWriter streamWriter = new StreamWriter("log.txt");
            IXmlLineInfo lineInfo = input;
            if (lineInfo.HasLineInfo())
            {
                string toLog = "";
                toLog = toLog + lineInfo.LineNumber;
                toLog = toLog + "行目：" + errorStr;
                streamWriter.WriteLine(toLog);
                ErrorString = ErrorString + toLog + "\n";
            }
            else
            {
                string toLog = "";
                toLog = errorStr;
                streamWriter.WriteLine(toLog);
                ErrorString = ErrorString + toLog + "\n";
            }
            streamWriter.Close();
        }
        static void ErrorLogger(string errorStr)
        {
            hasSyntaxError = true;
            StreamWriter streamWriter = new StreamWriter("log.txt");
            {
                string toLog = "";
                toLog = errorStr;
                streamWriter.WriteLine(toLog);
                ErrorString = ErrorString + toLog + "\n";
            }
            streamWriter.Close();
        }
    }
    internal class AttributeValue
    {
        bool isNum;
        string StrValue;
        double NumValue;
        public AttributeValue(string newValue)
        {
            isNum = false;
            StrValue = newValue;
            NumValue = 0;
        }
        public AttributeValue(double newValue)
        {
            isNum = true;
            StrValue = "";
            NumValue = newValue;
        }
        static public implicit operator AttributeValue(double input)
        {
            AttributeValue output = new AttributeValue(input);
            return output;
        }
        static public implicit operator AttributeValue(string input)
        {
            AttributeValue output = new AttributeValue(input);
            return output;
        }
        static public implicit operator string(AttributeValue input)
        {
            return input.StrValue;
        }
        static public implicit operator double(AttributeValue input)
        {
            return input.NumValue;
        }
    }
    internal class Paint_Data
    {
        List<Line_toDraw> lines;
        public List<String_toDraw> strings;
        public Paint_Data()
        {
            lines = new List<Line_toDraw>();
            strings = new List<String_toDraw>();
        }
        public void AddLine(float startX, float startY, float endX, float endY, Color color)
        {
            Line_toDraw line_ToDraw = new Line_toDraw(startX, startY, endX, endY, color);
            lines.Add(line_ToDraw);
        }
        public void AddString(float x, float y, string content, Color color)
        {
            String_toDraw string_ToDraw = new String_toDraw(x, y, content, color);
            strings.Add(string_ToDraw);
        }
        public void Clear()
        {
            lines.Clear();
            strings.Clear();
        }
        public void Draw(PaintEventArgs eventArgs)
        {
            foreach (Line_toDraw line in lines)
            {
                line.Draw(ref eventArgs);
            }
            foreach (String_toDraw string_toDraw in strings)
            {
                string_toDraw.Draw(ref eventArgs);
            }
        }
        public Paint_Data Copy()
        {
            Paint_Data output = new Paint_Data();
            output.lines = new List<Line_toDraw>();
            output.strings = new List<String_toDraw>();
            foreach (Line_toDraw line in lines)
            {
                output.lines.Add(line);
            }
            foreach (String_toDraw str in strings)
            {
                output.strings.Add(str);
            }
            return output;
        }
    }
    internal class Line_toDraw
    {
        float StartX;
        float StartY;
        float EndX;
        float EndY;
        Color C;
        public Line_toDraw(float startX,float startY,float endX,float endY,Color color)
        {
            StartX = startX;
            StartY = startY;
            EndX = endX;
            EndY = endY;
            C = color;
        }
        public void Draw(ref PaintEventArgs eventArgs)
        {
            float width = eventArgs.ClipRectangle.Width;
            float height = eventArgs.ClipRectangle.Height;
            Pen pen = new Pen(C);
            Point start = new Point((int)(width * (1 + StartX) / 2), (int)(height * (1 - StartY) / 2));
            Point end = new Point((int)(width * (1 + EndX) / 2), (int)(height * (1 - EndY) / 2));
            eventArgs.Graphics.DrawLine(pen, start, end);
        }
    }
    internal class String_toDraw
    {
        string Content;
        float X;
        float Y;
        Color C;
        public String_toDraw(float x,float y,string content,Color color)
        {
            X = x;
            Y = y;
            C = color;
            Content = content;
        }
        public void Draw(ref PaintEventArgs eventArgs)
        {
            float width = eventArgs.ClipRectangle.Width;
            float height = eventArgs.ClipRectangle.Height;
            SolidBrush brush = new SolidBrush(C);
            FontFamily fontFamily = new FontFamily("ＭＳ ゴシック");
            int size = 20;
            if (Content.Length > 3)
            {
                size = size * 3 / Content.Length;
            }
            Font font = new Font(fontFamily, size, FontStyle.Bold, GraphicsUnit.World);
            float x = width * (1 + X) / 2;
            float y = height * (1 - Y) / 2;
            x = x - Content.Length * size / 2;
            y = y - size / 2;

            eventArgs.Graphics.DrawString(Content, font, brush, x, y);
        }
    }
    internal class Output_Sender
    {
        protected List<string> ToSend;
        protected List<string> Label;
        protected List<bool> IsMode;
        public Input_State Type;
        protected Dictionary<string,AttributeValue> Attributes;
        protected string Once_Send;
        protected string Mode_Send;
        public bool InvalidateNeeded = false;
        public bool isPushedNow = false;
        public Output_Sender()
        {
            ToSend = new List<string>();
            Label = new List<string>();
            IsMode = new List<bool>();
            Attributes = new Dictionary<string, AttributeValue>();
            Type = new Input_State();
            Once_Send = "";
            Mode_Send = "";
        }
        virtual public void WhenDown(ref Paint_Data paint_Data) { }
        virtual public void WhenUp(ref Paint_Data paint_Data) { }
        virtual public void Paint(ref Paint_Data paint_Data) { }
        virtual public void WhenPushed(ref Paint_Data paint_Data) { }
        //4番？
        public void GetSend(ref string modeSend)
        {
            SendKeys.SendWait(Once_Send);
            SendKeys.SendWait(Mode_Send);
            modeSend = Mode_Send;
            Once_Send = "";
            Mode_Send = "";
        }
        protected void SetSend(int index)
        {
            if (IsMode[index])
            {
                Mode_Send = ToSend[index];
            }
            else
            {
                Once_Send = ToSend[index];
            }
        }
        protected int Direction(int x, int y)
        {
            double Direction = Math.Atan2(y, x) * -1;
            Direction = Direction + Math.PI / ToSend.Count;
            while (Direction > Math.PI * 2)
            {
                Direction = Direction - Math.PI * 2;
            }
            while (Direction < 0)
            {
                Direction = Direction + Math.PI * 2;
            }
            Direction = (Direction * ToSend.Count) / (2 * Math.PI);
            return (int)Math.Floor(Direction);
        }
        protected int Distance(int x, int y)
        {
            return (int)Math.Sqrt(x * x + y * y);
        }
        public void parseOnePair(int order, string name, XElement input,ref bool keyNumNotFound)
        {
            keyNumNotFound = false;   
            string keyNum = "キー" + order;
            if ((from types in input.Elements(name).Elements(keyNum) select types.Value).Count() < 1)
            {
                keyNumNotFound = true;
                return;
            }
            if ((from types in input.Elements(name).Elements(keyNum).Attributes("ラベル") select types.Value).Count() < 1)
            {
                ErrorLogger((from types in input.Elements(name).Elements(keyNum) select types).First(), name + "," + keyNum +"に「ラベル」がありません。");
                return;
            }
            if ((from types in input.Elements(name).Elements(keyNum).Attributes("ラベル") select types.Value).Count() < 1)
            {
                ErrorLogger((from types in input.Elements(name).Elements(keyNum) select types).First(), name + "," + keyNum + "に「トグル」がありません。");
                return;
            }
            Label.Add((from types in input.Elements(name).Elements(keyNum).Attributes("ラベル") select types.Value).First());
            parseToggle(from types in input.Elements(name).Elements(keyNum).Attributes("トグル") select types.Value);
            parseToSend(from types in input.Elements(name).Elements(keyNum).Elements("出力") select types.Value);
            IEnumerable<XName> attributes = from types in input.Elements(name).Elements("種別").Attributes() select types.Name;
            foreach(XName atrName in attributes)
            {
                IEnumerable<string> Value = from types in input.Elements(name).Elements("種別").Attributes(atrName) select types.Value;
                if(int.TryParse(Value.First(),out int result))
                {
                    Attributes[atrName.ToString()] = result;
                }
                else
                {
                    Attributes[atrName.ToString()] = Value.First();
                    if (!sameString(atrName.ToString(), "ラベル"))
                    {
                        ErrorLogger((from types in input.Elements(name).Elements("種別") select types).First()
                            , "「" + atrName.ToString() + "」が数字ではありません。");
                    }
                }
            }
        }
        void parseToggle(IEnumerable<string> input)
        {
            if (input.Count() == 0)
            {
                IsMode.Add(false);
                return;
            }
            if (input.First()[0] == 'T')
            {
                IsMode.Add(true);
                return;
            }
            if (input.First()[0] == 'F')
            {
                IsMode.Add(false);
                return;
            }
            throw new Exception();
        }
        static string Nums = "0123456789";
        static string Lower = "abcdefghijklmnopqrstuvwxyz";
        static string Escapes = "+^~%(){}[]";
        void parseToSend(IEnumerable<string> input)
        {
            string toAdd = "";
            foreach(string s in input)
            {
                string newS = s.ToLower();
                if (newS.Length == 1)
                {
                    bool finished = false;
                    foreach(char escape in Escapes)
                    {
                        if (newS[0] == escape)
                        {
                            toAdd=toAdd+ '{'+escape+'}';
                            finished = true;
                            break;
                        }
                    }
                    if (!finished)
                    {
                        toAdd = toAdd + newS[0];
                    }
                }
                else
                {
                    if (sameString(newS, "shift"))
                    {
                        toAdd = toAdd + "+";
                    }
                    if (sameString(newS, "tab"))
                    {
                        toAdd = toAdd + "{TAB}";
                    }
                    if (sameString(newS, "ctrl"))
                    {
                        toAdd = toAdd + "^";
                    }
                    if (sameString(newS, "alt"))
                    {
                        toAdd = toAdd + "%";
                    }
                    if (sameString(newS, "left"))
                    {
                        toAdd = toAdd + "{LEFT}";
                    }
                    if (sameString(newS, "right"))
                    {
                        toAdd = toAdd + "{RIGHT}";
                    }
                    if (sameString(newS, "up"))
                    {
                        toAdd = toAdd + "{UP}";
                    }
                    if (sameString(newS, "down"))
                    {
                        toAdd = toAdd + "{DOWN}";
                    }
                    if (sameString(newS, "enter"))
                    {
                        toAdd = toAdd + "~";
                    }
                    if (sameString(newS, "delete"))
                    {
                        toAdd = toAdd + "{DELETE}";
                    }
                }
            }
            ToSend.Add(toAdd);
        }
        public void parseTypeArray(IEnumerable<string> input)
        {
            foreach(string s in input)
            {
                string newS = s.ToLower();
                if (newS.Length == 1)
                {
                    for(int i = 0; i < Lower.Length; i++)
                    {
                        if (Lower[i] == newS[0])
                        {
                            Type.Add((Keys)(i + 65));
                        }
                    }
                    for (int i = 0; i < Nums.Length; i++)
                    {
                        if (Nums[i] == newS[0])
                        {
                            Type.Add((Keys)(i + 48));
                        }
                    }
                }
                else if (newS.Length == 2 && newS[0]=='n')
                {
                    for (int i = 0; i < Nums.Length; i++)
                    {
                        if (Nums[i] == newS[1])
                        {
                            Type.Add((Keys)(i + 96));
                        }
                    }
                }
                else if (newS.Length == 2 && newS[0] == 'k')
                {
                    string after1 = newS.Substring(1);
                    if (int.TryParse(after1, out int result))
                    {
                        Type.Add(Control_Kind.PentabletKey, result);
                    }
                }
                else if (newS.Length == 2 && newS[0] == 'p')
                {
                    string after1 = newS.Substring(1);
                    if (int.TryParse(after1, out int result))
                    {
                        Type.Add(Control_Kind.PenButton, result);
                    }
                }
                else
                {
                    if (sameString(newS, "shift"))
                    {
                        Type.Add(Keys.ShiftKey);
                    }
                    if (sameString(newS, "tab"))
                    {
                        Type.Add(Keys.Tab);
                    }
                    if (sameString(newS, "ctrl"))
                    {
                        Type.Add(Keys.ControlKey);
                    }
                    if (sameString(newS, "up"))
                    {
                        Type.Add(Keys.Up);
                    }
                    if (sameString(newS, "down"))
                    {
                        Type.Add(Keys.Down);
                    }
                    if (sameString(newS, "left"))
                    {
                        Type.Add(Keys.Left);
                    }
                    if (sameString(newS, "right"))
                    {
                        Type.Add(Keys.Right);
                    }
                    if (sameString(newS, "enter"))
                    {
                        Type.Add(Keys.Enter);
                    }
                    if (sameString(newS, "delete"))
                    {
                        Type.Add(Keys.Delete);
                    }
                }
            }
        }
        public void CheckAttributes(XObject input)
        {
            List<string> required = new List<string>();
            if (GetType() == typeof(DirectionSelect))
            {
                required =  requiredAttribute();
            }
            if (GetType() == typeof(TwoAxis))
            {
                required = requiredAttribute("横感度","縦感度");
            }
            if (GetType() == typeof(Rotation))
            {
                required = requiredAttribute();
            }
            if (GetType() == typeof(Once))
            {
                required = requiredAttribute();
            }
            if (GetType() == typeof(WhilePush))
            {
                required = requiredAttribute();
            }
            if (required.Count != 0)
            {
                Total_Processer.hasSyntaxError = true;
                StreamWriter streamWriter = new StreamWriter("log.txt");
                IXmlLineInfo lineInfo = input;
                if (lineInfo.HasLineInfo())
                {
                    string toLog = "";
                    toLog = toLog + lineInfo.LineNumber;
                    toLog = toLog + "行目：" + "以下の要素が不足しています";
                    foreach (string str in required)
                    {
                        toLog = toLog + "," + str;
                    }
                    streamWriter.WriteLine(toLog);
                }
                else
                {
                    string toLog = "";
                    toLog = "以下の要素が不足しています";
                    foreach (string str in required)
                    {
                        toLog = toLog + "," + str;
                    }
                    streamWriter.WriteLine(toLog);
                    Total_Processer.ErrorString = Total_Processer.ErrorString + toLog + "\n";
                }
                streamWriter.Close();
            }
        }
        List<string> requiredAttribute(params string[] strings)
        {
            List<string> output = new List<string>();
            foreach(string str in strings)
            {
                if (!Attributes.ContainsKey(str))
                {
                    output.Add(str);
                }
            }
            return output;
        }
        bool sameString(string A,string B)
        {
            if(A.Count() != B.Count())
            {
                return false;
            }
            for(int i=0;i<A.Count(); i++)
            {
                if (A[i] != B[i])
                {
                    return false;
                }
            }
            return true;
        }
        void ErrorLogger(XObject input,string errorStr)
        {
            Total_Processer.hasSyntaxError = true;
            StreamWriter streamWriter = new StreamWriter("log.txt");
            IXmlLineInfo lineInfo = input;
            if (lineInfo.HasLineInfo())
            {
                string toLog = "";
                toLog = toLog + lineInfo.LineNumber;
                toLog = toLog + "行目："+errorStr;
                streamWriter.WriteLine(toLog);
                Total_Processer.ErrorString = Total_Processer.ErrorString + toLog + "\n";
            }
            else
            {
                string toLog = "";
                toLog = errorStr;
                streamWriter.WriteLine(toLog);
                Total_Processer.ErrorString = Total_Processer.ErrorString + toLog + "\n";
            }
            streamWriter.Close();
        }
    }
    //右から反時計回り
    internal class DirectionSelect : Output_Sender
    {
        public DirectionSelect() :base()
        {

        }
        public override void WhenDown(ref Paint_Data paint_Data) { }
        public override void WhenUp(ref Paint_Data paint_Data)
        {
            if(Distance(Type.CurrentX - Type.StartX, Type.CurrentY - Type.StartY) > 1)
            {
                SetSend(Direction(Type.CurrentX - Type.StartX, Type.CurrentY - Type.StartY));
            }
        }
        public override void WhenPushed(ref Paint_Data paint_Data)
        {
            Paint(ref paint_Data);
            InvalidateNeeded = true;
        }
        public override void Paint(ref Paint_Data paint_Data)
        {
            paint_Data.Clear();
            for (int i = 0; i < ToSend.Count; i++)
            {
                Pen pen = new Pen(Color.Black);
                double directionM = Math.PI * 2 * (-2 * i) / (2 * ToSend.Count);
                double directionT = Math.PI * 2 * (1 + -2 * i) / (2 * ToSend.Count);
                //線の描画
                paint_Data.AddLine(0, 0, (float)Math.Cos(directionT), (float)Math.Sin(directionT), Color.Black);
                //字の描画
                paint_Data.AddString((float)Math.Cos(directionM), (float)Math.Sin(directionM), Label[i], Color.Black);
                if (Direction(Type.CurrentX - Type.StartX, Type.CurrentY - Type.StartY) == i &&
                    Distance(Type.CurrentX - Type.StartX, Type.CurrentY - Type.StartY) > 1)
                {
                    paint_Data.AddString((float)Math.Cos(directionM), (float)Math.Sin(directionM), Label[i], Color.Red);
                }
                else
                {
                    paint_Data.AddString((float)Math.Cos(directionM), (float)Math.Sin(directionM), Label[i], Color.Black);
                }
                pen.Dispose();
            }
        }
    }
    //右左上下
    internal class TwoAxis:Output_Sender
    {
        int TickFromChange = 0;
        bool RightChanged = false;
        bool LeftChanged = false;
        bool UpChanged = false;
        bool DownChanged = false;
        bool verticalFixed = false;
        bool horizontalFixed = false;
        public TwoAxis() : base() { }
        public override void WhenDown(ref Paint_Data paint_Data)
        {
            verticalFixed = false;
            horizontalFixed = false;
            InvalidateNeeded = true;
        }
        public override void WhenUp(ref Paint_Data paint_Data) { }
        public override void WhenPushed(ref Paint_Data paint_Data)
        {
            if (!verticalFixed && Math.Round((Type.CurrentX - Type.StartX) / Attributes["横感度"]) > Math.Round((Type.PreviousX - Type.StartX) / Attributes["横感度"]))
            {
                SetSend(0);
                RightChanged = true;
                horizontalFixed = true;
                InvalidateNeeded = true;
                Paint(ref paint_Data);
                TickFromChange = 0;
            }
            if (!verticalFixed && Math.Round((Type.CurrentX - Type.StartX) / Attributes["横感度"]) < Math.Round((Type.PreviousX - Type.StartX) / Attributes["横感度"]))
            {
                SetSend(1);
                LeftChanged = true;
                horizontalFixed = true;
                InvalidateNeeded = true;
                Paint(ref paint_Data);
                TickFromChange = 0;
            }
            if (!horizontalFixed && Math.Round((Type.CurrentY - Type.StartY) / Attributes["縦感度"]) < Math.Round((Type.PreviousY - Type.StartY) / Attributes["横感度"]))
            {
                SetSend(2);
                UpChanged = true;
                verticalFixed = true;
                InvalidateNeeded = true;
                Paint(ref paint_Data);
                TickFromChange = 0;
            }
            if (!horizontalFixed && Math.Round((Type.CurrentY - Type.StartY) / Attributes["縦感度"]) > Math.Round((Type.PreviousY - Type.StartY) / Attributes["横感度"]))
            {
                SetSend(3);
                DownChanged = true;
                verticalFixed = true;
                InvalidateNeeded = true;
                Paint(ref paint_Data);
                TickFromChange = 0;
            }
            if (TickFromChange > -1)
            {
                TickFromChange++;
            }
            if (TickFromChange > 20)
            {
                InvalidateNeeded = true;
                Paint(ref paint_Data);
                TickFromChange = -1;
            }
        }
        public override void Paint(ref Paint_Data paint_Data)
        {
            paint_Data.Clear();
            if (RightChanged)
            {
                RightChanged = false;
                paint_Data.AddString((float)0.5, 0, Label[0],Color.Red);
            }
            else
            {
                if (verticalFixed)
                {
                    paint_Data.AddString((float)0.5, 0, Label[0], Color.Gray);
                }
                else
                {
                    paint_Data.AddString((float)0.5, 0, Label[0], Color.Black);
                }
            }
            if (LeftChanged)
            {
                LeftChanged = false;
                paint_Data.AddString((float)-0.5, 0, Label[1], Color.Red);
            }
            else
            {
                if (verticalFixed)
                {
                    paint_Data.AddString((float)-0.5, 0, Label[1], Color.Gray);
                }
                else
                {
                    paint_Data.AddString((float)-0.5, 0, Label[1], Color.Black);
                }
            }
            if (UpChanged)
            {
                UpChanged = false;
                paint_Data.AddString(0, (float)0.5, Label[2], Color.Red);
            }
            else
            {
                if (horizontalFixed)
                {
                    paint_Data.AddString(0, (float)0.5, Label[2], Color.Gray);
                }
                else
                {
                    paint_Data.AddString(0, (float)0.5, Label[2], Color.Black);
                }
            }
            if (DownChanged)
            {
                DownChanged = false;
                paint_Data.AddString(0, (float)-0.5, Label[3], Color.Red);
            }
            else
            {
                if (horizontalFixed)
                {
                    paint_Data.AddString(0, (float)-0.5, Label[3], Color.Gray);
                }
                else
                {
                    paint_Data.AddString(0, (float)-0.5, Label[3], Color.Black);
                }
            }
        }
    }
    //0から、表示は右から反時計回り
    internal class Rotation : Output_Sender
    {
        public Rotation() : base() { }
        public override void WhenDown(ref Paint_Data paint_Data)
        {
            SetSend((Type.PushCount-1) % ToSend.Count);
            Paint(ref paint_Data);
            InvalidateNeeded = true;
        }
        public override void WhenUp(ref Paint_Data paint_Data) { }
        public override void WhenPushed(ref Paint_Data paint_Data) { }
        public override void Paint(ref Paint_Data paint_Data)
        {
            paint_Data.Clear();
            for (int i = 0; i < ToSend.Count; i++)
            {
                double directionM = Math.PI * 2 * (2 * i) / (2 * ToSend.Count);
                //字の描画
                if ((Type.PushCount-1) % ToSend.Count == i)
                {
                    paint_Data.AddString((float)0.5 * (float)Math.Cos(directionM), (float)0.5 * (float)Math.Sin(directionM), Label[i], Color.Red);
                }
                else
                {
                    paint_Data.AddString((float)0.5 * (float)Math.Cos(directionM), (float)0.5 * (float)Math.Sin(directionM), Label[i], Color.Black);
                }
            }
        }
    }
    //中心
    internal class Once : Output_Sender
    {
        public Once() : base() { }
        public override void WhenDown(ref Paint_Data paint_Data)
        {
            SetSend(0);
            InvalidateNeeded = true;
            Paint(ref paint_Data);
        }
        public override void WhenUp(ref Paint_Data paint_Data) { }
        public override void WhenPushed(ref Paint_Data paint_Data) { }
        public override void Paint(ref Paint_Data paint_Data)
        {
            paint_Data.Clear();
            paint_Data.AddString(0, 0, Label[0], Color.Red);
        }
    }
    internal class WhilePush : Output_Sender
    {
        string sendBuffer = "";
        Paint_Data labelBuffer;
        bool finished = false;
        public WhilePush() : base() { }
        public override void WhenDown(ref Paint_Data paint_Data)
        {
            sendBuffer = Total_Processer.ToggleToSend;
            labelBuffer = Total_Processer.P_Data.Copy();
            SetSend(0);
            finished = false;
            InvalidateNeeded = true;
            Paint(ref paint_Data);
        }
        public override void WhenUp(ref Paint_Data paint_Data)
        {
            finished = true;
            Mode_Send = sendBuffer;
            InvalidateNeeded = true;
            Paint(ref paint_Data);
        }
        public override void WhenPushed(ref Paint_Data paint_Data) { }
        public override void Paint(ref Paint_Data paint_Data)
        {
            paint_Data.Clear();
            if (finished)
            {
                paint_Data = labelBuffer;
            }
            else
            {
                paint_Data.AddString(0,0, Label[0], Color.Red);
            }
        }
    }
    internal class Input_State
    {
        //排他処理の実装
        public List<Input_Type> Array;
        public bool IsPushed;
        public int StartX;
        public int StartY;
        public int PreviousX;
        public int PreviousY;
        public int CurrentX;
        public int CurrentY;
        public int PushCount;
        public Input_State()
        {
            IsPushed = false;
            StartX = 0;
            StartY = 0;
            PreviousX = 0;
            PreviousY = 0;
            CurrentX = 0;
            CurrentY = 0;
            PushCount = 0;
            Array = new List<Input_Type>();
        }
        public void Add(Control_Kind control_Kind,params int[] args)
        {
            for(int i=0;i<args.Length; i++)
            {
                Input_Type toAdd = new Input_Type(args[i],control_Kind);
                Array.Add(toAdd);
            }
        }
        public void Add(params Keys[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                Input_Type toAdd = new Input_Type(args[i]);
                Array.Add(toAdd);
            }
        }
        public void WhenDown(DATAPACKET packet)
        {
            IsPushed = true;
            StartX = CurrentX;
            StartY = CurrentY;
            PushCount++;

        }
        public void WhenUp(DATAPACKET packet)
        {
            IsPushed = false;
        }
        //実行で呼ぶ必要あり3番
        //他が入力を満たしたとき用
        public void WhenAnyDown(DATAPACKET packet)
        {
            IsPushed = false;
        }
        public void ResetCount(DATAPACKET packet)
        {
            PushCount = 0;
        }
        //全期用初期化
        public void InAllTime(DATAPACKET packet)
        {
            if (packet.x!=0&&packet.y!=0)
            {
                PreviousX = CurrentX;
                PreviousY = CurrentY;
                CurrentX = packet.x;
                CurrentY = packet.y;
            }
        }
        public void setPushed(int index)
        {
            Array[index].Pushed = true;
        }
        public void setUnpushed(int index)
        {
            Array[index].Pushed = false;
        }
        //実行で呼ぶ必要あり2番
        //全て押されているかのチェック
        public bool CheckTotallyPushed(DATAPACKET packet)
        {
            InAllTime(packet);
            bool isPushedNow = true;
            foreach (Input_Type pushed in Array)
            {
                if (!pushed.Pushed)
                {
                    isPushedNow = false;
                }
            }
            if (isPushedNow && !IsPushed)
            {
                WhenDown(packet);
            }
            if (!isPushedNow && IsPushed)
            {
                WhenUp(packet);
            }
            return isPushedNow;
        }
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);
        //実行で呼ぶ必要あり1番
        //キーやDATAPACKETから読んだデータをArrayに反映させる
        public void InfluenceEvents(DATAPACKET packet)
        {
            for(int i=0;i<Array.Count;i++)
            {
                Input_Type type = Array[i];
                if (packet.eventtype == EventType.EventType_Key)
                {
                    if (type.Kind == Control_Kind.PentabletKey)
                    {
                        if ((packet.physical_key>> (type.Num-1)&0x01)==0x01)
                        {
                            if(packet.keystatus == KeyStatus.KeyStatus_Down)
                            {
                                setPushed(i);
                            }
                            if (packet.keystatus == KeyStatus.KeyStatus_Up)
                            {
                                setUnpushed(i);
                            }
                        }
                        if ((packet.virtual_key >> (type.Num - 1) & 0x01) == 0x01)
                        {
                            if (packet.keystatus == KeyStatus.KeyStatus_Down)
                            {
                                setPushed(i);
                            }
                            if (packet.keystatus == KeyStatus.KeyStatus_Up)
                            {
                                setUnpushed(i);
                            }
                        }
                    }
                }
                if (packet.eventtype == EventType.EventType_Pen)
                {
                    if (type.Kind == Control_Kind.PenButton)
                    {
                        if(type.Num == 1)
                        {
                            if (packet.button == 1)
                            {
                                setPushed(i);
                            }
                            else
                            {
                                setUnpushed(i);
                            }
                        }
                        else
                        {
                            if (packet.button == 3)
                            {
                                setPushed(i);
                            }
                            else
                            {
                                setUnpushed(i);
                            }
                        }
                    }
                }
                if (type.Kind == Control_Kind.KeyboardKey)
                {
                    bool PushedNow = GetKeyState((int)type.Key) >> 7 == -1;
                    if (PushedNow)
                    {
                        setPushed(i);
                    }
                    else
                    {
                        setUnpushed(i);
                    }
                }
            }
        }
    }
    internal class Input_Type
    {
        public Control_Kind Kind;
        public int Num;
        public Keys Key;
        public bool Pushed;
        public static bool operator ==(Input_Type preceding, Input_Type subsequent)
        {
            if(preceding.Kind!= subsequent.Kind)
            {
                return false;
            }
            if(preceding.Kind == Control_Kind.PentabletKey)
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
        public static bool operator !=(Input_Type preceding, Input_Type subsequent)
        {
            if (preceding.Kind != subsequent.Kind)
            {
                return true;
            }
            if (preceding.Kind == Control_Kind.PentabletKey)
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
        public Input_Type(int num,Control_Kind control_Kind)
        {
            Kind = control_Kind;
            Num = num;
            Key = 0;
            Pushed = false;
        }
        public Input_Type(Keys key)
        {
            Kind = Control_Kind.KeyboardKey;
            Num = 0;
            Key = key;
            Pushed = false;
        }
        public static Input_Type getType(int num,Control_Kind control_Kind)
        {
            return new Input_Type(num,control_Kind);
        }
        public static Input_Type getType(Keys key)
        {
            return new Input_Type(key);
        }
    }
    enum Control_Kind 
    {
        PentabletKey=0,
        KeyboardKey=1,
        PenButton=2
    }
}
