using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
//using Sharp7; //temp for delete

namespace SimaticClientService
{
    public partial class Service1 : ServiceBase
    {
        System.Timers.Timer timer;



        WinLogger WinLog = new WinLogger(AppDomain.CurrentDomain.FriendlyName);
        EventLog log = new EventLog(); //only test
        MainModule Mdl = new MainModule();

        public Service1()
        {
            InitializeComponent();
            WinLog.Write(1, "Initializing: Ok");

        }

        protected override void OnStart(string[] args)
        {

            try
            {
                Thread serviceThread = new Thread(new ThreadStart(InitTimer));
                serviceThread.Start();
                WinLog.Write(1, /*$"{DateTime.Now} :*/"Initializing: Starting");
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry($"OnStart ex: " + ex.Message, System.Diagnostics.EventLogEntryType.Error);
            }

            Mdl.OpenBufferValues();


        }

        protected override void OnStop()
        {

            try
            {
                timer.Stop();
                WinLog.Write(1, "Stopping");
            }
            catch (Exception ex)
            {
                WinLog.Write(1, $"OnStop ex: " + ex.Message);
            }
            Mdl.SaveBufferValues();
        }

        protected override void OnShutdown()
        {
            WinLog.Write(1, "Shutdown");

        }

        protected override void OnPause()
        {
        }
        protected override void OnContinue()
        {
        }

        protected void InitTimer()
        {
            try
            {
                timer = new System.Timers.Timer();
                timer.Interval = 1000;
                timer.Elapsed += new System.Timers.ElapsedEventHandler(this.OnTimer);
                timer.Start();
            }
            catch (Exception ex)
            {
                WinLog.Write(1, $"InitTimer ex: " + ex.Message);
            }

        }

        private void OnTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                Mdl.RunModule();

                //PLC.RunUpdate();
            }
            catch (Exception ex)
            {
                WinLog.Write(1, $"OnTimer ex: " + ex.Message);
            }
        }

    }
}
