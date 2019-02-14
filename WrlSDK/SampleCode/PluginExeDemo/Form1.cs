﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

/// 添加核心组件引用
using WrlEngine;
using WrlBase;

namespace PluginExeDemo
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// web socket服务对象
        /// </summary>
        SocketProxyClass WebSocketServer = null;
        /// <summary>
        /// web socket服务事件对象
        /// </summary>
        WebSocketEventSink WebSocketEvent = null;

        /// <summary>
        /// 命令行启动参数
        /// </summary>
        Dictionary<string, string> m_Para;

        public Form1(string strPara)
        {
            InitializeComponent();

            m_Para = new Dictionary<string, string>();

            /// 解析命令行参数
            string[] cmdArray = strPara.Split('&');
            foreach (string cmd in cmdArray)
            {
                string[] paraArray = cmd.Split('=');
                foreach (string para in paraArray)
                {
                    m_Para.Add(paraArray[0], paraArray[1]);
                    break;
                }
            }
        }

        public void Send(string strSID,string strContent)
        {
            WebSocketServer.AsynSendText(strSID,strContent);
        }

        public void OpenUrl(string strUrl)
        {
            this.IEBrowser.Navigate(strUrl);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            WebSocketServer = new SocketProxyClass();
            if (null == WebSocketServer)
                return;
            WebSocketEvent = new WebSocketEventSink();
            if (null == WebSocketEvent)
                return;
            WebSocketEvent.SetForm(this);
            ushort nPort = ushort.Parse(m_Para["PORT"]);
            ushort nListenPort = WebSocketServer.Listen(nPort, m_Para["SID"], m_Para["AI"]);

                /// 建立事件通知
            WebSocketServer.NewConnEvent += WebSocketEvent.NewConnEvent;
            WebSocketServer.RecMsgEvent += WebSocketEvent.RecMsgEvent;
            WebSocketServer.RecTextEvent += WebSocketEvent.RecTextEvent;
            WebSocketServer.ConnCloseEvent += WebSocketEvent.ConnCloseEvent;
       }

        private void Form1_Closed(object sender, EventArgs e)
        {
            /// 移除事件通知
            WebSocketServer.NewConnEvent -= WebSocketEvent.NewConnEvent;
            WebSocketServer.RecMsgEvent -= WebSocketEvent.RecMsgEvent;
            WebSocketServer.RecTextEvent -= WebSocketEvent.RecTextEvent;
            WebSocketServer.ConnCloseEvent -= WebSocketEvent.ConnCloseEvent;
            /// 释放对象
            if (null != WebSocketServer)
            {
                WebSocketServer.Close();
                WebSocketServer = null;
            }

            WebSocketServer = null;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string strLastSID = WebSocketEvent.GetLastSID();
            if(0 == strLastSID.Length)
            {
                MessageBox.Show("还未有来自网页的连接！");
                return;
            }
            Send(strLastSID, this.textBox1.Text);
         }

        /// <summary>
        /// WebSocket服务事件通知
        /// </summary>
        public class WebSocketEventSink : _ISocketProxyEvents
        {
            /// <summary>
            ///  主窗口
            /// </summary>

            Form1 m_Form;

            string m_strLastSID;

            public void SetForm(Form1 Form)
            {
                m_Form = Form;
            }

            public string GetLastSID()
            {
                return m_strLastSID;
            }

            /// <summary>
            /// 通知新连接
            /// </summary>
            /// <param name="bstrSID"></param>
            public void NewConnEvent(string bstrSID)
            {
                m_strLastSID = bstrSID;
                m_Form.textBox2.AppendText("收到新连接：");
                m_Form.textBox2.AppendText(bstrSID);
                m_Form.textBox2.AppendText("\r\n");
            }

            /// <summary>
            /// 通知连接收到JSON数据包
            /// </summary>
            /// <param name="bstrSID"></param>
            /// <param name="nReqID"></param>
            /// <param name="bstrReqName"></param>
            /// <param name="bstrContent"></param>
            public void RecMsgEvent(string bstrSID, uint nReqID, string bstrPushName, string bstrMsg)
            {
                m_Form.textBox2.AppendText("收到新数据包，请求序号：");
                m_Form.textBox2.AppendText(nReqID.ToString());
                m_Form.textBox2.AppendText("协议名：");
                m_Form.textBox2.AppendText(bstrPushName);
                m_Form.textBox2.AppendText("内容：");
                m_Form.textBox2.AppendText(bstrMsg);
                m_Form.textBox2.AppendText("\r\n");

                if (bstrPushName == "Demo_OpenUrl")
                {
                    /// 获得打开URL地址，调用浏览器打开
                    JsonServiceClass JsonService = new JsonServiceClass();
                    JsonService.ParseString(bstrMsg);
                    string strUrl = JsonService.GetStringValue("url");
                    JsonService = null;
                    m_Form.OpenUrl(strUrl);
                    return;
                }
                /// 回传给网页内容
                 m_Form.Send(bstrSID,"收到请求" + bstrPushName);
           }

            /// <summary>
            /// 通知连接收到文本内容
            /// </summary>
            /// <param name="bstrSID"></param>
            /// <param name="bstrText"></param>
            public void RecTextEvent(string bstrSID, string bstrText)
            {
                 m_Form.textBox2.AppendText("收到文本内容：");
                m_Form.textBox2.AppendText(bstrText);
                m_Form.textBox2.AppendText("\r\n");
                /// 回传给网页内容
                m_Form.Send(bstrSID,"收到文本内容" + bstrText);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="bstrSID"></param>
            /// <param name="Content"></param>
            /// <param name="nLen"></param>
            public void RecByteEvent(string bstrSID, ref byte Content, uint nLen)
            {
            }

            /// <summary>
            /// 通知关闭连接
            /// </summary>
            /// <param name="bstrSID"></param>
            public void ConnCloseEvent(string bstrSID)
            {
            }
        }
    }
}