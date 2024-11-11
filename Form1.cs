using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using SignAPI;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Xml;
using System.Reflection;
using System.IO;

namespace New_XPPen_Auxiliary_Driver
{
    public partial class XAD : Form
    {
        static EachKeyStateBlock KeyState;
        static EachCommandStateBlock CommandState;
        static CommandBlock Command;
        string ErrorString;
        static bool Stopped;
        static SignAPI.DATAPACKETPRCO DATAPACKETPRCO = new SignAPI.DATAPACKETPRCO(Execute);
        public XAD()
        {
            InitializeComponent();
        }
        static int Execute(DATAPACKET packet)
        {
            if (!Stopped)
            {
                try
                {
                    KeyState.ApplyCurrentState(packet);
                    CommandState.ApplyCurrentState(KeyState, packet);
                    Command.UpDownPushed(CommandState);

                }
                catch (Exception e)
                {
                    StreamWriter writer = new StreamWriter("log.txt");
                    writer.WriteLine(e);
                    writer.Close();
                }
            }
            return 0;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ErrorString = "";
            Parser parser;
            try
            {
                parser = new Parser();
                parser.Parsing();
                KeyState = new EachKeyStateBlock();
                CommandState = new EachCommandStateBlock(parser.States);
                Command = new CommandBlock(parser.Commands);
                ErrorString = parser.ErrorString;
            }
            catch (Exception ex)
            {
                ErrorString = ErrorString + ex.Message;
            }
            if (ErrorString.Count()>0)
            {
                StreamWriter writer = new StreamWriter("log.txt");
                writer.WriteLine(ErrorString);
                writer.Close();
                ErrorString = "log.txtを確認してください。\n"+ErrorString;

                MessageBox.Show(ErrorString
                    , "確認", MessageBoxButtons.OK);
                Application.Exit();
            }
            else
            {
                if (SignAPI.Win32SignAPI.signInitialize() != ErrorCode.ERR_OK||
                    SignAPI.Win32SignAPI.signOpenDevice() != ErrorCode.ERR_OK)
                {
                    MessageBox.Show("XPPenのペンタブレットに接続できませんでした。"
                        ,"確認",MessageBoxButtons.OK);
                    Application.Exit();
                }
                else
                {
                    SignAPI.Win32SignAPI.signRegisterDataCallBack(DATAPACKETPRCO);
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Invalidate();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            try
            {
                if (!Command.DrawBlocked)
                {
                    DrawingContextBlock toDraw = Command.GetDrawBlock();
                    toDraw.Draw(ref e);
                }
            }
            catch
            {

            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(Stopped)
            {
                button1.Text = "作動中";
                Stopped = false;
            }
            else
            {
                button1.Text = "停止中";
                Stopped =true;
            }
        }

        private void XAD_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        private void XAD_FormClosed(object sender, FormClosedEventArgs e)
        {

        }
    }
}
