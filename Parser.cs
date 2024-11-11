using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace New_XPPen_Auxiliary_Driver
{
    internal class Parser
    {
        static public List<Keys> AvailableKeys;
        public List<Command> Commands;
        public List<EachCommandState> States;
        public string ErrorString;
        public Parser()
        {
            Commands = new List<Command>();
            States = new List<EachCommandState>();
            ErrorString = "";
            InitAvailableKeys();
        }
        public void InitAvailableKeys()
        {
            AvailableKeys = new List<Keys>();
            for (int i = 0; i < Lower.Length; i++)
            {
                AvailableKeys.Add((Keys)(i + 65));
            }
            for (int i = 0; i < Nums.Length; i++)
            {
                AvailableKeys.Add((Keys)(i + 48));
            }
            for (int i = 0; i < Nums.Length; i++)
            {
                AvailableKeys.Add((Keys)(i + 96));
            }
            AvailableKeys.Add(Keys.ShiftKey);
            AvailableKeys.Add(Keys.Tab);
            AvailableKeys.Add(Keys.ControlKey);
            AvailableKeys.Add(Keys.Up);
            AvailableKeys.Add(Keys.Down);
            AvailableKeys.Add(Keys.Left);
            AvailableKeys.Add(Keys.Right);
            AvailableKeys.Add(Keys.Enter);
            AvailableKeys.Add(Keys.Delete);
        }
        public void Parsing()
        {
            XElement original = XElement.Load("config.txt", LoadOptions.SetLineInfo);
            //コマンド一覧取得
            IEnumerable<string> commandNames = from name in original.Elements("一覧").Elements("名前") select name.Value;


            // TODO この書き方でもよいようなのでどこかで全部書き換えを…
            //IEnumerable<string> commandNames = original.Elements("一覧").Elements("名前").Select(name => name.Value);


            //各コマンド処理
            foreach (string commandName in commandNames)
            {
                //入力取得
                IEnumerable<string> inputs = from input in original.Elements(commandName).Elements("入力") select input.Value;
                EachCommandState eachCommandStateToAdd = new EachCommandState();
                eachCommandStateToAdd = InputParse(inputs);
                States.Add(eachCommandStateToAdd);




                //種別取得
                string commandType = (from input in original.Elements(commandName).Elements("種別") select input.Value).First();
                //属性取得
                Dictionary<string,int> attributes = new Dictionary<string,int>();
                IEnumerable<XName> attributesNames = from atr in original.Elements(commandName).Elements("種別").Attributes() select atr.Name;
                foreach (XName name in attributesNames)
                {
                    string value = (from atr in original.Elements(commandName).Elements("種別").Attributes(name) select atr.Value).First();
                    string nameS = name.ToString();
                    if(!int.TryParse(value,out int valueI))
                    {
                        XAttribute errorAttribute = (from atr in original.Elements(commandName).Elements("種別").Attributes(name) select atr).First();
                        ErrorLogger(errorAttribute, nameS + "が数字ではありません。");
                    }
                    attributes[nameS] = valueI;
                }
                //出力取得
                List<OutputType> outputs = new List<OutputType>();
                int counter = 1;
                while (true)
                {
                    string keyNum = "キー" + counter;
                    //キーの終わりかどうかを確認
                    if((from L in original.Elements(commandName).Elements(keyNum) select L).Count() == 0)
                    {
                        break;
                    }
                    //ラベル
                    string label = (from L in original.Elements(commandName).Elements(keyNum).Attributes("ラベル") select L.Value).First();
                    //トグル
                    bool toggle = false;
                    if((from L in original.Elements(commandName).Elements(keyNum).Attributes("トグル") select L.Value).Count() > 0)
                    {
                        string toggleS = (from L in original.Elements(commandName).Elements(keyNum).Attributes("トグル") select L.Value).First();
                        if (toggleS[0] == 'T'|| toggleS[0] == 't')
                        {
                            toggle = true;
                        }
                        else if (toggleS[0] != 'F' && toggleS[0] != 'f')
                        {
                            ErrorLogger((from L in original.Elements(commandName).Elements(keyNum).Attributes("トグル") select L).First(), "トグルの値が正しくないです。");
                        }
                    }
                    //出力
                    IEnumerable<string> outputKeys = from keys in original.Elements(commandName).Elements(keyNum).Elements("出力") select keys.Value;
                    string keysS = OutputKeyParse(outputKeys);
                    OutputType output = new OutputType(label,keysS,toggle);
                    outputs.Add(output);
                    counter++;
                }
                if (sameString(commandType, "ループ"))
                {
                    Rotation commandToAdd = new Rotation(attributes, outputs);
                    Commands.Add(commandToAdd);
                }
                else if (sameString(commandType, "長押し"))
                {
                    WhilePush commandToAdd = new WhilePush(attributes, outputs);
                    Commands.Add(commandToAdd);
                }
                else if (sameString(commandType, "2軸"))
                {
                    TwoAxis commandToAdd = new TwoAxis(attributes, outputs);
                    Commands.Add(commandToAdd);
                }
                else if (sameString(commandType, "方向"))
                {
                    DirectionSelect commandToAdd = new DirectionSelect(attributes, outputs);
                    Commands.Add(commandToAdd);
                }
                else if (sameString(commandType, "単発"))
                {
                    Once commandToAdd = new Once(attributes, outputs);
                    Commands.Add(commandToAdd);
                }
            }
        }
        static string Nums = "0123456789";
        static string Lower = "abcdefghijklmnopqrstuvwxyz";
        static string Escapes = "+^~%(){}[]";
        string OutputKeyParse(IEnumerable<string> input)
        {
            string toAdd = "";
            foreach (string s in input)
            {
                string newS = s.ToLower();
                if (newS.Length == 1)
                {
                    bool finished = false;
                    foreach (char escape in Escapes)
                    {
                        if (newS[0] == escape)
                        {
                            toAdd = toAdd + '{' + escape + '}';
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
            return toAdd;
        }
        public EachCommandState InputParse(IEnumerable<string> input)
        {
            EachCommandState output = new EachCommandState();
            foreach (string s in input)
            {
                string newS = s.ToLower();
                if (newS.Length == 1)
                {
                    for (int i = 0; i < Lower.Length; i++)
                    {
                        if (Lower[i] == newS[0])
                        {
                            output.Add((Keys)(i + 65));
                        }
                    }
                    for (int i = 0; i < Nums.Length; i++)
                    {
                        if (Nums[i] == newS[0])
                        {
                            output.Add((Keys)(i + 48));
                        }
                    }
                }
                else if (newS.Length == 2 && newS[0] == 'n')
                {
                    for (int i = 0; i < Nums.Length; i++)
                    {
                        if (Nums[i] == newS[1])
                        {
                            output.Add((Keys)(i + 96));
                        }
                    }
                }
                else if (newS.Length == 2 && newS[0] == 'k')
                {
                    string after1 = newS.Substring(1);
                    if (int.TryParse(after1, out int result))
                    {
                        output.Add(ControlKind.PentabletKey, result - 1);
                    }
                }
                else if (newS.Length == 2 && newS[0] == 'p')
                {
                    string after1 = newS.Substring(1);
                    if (int.TryParse(after1, out int result))
                    {
                        output.Add(ControlKind.PenButton, result - 1);
                    }
                }
                else
                {
                    if (sameString(newS, "shift"))
                    {
                        output.Add(Keys.ShiftKey);
                    }
                    if (sameString(newS, "tab"))
                    {
                        output.Add(Keys.Tab);
                    }
                    if (sameString(newS, "ctrl"))
                    {
                        output.Add(Keys.ControlKey);
                    }
                    if (sameString(newS, "up"))
                    {
                        output.Add(Keys.Up);
                    }
                    if (sameString(newS, "down"))
                    {
                        output.Add(Keys.Down);
                    }
                    if (sameString(newS, "left"))
                    {
                        output.Add(Keys.Left);
                    }
                    if (sameString(newS, "right"))
                    {
                        output.Add(Keys.Right);
                    }
                    if (sameString(newS, "enter"))
                    {
                        output.Add(Keys.Enter);
                    }
                    if (sameString(newS, "delete"))
                    {
                        output.Add(Keys.Delete);
                    }
                }
            }
            return output;
        }
        void ErrorLogger(XObject input, string errorStr)
        {
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
        bool sameString(string A, string B)
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
    }
}
