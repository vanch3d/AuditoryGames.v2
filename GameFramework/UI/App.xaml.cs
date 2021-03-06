﻿/*  Auditory Training Games in Silverlight
    Copyright (C) 2008-2012 Nicolas Van Labeke (LSRI, Nottingham University)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace LSRI.AuditoryGames.GameFramework
{
    /// <summary>
    /// Common Application class for all auditory games
    /// </summary>
    public partial class AuditoryGameApp : Application
    {

        public AuditoryGameApp()
        {
            this.Startup += this.Application_Startup;
            this.Exit += this.Application_Exit;
            this.UnhandledException += this.Application_UnhandledException;
             InitializeComponent();
        }


        protected virtual void Application_Startup(object sender, StartupEventArgs e)
        {
            if (Application.Current.IsRunningOutOfBrowser)
            {
                //this.CheckAndDownloadUpdateCompleted += new CheckAndDownloadUpdateCompletedEventHandler(Application_CheckAndDownloadUpdateCompleted);
                //Application.Current.CheckAndDownloadUpdateAsync();
            }

            this.RootVisual = new GamePage();
            KeyHandler.Instance.startupKeyHandler(this.RootVisual as GamePage);
            IAppManager.Instance.startupApplicationManager();
            StateManager.Instance.startupStateManager();
        }

        protected virtual void Application_Exit(object sender, EventArgs e)
        {
            KeyHandler.Instance.shutdown();
            StateManager.Instance.shutdown();
            IAppManager.Instance.shutdown();
        }

        protected void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            // If the app is running outside of the debugger then report the exception using
            // the browser's exception mechanism. On IE this will display it a yellow alert 
            // icon in the status bar and Firefox will display a script error.
            if (!System.Diagnostics.Debugger.IsAttached)
            {

                // NOTE: This will allow the application to continue running after an exception has been thrown
                // but not handled. 
                // For production applications this error handling should be replaced with something that will 
                // report the error to the website and stop the application.
                e.Handled = true;
                Deployment.Current.Dispatcher.BeginInvoke(delegate { ReportErrorToDOM(e); });
            }
        }

        protected void ReportErrorToDOM(ApplicationUnhandledExceptionEventArgs e)
        {
            try
            {
                string errorMsg = e.ExceptionObject.Message + e.ExceptionObject.StackTrace;
                errorMsg = errorMsg.Replace('"', '\'').Replace("\r\n", @"\n");

                System.Windows.Browser.HtmlPage.Window.Eval("throw new Error(\"Unhandled Error in Silverlight 2 Application " + errorMsg + "\");");
            }
            catch (Exception)
            {
            }
        }

        protected void Application_CheckAndDownloadUpdateCompleted(object sender, CheckAndDownloadUpdateCompletedEventArgs e)
        {
            if (e.UpdateAvailable)
            {
                MessageBox.Show("An application update has been downloaded. " +
                    "Restart the application to run the new version.");
            }
            else if (e.Error != null)
            {
                MessageBox.Show(
                    "An application update is available, but an error has occurred.\n" +
                    "This can happen, for example, when the update requires\n" +
                    "a new version of Silverlight or requires elevated trust.\n" +
                    "To install the update, visit the application home page.");
            }
            else
            {
                MessageBox.Show("There is no update available.");
            }

        }

    }
}
