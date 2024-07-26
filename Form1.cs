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
using XPPen_Auxilliary_Driver;
using SignAPI;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Xml;
using System.Reflection;

namespace New_XPPen_Auxiliary_Driver
{
    public partial class XAD : Form
    {
        Thread thread = new Thread(new ThreadStart(Total_Processer.ExecuteWithoutPentablet));
        static SignAPI.DATAPACKETPRCO DATAPACKETPRCO = new SignAPI.DATAPACKETPRCO(Total_Processer.Execute);
        public XAD()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                Total_Processer.Initialize();
            }
            catch (Exception ex)
            {
                Total_Processer.hasSyntaxError = true;
                Total_Processer.ErrorString = Total_Processer.ErrorString + ex.Message;
            }
            if (Total_Processer.hasSyntaxError)
            {
                Total_Processer.ErrorString = "log.txtを確認してください。\n"+Total_Processer.ErrorString;

                MessageBox.Show(Total_Processer.ErrorString
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
                    //thread.Start();
                }
                else
                {
                    SignAPI.Win32SignAPI.signRegisterDataCallBack(DATAPACKETPRCO);
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (Total_Processer.counter > 0)
            {
                Total_Processer.counter--;
            }
            if (Total_Processer.InvalidationRequireIndex!=-1)
            {
                Invalidate();
                //描画の更新
            }
            if (Total_Processer.counter == 0)
            {
                Total_Processer.counter = -1;
                Total_Processer.P_Data = Total_Processer.Mode_Paint.Copy();
                Invalidate();
            }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Total_Processer.Paint(ref e);
        }

        private void timer2_Tick(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(Total_Processer.stopped)
            {
                button1.Text = "作動中";
                Total_Processer.stopped = false;
            }
            else
            {
                button1.Text = "停止中";
                Total_Processer.stopped=true;
            }
        }

        private void XAD_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(thread.IsAlive)
            {
                thread.Abort();
            }
        }

        private void XAD_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (thread.IsAlive)
            {
                thread.Abort();
            }
        }
    }
}
